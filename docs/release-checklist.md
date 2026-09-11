# Release 验收清单

在创建公开版本前，于未安装 .NET Runtime 的 Windows 10/11 x64 环境执行一次本清单。

## 便携包

- [ ] 下载并完整解压 `UsageLens-v{版本}-win-x64-portable.zip`。
- [ ] 双击 `UsageLens.exe` 后，悬浮窗和通知区域图标均出现。
- [ ] 无需安装 .NET Runtime，进程可持续运行。
- [ ] `SHA256SUMS.txt` 与 ZIP 文件校验值一致。

## 核心功能

- [ ] 本机已登录 Codex 时，额度可以读取；读取失败时界面显示安全的离线/无数据状态。
- [ ] 鼠标离开 1 秒后，窗口折叠到主屏幕顶部；移入把手后重新展开。
- [ ] 窗口只能沿顶部水平拖动；四套界面可通过两侧箭头循环切换。
- [ ] 右键菜单、托盘菜单、刷新和退出均可用。
- [ ] 控制中心可打开；设置修改后即时保存并在重启后保留。

## 视觉与隐私

- [ ] 背景不透明度 70% 时，背景约 30% 透明，文字、数字、按钮保持清晰。
- [ ] 各类错误和诊断信息不包含账号凭据、会话正文或 API Key。
- [ ] README 中的图片、版本号、下载链接和当前 UI 一致。

## 发布前命令

```powershell
dotnet build .\UsageLens.csproj -c Release
.\scripts\package-release.ps1 -Version {版本}
```

仅在以上项目全部通过后创建 Git 标签和 GitHub Release。
