using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodexQuotaFloat.Services;

public sealed class CodexAppServerClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private readonly CancellationTokenSource _disposeCancellation = new();

    private Process? _process;
    private CancellationTokenSource? _readerCancellation;
    private Task? _standardOutputReader;
    private Task? _standardErrorReader;
    private long _nextRequestId;
    private int _unexpectedExitRaised;
    private bool _initialized;
    private bool _stopping;
    private bool _disposed;

    public event EventHandler? Exited;

    public async Task<JsonElement> ReadRateLimitsAsync(CancellationToken cancellationToken)
    {
        await StartAsync(cancellationToken);
        return await SendRequestAsync("account/rateLimits/read", parameters: null, cancellationToken);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(cancellationToken);

        try
        {
            ThrowIfDisposed();

            if (IsRunning())
            {
                return;
            }

            await StopCoreAsync();

            var executable = await FindCodexExecutableAsync(cancellationToken);
            var process = StartAppServer(executable);
            var readerCancellation = CancellationTokenSource.CreateLinkedTokenSource(_disposeCancellation.Token);

            _stopping = false;
            _initialized = false;
            Interlocked.Exchange(ref _unexpectedExitRaised, 0);
            Volatile.Write(ref _process, process);
            Volatile.Write(ref _readerCancellation, readerCancellation);
            _standardOutputReader = ReadStandardOutputAsync(process, readerCancellation.Token);
            _standardErrorReader = DrainStandardErrorAsync(process, readerCancellation.Token);

            try
            {
                await SendRequestAsync(
                    "initialize",
                    new
                    {
                        clientInfo = new
                        {
                            name = "codex_quota_float",
                            title = "Codex Quota Float",
                            version = "0.1.0"
                        }
                    },
                    cancellationToken);
                await SendNotificationAsync("initialized", new { }, cancellationToken);
                _initialized = true;
            }
            catch
            {
                await StopCoreAsync();
                throw;
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lifecycleGate.WaitAsync();

        try
        {
            await StopCoreAsync();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stopping = true;
        _disposeCancellation.Cancel();
        FailPendingRequests(new OperationCanceledException("Codex App Server client is stopping."));

        try
        {
            _lifecycleGate.Wait();
            try
            {
                StopCoreAsync().GetAwaiter().GetResult();
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }
        catch
        {
            // 关闭阶段不应阻塞应用退出。
        }
    }

    private async Task<JsonElement> SendRequestAsync(
        string method,
        object? parameters,
        CancellationToken cancellationToken)
    {
        var requestId = Interlocked.Increment(ref _nextRequestId);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pendingRequests.TryAdd(requestId, completion))
        {
            throw new InvalidOperationException("Unable to register the Codex App Server request.");
        }

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            if (_pendingRequests.TryRemove(requestId, out var pendingRequest))
            {
                pendingRequest.TrySetCanceled(cancellationToken);
            }
        });

        try
        {
            var payload = JsonSerializer.Serialize(new RpcRequest(requestId, method, parameters), JsonOptions);
            await WriteJsonAsync(payload, cancellationToken);
            return await completion.Task;
        }
        catch
        {
            _pendingRequests.TryRemove(requestId, out _);
            throw;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private Task SendNotificationAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new RpcNotification(method, parameters), JsonOptions);
        return WriteJsonAsync(payload, cancellationToken);
    }

    private async Task WriteJsonAsync(string payload, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken);

        try
        {
            var process = Volatile.Read(ref _process);
            if (!IsAlive(process))
            {
                throw new IOException("Codex App Server is not running.");
            }

            await process!.StandardInput.WriteLineAsync(payload);
            await process.StandardInput.FlushAsync();
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task ReadStandardOutputAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
            {
                TryCompleteResponse(line);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常关闭读取循环。
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常关闭读取循环。
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                HandleUnexpectedExit(process);
            }
        }
    }

    private static async Task DrainStandardErrorAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            while (await process.StandardError.ReadLineAsync(cancellationToken) is not null)
            {
                // stderr 只负责排空，避免干扰 stdout 的 JSONL 协议通道。
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常关闭读取循环。
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常关闭读取循环。
        }
    }

    private void TryCompleteResponse(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("id", out var idElement) || !idElement.TryGetInt64(out var requestId))
            {
                return;
            }

            if (!_pendingRequests.TryRemove(requestId, out var completion))
            {
                return;
            }

            if (root.TryGetProperty("error", out _))
            {
                completion.TrySetException(new InvalidOperationException("Codex App Server rejected the request."));
                return;
            }

            if (root.TryGetProperty("result", out var result))
            {
                completion.TrySetResult(result.Clone());
                return;
            }

            completion.TrySetException(new InvalidOperationException("Codex App Server returned an invalid response."));
        }
        catch (JsonException)
        {
            // 非协议行不应令整个悬浮窗崩溃。
        }
    }

    private void ProcessOnExited(object? sender, EventArgs e)
    {
        if (sender is Process process)
        {
            HandleUnexpectedExit(process);
        }
    }

    private void HandleUnexpectedExit(Process process)
    {
        if (_disposed || _stopping || !ReferenceEquals(Volatile.Read(ref _process), process))
        {
            return;
        }

        _initialized = false;
        FailPendingRequests(new IOException("Codex App Server exited unexpectedly."));

        if (Interlocked.Exchange(ref _unexpectedExitRaised, 1) == 0)
        {
            Exited?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task StopCoreAsync()
    {
        _stopping = true;
        _initialized = false;
        FailPendingRequests(new OperationCanceledException("Codex App Server is stopping."));

        var process = Interlocked.Exchange(ref _process, null);
        var readerCancellation = Interlocked.Exchange(ref _readerCancellation, null);
        var outputReader = _standardOutputReader;
        var errorReader = _standardErrorReader;
        _standardOutputReader = null;
        _standardErrorReader = null;

        readerCancellation?.Cancel();

        if (process is not null)
        {
            process.Exited -= ProcessOnExited;

            try
            {
                process.StandardInput.Close();
            }
            catch (InvalidOperationException)
            {
                // 进程已经退出或已释放。
            }

            await StopProcessAsync(process);
        }

        var readerTasks = new[] { outputReader, errorReader }.Where(task => task is not null).Cast<Task>();
        try
        {
            await Task.WhenAll(readerTasks);
        }
        catch
        {
            // 已取消的读循环不影响资源回收。
        }

        readerCancellation?.Dispose();
        process?.Dispose();
    }

    private static async Task StopProcessAsync(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            var exitTask = process.WaitForExitAsync();
            if (await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromMilliseconds(500))) != exitTask &&
                !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        catch (InvalidOperationException)
        {
            // 进程在关闭过程中已退出。
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // 终止已经不可用的进程时忽略。
        }
    }

    private static async Task<string> FindCodexExecutableAsync(CancellationToken cancellationToken)
    {
        var managedCodexExecutable = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex",
            "bin",
            "codex.exe");

        // WindowsApps 内的包文件可能能被 where 找到，但普通桌面进程无权直接启动。
        // Codex 自身安装的用户级启动器才是 App Server 的稳定入口。
        if (File.Exists(managedCodexExecutable))
        {
            return managedCodexExecutable;
        }

        var candidates = new List<string>();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "where.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("codex");

            using var whereProcess = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start where.exe.");

            var standardOutput = whereProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = whereProcess.StandardError.ReadToEndAsync(cancellationToken);

            await whereProcess.WaitForExitAsync(cancellationToken);
            candidates.AddRange((await standardOutput)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            _ = await standardError;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // 仍可尝试 Codex 在当前用户目录下的标准安装路径。
        }

        var executable = candidates.FirstOrDefault(path =>
            path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(path) &&
            !path.Contains("\\WindowsApps\\", StringComparison.OrdinalIgnoreCase));

        return executable ?? throw new FileNotFoundException("未检测到可直接启动的 Codex.exe。", "codex");
    }

    private Process StartAppServer(string executable)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("app-server");
        startInfo.ArgumentList.Add("--listen");
        startInfo.ArgumentList.Add("stdio://");

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start Codex App Server.");
        process.Exited += ProcessOnExited;
        process.EnableRaisingEvents = true;
        return process;
    }

    private bool IsRunning() => _initialized && IsAlive(Volatile.Read(ref _process));

    private static bool IsAlive(Process? process)
    {
        if (process is null)
        {
            return false;
        }

        try
        {
            return !process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void FailPendingRequests(Exception exception)
    {
        foreach (var pair in _pendingRequests)
        {
            if (_pendingRequests.TryRemove(pair.Key, out var completion))
            {
                completion.TrySetException(exception);
            }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record RpcRequest(long Id, string Method, object? Params);

    private sealed record RpcNotification(string Method, object? Params);
}
