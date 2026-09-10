# CodexQuotaFloat Open Source Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 CodexQuotaFloat 整理为带标准开源文档、可重复自动发布流程和可校验 Windows 便携包的 MIT 开源项目。

**Architecture:** 源码仓库只保存文档、工作流与构建配置，生成物继续留在被忽略的 `release-floating-auto`。本地与 GitHub Actions 使用相同的 `dotnet publish` 参数，线上资产用版本化 ZIP 和 SHA256 校验值表达不可变版本。

**Tech Stack:** .NET 10、WPF、PowerShell、GitHub Actions、GitHub CLI、Markdown、MIT License

## Global Constraints

- 仅发布 Windows x64 便携版，不增加安装器、自动更新或代码签名。
- 版本为 `v1.3.0`；本次不推送标签、不创建线上 Release。
- 本地始终覆盖 `release-floating-auto`，不创建新的版本目录。
- ZIP 只包含 `CodexQuotaFloat.exe`、`README.txt` 和 `LICENSE`，不得包含 PDB、缓存或用户数据。
- 便携包无需 .NET Runtime，但本机必须已安装并登录 Codex。
- Token 价格只描述为本地估算，不等同真实账单。

---

### Task 1: 标准开源文档

**Files:**
- Modify: `README.md`
- Create: `LICENSE`
- Create: `CHANGELOG.md`
- Create: `CONTRIBUTING.md`
- Create: `SECURITY.md`

**Interfaces:**
- Consumes: 当前 WPF 功能和 `docs/superpowers/specs/2026-09-10-open-source-release-design.md`。
- Produces: 面向最终用户和贡献者的仓库入口文档；打包任务会复制 `LICENSE`。

- [ ] **Step 1: 重写 README**

加入 Release/Windows/MIT 徽章、最新版下载、功能、要求、三步启动、交互、隐私、故障排查、构建、测试和贡献链接；删除“下拉菜单切换”和“不支持窗口拖动”等过时内容。

- [ ] **Step 2: 添加 MIT License**

使用标准 MIT 正文，并设置 `Copyright (c) 2026 miaotingxu`。

- [ ] **Step 3: 添加社区文档**

`CHANGELOG.md` 记录 `1.3.0`；`CONTRIBUTING.md` 要求从 `develop` 建分支并运行两个测试项目；`SECURITY.md` 指向 GitHub Security Advisories 且禁止公开敏感数据。

- [ ] **Step 4: 检查内容一致性**

Run:

```powershell
Select-String -Path .\README.md -Pattern '下拉菜单|不包含.*窗口拖动'
```

Expected: 无匹配。

- [ ] **Step 5: 提交文档**

```powershell
git add README.md LICENSE CHANGELOG.md CONTRIBUTING.md SECURITY.md
git commit -m "docs: prepare public project documentation"
```

### Task 2: 可重复的便携包脚本

**Files:**
- Create: `scripts/package-release.ps1`
- Modify: `.gitignore`

**Interfaces:**
- Consumes: `CodexQuotaFloat.csproj`、根目录 `LICENSE`、参数 `-Version <semver>`。
- Produces: `release-floating-auto`、`artifacts/CodexQuotaFloat-v<version>-win-x64-portable.zip` 和 `artifacts/SHA256SUMS.txt`。

- [ ] **Step 1: 添加参数和路径安全检查**

脚本使用 `[ValidatePattern('^\d+\.\d+\.\d+$')] [string]$Version`，并将输出目录解析到仓库根目录下的固定 `release-floating-auto` 与 `artifacts`。

- [ ] **Step 2: 构建最终目录**

运行两个测试项目后执行：

```powershell
dotnet publish .\CodexQuotaFloat.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o .\release-floating-auto
```

清除该固定目录旧内容，复制 `LICENSE`，并生成包含运行前提、启动/退出、隐私边界和项目地址的 UTF-8 `README.txt`。

- [ ] **Step 3: 生成 ZIP 和校验值**

用 `Compress-Archive` 生成版本化 ZIP；用 `Get-FileHash -Algorithm SHA256` 写出小写哈希、两个空格和文件名。

- [ ] **Step 4: 验证包结构**

Run:

```powershell
.\scripts\package-release.ps1 -Version 1.3.0
tar -tf .\artifacts\CodexQuotaFloat-v1.3.0-win-x64-portable.zip
```

Expected: 仅列出 EXE、README.txt、LICENSE。

- [ ] **Step 5: 提交打包脚本**

```powershell
git add scripts/package-release.ps1 .gitignore
git commit -m "build: add reproducible portable packaging"
```

### Task 3: 标签驱动的 GitHub Release

**Files:**
- Create: `.github/workflows/release.yml`

**Interfaces:**
- Consumes: `v*` 标签和 `scripts/package-release.ps1 -Version`。
- Produces: GitHub Release，资产为版本化 ZIP 和 `SHA256SUMS.txt`。

- [ ] **Step 1: 添加触发器与最小权限**

工作流监听 `push.tags: ['v*']`，设置 `permissions.contents: write`，运行环境为 `windows-latest`。

- [ ] **Step 2: 安装 SDK 并调用统一脚本**

使用 `actions/checkout`、`actions/setup-dotnet` 的稳定主版本，从标签去掉前导 `v` 后调用 `scripts/package-release.ps1`。

- [ ] **Step 3: 创建 Release**

设置 `GH_TOKEN: ${{ github.token }}`，执行：

```powershell
gh release create $env:GITHUB_REF_NAME .\artifacts\*.zip .\artifacts\SHA256SUMS.txt --generate-notes --verify-tag --title $env:GITHUB_REF_NAME
```

- [ ] **Step 4: 静态检查工作流**

确认触发器、权限、SDK 版本、脚本参数和上传路径均存在，且不保存任何额外密钥。

- [ ] **Step 5: 提交工作流**

```powershell
git add .github/workflows/release.yml
git commit -m "ci: publish portable package from version tags"
```

### Task 4: 最终验证与交付

**Files:**
- Verify: `release-floating-auto/*`
- Verify: `artifacts/*`

**Interfaces:**
- Consumes: Tasks 1–3 的全部文件。
- Produces: 可运行的本地便携目录、可上传资产和验证报告。

- [ ] **Step 1: 运行完整打包**

```powershell
.\scripts\package-release.ps1 -Version 1.3.0
```

Expected: 两个测试项目通过、publish 成功、ZIP 与校验文件生成。

- [ ] **Step 2: 验证 SHA256**

重新运行 `Get-FileHash`，确认与 `artifacts/SHA256SUMS.txt` 完全一致。

- [ ] **Step 3: 验证 Git 状态**

```powershell
git status --short
git check-ignore release-floating-auto artifacts
git diff --check
```

Expected: 生成物被忽略，没有意外源码修改或空白错误。

- [ ] **Step 4: 报告发布边界**

明确说明已生成本地包、已配置标签发布，但未创建 `v1.3.0` 标签、未推送、未创建线上 Release。
