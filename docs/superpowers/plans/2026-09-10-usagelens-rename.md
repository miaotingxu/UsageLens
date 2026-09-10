# UsageLens Rename Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or **superpowers:executing-plans** to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 CodexQuotaFloat 全量重命名为 UsageLens，保留现有功能和用户配置，并发布 `v1.3.1` 便携版。

**Architecture:** 先完成代码和项目文件重命名，再在配置存储层加入旧目录只读迁移，最后更新文档、打包脚本、GitHub Actions 和远程仓库。迁移逻辑只复制样式与横坐标到新目录，不改变额度、Token 和 App Server 数据流。

**Tech Stack:** C#、WPF、.NET 10、PowerShell、GitHub CLI、GitHub Actions

## Global Constraints

- 项目名、程序集、可执行文件、默认命名空间和 GitHub 仓库统一使用 `UsageLens`。
- 旧配置 `%LOCALAPPDATA%\CodexQuotaFloat` 只用于一次性迁移，新配置写入 `%LOCALAPPDATA%\UsageLens`。
- 不删除旧配置目录，不迁移认证信息、Token、密码或会话内容。
- 现有额度、Token、价格估算、四套 UI、顶部水平拖动、自动折叠、刷新和单实例行为不变。
- 发布版本为 `v1.3.1`，旧 `v1.3.0` 标签和 Release 不修改。
- 新包名为 `UsageLens-v1.3.1-win-x64-portable.zip`，不保留旧 EXE 别名。
- `main` 和 `develop` 都必须包含改名提交；工作区生成物不提交。

---

### Task 1: 项目与代码标识符重命名

**Files:** Rename `CodexQuotaFloat.csproj` to `UsageLens.csproj`; rename both test project directories/files; modify all source and test files containing the product identifier.

**Interfaces:** Consumes existing WPF source and test references; produces `UsageLens` namespace, assembly and test project names without runtime behavior changes.

- [ ] **Step 1:** Use `git mv` for the root project and both test project files/directories.
- [ ] **Step 2:** Set `<AssemblyName>UsageLens</AssemblyName>` and `<RootNamespace>UsageLens</RootNamespace>`; update every `ProjectReference` and test path.
- [ ] **Step 3:** Replace namespace declarations, `using` statements, application/window titles, single-instance identifiers and user-facing product text. Keep only the legacy app-data literal required by Task 2.
- [ ] **Step 4:** Scan with `Get-ChildItem -Path . -Recurse -File -Force | Where-Object { $_.FullName -notmatch '\\(\.git|bin|obj|artifacts|release-floating-auto)\\' } | Select-String -Pattern 'CodexQuotaFloat|Codex Quota Float'`; only planned migration constants, migration tests and historical notes may remain.
- [ ] **Step 5:** Commit with `git add -A; git commit -m "refactor: rename application to UsageLens"`.

### Task 2: Backward-compatible appearance migration

**Files:** Modify `Services/AppearanceSettingsStore.cs` and `tests/UsageLens.PresentationTests/Program.cs`.

**Interfaces:** Consumes `FloatingStyleKind`, `Left` and `%LOCALAPPDATA%`; produces new-path read/write with old-directory fallback.

- [ ] **Step 1:** Define new `%LOCALAPPDATA%\UsageLens` and legacy `%LOCALAPPDATA%\CodexQuotaFloat` paths in the store.
- [ ] **Step 2:** Read new `appearance.json` first; if absent or invalid, read legacy JSON and save valid style/position to the new path without deleting the old directory.
- [ ] **Step 3:** Preserve enum validation, on-screen coordinate fallback and malformed JSON behavior; migrate no other files.
- [ ] **Step 4:** Add tests for new-directory preference, legacy fallback, valid migration, malformed JSON, invalid style, off-screen position, both directories and no configuration, using isolated temporary paths.
- [ ] **Step 5:** Run `dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release`; all existing and migration assertions must pass.

### Task 3: Documentation and packaging rename

**Files:** Modify `README.md`, `README_EN.md`, `CHANGELOG.md`, `CONTRIBUTING.md`, `SECURITY.md`, `scripts/package-release.ps1`, `.github/workflows/release.yml`, and screenshot labels if they contain the old name.

**Interfaces:** Consumes the renamed executable, repository URL and migration behavior; produces user-facing docs and package output using `UsageLens` and `1.3.1`.

- [ ] **Step 1:** Replace product titles, links, commands and package names; add the independent-project disclaimer and old app-data migration explanation.
- [ ] **Step 2:** Make the package script publish `UsageLens.exe`, generate `UsageLens-v<version>-win-x64-portable.zip`, copy `LICENSE` and `README.txt`, and reject old executable names in the release directory.
- [ ] **Step 3:** Update the release workflow to run the renamed test projects and upload the new package glob.
- [ ] **Step 4:** Run `.\scripts\package-release.ps1 -Version 1.3.1`; expect `UsageLens.exe`, `README.txt`, `LICENSE`, the new ZIP and `SHA256SUMS.txt` only.

### Task 4: Build and behavior verification

**Files:** Verify renamed source and generated package.

**Interfaces:** Consumes Tasks 1–3; produces evidence that the rename does not alter application behavior.

- [ ] **Step 1:** Run `dotnet run --project .\tests\UsageLens.TokenUsageTests\UsageLens.TokenUsageTests.csproj -c Release` and `dotnet run --project .\tests\UsageLens.PresentationTests\UsageLens.PresentationTests.csproj -c Release`.
- [ ] **Step 2:** Run `dotnet build .\UsageLens.csproj -c Release`; confirm output is `UsageLens.exe` with no PDB and no old product filename.
- [ ] **Step 3:** Start `release-floating-auto\UsageLens.exe`; confirm one process, existing quota/token display, exit behavior and `%LOCALAPPDATA%\UsageLens\appearance.json`; confirm legacy configuration remains untouched.
- [ ] **Step 4:** Compare the ZIP `Get-FileHash` result with `SHA256SUMS.txt`; inspect ZIP entries for exactly `UsageLens.exe`, `README.txt` and `LICENSE`.

### Task 5: Rename remote repository and synchronize branches

**Files:** Remote `miaotingxu/CodexQuotaFloat` and local Git remote `origin`.

**Interfaces:** Consumes verified Task 4 commit and package; produces renamed GitHub repository with synchronized `main` and `develop`.

- [ ] **Step 1:** Run `gh repo rename UsageLens --repo miaotingxu/CodexQuotaFloat --yes`.
- [ ] **Step 2:** Run `git remote set-url origin https://github.com/miaotingxu/UsageLens.git`.
- [ ] **Step 3:** Set Description to `Lightweight Windows widget for monitoring local AI usage, quotas, and estimated costs.`; update functional Topics and keep the independent-project disclaimer.
- [ ] **Step 4:** Push `develop`, fast-forward or cherry-pick the verified rename commit onto `main`, then push `main`; do not rewrite `v1.3.0`.
- [ ] **Step 5:** After both branches and package checks pass, create and push annotated tag `v1.3.1`; confirm the workflow publishes the `UsageLens` ZIP and SHA256 file.
