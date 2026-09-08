# Codex Quota Float 设计说明

## 已确认目标

开发一个 Windows 10/11 上运行的轻量 WPF 悬浮窗。它显示当前本机已登录 Codex 账号的 5 小时与 7 天额度剩余百分比及各自的重置倒计时；应用自身不提供登录、不会读取或保存账号密码、API Key、Token 或 `auth.json`。

窗口启动时展开，位于主显示器顶部中央，宽 300 px、高 140 px、深色半透明、无边框、始终置顶且不显示在任务栏。窗口容器保持透明，深色卡片与收起把手为 70% 不透明度（30% 透明）；这样在卡片向上滑出时，窗口其余区域不会留下可见底色。每项额度以周期名称、重置倒计时和右侧 17 px 百分比两行展示，窗口底部另有简短读取状态。百分比以 81/50/20 的四色阈值着色；倒计时按本地时间每分钟更新。用户首次悬停后离开 1 秒，卡片向上滑出屏幕，仅留顶部中央 80 x 8 px 的悬停区域；鼠标重新进入该横条时立即展开。右键菜单只提供“刷新”和“退出”。

## 已选方案

采用单个 .NET 10 WPF 项目，不引入 MVVM 框架、NuGet UI 库、数据库、后台服务或系统托盘。项目结构保持为小型、职责清晰的文件：

```text
CodexQuotaFloat/
├── App.xaml / App.xaml.cs                 # 单实例、应用生命周期
├── MainWindow.xaml / MainWindow.xaml.cs   # 悬浮窗 UI 与刷新协调
├── Models/QuotaState.cs                   # UI 所需的额度状态
├── Services/CodexAppServerClient.cs        # 长连接 JSON-RPC 客户端
├── Services/QuotaParser.cs                 # 协议响应到额度状态的转换
├── Services/QuotaPresentation.cs            # 倒计时格式化与额度颜色分级
├── Services/FloatingWindowController.cs    # 展开、延迟折叠、滑动动画
└── docs/                                   # 本设计和实施计划
```

## 数据流

```text
WPF 启动
  -> where.exe codex 检测并选择直接可执行的 codex.exe
  -> 直接启动 `codex app-server --listen stdio://`
  -> initialize -> initialized
  -> account/rateLimits/read
  -> QuotaParser 按 windowDurationMins 识别窗口
  -> QuotaState
  -> Dispatcher 更新两行 UI
```

App Server 是单个长连接：启动时读取一次，此后由 `DispatcherTimer` 每 60 秒读取一次。标准输出只作为 JSONL / JSON-RPC 通信通道，不经 `cmd.exe` 或 PowerShell 包装；所有 I/O 都在后台任务中执行，UI 更新回到 WPF Dispatcher。

本机已针对当前 Codex CLI 0.153.4 验证了上述握手与 `account/rateLimits/read`：接口可用，返回多额度桶，当前两个窗口的时长为 300 与 10080 分钟。

## 额度解析规则

优先读取 `result.rateLimitsByLimitId["codex"]`；该字段不可用时使用向后兼容的 `result.rateLimits`。只从 `primary` 与 `secondary` 窗口读取 `windowDurationMins`，绝不根据字段名推断用途：

- `300` 分钟：5 小时额度。
- `10080` 分钟：7 天额度。
- 剩余百分比：将 `usedPercent` 限制在 0 到 100 后计算 `100 - usedPercent`。

解析器保存 reset Unix 时间和最后更新时间。界面基于 reset 时间在本地每分钟重绘倒计时，不发起额外 RPC；若某个窗口或其重置时间不存在，则相应位置显示 `--`，不是错误。

## 状态与异常处理

`QuotaState` 包含 5 小时/周的可选剩余百分比、可选重置时间、最后成功更新时间和状态（Loading、Ready、Stale、Offline）。首次加载失败、未检测到 Codex 或 App Server 无法初始化时，两行显示 `--`。已有成功数据后发生网络或单次 RPC 失败时保留旧数值并标记为 Stale，不清空界面。

App Server 退出后，客户端进入 Offline，并按 3 秒、10 秒、30 秒、随后每 30 秒的间隔重连。关闭应用时停止定时器、取消后台读取、关闭 stdin，并终止由本应用启动的 App Server，避免留下孤儿进程。

## 交互状态机

状态只有 `Expanded`、`PendingCollapse`、`Collapsed`：

- `Expanded`：`Top` 为启动时的顶部位置，鼠标在窗口内时取消折叠定时器。
- `PendingCollapse`：鼠标离开后启动一个 1 秒的一次性定时器。
- `Collapsed`：窗口以约 180 ms 动画向上移动一个卡片高度。透明容器不绘制背景，因此只留下 80 px 宽、8 px 高的悬停横条。

鼠标在倒计时期间回来时取消折叠；折叠时进入保留的小横条立即展开。

## 明确排除

不实现登录页、多账号、设置页、系统托盘、开机启动、拖动或多显示器、主题设置、历史数据、图表、额度提醒、数据库、云同步、后端服务或自动更新。

## 验收

构建出的程序必须能：显示无边框半透明顶部悬浮窗；成功读取时显示两项额度、各自的重置倒计时和正确的四色百分比；缺失窗口或读取失败时安全显示 `--`；每分钟刷新及本地重绘倒计时；离开 1 秒折叠、悬停展开；右键手动刷新与退出；重复启动不产生第二个实例；退出后不遗留 App Server。
