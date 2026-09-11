# Changelog

本项目的重要变更记录在此文件中。格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [Unreleased]

## [1.1.0] - 2026-09-11

### Added

- 新增通知区域托盘图标和单实例控制中心。
- 新增总览、额度窗口、Token 用量、会话来源和设置页面。
- 新增可配置的透明度、折叠行为、刷新周期、托盘单击动作和开机启动。
- 新增共享刷新协调器，额度与 Token 读取互不阻塞，并在失败时保留最近成功结果。
- 新增设置文件校验、旧版外观配置迁移、设置与协调器自动化测试。

### Changed

- 设置统一保存到 `%LOCALAPPDATA%\\UsageLens\\settings.json`。
- 发布版本统一为 `1.1.0`，便携包继续使用单文件、自包含发布方式。

### Privacy

- 控制中心只显示 Codex 可用状态，不读取或保存账号凭据、邮箱或订阅信息。

## [1.0.1] - 2026-09-10

### Changed

- Renamed the application, executable, assembly, namespace, repository and local settings directory to `UsageLens`.
- Added one-time migration of style and horizontal position from the legacy `CodexQuotaFloat` settings directory.
- Updated the portable package name and documentation to use `UsageLens`.

## [1.3.0] - 2026-09-10

### Added

- 5 小时与 7 天 Codex 剩余额度、重置倒计时和本地重置时间。
- 当日、近 7 天和近 30 天本地 Token 用量统计与分模型价格估算。
- 精密仪表、双层玻璃、横向时间轴和极简终端四套可循环切换 UI。
- 固定主屏幕顶部的水平拖动、位置保存与越界恢复。
- 离开 1 秒后自动折叠到顶部，并保留 80×8 像素展开把手。
- 单实例、手动刷新、自动重连与读取失败时保留最近结果。
- Windows x64 自包含单文件便携包和 SHA256 校验文件。

[Unreleased]: https://github.com/miaotingxu/UsageLens/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/miaotingxu/UsageLens/releases/tag/v1.1.0
[1.0.1]: https://github.com/miaotingxu/UsageLens/releases/tag/v1.0.1
[1.3.0]: https://github.com/miaotingxu/UsageLens/releases/tag/v1.3.0
