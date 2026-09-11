# Settings Page Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将控制中心设置页升级为深色精装视觉，且不改变任何现有设置行为。

**Architecture:** 仅在 `SettingsPage.xaml` 增加页面本地控件模板与布局；现有 `SettingsViewModel` 和 `SettingsPage.xaml.cs` 继续负责状态、命令和样式选择，不调整业务层。

**Tech Stack:** .NET 8、WPF、XAML 数据绑定、现有 Presentation/Settings 控制台测试。

## Global Constraints

- 不新增持久化设置字段，不改变 `SettingsViewModel` 的公共接口。
- 保留全部既有 `AutomationProperties.Name`、绑定属性和命令。
- 不显示账号邮箱、订阅或其他身份信息。
- 保持 Windows 10/11 兼容和便携式打包结构。

---

### Task 1: 定义页面级视觉控件

**Files:**
- Modify: `Views/Pages/SettingsPage.xaml`

**Interfaces:**
- Consumes: WPF `RadioButton`、`CheckBox`、`ComboBox`、`Slider` 和 `Button`。
- Produces: `SettingsChipStyle`、`SettingsToggleStyle`、`SettingsComboBoxStyle`、`SettingsSliderStyle` 和三种操作按钮样式。

- [ ] **Step 1: 将系统默认控件替换为页面本地控件模板**

在 `UserControl.Resources` 定义深色卡片、样式胶囊、40×22px 开关、32px 下拉框、绿色滑块和按钮样式；模板只处理外观，不改变输入或绑定行为。

- [ ] **Step 2: 构建页面以验证 XAML 模板**

Run: `dotnet build .\UsageLens.csproj -c Release --no-restore`

Expected: `Build succeeded` 且没有 XAML 解析错误。

### Task 2: 以行式卡片重排设置内容

**Files:**
- Modify: `Views/Pages/SettingsPage.xaml`

**Interfaces:**
- Consumes: `SettingsViewModel` 的既有属性和命令，例如 `FloatingOpacity`、`AutoCollapseEnabled`、`RefreshNowCommand`。
- Produces: 与 HTML 原型一致的状态头、两列设置卡片和全宽诊断卡。

- [ ] **Step 1: 添加安全 Codex 状态头**

使用 `CodexStatusText` 显示连接状态和固定隐私说明，不增加或伪造账号资料。

- [ ] **Step 2: 将五个设置区域改为标签、说明和操作控件的行式布局**

保留每个旧控件的 `x:Name`、`Tag`、`Click`、`Command`、绑定和自动化名称；只替换容器与控件样式。

- [ ] **Step 3: 构建与运行针对设置的回归验证**

Run: `dotnet build .\UsageLens.csproj -c Release --no-restore`

Run: `dotnet run --project .\tests\UsageLens.SettingsTests\UsageLens.SettingsTests.csproj -c Release --no-build`

Expected: build 成功，测试输出通过。

### Task 3: 完成应用级回归与可运行包

**Files:**
- Modify: `Views/Pages/SettingsPage.xaml`
- Generated: `release-floating-auto\UsageLens.exe`

**Interfaces:**
- Consumes: 已完成的 XAML 和既有打包脚本。
- Produces: 可运行的便携式最新包。

- [ ] **Step 1: 运行呈现与协调器回归测试**

Run: `dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release --no-build`

Run: `dotnet run --project .\tests\UsageLens.CoordinatorTests\UsageLens.CoordinatorTests.csproj -c Release --no-build`

Expected: 两个项目均输出通过。

- [ ] **Step 2: 覆盖唯一发布目录生成便携包**

Run: `.\scripts\package-release.ps1 -Version 1.1.0`

Expected: `release-floating-auto\UsageLens.exe` 与 `artifacts\UsageLens-v1.1.0-win-x64-portable.zip` 更新，不创建版本目录。
