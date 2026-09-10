# UsageLens 全量改名设计

## 目标

将项目品牌从 `CodexQuotaFloat` 全量更新为 `UsageLens`，同步代码、构建产物、文档、GitHub 仓库和发布包名称，同时保证现有额度、Token、UI、拖动、折叠、刷新和单实例行为不变。

## 命名范围

用户可见和构建相关名称统一为：

```text
项目名：UsageLens
项目文件：UsageLens.csproj
程序集：UsageLens
可执行文件：UsageLens.exe
默认命名空间：UsageLens
仓库：https://github.com/miaotingxu/UsageLens
```

同步修改 C# 命名空间、项目文件名、程序集配置、测试项目引用、单实例标识、窗口标题、错误提示、README、英文 README、CHANGELOG、CONTRIBUTING、SECURITY、GitHub Actions、截图说明、Release 包名和下载链接。

旧名称只允许出现在迁移兼容常量、迁移测试和升级说明中，不得出现在新的产品标题、程序主界面、README 主文案或新发布文件名中。

## 用户数据迁移

旧版本路径：

```text
%LOCALAPPDATA%\CodexQuotaFloat
```

新版本路径：

```text
%LOCALAPPDATA%\UsageLens
```

启动时按以下顺序读取：

1. 如果新目录存在，读取新目录。
2. 新目录不存在且旧目录存在时，读取旧目录中的 `appearance.json`。
3. 成功读取后将样式和水平位置写入新目录。
4. 后续只使用新目录；不删除旧目录，避免回滚时丢失数据。
5. 不迁移账号、Token、密码或会话内容，因为程序不保存这些数据。

迁移必须覆盖有效配置、缺失配置、损坏 JSON、无效样式值、越界横坐标和新旧目录同时存在等情况。

## GitHub 仓库迁移

使用 GitHub 仓库重命名将 `miaotingxu/CodexQuotaFloat` 改为 `miaotingxu/UsageLens`。更新本地 `origin` URL 和所有文档链接。旧 GitHub URL 的跳转由 GitHub 保留，但项目不依赖旧地址作为新文档入口。

同步更新 GitHub Description：

> Lightweight Windows widget for monitoring local AI usage, quotas, and estimated costs.

Topics 保留 `windows`、`wpf`、`desktop-widget`、`token-usage`、`quota-monitor`、`dotnet`，移除产品名相关旧主题；是否保留 `codex` 和 `openai` 作为兼容性说明主题需避免造成官方关联误解，README 中使用独立社区项目声明。

## 发布版本

功能行为不变，品牌改名使用 `v1.3.1` 发布：

```text
UsageLens-v1.3.1-win-x64-portable.zip
```

便携目录只包含：

```text
UsageLens.exe
README.txt
LICENSE
```

`v1.3.0` 及其旧下载资产保持不变，作为历史版本保存。自动发布工作流从标签提取版本号，并使用 `UsageLens` 新名称生成资产。

## 兼容性与行为边界

- 不改变 Codex App Server 通信协议。
- 不改变额度读取、Token 聚合、价格估算、刷新周期或错误保留策略。
- 不改变四套 UI 的视觉和切换顺序。
- 不改变顶部固定、水平拖动、自动折叠、箭头切换和右键菜单行为。
- 不保留旧 EXE 别名，避免用户继续获得两个可能同时运行的单实例程序。
- 旧版本用户通过新程序首次启动完成配置迁移；README 明确说明升级行为。

## 验证

- 全项目文本和项目文件中的旧产品名只剩迁移常量、迁移测试或历史变更说明。
- `dotnet build`、Token 测试和展示层测试全部通过。
- Release 构建结果为 `UsageLens.exe`，ZIP 不含 `CodexQuotaFloat` 文件名或 PDB。
- 新目录配置读写和旧目录迁移测试通过。
- 新 EXE 单实例运行、额度读取、Token 统计和退出功能正常。
- GitHub 仓库新地址、Description、Topics、README 和 Release 链接一致。
- `main` 和 `develop` 同步改名提交；旧 `v1.3.0` 标签不重写。
