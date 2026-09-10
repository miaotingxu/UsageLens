# 参与贡献

感谢你愿意改进 UsageLens。

## 开发环境

- Windows 10 或 Windows 11
- .NET 10 SDK
- 已安装并登录 Codex（验证真实额度读取时需要）

## 工作流程

1. Fork 仓库，并从最新的 `develop` 创建功能或修复分支。
2. 保持改动聚焦，不提交 `bin`、`obj`、`release-floating-auto`、`artifacts`、日志或本地用户数据。
3. 新增代码遵循现有 C# 与 WPF 结构；注释默认使用简体中文。
4. 提交 Pull Request 前运行构建和全部测试。

```powershell
dotnet build .\UsageLens.csproj -c Release
dotnet run --project .\tests\UsageLens.TokenUsageTests\UsageLens.TokenUsageTests.csproj -c Release
dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release
```

## Pull Request

请在说明中包含修改目的、用户可见变化、运行过的验证命令、UI 截图（如适用）和已知限制。

不要在 Issue、提交、日志或截图中包含 API Key、认证文件、完整本地路径、会话正文或其他个人信息。
