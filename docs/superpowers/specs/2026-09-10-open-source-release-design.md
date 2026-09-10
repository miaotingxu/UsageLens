# CodexQuotaFloat 开源发布设计

## 目标

将当前 Windows 悬浮窗整理成用户可直接理解、下载、校验和运行的开源项目，同时建立可重复的 GitHub Release 流程。源码采用 MIT License；本次不增加安装器、自动更新或代码签名。

## 参考项目结论

- CC Switch 将 README 作为产品首页，优先呈现定位、截图、功能、快速开始、系统要求和按平台区分的下载方式；Release 同时提供安装版与 Portable ZIP。
- Cockpit Tools 使用更详细的功能说明、隐私说明和故障排查，并在 Release 中提供按平台命名的安装包、签名与 SHA256 校验文件。
- CodexQuotaFloat 是 Windows 单用途小工具，应继承清晰下载入口、隐私边界、故障排查、版本化压缩包和校验文件，不照搬多平台安装器体系。

## README 信息架构

README 使用简体中文，代码标识符和命令保持原文。页面顺序为：

1. 项目名、单句定位、Release/Windows/MIT 徽章。
2. 最新版下载入口，并明确这是免安装便携版。
3. 核心能力：额度、Token、价格估算、四套 UI、顶部折叠和水平拖动。
4. 系统要求和三步快速开始。
5. 界面与交互说明。
6. 数据来源、刷新频率、隐私边界和估算价格限制。
7. 常见问题与读取失败处理。
8. 源码构建、测试和手工打包命令。
9. 贡献、安全报告和 MIT License 链接。

README 必须与当前实现一致：窗口固定主屏幕顶部且只能水平拖动；左右箭头循环切换四套 UI；折叠后保留 80×8 把手；额度每分钟刷新，Token 每五分钟刷新；便携包不要求 .NET Runtime，但要求本机已安装并登录 Codex。

## 社区文件

- `LICENSE`：MIT License，版权人为 `miaotingxu`，年份 2026。
- `CHANGELOG.md`：采用 Keep a Changelog 风格，记录 `1.3.0` 的当前完整能力。
- `CONTRIBUTING.md`：说明分支、构建、测试、提交和 Pull Request 最低要求。
- `SECURITY.md`：要求通过 GitHub Security Advisory 私下报告漏洞，禁止在公开 Issue 中提交凭据或本地会话内容。

## 软件包

本地始终覆盖 `release-floating-auto`，不创建版本目录。目录最终只保留：

```text
release-floating-auto/
├── CodexQuotaFloat.exe
├── README.txt
└── LICENSE
```

用于 GitHub Release 的资产采用版本化名称：

```text
CodexQuotaFloat-v1.3.0-win-x64-portable.zip
SHA256SUMS.txt
```

ZIP 内不包含 PDB、源码、缓存或用户数据。`README.txt` 只包含运行前提、启动方法、数据说明、退出方式和项目地址。SHA256 使用小写十六进制，并以两个空格分隔文件名。

## 自动发布

新增 GitHub Actions 工作流：

- 仅在推送匹配 `v*` 的标签时发布。
- 在 `windows-latest` 上安装 .NET 10 SDK。
- 依次执行两个现有测试项目。
- 以 `win-x64`、self-contained、single-file、无调试符号方式发布。
- 生成 ZIP 与 `SHA256SUMS.txt`。
- 使用 GitHub CLI 创建对应 GitHub Release 并上传两个资产。
- 工作流只依赖 GitHub 自带 `GITHUB_TOKEN`，权限限制为 `contents: write`。

## 版本与边界

- 首个公开 Release 版本为 `v1.3.0`，与已有 `release/v1.0.0` 至 `release/v1.2.0` 分支递进关系一致。
- 本次只准备发布能力，不自动推送标签或创建线上 Release。
- 不把生成的二进制、ZIP 或校验文件提交到 Git；本地成品继续由 `.gitignore` 排除。
- 不宣称 Token 估算等同真实账单，也不宣称读取范围覆盖其他电脑或已删除会话。

## 验证

- 两个测试项目必须通过。
- Release 构建必须成功，且最终目录不包含 PDB。
- ZIP 必须能列出 EXE、README.txt、LICENSE 三个文件。
- 重新计算的 SHA256 必须与 `SHA256SUMS.txt` 一致。
- README 中不得保留旧的下拉菜单切换或“不支持拖动”描述。
- GitHub Actions YAML 应能通过静态结构检查，并与本地打包命令保持一致。
