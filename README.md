# Codex Quota Float

一个仅用于 Windows 10/11 的轻量 Codex 额度悬浮窗。它显示当前本机已登录 Codex 账号的 5 小时和 7 天额度剩余百分比，以及各自的重置倒计时。

## 特性

- 顶部居中的、始终置顶的无边框半透明悬浮窗；深色卡片与收起把手为 70% 不透明度（30% 透明）。每项额度以名称、倒计时和百分比两行展示，并显示连接/刷新状态。
- 首次启动保持展开；在窗口上悬停后离开 1 秒，半透明卡片会向上滑出屏幕，只留下顶部中央 80×8 像素的小横条，悬停后立即展开。
- 启动时读取一次，此后每 60 秒刷新一次。
- 倒计时每分钟按本地时间更新，并以英数字格式显示倒计时和具体重置时间点（例如 `2h 18m · 09/12 18:30`），不会额外调用 Codex 或网络。
- 每项额度下方显示一条约 200×2 像素的细进度轨道，填充长度对应剩余百分比并沿用四色规则；无数据时只显示灰色轨道。
- 剩余比例以四级颜色区分：81–100% 绿色、50–80% 黄色、20–49% 琥珀色、0–19% 红色；缺失数据为中性灰。
- 直接使用本机 `codex app-server` 的现有登录状态；不提供登录页面，不读取或保存 `auth.json`、Token、API Key 或密码。
- 右键菜单只有“刷新”和“退出”。

## 前提条件

1. Windows 10 或 Windows 11。
2. 已安装并登录 Codex。
3. 构建项目时安装 .NET 10 SDK。

## 构建与运行

在本目录执行：

```powershell
dotnet build .\CodexQuotaFloat.csproj -c Release
dotnet run --project .\CodexQuotaFloat.csproj
```

发布为可分发目录：

```powershell
dotnet publish .\CodexQuotaFloat.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o .\release-floating-auto
```

发布结果为单个便携式 `CodexQuotaFloat.exe`，目标电脑无需安装 .NET Runtime；但仍需安装并登录 Codex，以便读取本机额度。

## Token 用量

悬浮窗右下角的两个页码点可在额度页和 Token 页之间切换。Token 页显示当日、近 7 天、近 30 天的总 Token；悬停可查看输入与输出明细，输入统计包含 cached input。数值统一使用中文数量级，1 万以上显示“万”，达到 1 亿显示“亿”。价格按每次请求实际使用的模型估算：GPT-5.6 Sol 输入 $4、输出 $20；Terra 输入 $2、输出 $12；Luna 输入 $0.20、输出 $1.20；GPT-6 Astra 输入 $8、输出 $40；codex-auto-review 与 Terra 同价（均为每100万 Token）。给定价表未覆盖的模型不计入价格，界面价格后的 `*` 表示存在未计价 Token；仅供参考，不代表实际账单。

Token 数据只从当前电脑的 `%USERPROFILE%\.codex\sessions` 本地会话元数据读取，不上传对话内容；不包含其他电脑、已删除记录或没有保存在本机的会话。额度每分钟刷新，Token 用量每 5 分钟增量刷新，右键“刷新”会同时更新两者。

## 使用方式

- 启动后窗口会以无边框半透明样式显示在顶部中央，不出现在任务栏；先显示 `...`，成功读取后显示两个百分比、各自的重置倒计时和本地重置时间点。
- 在窗口上悬停后离开 1 秒，卡片会向上滑出屏幕，仅留下顶部中央的 80×8 像素小横条。
- 将鼠标移到保留的小横条会展开窗口。
- 在窗口上右键可手动刷新或退出。
- 再次启动程序不会创建第二个实例。

## 读取失败时

首次读取失败时两项均显示 `--`，窗口底部会给出“未检测到 Codex”或“请确认已登录”等状态提示；成功读取后临时失败时，保留最近一次成功值，并继续显示非负倒计时。常见原因是 Codex 未安装、未登录、网络不可用，或 Codex App Server 暂时不可用。该工具不会代替 Codex 登录，也不离线缓存额度。

## MVP 范围

本版本刻意不包含系统托盘、设置页、开机启动、多账号、多显示器、窗口拖动、历史记录、图表、提醒、云同步或数据库。
