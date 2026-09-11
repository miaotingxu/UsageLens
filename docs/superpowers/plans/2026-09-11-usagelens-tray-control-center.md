# UsageLens Tray Control Center Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将现有的 UsageLens 顶部悬浮窗扩展为单进程 Windows 工具：保留所有悬浮能力，并新增系统托盘、完整控制中心与可持久化设置页。

**Architecture:** App 创建一个应用宿主；宿主拥有唯一的数据刷新协调器、设置存储、浮窗、控制中心和托盘控制器。悬浮窗和控制中心订阅同一份 UsageSnapshot，不各自启动 codex app-server、定时器或本地会话扫描。现有悬浮窗保持 WPF 代码后置模式；控制中心使用轻量 ViewModel 承载导航、命令和设置。

**Tech Stack:** .NET 10、C#、WPF、H.NotifyIcon.Wpf、现有 codex app-server stdio JSON-RPC 客户端、现有本地 .codex\sessions JSONL 聚合器、Windows Registry HKCU\Software\Microsoft\Windows\CurrentVersion\Run。

## Global Constraints

- 平台保持 Windows 10 / Windows 11 x64；发布保持 win-x64、self-contained、single-file 便携包。
- 应用仍只有一个 UsageLens.exe 进程和一个 Local\UsageLens 单实例互斥锁。
- 额度继续通过已登录的本机 Codex App Server 读取；UsageLens 不提供登录。
- 不读取、复制、解析、上传或持久化 auth.json、JWT、API Key、密码或对话正文。
- 原型中的 account@example.com 是视觉示例；正式版只显示“已检测到 Codex 登录状态”或“未连接 Codex”，不伪造邮箱、套餐或订阅状态。
- Token 统计继续只解析 .codex\sessions 内 token_count 元数据；不改变现有输入、输出、总量、价格和未定价 Token 的计算规则。
- 额度自动刷新下限为 1 分钟；Token 自动刷新下限为 5 分钟；一次“立即刷新”同时刷新两类数据。
- 顶部悬浮窗始终贴附主屏幕工作区顶部，只能横向拖动；折叠仍折叠到顶部 80×8 把手，默认离开 1 秒折叠。
- 保留现有四种悬浮窗样式：Instrument、Glass、Timeline、Terminal；任何新设置不得改变其既有 352×205 展开尺寸和数据展示。
- 界面偏好仅保存在 %LOCALAPPDATA%\UsageLens\settings.json；不新增数据库，也不持久化额度、Token 统计或会话内容。
- 控制中心关闭默认隐藏而不是退出；退出只能经托盘菜单“退出 UsageLens”执行。
- 控制中心使用原型的深色玻璃视觉语言，但实现为原生 WPF，不嵌入 WebView 或 HTML。

---

## 范围和可验收行为

### 本次交付

1. 右下角常驻托盘图标：左键打开控制中心，双击切换悬浮窗显示，右键菜单含打开控制中心、显示/隐藏浮窗、立即刷新、四套样式、设置、退出。
2. 一个可重复打开且不重复实例化的控制中心窗口，含“概览、额度、Token、会话来源、设置”五页。
3. 概览页同时显示 Codex 连接状态、5 小时额度、7 天额度、当日/近 7 天/近 30 天 Token 与估算价格。
4. 额度页复用现有倒计时、绝对重置时间与色彩阈值；Token 页复用现有总量与估价；会话来源页只展示安全的扫描摘要。
5. 设置可即时改变四种样式、透明度、自动折叠、折叠时长、悬停展开、刷新频率、开机启动、关闭窗口行为、托盘主操作、重置界面偏好和复制脱敏诊断。
6. 原浮窗功能不退化：主屏顶部固定、横向拖动、1 秒自动折叠、顶部把手展开、左右箭头循环样式、右键刷新/退出、每分钟额度刷新、每五分钟 Token 刷新。
7. 自包含发布包仍只产生一个最终发布目录和一个 ZIP，README.txt 说明新的托盘和控制中心操作。

### 明确不做

- 不做账号切换、登录、注销、订阅管理、支付或账单查询。
- 不从 Codex 私有凭据文件推断邮箱、套餐、到期时间或账户身份。
- 不展示对话正文、提示词、文件路径、会话标题或原始 JSONL 字段。
- 不新增本地 Token 历史数据库；“重置界面偏好”只删除本应用偏好，不触碰 Codex 会话文件。
- 不支持多显示器垂直拖动，也不把浮窗改成自由移动窗口。

## 目标架构

    UsageLens.exe（一个进程）
    │
    ├── AppHost
    │   ├── UsageDataCoordinator ── CodexAppServerClient / QuotaParser
    │   │                         └─ LocalTokenUsageService
    │   ├── AppSettingsStore ─────── %LOCALAPPDATA%\UsageLens\settings.json
    │   ├── StartupRegistrationService ─ HKCU Run\UsageLens
    │   ├── MainWindow（顶部悬浮窗）
    │   ├── ControlCenterWindowManager ─ ControlCenterWindow
    │   └── TrayIconController ────── Windows notification area
    │
    └── 两个 UI 订阅同一条 SnapshotChanged 事件
        ├── MainWindow：保持四套悬浮窗视觉
        └── ControlCenterWindow：概览 / 额度 / Token / 会话来源 / 设置

## 文件变更清单

    UsageLens.csproj                                      添加托盘依赖和图标资源
    App.xaml / App.xaml.cs                                显式退出和 AppHost 生命周期
    AppHost.cs                                            新建：应用服务装配
    MainWindow.xaml.cs                                    改为订阅 Snapshot，不再拥有读取计时器
    Models/AppSettings.cs                                 新建：完整界面偏好模型
    Models/UsageSnapshot.cs                               新建：共享额度、Token、连接状态快照
    Models/RefreshScope.cs                                新建：Quota、TokenUsage、All 刷新范围
    Models/TrayPrimaryAction.cs                           新建：托盘单击动作
    Services/AppSettingsStore.cs                          新建：settings.json 读取、迁移、原子写入
    Services/AppearanceSettingsStore.cs                   删除，迁入 AppSettingsStore
    Services/UsageDataCoordinator.cs                      新建：唯一刷新入口、timer 和 stale 保留
    Services/TrayCommandRouter.cs                         新建：可测试的纯命令路由
    Services/TrayIconController.cs                        新建：TaskbarIcon、菜单、鼠标事件
    Services/ControlCenterWindowManager.cs                新建：控制中心单实例显示/隐藏
    Services/StartupRegistrationService.cs                新建：当前用户开机启动
    Services/FloatingWindowController.cs                  修改：折叠策略可设置
    Views/ControlCenterWindow.xaml(.cs)                   新建：控制中心窗口壳
    ViewModels/ControlCenterViewModel.cs                  新建：快照与导航状态
    ViewModels/SettingsViewModel.cs                       新建：设置编辑、保存、即时应用
    Views/Pages/*.xaml                                    新建：五个页面
    assets/UsageLens.ico                                  新建：16/24/32/48/256 px 原创图标
    tests/UsageLens.SettingsTests                         新建：设置、迁移、启动注册
    tests/UsageLens.CoordinatorTests                      新建：共享刷新协调器
    scripts/package-release.ps1 / README.md / CHANGELOG.md  更新发布交付物

## 数据契约

先建立以下契约，窗口不得直接调用 CodexAppServerClient 或 LocalTokenUsageService。

    // Models/RefreshScope.cs
    namespace UsageLens.Models;

    [Flags]
    public enum RefreshScope
    {
        None = 0,
        Quota = 1,
        TokenUsage = 2,
        All = Quota | TokenUsage
    }

    // Models/TrayPrimaryAction.cs
    namespace UsageLens.Models;

    public enum TrayPrimaryAction
    {
        OpenControlCenter,
        ToggleFloatingWindow
    }

    // Models/UsageSnapshot.cs
    namespace UsageLens.Models;

    public sealed record UsageSnapshot(
        QuotaState Quota,
        TokenUsageState TokenUsage,
        bool IsCodexAvailable,
        DateTimeOffset CapturedAt)
    {
        public static UsageSnapshot Initial(DateTimeOffset now) => new(
            QuotaState.Loading(),
            TokenUsageState.Loading(),
            IsCodexAvailable: false,
            CapturedAt: now);
    }

    // Models/AppSettings.cs
    namespace UsageLens.Models;

    public sealed record AppSettings(
        FloatingStyleKind FloatingStyle,
        double? FloatingLeft,
        double FloatingOpacity,
        bool AutoCollapseEnabled,
        TimeSpan CollapseDelay,
        bool ExpandOnHandleHover,
        TimeSpan QuotaRefreshInterval,
        TimeSpan TokenRefreshInterval,
        bool StartWithWindows,
        bool HideControlCenterOnClose,
        TrayPrimaryAction TrayPrimaryAction)
    {
        public static AppSettings Default { get; } = new(
            FloatingStyleKind.Glass, null, 0.70,
            true, TimeSpan.FromSeconds(1), true,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5),
            false, true, TrayPrimaryAction.OpenControlCenter);
    }

AppSettingsStore.Load() 必须返回已验证的 AppSettings。透明度不在 [0.35, 1.00]、非有限位置、未知枚举、额度刷新不在 {1, 3, 5} 分钟、Token 刷新不在 {5, 10, 15} 分钟、折叠不在 {1, 2, 3} 秒的字段都回退到该字段默认值。

## 实施任务

### Task 1: 建立生命周期、分支和托盘依赖

**Files:**
- Modify: UsageLens.csproj
- Modify: App.xaml
- Modify: App.xaml.cs
- Create: AppHost.cs
- Create: Assets/UsageLens.ico

**Consumes:** 现有 MainWindow : Window, IDisposable 和单实例互斥锁。

**Produces:** AppHost.Start()、Exit()、Dispose()；后续任务只经宿主取得窗口和服务。

- [ ] **Step 1: 基线分支与工作区检查**

Run:

    git switch develop
    git pull --ff-only origin develop
    git status --short
    git switch -c feature/tray-control-center

Expected: 工作区只包含已确认的本计划文档和原型文件；不携带未确认源代码。

- [ ] **Step 2: 添加托盘依赖及图标**

Run:

    dotnet add .\UsageLens.csproj package H.NotifyIcon.Wpf

Add to UsageLens.csproj:

    <ItemGroup>
      <Resource Include="Assets\UsageLens.ico" />
    </ItemGroup>

创建原创 UsageLens.ico，含 16/24/32/48/256 px；16 px 下可识别为青蓝色底上的简洁镜头/环形，不使用 OpenAI、Codex 或其他产品标志。

- [ ] **Step 3: 让 WPF 只在显式退出时关闭**

Set App.xaml:

    <Application x:Class="UsageLens.App"
                 ShutdownMode="OnExplicitShutdown"
                 Startup="OnStartup"
                 Exit="OnExit">

这保证关闭控制中心不会退出浮窗和托盘。

- [ ] **Step 4: App 改为委托 AppHost**

Create AppHost.cs public boundary:

    namespace UsageLens;

    public sealed class AppHost : IDisposable
    {
        public void Start();
        public void Exit();
        public void Dispose();
    }

App.xaml.cs 保持既有 Local\UsageLens mutex；成功取得 mutex 后创建 _host = new AppHost() 并调用 Start()。OnExit 中先调用 _host.Dispose()，再释放 mutex。AppHost.Exit() 是唯一调用 Application.Current.Shutdown() 的位置。

- [ ] **Step 5: 构建和启动回归**

Run:

    dotnet build .\UsageLens.csproj -c Release
    Start-Process .\bin\Release\net10.0-windows\UsageLens.exe

Expected: build exits 0；只存在一个 UsageLens.exe；旧浮窗仍出现在主屏工作区顶部。用既有 Exit 命令关闭。

- [ ] **Step 6: Commit**

    git add UsageLens.csproj App.xaml App.xaml.cs AppHost.cs Assets\UsageLens.ico
    git commit -m "feat: add application host and tray dependency"

### Task 2: 建立完整、可迁移的设置模型

**Files:**
- Create: Models/AppSettings.cs
- Create: Models/TrayPrimaryAction.cs
- Create: Services/AppSettingsStore.cs
- Delete: Services/AppearanceSettingsStore.cs
- Modify: MainWindow.xaml.cs
- Create: tests/UsageLens.SettingsTests/UsageLens.SettingsTests.csproj
- Create: tests/UsageLens.SettingsTests/Program.cs

**Consumes:** FloatingStyleKind 与旧 appearance.json（UsageLens 和 CodexQuotaFloat 两个目录）。

**Produces:** AppSettingsStore.Load()、Save(AppSettings)、Reset()；任何界面偏好只经此服务读写。

- [ ] **Step 1: 写失败测试**

测试用临时目录，必须断言：

    var store = new AppSettingsStore(tempRoot);
    Assert.Equal(AppSettings.Default, store.Load());

    File.WriteAllText(Path.Combine(tempRoot, "UsageLens", "appearance.json"),
        "{"Style":"Terminal","Left":120.5}");
    var migrated = new AppSettingsStore(tempRoot).Load();
    Assert.Equal(FloatingStyleKind.Terminal, migrated.FloatingStyle);
    Assert.Equal(120.5, migrated.FloatingLeft);
    Assert.Equal(TimeSpan.FromSeconds(1), migrated.CollapseDelay);

    File.WriteAllText(Path.Combine(tempRoot, "UsageLens", "settings.json"),
        "{"FloatingOpacity":3,"QuotaRefreshMinutes":0,"TokenRefreshMinutes":1}");
    var repaired = new AppSettingsStore(tempRoot).Load();
    Assert.Equal(0.70, repaired.FloatingOpacity);
    Assert.Equal(TimeSpan.FromMinutes(1), repaired.QuotaRefreshInterval);
    Assert.Equal(TimeSpan.FromMinutes(5), repaired.TokenRefreshInterval);

SettingsTests 目标 net10.0，只链接 AppSettings、FloatingStyleKind、TrayPrimaryAction、AppSettingsStore，不能引用 WPF。

- [ ] **Step 2: 运行失败测试**

    dotnet run --project .\tests\UsageLens.SettingsTests\UsageLens.SettingsTests.csproj -c Release

Expected: 因 AppSettings 与 AppSettingsStore 缺失而编译失败。

- [ ] **Step 3: 实现读取、迁移与原子写入**

设置路径必须是：

    _settingsPath = Path.Combine(baseDirectory, "UsageLens", "settings.json");
    _currentAppearancePath = Path.Combine(baseDirectory, "UsageLens", "appearance.json");
    _legacyAppearancePath = Path.Combine(baseDirectory, "CodexQuotaFloat", "appearance.json");

顺序必须为：有效 settings.json → 有效的当前 appearance.json → 有效的旧 appearance.json → AppSettings.Default。迁移只把样式与 Left 带入默认 AppSettings，写新 settings.json，绝不删除旧文件。

Save 先写同目录 settings.json.tmp，随后执行 File.Move(temp, target, overwrite: true)，finally 删除残留 tmp。捕获 IOException、UnauthorizedAccessException、JsonException，写入失败不可影响主程序。JSON DTO 只能包含原始值：SchemaVersion、FloatingStyle、FloatingLeft、FloatingOpacity、AutoCollapseEnabled、CollapseDelaySeconds、ExpandOnHandleHover、QuotaRefreshMinutes、TokenRefreshMinutes、StartWithWindows、HideControlCenterOnClose、TrayPrimaryAction。

- [ ] **Step 4: 测试迁移和校验**

    dotnet run --project .\tests\UsageLens.SettingsTests\UsageLens.SettingsTests.csproj -c Release

Expected: exit 0；Terminal/120.5 被保留，其他值正确回退默认值。

- [ ] **Step 5: 用新存储替换 MainWindow 旧引用**

将 _appearanceSettingsStore 替换为 _settingsStore。加载时分别取 settings.FloatingStyle 和 settings.FloatingLeft；保存样式或横向位置时用现有设置的 with 表达式更新完整记录，不覆盖无关设置。

- [ ] **Step 6: Commit**

    git add Models\AppSettings.cs Models\TrayPrimaryAction.cs Services\AppSettingsStore.cs Services\AppearanceSettingsStore.cs MainWindow.xaml.cs tests\UsageLens.SettingsTests
    git commit -m "feat: add unified application settings"

### Task 3: 让浮窗行为可设置，仍严格贴顶

**Files:**
- Modify: Models/AppSettings.cs
- Modify: Services/FloatingWindowController.cs
- Modify: MainWindow.xaml.cs
- Modify: tests/UsageLens.PresentationTests/Program.cs

**Consumes:** AppSettings 的折叠与透明度字段。

**Produces:** FloatingWindowController.ApplyBehavior(FloatingWindowBehavior) 与 MainWindow.ApplySettings(AppSettings)。

- [ ] **Step 1: 添加纯行为记录和测试**

Append:

    public sealed record FloatingWindowBehavior(
        bool AutoCollapseEnabled,
        TimeSpan CollapseDelay,
        bool ExpandOnHandleHover)
    {
        public static FloatingWindowBehavior From(AppSettings settings) => new(
            settings.AutoCollapseEnabled,
            settings.CollapseDelay,
            settings.ExpandOnHandleHover);
    }

PresentationTests 断言 Default 产生 enabled、1 秒、handle hover enabled；SettingsTests 断言 500 ms 折叠配置回退为 1 秒。

- [ ] **Step 2: 运行测试**

    dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release

Expected: 在 behavior 未实现前失败。

- [ ] **Step 3: 改造 FloatingWindowController**

用实例 _behavior 初始化为 FloatingWindowBehavior.From(AppSettings.Default)，新增：

    public void ApplyBehavior(FloatingWindowBehavior behavior)
    {
        ThrowIfDisposed();
        _behavior = behavior;
        _collapseTimer.Interval = behavior.CollapseDelay;
        if (!behavior.AutoCollapseEnabled)
        {
            CancelCollapse();
            Expand();
        }
    }

ScheduleCollapse 在 !_behavior.AutoCollapseEnabled 时立刻返回。HoverZoneOnMouseEnter 只有 _behavior.ExpandOnHandleHover 才 Expand；卡片悬停始终可展开。禁止在此任务引入任何纵向位置改写或自由拖动。

- [ ] **Step 4: MainWindow 应用设置**

浮窗 loaded 后创建 controller，立即调用：

    var settings = _settingsStore.Load();
    _floatingWindowController.ApplyBehavior(FloatingWindowBehavior.From(settings));
    Opacity = settings.FloatingOpacity;

新增 public ApplySettings(AppSettings settings)，在 Dispatcher 执行：应用 opacity、样式、behavior，并保证 Top = SystemParameters.WorkArea.Top；不得调用 PositionOnPrimaryScreen 或覆盖保存的横向 Left。

- [ ] **Step 5: 验证**

    dotnet build .\UsageLens.csproj -c Release
    dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release

Manual expected: 四套样式都可横移；拖动时和拖动后 Top 始终是工作区顶部；启用折叠后只留 80×8 把手；关闭折叠后保持展开。

- [ ] **Step 6: Commit**

    git add Models\AppSettings.cs Services\FloatingWindowController.cs MainWindow.xaml.cs tests\UsageLens.PresentationTests
    git commit -m "feat: make floating window behavior configurable"

### Task 4: 抽出唯一的数据刷新协调器

**Files:**
- Create: Models/RefreshScope.cs
- Create: Models/UsageSnapshot.cs
- Create: Services/IQuotaReader.cs
- Create: Services/ITokenUsageReader.cs
- Create: Services/UsageDataCoordinator.cs
- Modify: Services/CodexAppServerClient.cs
- Modify: MainWindow.xaml.cs
- Create: tests/UsageLens.CoordinatorTests/UsageLens.CoordinatorTests.csproj
- Create: tests/UsageLens.CoordinatorTests/Program.cs

**Consumes:** QuotaState、TokenUsageState、QuotaParser、LocalTokenUsageService。

**Produces:** UsageDataCoordinator.SnapshotChanged、Snapshot、Start()、RefreshAsync(RefreshScope)、ApplySettings(AppSettings)。

- [ ] **Step 1: 写协调器失败测试**

Interfaces:

    public interface IQuotaReader
    {
        Task<QuotaState> ReadAsync(CancellationToken cancellationToken);
    }

    public interface ITokenUsageReader
    {
        Task<TokenUsageState> ReadAsync(DateTimeOffset now, CancellationToken cancellationToken);
    }

使用 FakeQuotaReader/FakeTokenUsageReader（各自公开 CallCount、Next 与 NextException）并断言：

    const int readyFiveHourRemaining = 72;
    await coordinator.RefreshAsync(RefreshScope.All);
    Assert.Equal(1, quotaReader.CallCount);
    Assert.Equal(1, tokenReader.CallCount);
    Assert.Equal(QuotaStatus.Ready, coordinator.Snapshot.Quota.Status);
    Assert.Equal(TokenUsageStatus.Ready, coordinator.Snapshot.TokenUsage.Status);

    quotaReader.NextException = new IOException("offline");
    await coordinator.RefreshAsync(RefreshScope.Quota);
    Assert.Equal(QuotaStatus.Stale, coordinator.Snapshot.Quota.Status);
    Assert.Equal(readyFiveHourRemaining, coordinator.Snapshot.Quota.FiveHourRemaining);

同一测试必须断言 quota-only 不增加 token 调用数，Token 失败也保留旧成功数据并标记 Stale。

- [ ] **Step 2: 运行失败测试**

    dotnet run --project .\tests\UsageLens.CoordinatorTests\UsageLens.CoordinatorTests.csproj -c Release

Expected: 因协调器和接口不存在而编译失败。

- [ ] **Step 3: 编写读取器适配层**

新增内部 CodexQuotaReader : IQuotaReader，封装既有 CodexAppServerClient 和 QuotaParser，保留 initialize/read/reconnect 协议。新增 LocalTokenUsageReader : ITokenUsageReader，封装 LocalTokenUsageService.ReadAsync(now, cancellationToken)。窗口不可获取这两个对象。

Quota reader 在 App Server 不可用时抛异常，让协调器根据是否已有成功值决定 Offline 或 Stale。Token reader 在 sessions 不存在时仍返回 Ready 零值，保持现有语义。

- [ ] **Step 4: 实现协调器**

    public sealed class UsageDataCoordinator : IDisposable
    {
        public UsageSnapshot Snapshot { get; }
        public event EventHandler<UsageSnapshot>? SnapshotChanged;
        public void Start();
        public void ApplySettings(AppSettings settings);
        public Task RefreshAsync(RefreshScope scope, CancellationToken cancellationToken = default);
        public void Dispose();
    }

规则：
- 每种数据使用一个 SemaphoreSlim，防止同类读重叠；额度读和 Token 扫描可以并行。
- Start 使用默认 1/5 分钟计时器，发布 UsageSnapshot.Initial(DateTimeOffset.Now)，非阻塞触发 All 刷新。
- ApplySettings 只能应用白名单周期。
- 所有快照在捕获到的 UI Dispatcher 上发布，保持不可变。
- 读取失败：有最后成功值则 MarkStale；从未成功则 Offline。
- IsCodexAvailable 仅在额度 Read 成功时为 true，在额度 Offline 时为 false；Token 成功不改变该字段。
- Dispose 停止 timer、取消生命周期 token、各 reader 只释放一次。

- [ ] **Step 5: MainWindow 改为订阅者**

删除 MainWindow 中的 _client、_quotaParser、_tokenUsageService、额度/Token/reconnect timers、两组 refresh gate、lifetime cancellation、RefreshAsync、RefreshTokenUsageAsync、client exit handlers。保留分钟倒计时 timer，因为它只刷新标签。

新增：

    public void ApplySnapshot(UsageSnapshot snapshot)
    {
        SetQuotaState(snapshot.Quota);
        SetTokenUsageState(snapshot.TokenUsage);
    }

右键“刷新”调用注入的 Func<Task> refreshAllAsync；“退出”调用注入 Action exitApplication，绝不直接 Close()。

- [ ] **Step 6: 回归测试**

    dotnet run --project .\tests\UsageLens.CoordinatorTests\UsageLens.CoordinatorTests.csproj -c Release
    dotnet run --project .\tests\UsageLens.TokenUsageTests\UsageLens.TokenUsageTests.csproj -c Release
    dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release

Expected: 全部 exit 0，现有 Token 聚合与估价没有变化。

- [ ] **Step 7: Commit**

    git add Models\RefreshScope.cs Models\UsageSnapshot.cs Services\IQuotaReader.cs Services\ITokenUsageReader.cs Services\UsageDataCoordinator.cs Services\CodexAppServerClient.cs MainWindow.xaml.cs tests\UsageLens.CoordinatorTests
    git commit -m "refactor: share usage refreshes across windows"

### Task 5: 实现控制中心和安全账户状态

**Files:**
- Create: ViewModels/ObservableObject.cs
- Create: ViewModels/RelayCommand.cs
- Create: ViewModels/ControlCenterViewModel.cs
- Create: Views/ControlCenterWindow.xaml
- Create: Views/ControlCenterWindow.xaml.cs
- Create: Views/Pages/OverviewPage.xaml
- Create: Views/Pages/QuotaPage.xaml
- Create: Views/Pages/TokenUsagePage.xaml
- Create: Views/Pages/SessionSourcePage.xaml
- Create: Services/ControlCenterWindowManager.cs
- Modify: tests/UsageLens.PresentationTests/Program.cs

**Consumes:** UsageDataCoordinator、UsageSnapshot、现有 QuotaPresentation 与 TokenUsagePresentation。

**Produces:** ControlCenterWindowManager.Show()、Hide()、ShowSettings()，以及一个可重用控制中心实例。

- [ ] **Step 1: 写导航和账户状态测试**

Define:

    public enum ControlCenterPage
    {
        Overview,
        Quota,
        TokenUsage,
        SessionSource,
        Settings
    }

测试 ControlCenterViewModel.SelectPage("tokens") 把 SelectedPage 设为 TokenUsage；IsCodexAvailable 为 true 时 AccountStatusText 精确为“已检测到 Codex 登录状态”，为 false 时精确为“未连接 Codex”。禁止给此 model 增加 email、subscription、expiry、account switch 或 credential 字段。

- [ ] **Step 2: 运行失败测试**

    dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release

Expected: 因 ControlCenterViewModel 与 ControlCenterPage 缺失失败。

- [ ] **Step 3: 实现 ViewModel 契约**

ObservableObject 实现 INotifyPropertyChanged；RelayCommand 实现 ICommand，构造参数为 Action<object?> 和可选 Func<object?, bool>。

ControlCenterViewModel 订阅 UsageDataCoordinator.SnapshotChanged 并经注入 Dispatcher 更新，暴露：

    public UsageSnapshot Snapshot { get; }
    public ControlCenterPage SelectedPage { get; }
    public string AccountStatusText { get; }
    public void SelectPage(string pageKey);
    public ICommand SelectOverviewCommand { get; }
    public ICommand SelectQuotaCommand { get; }
    public ICommand SelectTokenUsageCommand { get; }
    public ICommand SelectSessionSourceCommand { get; }
    public ICommand SelectSettingsCommand { get; }
    public ICommand RefreshCommand { get; }

账户区只显示 AccountStatusText 和“UsageLens 复用本机 Codex 登录态；不会读取或保存凭据。”。

SelectPage 只接受 overview、quota、tokens、sessions、settings 五个 pageKey，并分别映射到 Overview、Quota、TokenUsage、SessionSource、Settings；其他值抛出 ArgumentException。五个导航命令分别传入这五个固定 key。

- [ ] **Step 4: 实现控制中心壳**

创建 920×640、WindowStartupLocation=CenterScreen 的深色玻璃 ControlCenterWindow。左侧为概览/额度/Token/会话来源/设置；右上为账户状态；中间 ContentControl 以 DataTemplate 映射五个 enum。导航选中状态必须同时有文本和外形变化，不能只用颜色。

正式账户区必须渲染：

    UsageLens
    已检测到 Codex 登录状态 | 未连接 Codex
    复用本机 Codex 登录态；不会读取或保存凭据。

不能保留原型的 account@example.com 或 Plus · 订阅有效。

- [ ] **Step 5: 实现四个数据页**

- 概览：两张额度卡（5 小时、7 天额度）和三张 Token 卡（当日、近 7 天、近 30 天），显示既有格式化总量、估价、更新时间。
- 额度：保留 >80% 绿色、50–80% 黄色、20–49% 琥珀、<20% 红色、2 px 进度条、倒计时和绝对重置时间。
- Token：显示 IN、OUT、总 Token、$ 估价和未定价提示；保留 K/M/亿 格式。
- 会话来源：只显示“数据源：本机 .codex\sessions”“统计窗口：近 30 个本地自然日”“上次扫描：HH:mm:ss”“状态：正在读取/可用/离线”以及隐私说明；不可列文件或原始字段。

- [ ] **Step 6: 管理窗口显示与关闭**

ControlCenterWindowManager 只能构造一个窗口。Show() 从最小化恢复、Show、Activate、BringIntoView。Closing 在 HideControlCenterOnClose 为 true 时 Cancel=true 并 Hide。Dispose 解除事件后真实关闭，仅由显式应用退出调用。

- [ ] **Step 7: 验证**

    dotnet build .\UsageLens.csproj -c Release
    dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release

Manual expected: 五页可鼠标和键盘切换；关闭控制中心不影响浮窗；再次打开仍是同一实例；账户区不会显示邮箱或订阅推断。

- [ ] **Step 8: Commit**

    git add ViewModels Views Services\ControlCenterWindowManager.cs tests\UsageLens.PresentationTests
    git commit -m "feat: add usage control center dashboard"

### Task 6: 实现托盘与唯一退出路径

**Files:**
- Create: Services/TrayCommandRouter.cs
- Create: Services/TrayIconController.cs
- Modify: AppHost.cs
- Modify: MainWindow.xaml.cs
- Modify: tests/UsageLens.CoordinatorTests/Program.cs

**Consumes:** ControlCenterWindowManager、UsageDataCoordinator、MainWindow、AppSettings。

**Produces:** 可靠的单击、双击、右键菜单，且退出不误关/遗漏其他窗口。

- [ ] **Step 1: 为纯命令路由写测试**

    public sealed class TrayCommandRouter
    {
        public void OpenControlCenter();
        public void ToggleFloatingWindow();
        public Task RefreshAllAsync();
        public void OpenSettings();
        public void SelectStyle(FloatingStyleKind style);
        public void ExitApplication();
    }

用注入 delegate 计数，断言每个方法只触发一个预期动作。RefreshAllAsync 必须仅调用 UsageDataCoordinator.RefreshAsync(RefreshScope.All) 一次，不能创建或关闭窗口。

- [ ] **Step 2: 运行失败测试**

    dotnet run --project .\tests\UsageLens.CoordinatorTests\UsageLens.CoordinatorTests.csproj -c Release

Expected: TrayCommandRouter 未实现而失败。

- [ ] **Step 3: 实现 TrayIconController**

TrayIconController 独占一个 H.NotifyIcon.Wpf TaskbarIcon，tooltip 为 UsageLens，图标从 /Assets/UsageLens.ico WPF resource 读取。右键菜单顺序必须为：

    打开控制中心
    显示悬浮窗 / 隐藏悬浮窗
    立即刷新
    界面样式 > 仪表 / 玻璃 / 时间线 / 终端
    设置
    —
    退出 UsageLens

行为：
- 单击按 AppSettings.TrayPrimaryAction，默认打开控制中心。
- 双击永远切换浮窗显示。
- 右击只打开菜单。
- “立即刷新” await RefreshAllAsync，运行中仅禁用自身。
- 样式项保存 FloatingStyle、立即应用给 MainWindow，并对当前项显示选中状态。
- 退出调用 AppHost.Exit()，不得调用 Window.Close()。

- [ ] **Step 4: AppHost 成为唯一装配点**

Start 顺序：Load settings → 构建一个 UsageDataCoordinator 并 Start → 构建/Show MainWindow、订阅 SnapshotChanged、ApplySettings → 构建 ControlCenterWindowManager → 构建 TrayCommandRouter/TrayIconController。

Exit 顺序：Dispose tray → dispose control center → dispose floating window → dispose coordinator → Application.Current.Shutdown()。此顺序防止迟到的托盘事件命中已释放窗口。

- [ ] **Step 5: 本机托盘手测**

    dotnet build .\UsageLens.csproj -c Release
    Start-Process .\bin\Release\net10.0-windows\UsageLens.exe

Expected: 系统通知区域出现图标；单击只打开一个控制中心；双击只隐藏/显示浮窗；右键所有菜单工作；退出后无 UsageLens.exe。该项是 Windows Shell 手测，console test 不足以证明通知区域渲染。

- [ ] **Step 6: Commit**

    git add Services\TrayCommandRouter.cs Services\TrayIconController.cs AppHost.cs MainWindow.xaml.cs tests\UsageLens.CoordinatorTests
    git commit -m "feat: add system tray controls"

### Task 7: 完成设置页与当前用户开机启动

**Files:**
- Create: Services/StartupRegistrationService.cs
- Create: ViewModels/SettingsViewModel.cs
- Create: Views/Pages/SettingsPage.xaml
- Modify: AppHost.cs
- Modify: ViewModels/ControlCenterViewModel.cs
- Modify: tests/UsageLens.SettingsTests/Program.cs

**Consumes:** AppSettingsStore、UsageDataCoordinator.ApplySettings、MainWindow.ApplySettings、TrayIconController、ControlCenterWindowManager。

**Produces:** 设置每次修改即时生效且持久化；开机启动仅影响当前用户。

- [ ] **Step 1: 写启动注册与设置保存失败测试**

    public interface IRunKeyStore
    {
        string? GetValue(string name);
        void SetValue(string name, string command);
        void DeleteValue(string name);
    }

使用 fake store，断言开启写入名为 UsageLens 的带引号 exe path；关闭只删除 UsageLens；不触碰其他应用 value。另断言保存 FloatingOpacity=0.85 时，AppSettingsStore.Save 与 MainWindow.ApplySettings 都收到 0.85。

- [ ] **Step 2: 运行失败测试**

    dotnet run --project .\tests\UsageLens.SettingsTests\UsageLens.SettingsTests.csproj -c Release

Expected: StartupRegistrationService、IRunKeyStore、SettingsViewModel 缺失。

- [ ] **Step 3: 实现当前用户启动项**

StartupRegistrationService 打开 Registry.CurrentUser 的 Software\Microsoft\Windows\CurrentVersion\Run。启用时仅写名称 UsageLens，值为：

    $""{Environment.ProcessPath}""

禁用时仅删除该名称。Environment.ProcessPath 为 null、key 不可用或拒绝访问，Apply 返回 false 且不保存设置。每次成功启用以当前绝对路径更新，因此便携程序移动后重新启用可修正路径。

- [ ] **Step 4: 实现 SettingsViewModel 保存顺序**

暴露 SelectedStyle、FloatingOpacity、AutoCollapseEnabled、CollapseDelaySeconds、ExpandOnHandleHover、QuotaRefreshMinutes、TokenRefreshMinutes、StartWithWindows、HideControlCenterOnClose、TrayPrimaryAction 和 RefreshNowCommand、ResetUiPreferencesCommand、CopyDiagnosticsCommand。

每个可编辑值构造经验证 AppSettings，调用顺序必须为：

    AppSettingsStore.Save(settings)
    UsageDataCoordinator.ApplySettings(settings)
    MainWindow.ApplySettings(settings)
    TrayIconController.ApplySettings(settings)
    ControlCenterWindowManager.ApplySettings(settings)

StartWithWindows 先调用 StartupRegistrationService.Apply；仅其返回 true 才保存及应用。

ResetUiPreferences：store.Reset()，StartupRegistrationService.Apply(false)，应用 AppSettings.Default；绝不删除 .codex、sessions、凭据、其他注册表值或日志。

CopyDiagnostics 写入剪贴板的精确格式：

    UsageLens 1.1.0
    Codex connection: Ready|Offline|Stale|Loading
    Quota status: Ready|Offline|Stale|Loading
    Token status: Ready|Offline|Stale|Loading
    Last update: yyyy-MM-dd HH:mm:ss|--

不得包含用户名、邮箱、session path、Token 数、原始异常、授权数据或剪贴板旧内容。

- [ ] **Step 5: 实现设置页**

五组控件：
1. 外观：四套样式单选、35%–100% 透明度滑块、重置顶部横向位置按钮。
2. 悬浮窗：自动折叠、1/2/3 秒、悬停顶部把手展开；只读说明“固定于主屏幕顶部，仅可左右移动”。
3. 数据刷新：额度 1/3/5 分钟、Token 5/10/15 分钟、立即刷新。
4. 托盘与启动：开机启动、关闭控制中心时隐藏、托盘单击动作。
5. 隐私与诊断：当前状态、数据边界、重置、复制诊断。

每个 ToggleButton、ComboBox、Slider、Button 必须有 AutomationProperties.Name。启动项失败时内联显示“无法写入当前用户的开机启动项，设置未保存。”，不可显示原始注册表异常。

- [ ] **Step 6: 测试与手测**

    dotnet run --project .\tests\UsageLens.SettingsTests\UsageLens.SettingsTests.csproj -c Release
    dotnet build .\UsageLens.csproj -c Release

Manual expected: 设置无需重启即生效，重启后保留；重置恢复 Glass、70%、1 秒、1/5 分钟和单击打开控制中心；HKCU\...\Run\UsageLens 仅在开启时存在。

- [ ] **Step 7: Commit**

    git add Services\StartupRegistrationService.cs ViewModels\SettingsViewModel.cs Views\Pages\SettingsPage.xaml AppHost.cs ViewModels\ControlCenterViewModel.cs tests\UsageLens.SettingsTests
    git commit -m "feat: add control center settings"

### Task 8: 打包、文档、版本和发布

**Files:**
- Modify: UsageLens.csproj
- Modify: Services/CodexAppServerClient.cs
- Modify: scripts/package-release.ps1
- Modify: README.md
- Modify: CHANGELOG.md
- Modify: .github/workflows/release.yml
- Create: docs/screenshots/control-center.png

**Consumes:** 托盘、控制中心、设置和现有便携式发布脚本。

**Produces:** v1.1.0 单文件 Windows 便携包和完整开源文档。

- [ ] **Step 1: 统一 1.1.0**

UsageLens.csproj 的 Version 由 1.0.1 改为 1.1.0；CodexAppServerClient 内现有 clientInfo version 同样改 1.1.0。发布 tag 为 v1.1.0，不创建额外版本目录。

- [ ] **Step 2: 为发布脚本添加完整测试门禁**

在 dotnet publish 前按顺序运行：

    dotnet run --project .\tests\UsageLens.TokenUsageTests\UsageLens.TokenUsageTests.csproj -c Release
    dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release
    dotnet run --project .\tests\UsageLens.SettingsTests\UsageLens.SettingsTests.csproj -c Release
    dotnet run --project .\tests\UsageLens.CoordinatorTests\UsageLens.CoordinatorTests.csproj -c Release

更新生成的 README.txt：

    托盘操作
    --------
    1. 运行后，右下角出现 UsageLens 图标。
    2. 单击默认打开控制中心；双击显示或隐藏顶部悬浮窗。
    3. 右键可立即刷新、切换样式、打开设置或退出程序。

    控制中心与隐私
    --------------
    控制中心展示与悬浮窗相同的一份额度和 Token 数据。程序复用本机 Codex 的已有登录状态，不读取或保存账号凭据；Token 统计仅扫描本机会话的用量元数据。

- [ ] **Step 3: 更新 README、CHANGELOG 和截图**

README 新增“控制中心与系统托盘”“设置”“隐私边界”。明确账户邮箱/套餐未读取。截图保存为 docs/screenshots/control-center.png：背景用中性纯色，不含个人桌面、浏览器标签、用户名或真实 Token。

CHANGELOG 追加：

    ## [1.1.0] - 2026-09-11

    ### Added
    - 系统托盘与控制中心，和顶部悬浮窗共享同一份刷新数据。
    - 可配置外观、折叠、刷新、托盘和当前用户开机启动。

    ### Changed
    - 设置从 appearance.json 迁移至 settings.json，旧文件保留不删除。

    ### Privacy
    - 控制中心只显示 Codex 可用状态，不读取或保存账号凭据、邮箱或订阅信息。

- [ ] **Step 4: 发布构建与产物清点**

    .\scripts\package-release.ps1 -Version 1.1.0
    Get-ChildItem .\release-floating-auto -Force
    Get-ChildItem .\artifacts -Force

Expected:
- release-floating-auto 仅含 UsageLens.exe、LICENSE、README.txt。
- artifacts 含 UsageLens-v1.1.0-win-x64-portable.zip 和 SHA256SUMS.txt。
- 双击包内 exe：单进程、浮窗、托盘、控制中心、关闭隐藏、菜单退出全部可用。

- [ ] **Step 5: 完整回归清单**

- [ ] 额度：5 小时、7 天、倒计时、绝对重置时间、2 px 进度条与四段色彩未变。
- [ ] Token：当日、近 7 天、近 30 天的 IN/OUT/总量、K/M/亿、美元估价和未定价标记未变。
- [ ] 浮窗：四样式、左右循环、横向拖动、主屏贴顶、80×8 把手、默认 1 秒折叠可用。
- [ ] 托盘：单击、双击、右键、重复打开、隐藏、刷新、退出按定义工作。
- [ ] 控制中心：五页、关闭隐藏、即时设置、重启持久化、账户区无虚构身份数据。
- [ ] 无 Codex / 无 sessions：额度离线或 stale；Token 为零或 stale；两类数据互不清空。
- [ ] 单实例：第二次运行不产生第二个窗口、图标或 UsageLens.exe。

- [ ] **Step 6: 提交、推送、合并和打 tag**

    git add UsageLens.csproj Services\CodexAppServerClient.cs scripts\package-release.ps1 README.md CHANGELOG.md .github\workflows\release.yml docs\screenshots\control-center.png
    git commit -m "release: prepare v1.1.0 control center"
    git push -u origin feature/tray-control-center

Review 后：

    git switch develop
    git merge --ff-only feature/tray-control-center
    git push origin develop
    git switch main
    git merge --ff-only develop
    git tag -a v1.1.0 -m "UsageLens v1.1.0"
    git push origin main v1.1.0

## 验证边界

- Console tests 证明设置校验/迁移、协调器 stale 保留、周期选择、本地 Token 算法、展示格式和纯托盘路由。
- dotnet build 与发布脚本证明编译和便携式打包。
- Windows 通知区域图标、单/双/右键、Registry 启动项、动画流畅度、主屏贴顶属于手测范围，成功构建不等于已证明这些系统交互。
- 账户区故意不验证邮箱或订阅状态，因为本计划禁止读取凭据；只验证 Codex 额度连接可用与否。

## 计划自检

- [x] 覆盖原型的控制中心导航、右上账户区、设置和托盘，并将示例邮箱替换为安全连接状态。
- [x] 覆盖原浮窗保持不变、共享刷新、防双读、顶部横向拖动和显式退出。
- [x] 覆盖设置迁移、即时生效、开机启动、重置边界、测试、便携打包、文档和 v1.1.0 发布。
- [x] 后续引用的公开类型均在数据契约或任务中定义，实施步骤没有未定义的工作项。

# 产品设计规范

本节将控制中心从可点击原型收敛为 WPF 可实现的视觉与交互标准。除非本节明确覆盖，否则沿用前文的数据、隐私和功能约束；发生冲突时，以本节产品约束为准。

## 设计原则

1. 一个数据源，两个入口。悬浮窗用于快速查看；控制中心用于阅读、配置和诊断；两者只消费同一份 UsageSnapshot。
2. 数据优先于装饰。额度、Token、价格、状态、更新时间必须可直接读取，不能被图形替代。
3. 安全状态胜过伪造身份。正式版不显示示例邮箱、套餐、订阅到期时间或账号头像，只显示 Codex 可用状态。
4. 低干扰。控制中心关闭隐藏到托盘；浮窗固定在主屏顶部，设置不能解除该限制。
5. 原型不是数据承诺。原型中的 7 日柱状趋势不在 v1.1.0 实现，因为 TokenUsageState 只提供当日、7 日、30 日聚合。正式总览以三张周期汇总卡代替；未来只有先增加按日数据契约与测试，才能增加趋势图。

## 视觉令牌

### 窗口尺寸与栅格

| 项目 | 标准 |
| --- | --- |
| 控制中心默认尺寸 | 920 × 640 DIP |
| 最小尺寸 | 860 × 600 DIP |
| 最大化 | 禁用；允许普通缩放，内容始终满足最小尺寸 |
| 窗口圆角 | 16 DIP |
| 窗口边框 | 1 DIP，#2C4053，55% 不透明 |
| 窗口底色 | #0A111C，95% 不透明 |
| 玻璃叠层 | #192839，72% 到 #080E18，55% 的纵向渐变 |
| 外缘阴影 | 0 18 48 #000000，42% 不透明，仅控制中心外缘使用 |
| 间距基准 | 8 DIP；允许 4、8、12、16、20、24、28、32 DIP |
| 左侧导航栏 | 176 DIP 宽，16 DIP 内边距 |
| 内容区 | 左右 28 DIP、顶部 24 DIP、底部 20 DIP |
| 卡片圆角 / 内边距 | 14 DIP / 标准 16 DIP，紧凑统计卡 14 DIP |
| 卡片间距 | 同行与上下均为 12 DIP |

在 100% Windows 缩放下使用上述 DIP。125%、150%、200% 缩放由 WPF DPI 自动适配；不得使用屏幕物理像素计算字体、窗口或鼠标命中区。

### 字体与数值

字体族为 Segoe UI, Microsoft YaHei UI, Microsoft YaHei, sans-serif；不得加载网络字体。

| 用途 | 字号 / 字重 / 行高 | 颜色 |
| --- | --- | --- |
| 应用品牌 | 16 / SemiBold / 22 | #F2F7FF |
| 页面标题 | 24 / SemiBold / 32 | #F2F7FF |
| 页面副标题 | 12 / Regular / 18 | #8FA1B7 |
| 区块标题 | 14 / SemiBold / 20 | #E2EBF7 |
| 卡片标签 | 11 / Medium / 16 | #8FA1B7 |
| 额度百分比 | 30 / Bold / 36 | 对应状态色 |
| Token 主数值 | 24 / Bold / 30 | #F2F7FF |
| IN / OUT / 价格 | 14 / SemiBold / 20 | #7ECBFF / #C7A4FF / #79E5B7 |
| 正文 / 辅助 | 12 / Regular / 18；10 / Regular / 15 | #BAC8D8 / #74879E |
| 按钮 / 状态胶囊 | 11 / SemiBold / 16；10 / SemiBold / 14 | #D7E4F2 / 状态色 |

数字使用 WPF Typography.NumeralAlignment=Tabular，百分比、Token 和价格右对齐。无数据展示 --；会话目录不存在时 Token 为真实零值，显示 0 并注明“未找到本地会话记录”。

### 颜色、控件和动效

| 语义 | 前景 | 填充或轨道 | 说明 |
| --- | --- | --- | --- |
| 主文字 / 次文字 / 弱文字 | #F2F7FF / #BAC8D8 / #74879E | — | 标题、正文、更新时间 |
| 面板 / 分隔线 | #192839 | #0D1723 / #FFFFFF 8% | 卡片渐变、细分隔线 |
| 额度健康 | #79E5B7 | #2E8D6A | 剩余 >80%，已连接 |
| 额度注意 | #F2CF73 | #B88B32 | 剩余 50–80% |
| 额度偏低 | #F0A45D | #A85C24 | 剩余 20–49% |
| 额度危险 | #F27575 | #AE3E46 | 剩余 <20%，离线错误 |
| 输入 / 输出 / 价格 | #7ECBFF / #C7A4FF / #79E5B7 | #255A7A / #664E8A / #245D4B | Token 与估价 |
| Loading / Disabled | #9FB1C7 / #647489 | #314255 / #1C2937 | 骨架与禁用状态 |

所有状态不能只依赖颜色：额度同时给出百分比、文字和 tooltip；连接状态同时给出圆点、文字和最近更新时间。

- 命中目标最小 32 × 32 DIP；普通按钮高 32 DIP，图标按钮为 32 × 32 DIP。
- 导航项高 40 DIP、圆角 10 DIP；图标 20 DIP，图标与文字间距 10 DIP。
- 进度条高 6 DIP、圆角 3 DIP，灰色轨道 #263649，填充用状态纯色。
- hover 为 120 ms ease-out，按下为 80 ms；页面切换为 160 ms 淡入并上移 4 DIP。系统启用“减少动态效果”时取消页面与数值动画。
- 禁止控制中心使用霓虹光晕、自动数字滚动、厚重浮层阴影；悬浮窗继续沿用现有风格，不由本节重绘。

## 控制中心页面规格

### 共享顶部栏与导航

顶部栏高 58 DIP，不随页面内容滚动。左侧是页面标题和副标题，二者垂直间距 4 DIP；右侧依次是连接状态胶囊、立即刷新图标按钮、更多菜单按钮，元素间距 8 DIP。

连接胶囊最小宽 126 DIP，只允许以下文本：

- 已检测到 Codex 登录状态
- 正在连接 Codex
- 未连接 Codex
- 使用缓存数据

立即刷新执行时替换为 16 DIP 转圈并禁用重复点击，最长 30 秒；超时恢复按钮并显示脱敏错误提示。

左侧导航顺序固定为：总览（overview）、额度窗口（quota）、Token 用量（tokens）、会话来源（sessions）、设置（settings）。品牌区高 52 DIP，品牌符号 30 × 30 DIP；底部状态卡距离侧栏边缘 16 DIP，显示“本机数据 · 已保护”和最近成功刷新时间。离线时改为“本机数据 · 等待 Codex”，不得暴露原始错误。

### 总览页

内容顺序固定：

1. 三列摘要卡：5 小时额度、7 天额度、今日 Token；每列 1fr，最小高 112 DIP。
2. 满宽额度窗口卡，最小高 132 DIP；每行采用 105 DIP 名称、弹性进度条、54 DIP 百分比三列，两行间距 14 DIP。
3. 满宽 Token 周期卡，最小高 132 DIP；横向三列“当日、近 7 天、近 30 天”，每列显示总 Token、估价与 IN/OUT 小标签。
4. 底部信息行：左侧“数据来源：Codex 本地会话”，右侧“最近同步 HH:mm:ss”。

总览不渲染原型中的 7 日柱状图。Token Loading 时三张卡显示两行骨架；Offline 或 Stale 时保留上次数据并显示状态胶囊。

### 额度窗口页

- 顶部两个等宽卡片：5 小时与 7 天额度，每卡高 150 DIP。
- 卡片内顺序：标签、百分比、6 DIP 进度条、RESET 2h 29m · 09/09 12:59。
- 重置文本固定 11 DIP，日期格式 MM/dd HH:mm；没有 reset timestamp 显示 RESET --。
- 底部“数据说明”卡高 88 DIP，说明“剩余百分比 = 100 - Codex reported usedPercent；更新频率按设置执行。”
- Offline 时主数值为 --，只显示灰色轨道；Stale 时保留末次数据，在卡片右上显示“缓存”胶囊。

### Token 用量页

- 顶部状态行左侧“本机 Token 统计”，右侧 Updated HH:mm:ss、Loading… 或使用缓存数据。
- 主体三行周期卡：当日、近 7 天、近 30 天；每行高 104 DIP，行间距 10 DIP。
- 行内左列 112 DIP 为周期名；中列为总 Token（24 DIP）和 IN/OUT（14 DIP）；右列 132 DIP 为绿色估价和未定价提示。
- 格式规则：<10,000 原值；≥10,000 用 万；≥100,000,000 用 亿。保留两位小数并去除无意义尾零；例如 `12,500` 显示 `1.25万`，`123,456,789` 显示 `1.23亿`。总量为 input_tokens + output_tokens。
- 估价使用 $12.27；没有模型价格显示 $--，并显示未定价 12.4K，不得把未定价 Token 当 $0。
- IN tooltip：输入 Token（包含 cached input）: 1,234,567；OUT tooltip：输出 Token: 234,567；总量 tooltip 使用精确整数。
- 控制中心只能显示服务层已计算结果，不得在 UI 重算模型价格或累计值。

### 会话来源页

页面名称是“会话来源”，而不是“会话记录”，避免暗示可浏览聊天内容。

- 统计范围卡：数据源：本机 .codex\sessions；扫描窗口：最近 30 个本地自然日；读取内容：token_count 用量元数据。
- 隐私边界卡：不会上传会话文件、不会读取对话正文、不会保存账号凭据。
- 扫描状态卡：显示 token status、最近扫描时间、无本地记录或已发现本地记录。
- 不得列出文件路径、文件名、会话标题、工作区名、模型提示词或原始 JSON；没有“清空统计”按钮。

### 设置页

两列卡片布局，窗口窄于 900 DIP 自动转单列；每卡至少 146 DIP 高，卡间距 12 DIP。

| 分组 | 控件 | 默认值 | 写入行为 |
| --- | --- | --- | --- |
| 外观 | 四样式单选、35–100% 透明度、重置水平位置 | Glass、70%、顶部居中 | 保存 settings.json 并立即 ApplySettings |
| 悬浮行为 | 自动折叠、1/2/3 秒、把手悬停展开 | 开、1 秒、开 | 保存并调用 ApplyBehavior |
| 数据刷新 | 额度 1/3/5 分钟、Token 5/10/15 分钟、立即刷新 | 1 / 5 分钟 | 周期立即生效；立即刷新不改配置 |
| 托盘与启动 | 开机启动、关闭中心隐藏、托盘单击动作 | 关、开、打开中心 | 启动项先成功写 Registry 再保存 |
| 隐私与诊断 | 数据边界、连接状态、复制诊断、重置界面偏好 | 不适用 | 重置仅处理 UsageLens 设置和启动项 |

每次保存成功在右下显示 1.8 秒“已保存”toast。失败显示单行、无敏感字段的提示，例如“无法写入当前用户的开机启动项，设置未保存。”。

### 页面视觉规格矩阵

下面的矩阵是实现和验收时的单一视觉基线。尺寸均为 DIP；页面内容区以默认窗口 920 × 640 为基准，实际尺寸变化时只允许内容列拉伸，不改变固定列、行高和间距。

| 页面 | 内容布局与尺寸 | 关键间距 | 主字体与数值 | 页面专用颜色 |
| --- | --- | --- | --- | --- |
| 总览 | 摘要卡 3 列、每列最小 112 高；额度卡 132 高；Token 卡 132 高 | 页面顶部 0；卡片之间 12；额度行间 14；卡片内 16 | 页面标题 24/32；卡片数字 30/36；Token 总量 24/30 | 额度按健康/注意/偏低/危险色；Token 总量 #7ECBFF；价格 #79E5B7 |
| 额度窗口 | 5 小时、7 天两列等宽，每卡 150 高；说明卡 88 高 | 两卡间 12；进度条上下 12/10；重置文本距进度条 10 | 百分比 30/36 Bold；标签 11/16；重置时间 11/16 | 进度填充沿用阈值色；无数据轨道 #263649；缓存胶囊 #F2CF73 |
| Token 用量 | 三行周期卡，每行 104 高；周期列 112；价格列 132 | 行间 10；卡片内 16；IN/OUT 行间 4 | 总量 24/30 Bold；IN/OUT 14/20 SemiBold；价格 14/20 | IN #7ECBFF；OUT #C7A4FF；价格 #79E5B7；未定价 #F2CF73 |
| 会话来源 | 统计范围、隐私边界、扫描状态三张满宽卡；每卡最小 104 高 | 卡片间 12；标题到第一行 12；说明行间 6 | 区块标题 14/20；正文 12/18；辅助 10/15 | 隐私说明 #BAC8D8；状态正常 #79E5B7；等待/缓存 #F2CF73 |
| 设置 | 外观/悬浮行为、数据刷新/托盘启动两行双列；隐私与诊断满宽 | 列间 12；行间 12；卡片内 16；控件垂直间距 10 | 分组标题 14/20；控件文本 12/18；选项标签 11/16 | 可编辑值 #D7E4F2；成功提示 #79E5B7；失败提示 #F27575 |

页面状态统一使用以下表现，避免每个页面自行发明状态文案：

| 状态 | 主数据 | 辅助文案 | 颜色与控件 |
| --- | --- | --- | --- |
| Loading | 保留已成功值；没有成功值时显示骨架或 `--` | `正在连接 Codex` / `Loading…` | #9FB1C7；刷新按钮禁用，最长 30 秒 |
| Ready | 显示最新值 | `已检测到 Codex 登录状态` 或 `Updated HH:mm:ss` | 主色和对应数据语义色 |
| Stale | 保留最后一次成功值 | `使用缓存数据` | 胶囊 #F2CF73；不得把缓存值伪装成最新值 |
| Offline | 额度显示 `--`；Token 若确认为无本地记录显示 `0` | `未连接 Codex` 或 `未找到本地会话记录` | 灰色轨道 #263649；错误只显示脱敏文案 |

数值列统一使用右对齐和 Tabular 数字；页面之间不允许改变同一数据的字号、单位或颜色。所有价格都使用美元 `$`，所有时间都按本机时区显示 `HH:mm:ss` 或 `MM/dd HH:mm`。

控制中心预览图：`docs/screenshots/control-center.png`。预览使用中性示例数据，不包含真实账号、路径、会话标题或凭据；它只用于确认布局和视觉层级，不能作为数据准确性的证据。

## 页面状态流转图

~~~mermaid
stateDiagram-v2
    [*] --> Starting
    Starting --> FloatingVisible: 单实例锁成功 / AppHost.Start
    Starting --> ExistingInstance: 单实例锁失败
    ExistingInstance --> [*]: 立即退出

    FloatingVisible --> Refreshing: 首次读取或定时刷新
    Refreshing --> FloatingVisible: 读取成功
    Refreshing --> CachedVisible: 失败且存在上次成功值
    Refreshing --> OfflineVisible: 失败且无成功值
    CachedVisible --> Refreshing: 定时或手动刷新
    OfflineVisible --> Refreshing: 定时或手动刷新

    FloatingVisible --> FloatingCollapsed: 离开 + 折叠延迟
    CachedVisible --> FloatingCollapsed: 离开 + 折叠延迟
    OfflineVisible --> FloatingCollapsed: 离开 + 折叠延迟
    FloatingCollapsed --> FloatingVisible: 卡片或把手悬停
    FloatingVisible --> FloatingHidden: 托盘双击或菜单隐藏
    FloatingCollapsed --> FloatingHidden: 托盘双击或菜单隐藏
    FloatingHidden --> FloatingVisible: 托盘双击或菜单显示

    FloatingVisible --> CenterVisible: 托盘单击或菜单打开中心
    FloatingCollapsed --> CenterVisible: 托盘单击或菜单打开中心
    FloatingHidden --> CenterVisible: 托盘单击或菜单打开中心
    CenterVisible --> CenterHidden: 关闭中心且设置为隐藏
    CenterHidden --> CenterVisible: 托盘打开中心
    CenterVisible --> CenterVisible: 页签切换、设置保存、立即刷新

    FloatingVisible --> Exiting: 托盘退出
    FloatingCollapsed --> Exiting: 托盘退出
    FloatingHidden --> Exiting: 托盘退出
    CenterVisible --> Exiting: 托盘退出
    CenterHidden --> Exiting: 托盘退出
    Exiting --> [*]: 依次释放托盘、中心、浮窗、协调器
~~~

补充规则：关闭自动折叠后，FloatingVisible 不可转入 FloatingCollapsed；关闭把手悬停展开后，只有进入已展开卡片才展开；控制中心不会锁定浮窗；设置即时应用失败时必须恢复最后成功保存值。

## 托盘与窗口交互时序图

~~~mermaid
sequenceDiagram
    participant User as 用户
    participant Tray as TrayIconController
    participant Router as TrayCommandRouter
    participant Host as AppHost
    participant Center as ControlCenterWindowManager
    participant Float as MainWindow
    participant Data as UsageDataCoordinator
    participant Codex as codex app-server

    User->>Tray: 左键单击
    Tray->>Router: OpenControlCenter()
    Router->>Center: Show()
    Center-->>User: 激活已有窗口或显示新窗口

    User->>Tray: 双击
    Tray->>Router: ToggleFloatingWindow()
    Router->>Float: Show() 或 Hide()
    Float-->>User: 显示或隐藏顶部悬浮窗

    User->>Tray: 右键 > 立即刷新
    Tray->>Router: RefreshAllAsync()
    Router->>Data: RefreshAsync(All)
    par 额度读取
        Data->>Codex: account/rateLimits/read
        Codex-->>Data: JSON-RPC result 或 error
    and Token 扫描
        Data->>Data: 扫描新增 JSONL 用量元数据
    end
    Data-->>Float: SnapshotChanged
    Data-->>Center: SnapshotChanged
    Float-->>User: 更新悬浮窗
    Center-->>User: 更新当前页面

    User->>Tray: 右键 > 退出 UsageLens
    Tray->>Host: Exit()
    Host->>Tray: Dispose()
    Host->>Center: Dispose()
    Host->>Float: Dispose()
    Host->>Data: Dispose()
    Host-->>User: 进程退出
~~~

## Codex App Server JSON-RPC 请求与响应

### 传输和生命周期

- 启动命令为 codex app-server --listen stdio://。
- stdin/stdout 使用 UTF-8 JSON Lines；每行恰好一个 JSON-RPC 请求、通知或响应。
- stderr 只排空用于避免子进程阻塞，不是协议输入，也不向用户显示。
- 请求 id 为递增正整数；每个未完成 id 对应一个 TaskCompletionSource。
- initialize 成功并发送 initialized 通知前，禁止调用 account/rateLimits/read。
- 30 秒内未收到响应视为本次失败；协调器依据是否存在末次成功值，分别显示 Stale 或 Offline。

### 初始化

请求：

~~~json
{"id":1,"method":"initialize","params":{"clientInfo":{"name":"usage_lens","title":"UsageLens","version":"1.1.0"}}}
~~~

成功响应示例。服务端 result 的能力字段可能随 Codex 版本变化；客户端只要求 result 存在，不绑定示例中的 serverInfo 内容：

~~~json
{"id":1,"result":{"serverInfo":{"name":"codex-app-server","version":"example"},"capabilities":{}}}
~~~

初始化完成通知：

~~~json
{"method":"initialized","params":{}}
~~~

### 读取额度

请求不发送 params 字段：

~~~json
{"id":2,"method":"account/rateLimits/read"}
~~~

成功响应示例。UsageLens 首选读取 rateLimitsByLimitId.codex；该字段不存在时回退读取旧字段 rateLimits。primary/secondary 的位置不代表周期，必须由 windowDurationMins 判定：

~~~json
{
  "id":2,
  "result":{
    "rateLimitsByLimitId":{
      "codex":{
        "primary":{
          "usedPercent":4,
          "windowDurationMins":300,
          "resetsAt":1788870540
        },
        "secondary":{
          "usedPercent":39,
          "windowDurationMins":10080,
          "resetsAt":1789396140
        }
      }
    }
  }
}
~~~

| 服务端字段 | UsageLens 字段 | 解析规则 |
| --- | --- | --- |
| usedPercent | FiveHourRemaining / WeeklyRemaining | 100 - Clamp(usedPercent, 0, 100) |
| windowDurationMins = 300 | 5 小时额度 | 接受第一个有效 300 分钟窗口 |
| windowDurationMins = 10080 | 7 天额度 | 接受第一个有效 10080 分钟窗口 |
| resetsAt | FiveHourResetAt / WeeklyResetAt | Unix 秒转 DateTimeOffset，再按本地时区显示 |
| 其他窗口 | 不展示 | 忽略，不影响已识别窗口 |

错误响应示例。UI 只显示“未连接 Codex”或“使用缓存数据”，不得展示 message 原文：

~~~json
{"id":2,"error":{"code":-32000,"message":"rate limit data unavailable"}}
~~~

### 断线与重连

- app-server 意外退出时，使未完成请求失败，协调器保留上次成功快照并标记 Stale。
- 下一次定时刷新或立即刷新创建新的 app-server、重新 initialize 后再读取额度。
- 主程序退出时先取消读取和关闭 stdin，最多等待 500 ms；仍未退出才终止本应用创建的 app-server 子进程。

### 一次完整的 JSONL 会话示例

以下内容按实际 stdin/stdout 顺序排列；每行是独立 JSON，不包含 Markdown 换行。`initialized` 是服务端通知，不带 `id`，客户端不能等待它的响应：

~~~text
// stdin
{"id":1,"method":"initialize","params":{"clientInfo":{"name":"usage_lens","title":"UsageLens","version":"1.1.0"}}}
// stdout
{"id":1,"result":{"serverInfo":{"name":"codex-app-server","version":"example"},"capabilities":{}}}
// stdin
{"method":"initialized","params":{}}
// stdin
{"id":2,"method":"account/rateLimits/read"}
// stdout
{"id":2,"result":{"rateLimitsByLimitId":{"codex":{"primary":{"usedPercent":4,"windowDurationMins":300,"resetsAt":1788870540},"secondary":{"usedPercent":39,"windowDurationMins":10080,"resetsAt":1789396140}}}}}
~~~

UsageLens 不通过 App Server 请求 Token 汇总。Token 页的输入/输出/总量与估价来自本机 `.codex\sessions` 中的 `event_msg → token_count → total_token_usage` 元数据；这条边界必须在实现和排障时保持清晰，不能把额度响应误当作 Token 统计来源。

## settings.json 完整示例

位置：%LOCALAPPDATA%\UsageLens\settings.json。

~~~json
{
  "schemaVersion": 1,
  "floatingStyle": "Glass",
  "floatingLeft": 784.0,
  "floatingOpacity": 0.70,
  "autoCollapseEnabled": true,
  "collapseDelaySeconds": 1,
  "expandOnHandleHover": true,
  "quotaRefreshMinutes": 1,
  "tokenRefreshMinutes": 5,
  "startWithWindows": false,
  "hideControlCenterOnClose": true,
  "trayPrimaryAction": "OpenControlCenter"
}
~~~

| 字段 | 类型 | 合法值 | 缺失或非法时 |
| --- | --- | --- | --- |
| schemaVersion | integer | 1 | 使用默认设置并在下一次成功保存写入 1 |
| floatingStyle | string | Instrument / Glass / Timeline / Terminal | Glass |
| floatingLeft | number 或 null | 有限数值；启动时夹紧到主屏工作区 | null，顶部居中 |
| floatingOpacity | number | 0.35–1.00 | 0.70 |
| autoCollapseEnabled | boolean | true / false | true |
| collapseDelaySeconds | integer | 1 / 2 / 3 | 1 |
| expandOnHandleHover | boolean | true / false | true |
| quotaRefreshMinutes | integer | 1 / 3 / 5 | 1 |
| tokenRefreshMinutes | integer | 5 / 10 / 15 | 5 |
| startWithWindows | boolean | true / false | false |
| hideControlCenterOnClose | boolean | true / false | true |
| trayPrimaryAction | string | OpenControlCenter / ToggleFloatingWindow | OpenControlCenter |

存储规则：

1. 先写同目录 settings.json.tmp，再原子替换 settings.json。
2. JSON 损坏或 schemaVersion 不等于 1 时保留原文件，运行时使用默认值；下一次用户成功保存才覆写。
3. 从旧 appearance.json 迁移时只带入 Style 与 Left，旧文件保留供旧版本回滚。
4. settings.json 不得保存额度、Token、会话、账户、路径、用户名、凭据或原始错误。

## 用户操作验收表

| 编号 | 场景 | 操作 | 预期结果 | 验证类型 |
| --- | --- | --- | --- | --- |
| AC-01 | 首次启动 | 双击 UsageLens.exe | 主屏顶部显示浮窗；通知区域出现一个图标；控制中心未自动弹出 | 手测 |
| AC-02 | 单实例 | 再次启动 UsageLens.exe | 不出现第二个进程、浮窗或托盘图标 | 手测 |
| AC-03 | 控制中心 | 托盘左键单击 | 显示或激活一个 920×640 控制中心，默认总览页 | 手测 |
| AC-04 | 重复打开 | 连续单击托盘 3 次 | 始终是同一个控制中心实例 | 手测 |
| AC-05 | 浮窗切换 | 托盘双击两次 | 第一次隐藏浮窗，第二次恢复同一位置和样式 | 手测 |
| AC-06 | 右键菜单 | 右击托盘图标 | 菜单顺序、文案和四种样式子菜单符合本规范 | 手测 |
| AC-07 | 数据共享 | 控制中心点立即刷新 | 浮窗与控制中心在同一次 SnapshotChanged 后一致更新 | 集成测试 + 手测 |
| AC-08 | 额度成功 | App Server 返回 300/10080 分钟窗口 | 正确显示 5 小时/7 天剩余、进度、重置时间 | 单元测试 |
| AC-09 | 额度失败 | 先成功再断开 Codex | 保留数值，显示“使用缓存数据”，不清空 Token | 单元测试 + 手测 |
| AC-10 | 初次离线 | 未安装或未登录 Codex | 额度显示 -- 与“未连接 Codex”；应用仍能打开设置与读取 Token | 手测 |
| AC-11 | 无会话 | .codex\sessions 不存在 | Token 显示 0，不显示 $-- 错误状态，来源页说明未找到记录 | 单元测试 |
| AC-12 | Token 价格 | 存在已定价和未定价模型 | 估价只累积已定价模型；未定价 Token 单独标注 | 现有 Token 测试 |
| AC-13 | 格式化 | 总 Token 为 123456789 | 显示 1.23亿，tooltip 为精确整数 | 展示层测试 |
| AC-14 | 浮窗位置 | 拖动浮窗 | 只能左右移动，Top 始终等于主屏工作区顶部 | 手测 |
| AC-15 | 自动折叠 | 鼠标离开 1 秒 | 只保留 80×8 顶部把手；悬停按设置展开 | 手测 |
| AC-16 | 样式设置 | 设置页选择 Terminal | 浮窗即时切换并重启后保持 Terminal | 设置测试 + 手测 |
| AC-17 | 透明度设置 | 改为 85% | 浮窗即时为 0.85，重启后保持 | 设置测试 + 手测 |
| AC-18 | 开机启动 | 开启后检查 HKCU Run | 只创建 UsageLens 值，值为带引号当前 exe 路径 | 单元测试 + 手测 |
| AC-19 | 关闭中心 | 点击控制中心关闭按钮 | 默认隐藏中心，浮窗和托盘仍运行 | 手测 |
| AC-20 | 显式退出 | 托盘菜单“退出 UsageLens” | 托盘、中心、浮窗和 app-server 都释放，UsageLens.exe 不残留 | 手测 |
| AC-21 | 隐私 | 查看所有页面和 diagnostics | 不出现邮箱、套餐、会话标题、路径、原始错误或凭据 | 手测 |
| AC-22 | 重置偏好 | 点击重置界面偏好并确认 | 仅删除 UsageLens settings 与启动项；.codex\sessions 不变 | 设置测试 + 手测 |
| AC-23 | 发布包 | 解压 ZIP，在无 .NET Runtime 环境运行 | 应用能启动；只要求本机安装并登录 Codex 才能读取额度 | 发布手测 |
| AC-24 | 可访问性 | Tab、Shift+Tab 和读屏工具浏览设置 | 所有交互控件可聚焦且有 AutomationProperties.Name | 手测 |

## 常见故障排查

| 现象 | 判定依据 | 处理方式 | 数据影响 |
| --- | --- | --- | --- |
| 双击 exe 后没有浮窗 | 任务管理器无 UsageLens.exe | 检查 Windows Defender 或 SmartScreen 是否阻止启动；确认 ZIP 完整解压；从 release-floating-auto 运行一次 | 不写配置，不影响 Codex |
| 进程存在但看不到浮窗 | 浮窗被隐藏或 left 超出工作区 | 托盘右键选择“显示悬浮窗”；设置页点击“重置顶部横向位置” | 仅重置 UsageLens 位置 |
| 托盘图标看不到 | 进程存在但通知区域折叠 | 展开通知区域隐藏图标；在 Windows 任务栏设置启用 UsageLens | 不影响运行 |
| 控制中心关闭后“消失” | 托盘图标和浮窗仍在 | 默认行为为隐藏；单击托盘重新打开。完整退出只能用托盘菜单 | 不丢数据 |
| 额度显示 -- / 未连接 Codex | 连接状态 Offline | 确认已安装并登录 Codex；终端运行 codex --version；点击立即刷新 | 只影响额度，Token 不受影响 |
| 额度长期缓存 | 状态 Stale，更新时间不前进 | 检查网络和 Codex；重开 UsageLens；仍失败时复制脱敏诊断并附到 Issue | 保留最后成功额度 |
| 只显示 7 天额度 | 返回没有 300 分钟窗口 | 这是 Codex 账户或版本字段差异；5 小时显示 --，程序不可猜测数值 | 不影响 7 天和 Token |
| Token 为 0 | 来源页显示“未找到本地会话记录” | 使用 Codex 后等待最多一个 Token 刷新周期；确认当前 Windows 用户的 sessions 目录存在 | 不创建或删除会话 |
| Token 低于预期 | 会话已删除、使用了其他电脑或仅云端存在 | 本工具只统计当前电脑保留的本地元数据，刷新无法补齐 | 不影响 Codex 额度或账单 |
| 价格为 $-- | 存在未定价模型 | 仍显示 Token 总量；复制脱敏诊断并报告模型名，后续可增加定价映射 | 不把未定价误算为 $0 |
| 开机启动未保存 | 页面显示写入失败 | 检查当前用户 Registry 权限；确认 exe 未移动或删除；重新打开开关 | 不修改其他软件启动项 |
| 移动便携目录后未自启 | Run 项指向旧路径 | 在新目录运行后关闭再开启“开机自动启动” | 只更新 UsageLens Run 值 |
| 透明度或样式重启丢失 | settings.json 损坏或不可写 | 设置页重新保存任一项；检查 %LOCALAPPDATA%\UsageLens 可写 | 只影响界面偏好 |
| 右键刷新无变化 | 按钮短暂禁用或数据未变化 | 等待最多 30 秒；检查状态胶囊与更新时间；复制脱敏诊断 | 不阻塞浮窗交互 |
| 无法退出 | 托盘菜单无响应或子进程残留 | 任务管理器结束 UsageLens.exe；下次启动会创建新的受控 app-server | 不修改 Codex 安装与会话 |
| 发布包被安全软件提示 | 未签名开源发布包 | 校验 GitHub Release SHA256 后再决定是否允许；不要关闭系统全局安全策略 | 不影响本地数据 |

## 产品规范自检

- [x] 每页均定义尺寸、间距、字体、颜色、Loading、Offline、Stale 状态和数据边界。
- [x] 原型中的邮箱、订阅、会话浏览和趋势图已收敛为可安全实现的功能。
- [x] 状态流转和托盘时序涵盖启动、折叠、隐藏、重开、刷新与退出。
- [x] JSON-RPC、settings.json、验收表和排障使用当前代码或前文计划已定义的字段与职责。
- [x] 未把构建成功等同于 Windows Shell、浮窗拖动或托盘交互已经验证。
