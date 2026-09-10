# CodexQuotaFloat README Branding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or **superpowers:executing-plans** to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 CodexQuotaFloat 的仓库首页改造成中文优先、英文完整镜像、定位清晰且符合成熟开源项目习惯的产品型 README，并同步 GitHub 仓库展示信息。

**Architecture:** `README.md` 负责中文产品首页，`README_EN.md` 保持等义英文内容；两者共享 Release、License、构建命令和隐私边界。GitHub Description 与 Topics 通过 `gh repo edit` 更新，真实截图只有在确认存在可用素材后才加入。

**Tech Stack:** Markdown、PowerShell、GitHub CLI (`gh`)、Git

## Global Constraints

- 默认 README 使用简体中文，同时提供完整 `README_EN.md`。
- GitHub Description 使用：`Lightweight Windows widget for Codex quotas, reset times, local token usage, and estimated costs.`
- Topics 使用：`codex openai windows wpf desktop-widget token-usage quota-monitor dotnet`。
- 不修改 WPF 功能、发布包、版本标签或分支结构。
- 不暗示 OpenAI 官方关系，不把本地 Token 价格估算描述为真实账单。
- 不提交伪造截图、真实账号信息、会话正文或本地路径。

---

### Task 1: 中文产品型 README

**Files:** Modify `README.md`.

**Interfaces:** Consumes 当前 `v1.3.0` 实现、Release 下载地址、MIT License 和已确认规格；produces 中文用户默认看到的项目首页。

- [ ] **Step 1: 写首屏定位**：加入项目名、中文主标语、英文副标语、语言入口、Release/Windows/.NET/MIT/Downloads 徽章，以及“实时查看 Codex 额度、重置倒计时、本地 Token 用量和预估成本。无需额外登录，无需 API Key，数据留在本机。”
- [ ] **Step 2: 重组章节**：按“为什么使用 → 功能概览 → 下载与安装 → 使用方法 → 数据与隐私 → 价格估算 → FAQ → 构建测试 → 路线图 → 贡献安全许可证”排序，不以杂糅长列表开场。
- [ ] **Step 3: 保留事实边界**：准确描述 `codex app-server`、`%USERPROFILE%\.codex\sessions`、60 秒额度刷新、5 分钟 Token 刷新、模型价格和 SHA256；明确价格估算不是真实账单。
- [ ] **Step 4: 静态检查**：运行 `Select-String -Path .\README.md -Pattern '下拉菜单|自由垂直拖动|OpenAI 官方|真实账单完全一致'`，预期无匹配；运行 `git diff --check`。

### Task 2: 英文 README 镜像

**Files:** Create `README_EN.md`.

**Interfaces:** Consumes `README.md` 的事实内容和相同 Release 资产；produces 可独立阅读、顶部与中文互链的英文首页。

- [ ] **Step 1: 写英文首屏**：使用 `A lightweight, always-on-top Windows widget for Codex usage monitoring.` 和 `Track Codex rate limits, reset countdowns, local token usage, and estimated costs without an extra login or API key.`
- [ ] **Step 2: 镜像全部使用章节**：保留下载、系统要求、操作方式、数据来源、价格表、FAQ、源码构建、路线图、贡献、安全和 MIT License；命令、路径和版本事实与中文一致。
- [ ] **Step 3: 检查双语互链**：运行 `Select-String -Path .\README.md, .\README_EN.md -Pattern 'README_EN|README\.md|codex app-server'` 和 `git diff --check`，确认两个文件都含语言切换、下载入口和隐私边界。

### Task 3: 截图资产与链接审计

**Files:** Inspect `assets/`, `docs/`, `release-floating-auto/`; create `assets/screenshots/ui-overview.png` only when a verified real screenshot exists; modify both README files only when the image exists.

**Interfaces:** Consumes 当前 `v1.3.0` 四套 UI 的真实截图；produces 统一预览，或明确不提交截图。

- [ ] **Step 1: 查找素材**：运行 `Get-ChildItem -Path .\assets, .\docs, .\release-floating-auto -Recurse -File -Include *.png,*.jpg,*.jpeg,*.webp -ErrorAction SilentlyContinue`。
- [ ] **Step 2: 安全加入预览**：仅当四套真实 UI 截图不含账号、路径、会话内容时保存为 `assets/screenshots/ui-overview.png` 并用相对路径引用；没有可靠素材时不创建伪造或失效图片。
- [ ] **Step 3: 链接检查**：确认本地文档、Release、License 和项目链接均指向真实目标，并运行 `git diff --check`。

### Task 4: GitHub 仓库展示信息

**Files:** Remote metadata for `miaotingxu/CodexQuotaFloat`.

**Interfaces:** Consumes 已确认 Description 与 Topics；produces 可搜索、定位清晰的 GitHub 仓库首页。

- [ ] **Step 1: 更新 Description**：运行 `gh repo edit miaotingxu/CodexQuotaFloat --description "Lightweight Windows widget for Codex quotas, reset times, local token usage, and estimated costs."`。
- [ ] **Step 2: 更新 Topics**：运行 `gh repo edit miaotingxu/CodexQuotaFloat --add-topic codex --add-topic openai --add-topic windows --add-topic wpf --add-topic desktop-widget --add-topic token-usage --add-topic quota-monitor --add-topic dotnet`。
- [ ] **Step 3: 核对远程**：运行 `gh repo view miaotingxu/CodexQuotaFloat --json description,repositoryTopics,defaultBranchRef`，确认 Description、8 个 Topics 和 `main` 默认分支正确。

### Task 5: 提交与最终验证

**Files:** `README.md`, `README_EN.md`, optional `assets/screenshots/ui-overview.png`.

**Interfaces:** Consumes Tasks 1–4；produces 可审阅、可推送的 README 品牌更新。

- [ ] **Step 1: 检查范围**：运行 `git status --short`、`git diff --stat`、`git diff --check`，确认只有双语 README、经验证截图和设计相关文件变化。
- [ ] **Step 2: 核对实现事实**：确认下载链接、.NET 10 命令、测试项目、四套 UI、顶部水平拖动、1 秒折叠、60 秒额度刷新和 5 分钟 Token 刷新描述准确。
- [ ] **Step 3: 提交**：运行 `git add README.md README_EN.md assets\screenshots\ui-overview.png; git commit -m "docs: refine project branding and bilingual README"`；若截图不存在，不把不存在的路径加入命令。
- [ ] **Step 4: 推送复核**：运行 `git push origin develop`，再用 `gh api repos/miaotingxu/CodexQuotaFloat/contents/README.md?ref=develop` 和对应 `README_EN.md` 确认远程文件存在。
