# CodexQuotaFloat README 与项目定位设计

## 目标

将 CodexQuotaFloat 从“功能说明文档”提升为具有清晰产品定位、下载入口、视觉预览、可信边界和双语支持的成熟开源项目主页。默认 README 使用简体中文，英文版作为并列入口；不修改软件功能和发布包行为。

## 目标用户

主要面向使用 Windows 版 Codex 的中文用户，同时通过完整英文 README 服务国际用户。读者分为两类：希望直接下载使用的普通用户，以及希望检查实现、构建源码或参与贡献的开发者。

## 项目定位

项目采用克制的产品型表达，先说明用户价值，再介绍实现细节。

中文定位：

> 一款轻量的 Windows Codex 状态悬浮窗，用于查看额度、重置时间、本地 Token 用量与预估成本。

英文定位：

> A lightweight Windows desktop widget for monitoring Codex rate limits, reset times, local token usage, and estimated costs.

GitHub Description 使用英文，以获得更清晰的跨语言搜索结果：

> Lightweight Windows widget for Codex quotas, reset times, local token usage, and estimated costs.

## README 语言结构

- `README.md`：默认简体中文。
- `README_EN.md`：完整英文版，不是摘要版。
- 两个 README 顶部提供 `简体中文 | English` 互链。
- 两个版本的章节、功能事实、下载入口、限制和构建命令保持一致。
- 产品名、代码标识符、路径、命令、状态文本和文件名不翻译。

## 首屏结构

README 首屏居中展示：

1. 项目名 `CodexQuotaFloat`。
2. 中文主标语：`轻量、常驻顶部的 Codex 额度与 Token 用量悬浮窗`。
3. 英文副标语：`A lightweight, always-on-top Windows widget for Codex usage monitoring.`
4. 语言切换。
5. Release、Windows、.NET、MIT License、Downloads 徽章。
6. `下载最新版`、`使用说明`、`English` 三个文本入口。

首屏补充两行价值说明：

> 实时查看 Codex 额度、重置倒计时、本地 Token 用量和预估成本。  
> 无需额外登录，无需 API Key，数据留在本机。

英文版使用等义表达，不采用逐字直译。

## 视觉预览

首屏之后设置“界面预览 / Preview”章节，展示一张四套 UI 的统一组合图。图片要求：

- 使用当前 `v1.3.0` 实际界面，不制作与软件不一致的概念图。
- 同时包含 Instrument、Glass、Timeline、Terminal 四种样式。
- 四张窗口保持相同缩放比例、背景和间距。
- 不出现浏览器、终端、个人路径、账号信息或其他窗口内容。
- 额度、时间、Token 和价格可使用演示数据，但不得包含真实用户数据。
- 文件保存为 `assets/screenshots/ui-overview.png`，README 使用相对路径引用。
- 如果当前环境无法可靠生成真实截图，先建立截图位置与说明，但不得放置伪造图片或失效链接；在最终交付中明确截图仍待人工采集。

## 信息架构

中文和英文 README 使用相同顺序：

1. 产品首屏。
2. 界面预览。
3. 为什么使用 CodexQuotaFloat。
4. 功能概览。
5. 下载与安装。
6. 使用方法。
7. 数据来源与隐私。
8. Token 价格估算。
9. 常见问题。
10. 从源码构建与测试。
11. 路线图。
12. 参与贡献、安全报告和许可证。

## 核心价值表达

“为什么使用”章节只保留三个价值分组：

### 实时掌握额度

展示 5 小时与 7 天额度、重置倒计时、具体重置时间和颜色状态。

### 了解本地用量

汇总当日、近 7 天、近 30 天 Token，并按本地会话模型估算成本。

### 安静地常驻顶部

固定在主屏幕顶部、只允许水平拖动、离开后自动折叠、提供四套 UI，并以免安装便携包分发。

详细功能继续放在后续功能概览中，避免首屏堆叠长列表。

## 下载与使用

- 主下载入口直接指向 `https://github.com/miaotingxu/CodexQuotaFloat/releases/latest`。
- 明确推荐资产名为 `CodexQuotaFloat-v{版本号}-win-x64-portable.zip`。
- 用三步描述：下载、完整解压、运行 EXE。
- 明确便携包不要求安装 .NET Runtime。
- 明确仍需本机安装并登录 Codex。
- 保留 SHA256 校验方法，但把具体版本示例替换为不易过时的变量写法，或同时注明以 Release 实际文件名为准。

## 数据、隐私与可信边界

README 必须明确：

- 额度通过本机 `codex app-server` 和现有登录状态读取。
- 不读取或保存 `auth.json`、API Key、密码和对话正文。
- Token 只聚合当前电脑 `%USERPROFILE%\.codex\sessions` 中的本地会话元数据。
- Token 统计不包含其他电脑、已删除记录或未落盘会话。
- 价格是根据项目内置规则计算的估算值，不等同于真实账单、套餐价值或额度扣减金额。
- 未识别模型对应的 Token 不计入估价，并通过 `*` 标识。

## 路线图

路线图保持短小，只列方向，不承诺日期：

- 改善多显示器体验。
- 提供可配置的刷新与折叠行为。
- 扩展可识别模型和价格规则。
- 改善发布包签名和更新体验。

系统托盘、开机启动和提醒功能只有在确定进入开发后才加入路线图，避免把想法写成承诺。

## GitHub 仓库展示信息

更新 GitHub Description 为：

> Lightweight Windows widget for Codex quotas, reset times, local token usage, and estimated costs.

设置 Topics：

```text
codex
openai
windows
wpf
desktop-widget
token-usage
quota-monitor
dotnet
```

Homepage 暂时留空，避免将 Release 页面重复设置为项目主页。

## 文案规则

- 使用“额度”描述 Codex rate limits，使用“Token 用量”描述本地统计。
- 首屏不出现协议细节、文件路径和扫描算法。
- 不使用“强大”“极致”“全能”等缺少证据的营销词。
- 不暗示项目是 OpenAI 官方产品。
- 不承诺统计值或价格与服务端账单完全一致。
- 命令必须可以直接复制执行，下载链接必须指向真实页面。
- 中文标点、英文大小写、`Token`、`CodexQuotaFloat`、`.NET` 的写法在全文统一。

## 验收标准

- `README.md` 与 `README_EN.md` 内容结构一致，互相可达。
- GitHub 首屏在不滚动时能看见项目定位、语言切换、徽章和下载入口。
- README 不再以长功能列表直接开场。
- GitHub Description 和 Topics 与设计一致。
- 所有相对文档链接和外部下载链接有效。
- README 描述与 `v1.3.0` 当前实现一致，不出现旧的下拉切换、自由垂直拖动或额外登录描述。
- 未获得真实截图时不提交伪造或失效截图。
- 文档修改通过 `git diff --check`，工作区不包含生成缓存或用户数据。
