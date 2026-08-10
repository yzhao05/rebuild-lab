# GitHub Workflow For Beginners

本文档按本项目当前规则写给 Git/GitHub 初学者。你可以把 Git 想成“可回退的存档系统”，把分支想成“不同实验线”。

## 本项目的分支规则

- `master`：老师原始旧代码。暂时不要改，不要提交，不要 merge。
- `Summer26`：暑期集成分支。它是新的共同起点。
- `feature/...`：日常开发分支。每次做一个小主题，完成后再考虑合回 `Summer26`。

本轮创建的分支是：

`feature/refactor-docs-and-config-skeleton`

它的作用是放置低风险的文档、`.gitignore` 和配置骨架变更。

## 每次开工前先看三件事

在终端里看：

```bash
git status
git branch --show-current
git log --oneline -5
```

如果当前分支是 `master`，停止操作，先切到 `Summer26` 或 feature 分支。

## GitHub Desktop 里怎么看当前状态

1. 打开 GitHub Desktop。
2. 左上角确认仓库是 `EMVRLABS`。
3. 顶部 Current Branch 应该是 `Summer26` 或 `feature/...`，不要是 `master`。
4. 左侧 Changes 会显示当前改动文件。
5. 每个文件可以点开看具体差异。

## 什么文件应该提交

通常应该提交：

- `Assets/`
- `Assets/**/*.meta`
- `Packages/`
- `ProjectSettings/`
- `docs/`
- `README.md`
- `Plan/`
- `Research/`

通常不要提交：

- `Library/`
- `Temp/`
- `Logs/`
- `Obj/` 或 `obj/`
- `Build/` 或 `Builds/`
- `.vs/`
- `.idea/`
- `UserSettings/`
- `*.csproj`
- `*.sln`
- `*.user`
- `*.userprefs`

## 本次发现的已跟踪生成文件

下面这些文件已经被 Git 跟踪。因为它们已经进了仓库，仅修改 `.gitignore` 不会自动移除它们：

- `Unity.XR.Interaction.Toolkit.Samples.DeviceSimulator.csproj`
- `Unity.XR.Interaction.Toolkit.Samples.StarterAssets.Editor.csproj`
- `Unity.XR.Interaction.Toolkit.Samples.StarterAssets.csproj`
- `com.IvanMurzak.Unity.MCP.Installer.csproj`
- `UserSettings/AI-Game-Developer-Config.json`
- `UserSettings/EditorUserSettings.asset`
- `UserSettings/Layouts/CurrentMaximizeLayout.dwlt`
- `UserSettings/Layouts/default-2022.dwlt`
- `UserSettings/Search.index`
- `UserSettings/Search.settings`

建议后续单独开一个分支处理，例如：

`fix/remove-unity-generated-files`

处理方式不是从电脑上删除文件，而是让 Git 停止跟踪它们。这个操作需要先确认，因为它会改变版本控制状态。

## 如何提交一次低风险改动

在 GitHub Desktop：

1. 确认 Current Branch 不是 `master`。
2. 在 Changes 列表中检查文件。
3. 不确定的文件不要勾选。
4. 在 Summary 写一句短消息，例如：
   - `docs: add refactor map and electrostatics config skeleton`
5. 点击 Commit to 当前 feature 分支。
6. 先不要 merge 到 `Summer26`。

## 如何推送到 GitHub

在 GitHub Desktop：

1. 提交后点击 Push origin。
2. 等待完成。
3. GitHub 网页上会出现这个 feature 分支。

注意：不要点 force push。不要执行 `git reset --hard`。

## 如何开 Pull Request

1. 在 GitHub 网页进入 `janelisi/EMVRLABS`。
2. 选择 Compare & pull request。
3. base 分支选 `Summer26`。
4. compare 分支选你的 `feature/...`。
5. PR 标题写清楚主题。
6. 描述里说明：
   - 改了什么。
   - 为什么低风险。
   - 是否需要打开 Unity 测试。
7. 不要把 PR 合并到 `master`。

## 遇到冲突怎么办

不要慌，也不要随手点“覆盖”。先停下来，让项目工程师帮你看：

- 哪些文件冲突。
- 是不是 scene/prefab/meta 冲突。
- 是否需要 Unity 打开后重新保存。

scene、prefab、`.meta` 冲突属于中高风险，要单独处理。

