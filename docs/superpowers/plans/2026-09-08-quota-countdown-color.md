# 额度重置倒计时与分级颜色 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在现有 WPF 悬浮窗中显示两个额度窗口各自的重置倒计时，并按已确认的四级阈值为剩余百分比着色。

**Architecture:** 将倒计时格式化和额度分级提取为不依赖 WPF 的 `QuotaPresentation` 纯逻辑，以便无界面测试。`MainWindow` 保留数据读取职责，使用额外的一分钟 UI 定时器重绘倒计时，XAML 只承担 B 版式和文本呈现。

**Tech Stack:** C# / .NET 10 / WPF；无新增 NuGet 包；独立 .NET 控制台测试运行器。

## Global Constraints

- 保留现有 `codex app-server --listen stdio://`、登录复用、60 秒额度刷新、重试、单实例、右键菜单和退出清理。
- 不新增数据库、配置、登录、API Key、网络请求、托盘或后台服务。
- 窗口为 360 × 140 px；卡片高度 132 px，折叠后保留底部 8 px、宽 110 px 的悬停区域。
- B 版式：周期名称与倒计时上下排列，百分比固定在右侧；只给百分比着色。
- 阈值精确为：81–100 绿色 `#55D6A4`；50–80 黄色 `#F7DE6B`；20–49 琥珀色 `#F2B85D`；0–19 红色 `#FF7C74`；缺失为中性灰 `#D7DEE7`。
- 倒计时格式：至少 1 天为 `X天Y小时后重置`；至少 1 小时为 `X小时Y分后重置`；至少 1 分钟为 `Y分后重置`；不足 1 分钟或已到时为 `即将重置`；缺失为 `--`。
- 倒计时每分钟仅在本地重绘；不增加 App Server 调用。

---

### Task 1: 抽取并验证展示层纯逻辑

**Files:**
- Create: `Services/QuotaPresentation.cs`
- Create: `tests/CodexQuotaFloat.PresentationTests/CodexQuotaFloat.PresentationTests.csproj`
- Create: `tests/CodexQuotaFloat.PresentationTests/Program.cs`

**Interfaces:**
- Produces: `public enum QuotaColorBand { Unknown, Green, Yellow, Amber, Red }`
- Produces: `public static QuotaColorBand QuotaPresentation.GetColorBand(int? remainingPercent)`
- Produces: `public static string QuotaPresentation.FormatResetCountdown(DateTimeOffset? resetAt, DateTimeOffset now)`
- Consumes: `int?` remaining percentage and nullable reset timestamp; does not depend on WPF or App Server types.

- [x] **Step 1: 创建会失败的控制台测试运行器。**

```xml
<!-- tests/CodexQuotaFloat.PresentationTests/CodexQuotaFloat.PresentationTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../../Services/QuotaPresentation.cs" Link="Services/QuotaPresentation.cs" />
  </ItemGroup>
</Project>
```

```csharp
using CodexQuotaFloat.Services;

var now = new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.Zero);
AssertEqual(QuotaColorBand.Green, QuotaPresentation.GetColorBand(81));
AssertEqual(QuotaColorBand.Yellow, QuotaPresentation.GetColorBand(80));
AssertEqual(QuotaColorBand.Yellow, QuotaPresentation.GetColorBand(50));
AssertEqual(QuotaColorBand.Amber, QuotaPresentation.GetColorBand(49));
AssertEqual(QuotaColorBand.Amber, QuotaPresentation.GetColorBand(20));
AssertEqual(QuotaColorBand.Red, QuotaPresentation.GetColorBand(19));
AssertEqual(QuotaColorBand.Unknown, QuotaPresentation.GetColorBand(null));
AssertEqual("4天12小时后重置", QuotaPresentation.FormatResetCountdown(now.AddDays(4).AddHours(12), now));
AssertEqual("2小时18分后重置", QuotaPresentation.FormatResetCountdown(now.AddHours(2).AddMinutes(18), now));
AssertEqual("42分后重置", QuotaPresentation.FormatResetCountdown(now.AddMinutes(42), now));
AssertEqual("即将重置", QuotaPresentation.FormatResetCountdown(now.AddSeconds(59), now));
AssertEqual("--", QuotaPresentation.FormatResetCountdown(null, now));
Console.WriteLine("QuotaPresentation tests passed.");

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}
```

- [x] **Step 2: 运行测试，确认它因缺少 `QuotaPresentation` 失败。**

Run: `dotnet run --project tests/CodexQuotaFloat.PresentationTests/CodexQuotaFloat.PresentationTests.csproj -c Release`

Expected: 失败，指出 `QuotaPresentation` 或 `QuotaColorBand` 不存在。

- [x] **Step 3: 以最小实现创建 `Services/QuotaPresentation.cs`。**

```csharp
namespace CodexQuotaFloat.Services;

public enum QuotaColorBand { Unknown, Green, Yellow, Amber, Red }

public static class QuotaPresentation
{
    public static QuotaColorBand GetColorBand(int? remainingPercent) => remainingPercent switch
    {
        >= 81 and <= 100 => QuotaColorBand.Green,
        >= 50 and <= 80 => QuotaColorBand.Yellow,
        >= 20 and <= 49 => QuotaColorBand.Amber,
        >= 0 and <= 19 => QuotaColorBand.Red,
        _ => QuotaColorBand.Unknown
    };

    public static string FormatResetCountdown(DateTimeOffset? resetAt, DateTimeOffset now)
    {
        if (resetAt is null)
        {
            return "--";
        }

        var remaining = resetAt.Value - now;
        if (remaining < TimeSpan.FromMinutes(1))
        {
            return "即将重置";
        }

        if (remaining >= TimeSpan.FromDays(1))
        {
            return $"{remaining.Days}天{remaining.Hours}小时后重置";
        }

        if (remaining >= TimeSpan.FromHours(1))
        {
            return $"{remaining.Hours}小时{remaining.Minutes}分后重置";
        }

        return $"{remaining.Minutes}分后重置";
    }
}
```

- [x] **Step 4: 运行展示逻辑测试。**

Run: `dotnet run --project tests/CodexQuotaFloat.PresentationTests/CodexQuotaFloat.PresentationTests.csproj -c Release`

Expected: 退出码 0，并输出所有边界值通过。

- [x] **Step 5: 提交。**

项目不是 Git 仓库；记录本步骤完成即可，不能执行 Git 提交。

### Task 2: 将悬浮窗调整为 B 版式

**Files:**
- Modify: `MainWindow.xaml:1-125`

**Interfaces:**
- Consumes: `FiveHourValue`、`WeeklyValue` 的现有命名，以及即将在 Task 3 更新的 `FiveHourResetValue`、`WeeklyResetValue`。
- Produces: 360 × 140 px 的窗口、132 px 卡片、两组左侧名称/倒计时文本和右侧百分比文本。

- [x] **Step 1: 修改窗口、根网格和卡片尺寸。**

```xml
<Window Width="360"
        Height="140"
        WindowStyle="None"
        ResizeMode="NoResize"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False">
  <Grid Width="360" Height="140">
    <Border x:Name="Card" Width="360" Height="132">
```

- [x] **Step 2: 将每个额度项目改为左侧两行、右侧百分比。**

```xml
<Grid Grid.Row="0">
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*" />
    <ColumnDefinition Width="Auto" />
  </Grid.ColumnDefinitions>
  <StackPanel Grid.Column="0" VerticalAlignment="Center">
    <TextBlock Text="5小时" FontSize="16" />
    <TextBlock x:Name="FiveHourResetValue" Text="--" FontSize="12" Foreground="#B8C5D4" />
  </StackPanel>
  <TextBlock x:Name="FiveHourValue" Grid.Column="1" Text="加载中" FontSize="22" />
</Grid>
```

对第二行使用 `WeeklyResetValue` 和 `Text="本周"`；保留单独的底部状态行。

- [x] **Step 3: 将悬停区域移动到卡片底部。**

```xml
<Border x:Name="HoverZone"
        Width="110"
        Height="8"
        Margin="0,132,0,0"
        HorizontalAlignment="Center"
        VerticalAlignment="Top" />
```

- [x] **Step 4: 构建 WPF 项目，验证 XAML 名称和布局可编译。**

Run: `dotnet build CodexQuotaFloat.csproj -c Release`

Expected: 0 errors；若旧程序锁定默认输出，改用隔离临时源副本构建，不覆盖正在运行的旧版目录。

- [x] **Step 5: 提交。**

项目不是 Git 仓库；记录本步骤完成即可，不能执行 Git 提交。

### Task 3: 接入倒计时定时器、百分比颜色和生命周期清理

**Files:**
- Modify: `MainWindow.xaml.cs:1-226`

**Interfaces:**
- Consumes: `QuotaPresentation.GetColorBand`, `QuotaPresentation.FormatResetCountdown` 和 `QuotaState` 的 `FiveHourResetAt`、`WeeklyResetAt`。
- Produces: `MainWindow` 每分钟更新倒计时、在每次 `SetQuotaState` 后同步更新颜色与文本、在 `Dispose` 时停止定时器。

- [x] **Step 1: 添加一分钟的 `_countdownTimer` 并在构造函数中注册 Tick。**

```csharp
private readonly DispatcherTimer _countdownTimer;

_countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
_countdownTimer.Tick += CountdownTimerOnTick;
```

- [x] **Step 2: 在窗口加载、额度状态变化和 Tick 时重绘展示层。**

```csharp
private void CountdownTimerOnTick(object? sender, EventArgs e) => UpdateQuotaPresentation();

private void UpdateQuotaPresentation()
{
    FiveHourValue.Text = FormatQuota(_currentState.FiveHourRemaining, _currentState.Status);
    WeeklyValue.Text = FormatQuota(_currentState.WeeklyRemaining, _currentState.Status);
    FiveHourValue.Foreground = GetQuotaBrush(_currentState.FiveHourRemaining);
    WeeklyValue.Foreground = GetQuotaBrush(_currentState.WeeklyRemaining);
    FiveHourResetValue.Text = QuotaPresentation.FormatResetCountdown(_currentState.FiveHourResetAt, DateTimeOffset.Now);
    WeeklyResetValue.Text = QuotaPresentation.FormatResetCountdown(_currentState.WeeklyResetAt, DateTimeOffset.Now);
}
```

在 `WindowOnLoaded` 启动 `_countdownTimer`，在 `Dispose` 停止并解绑；`SetQuotaState` 改为调用 `UpdateQuotaPresentation()`，避免两套展示逻辑分叉。

- [x] **Step 3: 按 `QuotaColorBand` 映射 WPF `Brush`。**

```csharp
private static Brush GetQuotaBrush(int? remainingPercent) => QuotaPresentation.GetColorBand(remainingPercent) switch
{
    QuotaColorBand.Green => new SolidColorBrush(Color.FromRgb(0x55, 0xD6, 0xA4)),
    QuotaColorBand.Yellow => new SolidColorBrush(Color.FromRgb(0xF7, 0xDE, 0x6B)),
    QuotaColorBand.Amber => new SolidColorBrush(Color.FromRgb(0xF2, 0xB8, 0x5D)),
    QuotaColorBand.Red => new SolidColorBrush(Color.FromRgb(0xFF, 0x7C, 0x74)),
    _ => new SolidColorBrush(Color.FromRgb(0xD7, 0xDE, 0xE7))
};
```

- [x] **Step 4: 运行展示逻辑测试和 WPF 构建。**

Run: `dotnet run --project tests/CodexQuotaFloat.PresentationTests/CodexQuotaFloat.PresentationTests.csproj -c Release`

Expected: 退出码 0。

Run: `dotnet build CodexQuotaFloat.csproj -c Release`

Expected: 0 errors、0 warnings。

- [x] **Step 5: 提交。**

项目不是 Git 仓库；记录本步骤完成即可，不能执行 Git 提交。

### Task 4: 更新交付说明并发布可运行版本

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-09-08-codex-quota-float-design.md`
- Modify: `docs/superpowers/plans/2026-09-08-codex-quota-float.md`
- Modify: `docs/superpowers/specs/2026-09-08-quota-countdown-color-design.md`

**Interfaces:**
- Consumes: 已实现的 360 × 140 尺寸、倒计时格式和四色阈值。
- Produces: 用户可理解的说明和新的 `release-v3/CodexQuotaFloat.exe` 交付文件。

- [x] **Step 1: 更新 README 与原始设计/计划中的尺寸和功能说明。**

在 README 的“功能”段落加入：

```markdown
- 分别显示 5 小时和周额度的重置倒计时；倒计时每分钟在本地更新，不会触发额外网络请求。
- 剩余比例以四级颜色区分：81–100% 绿色、50–80% 黄色、20–49% 琥珀色、0–19% 红色；缺失数据为中性灰。
- 读取失败时保留上一次成功的额度值，并继续显示非负倒计时。
```

- [x] **Step 2: 发布到独立目录，不覆盖正在运行的版本。**

Run: `dotnet publish CodexQuotaFloat.csproj -c Release -r win-x64 --self-contained false -o release-v3`

Expected: `release-v3/CodexQuotaFloat.exe` 存在。

- [x] **Step 3: 运行本机 App Server 读取回归验证。**

Run: 使用临时控制台程序调用 `CodexAppServerClient.ReadRateLimitsAsync`，只断言返回中含 `rateLimits` 或 `rateLimitsByLimitId`，不输出额度数值。

Expected: 已登录本机环境下读取成功；失败时保留构建和展示层测试结果，并如实报告本机集成读取受阻。

- [x] **Step 4: 清理本次创建的临时构建/测试目录。**

先逐项解析绝对路径、确认都在临时目录内，再删除；保留 `release-v3` 交付目录和项目内测试源文件。

- [x] **Step 5: 提交。**

项目不是 Git 仓库；记录本步骤完成即可，不能执行 Git 提交。
