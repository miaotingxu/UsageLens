# CodexQuotaFloat

[![Release](https://img.shields.io/github/v/release/miaotingxu/CodexQuotaFloat?display_name=tag&sort=semver)](https://github.com/miaotingxu/CodexQuotaFloat/releases/latest)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4)
[![License](https://img.shields.io/github/license/miaotingxu/CodexQuotaFloat)](LICENSE)

一个贴在 Windows 主屏幕顶部的 Codex 额度与本地 Token 用量悬浮窗。无需在本软件中登录，也无需填写 API Key。

## 下载

前往 [Releases](https://github.com/miaotingxu/CodexQuotaFloat/releases/latest) 下载 `CodexQuotaFloat-v{版本号}-win-x64-portable.zip`。

这是免安装便携版，解压后运行 `CodexQuotaFloat.exe` 即可，目标电脑不需要另外安装 .NET Runtime。

> 使用前仍需安装并登录 Codex。CodexQuotaFloat 会复用本机 Codex 的现有登录状态读取额度。

## 功能

- 显示 5 小时和 7 天额度的剩余百分比、重置倒计时与具体重置时间。
- 按剩余额度显示绿色、黄色、琥珀色和红色，并提供对应进度条或仪表弧线。
- 汇总当前电脑本地保存的当日、近 7 天和近 30 天 Token 用量。
- 按会话实际模型估算 Token 价格，并标出无法识别价格的 Token。
- 内置精密仪表、双层玻璃、横向时间轴和极简终端四套界面。
- 固定在主屏幕顶部，只允许左右拖动；窗口位置和界面样式会自动保存。
- 鼠标离开 1 秒后自动向顶部折叠，只保留 80×8 像素把手。
- 始终置顶、单实例运行，不在任务栏显示。
- 不读取或保存 `auth.json`、API Key、密码或对话正文。

## 系统要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows 10 或 Windows 11，x64 |
| Codex | 已安装且已登录 |
| 网络 | Codex 获取最新额度时需要可用网络 |
| .NET Runtime | 使用便携版时不需要 |

## 快速开始

1. 从 [最新 Release](https://github.com/miaotingxu/CodexQuotaFloat/releases/latest) 下载 Portable ZIP。
2. 解压全部文件，不要直接在压缩包内运行。
3. 双击 `CodexQuotaFloat.exe`。

窗口启动后会贴在主屏幕顶部。首次读取期间显示加载状态，数据成功返回后显示额度、倒计时和 Token 统计。

## 操作方式

| 操作 | 效果 |
| --- | --- |
| 在窗口上按住左键并水平移动 | 沿主屏幕顶部左右拖动 |
| 点击左侧或右侧箭头 | 在四套 UI 之间循环切换 |
| 鼠标离开窗口 1 秒 | 向屏幕顶部自动折叠 |
| 鼠标移入顶部把手 | 立即展开 |
| 右键 → 刷新 | 同时刷新额度与 Token 用量 |
| 右键 → 退出 | 关闭程序 |

所选 UI 和水平位置保存在 `%LOCALAPPDATA%\CodexQuotaFloat\appearance.json`。该文件只保存样式名称和窗口横坐标，不包含账号、额度或会话数据。无效或超出屏幕的位置会自动回到可见区域。

## 数据与刷新

### 额度

程序通过本机 `codex app-server` 复用 Codex 登录状态读取额度，启动时读取一次，之后每 60 秒刷新。倒计时在本地更新，不会因此额外请求服务。

颜色规则：

- 81–100%：绿色
- 50–80%：黄色
- 20–49%：琥珀色
- 0–19%：红色
- 无数据：中性灰

### Token 用量

程序只扫描当前电脑 `%USERPROFILE%\.codex\sessions` 最近 30 个本地自然日的会话元数据，统计当日、近 7 天和近 30 天的输入与输出 Token。它不会读取、保存或上传对话正文。

- Token 每 5 分钟增量刷新。
- 显示值采用中文数量级，1 万以上使用“万”，1 亿以上使用“亿”。
- 总量为输入与输出之和；悬停可查看未经缩写的输入、输出和总数。
- 输入量包含 cached input，但不会再次重复累计缓存输入。

统计不包含其他电脑、已删除记录或未保存在本机的会话，因此不等同于账号的服务端完整历史。

### 价格估算

价格按本地会话记录中的模型和输入/输出 Token 估算。当前规则为每 100 万 Token：

| 模型 | 输入 | 输出 |
| --- | ---: | ---: |
| GPT-5.6 Sol | $4.00 | $20.00 |
| GPT-5.6 Terra | $2.00 | $12.00 |
| GPT-5.6 Luna | $0.20 | $1.20 |
| GPT-6 Astra | $8.00 | $40.00 |
| codex-auto-review | $2.00 | $12.00 |

未覆盖的模型不会计入估价；界面价格后的 `*` 表示存在未计价 Token。该价格仅供参考，不代表 OpenAI 或任何服务商的实际账单、套餐价值或额度扣减金额。

## 常见问题

### 双击后看不到窗口

- 检查屏幕最上方是否只保留了折叠把手，将鼠标移到把手上即可展开。
- 在任务管理器结束已有的 `CodexQuotaFloat.exe` 后重新启动。程序只允许一个实例。
- 删除 `%LOCALAPPDATA%\CodexQuotaFloat\appearance.json` 可重置界面样式和横向位置。

### 额度显示 `--`

确认 Codex 已安装、已经登录且网络可用。首次读取失败时会显示提示；成功读取后发生临时失败，程序会保留最近一次成功结果并尝试重新连接。

### Token 显示为 0

这表示本机最近 30 个自然日内没有可统计的 Codex 会话记录，并不表示读取失败。Token 统计只覆盖当前电脑仍保留的 `%USERPROFILE%\.codex\sessions`。

### 如何验证下载文件

Release 同时提供 `SHA256SUMS.txt`。在 ZIP 所在目录运行：

```powershell
Get-FileHash .\CodexQuotaFloat-v1.3.0-win-x64-portable.zip -Algorithm SHA256
```

将输出哈希与 `SHA256SUMS.txt` 比较，两者应完全一致。

## 从源码构建

需要安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。

```powershell
git clone https://github.com/miaotingxu/CodexQuotaFloat.git
cd CodexQuotaFloat
dotnet build .\CodexQuotaFloat.csproj -c Release
dotnet run --project .\CodexQuotaFloat.csproj
```

运行测试：

```powershell
dotnet run --project .\tests\CodexQuotaFloat.TokenUsageTests\CodexQuotaFloat.TokenUsageTests.csproj -c Release
dotnet run --project .\tests\CodexQuotaFloat.PresentationTests\CodexQuotaFloat.PresentationTests.csproj -c Release
```

生成与 Release 相同的便携包：

```powershell
.\scripts\package-release.ps1 -Version 1.3.0
```

本地可运行目录会覆盖写入 `release-floating-auto`；版本化 ZIP 和校验文件生成在 `artifacts`。这些构建产物不会提交到 Git。

## 参与贡献

提交代码前请阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。安全问题请按照 [SECURITY.md](SECURITY.md) 私下报告，不要在公开 Issue 中提交凭据或本地会话内容。

## License

本项目采用 [MIT License](LICENSE)。
