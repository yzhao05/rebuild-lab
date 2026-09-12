# EMVR Unity VR Lab 自动重建：用户使用说明

## GitHub 上传与下载

GitHub 不要求 CRLF。本发布包通过 `.gitattributes` 的 `* -text` 保留每个文件的
原始字节，使 clone、Download ZIP 和本地文件使用相同哈希。不要批量转换 LF/CRLF，
也不要在下载后修改冻结哈希。Unity 光照设置文件保持已发布的 LF。

上传请使用干净发布包，包含隐藏的 `.agents`、`.gitattributes`、`.gitignore`；
不要上传当前 Lab 的运行记录、场景、保护基线、维护凭证或 Unity 缓存。
建议仓库名为 `EMVR_Blind_BuilderPack`。若更新已有仓库，维护方应在干净发布副本
中执行 `git add -A` 和 `git add --renormalize .`，检查暂存差异后再提交，确保旧
索引也按保留字节规则重新写入。不要只提交 `.gitattributes` 而保留旧转换后的文件。

下载时可用 Git clone 或 Download ZIP。ZIP 解压后的目录可能带 `-main` 等后缀，
请将主目录重命名为 `EMVR_Blind_BuilderPack`。在该目录运行边界检查：

```powershell
powershell -ExecutionPolicy Bypass -File .agents/skills/build-unity-vr-lab-blind/scripts/verify_builder_pack.ps1
```

看到 PASS 后再让 Builder 创建新的独立 Lab。出现哈希错误时保留错误信息并交给
维护方处理，不要重采基线或修改资源文件来绕过检查。

本文面向使用 AI 重建 EMVR Unity VR Lab 的用户。即使你没有使用过 Unity，
也可以按本文操作。AI 必须在每次需要你操作 Unity 时给出准确的菜单位置、点击
顺序、成功标准和失败截图要求。

> 重要：不要直接让 AI 在 `EMVR_Blind_BuilderPack` 主目录中搭建 Lab。
> 盲重建/复刻/效果对比任务必须先在主 Builder Pack 内创建固定名称的独立
> 子工作区，再把该子目录作为 AI 工作区。

## 1. 项目介绍

本项目是一套用于“盲重建”Unity VR 教学实验的 Builder Pack。所谓盲重建，是指
AI 根据用户提供的实验文档、图片和要求重新实现 Lab，但不能读取旧 Lab 的完整
实现代码、答案、坐标、类名或场景接线。

Builder Pack 为 AI 提供以下允许使用的基础：

- Unity 2022.3 空白工程；
- 已验证的 EMVR Common 通用包；
- XR 手柄、射线、桌面拖拽、状态保存、重置、遥测和 Assist 等通用能力；
- 经过批准的模型、材质、UI、音频和 XRI 资源；
- 必须复用的房间、XR Interaction Setup 和 XR Device Simulator Prefab；
- Gate 5 受保护的按钮、生成物可见性、EventSystem 和回归测试基础；
- 历次重建中提炼出的跨 Lab 构建经验；
- Gate 0–10 状态机、验证脚本和证据模板。

AI 只能直接调用 Common，通过指定工具导入 ApprovedAssets，并通过 Gate 5
materializer 原样复制受保护基础；不能手工复制或修改这些来源，也不能修改工作流、
Skill、Policy 或知识库。发现这些基础存在问题时，AI 必须暂停当前 Gate 并向用户
汇报，不能自行修改基础设施或写替代代码。

Unity MCP 当前禁用。Unity 中的编译、Test Runner、Game View 和交互检查由用户
按照 AI 给出的详细步骤完成。

## 2. 工作流介绍

整个重建过程分为 Gate 0–10。核心规则是：

1. 一次 AI 回复只完成一个 Gate；
2. AI 完成当前 Gate 后汇报结果并等待用户审批；
3. 用户表达明确的继续意图后，AI 才能记录审批并进入下一个 Gate；
4. 当前 Gate 有错误时必须先修复，不能继续堆叠后续代码；
5. AI 不能自行审批，也不能跳过 Gate；
6. 每个 Lab 使用一个独立运行副本，不能接触其他运行的代码和证据。

用户不需要回复固定口令。以下表达都可以批准当前已完成的 Gate：

```text
继续
可以
下一步
通过
确认
没问题
OK
go ahead
```

如果你的回复同时提出修改，例如“可以，但先修改按钮颜色”，则当前 Gate 仍保持
打开，AI 应先完成修改并重新提交审批。

## 3. 开始前需要准备什么

建议准备以下输入：

- Lab 的 PDF、Word、PPT、图片或文字需求；
- 实验名称和希望使用的 Lab 编号；
- 学习目标、实验步骤和预期结果；
- 已知的公式、单位和参数范围；
- 桌面操作和 VR 操作要求；
- 需要使用的模型或视觉参考；
- 无法立即测试的内容，例如暂时没有真实 VR 手柄。

你上传到当前 AI 对话中的文档属于允许输入。AI 可以读取这些准确文件，但不能
借此搜索文档父目录或旧 Lab 文件夹。

### Lab ID 命名规则

`lab_id` 只能使用小写英文字母、数字和下划线，必须以字母开头，长度为
3–64 个字符。例如：

```text
lab2_magnetostatic
lab4_optics
induction_demo
```

## 4. 让重建 Agent 创建干净运行副本

### 第一步：关闭 Unity，并在 Codex 中打开主 Pack

创建运行副本前，先关闭正在打开 Builder Pack 的 Unity 窗口。
在 Codex 中把下面的主目录作为初始工作区：

```text
E:\暑研\EMVR_Blind_BuilderPack
```

### 第二步：让重建 Agent 自动创建副本

在主 Pack 工作区上传需求文档，并按第 5 节的格式调用重建 Agent。重建 Agent
必须自行判断这是盲重建任务，并自动执行下面的复制工具；**用户不需要打开
PowerShell，也不应被要求手动输入这个命令或复制文件夹**：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/create_blind_run_pack.ps1 -LabId <lab_id>
```

Agent 必须等待并核对：

```text
[EMVR Builder Boundary] PASS
[EMVR Blind Rebuild Workspace] CREATED
```

成功后，Agent 必须直接给出生成目录的**完整绝对路径**，并只要求用户切换到
该目录。如果显示 `FAIL`、目标已存在或其他错误，Agent 必须向用户汇报，不能
覆盖或删除现有目录，也不能把执行命令的责任转交给用户。
创建和切换子工作区属于 Gate 0 之前的准备，不应被报告成“Gate 0 阻塞”。Agent
只有在子工作区中初始化运行后才开始 Gate 0。

### 第三步：把运行副本作为 AI 工作区

用户此时只需在 Codex 中选择“打开文件夹”，打开 Agent 给出的目录，例如：

```text
E:\暑研\EMVR_Blind_BuilderPack\RebuildWorkspaces\EMVR_Blind_Rebuild_lab2_magnetostatic
```

固定目录名必须是 `EMVR_Blind_Rebuild_<lab_id>`。不要把重建工作区创建到
`E:\暑研` 等主 Pack 父目录，也不要打开主目录
`E:\暑研\EMVR_Blind_BuilderPack` 作为本次 Lab 的实现工作区。

这一位置限制只适用于盲重建、复刻和效果对比。如果以后把 Builder Pack 集成到
其他项目内部，用它开发一个真正的新 Lab，则可以使用宿主项目工作区，并选择
`integrated-development` 模式，不要求使用上述内部子目录。

## 5. 调用 AI 的格式

第一次调用时，在主 Pack 工作区把需求文档上传到当前对话，然后发送以下提示
词。Agent 会先自动创建子工作区并让你切换；切换成功后，在同一对话回复
“继续”即可。如果切换工作区后 Codex 新建了对话，请重新上传需求文档并再次
发送同一提示词。Agent 应识别子工作区根目录中的 `.emvr-workspace.json`，直接
初始化或恢复当前 Lab，不得再次创建副本。

```text
$build-unity-vr-lab-blind

请根据我在当前对话中上传的需求文档重建这个 Unity VR Lab。
Lab ID：<lab_id>
Lab 名称：<实验名称>

这是盲重建任务。请由你在主 Pack 中自动运行运行副本创建工具；不要让我执行
PowerShell 或手动复制文件夹。创建成功后给出子工作区的完整绝对路径让我切换。

请从 Gate 0 开始，严格一次对话只完成一个 Gate。完成当前 Gate 后，向我汇报：
Current gate、Purpose、Verified、Remaining/blockers、Need from user，
然后等待我审批。不要提前执行下一个 Gate。

需要我操作 Unity 时，请把我当成第一次使用 Unity 的用户，明确告诉我：
菜单和窗口在哪里、按什么顺序点击、选择哪个当前 Lab 对象或测试、
成功时应看到什么、失败时需要截取哪些窗口，以及哪些操作不要做。
```

实际示例：

```text
$build-unity-vr-lab-blind

请根据我上传的 Lab 2 Magnetostatic.pdf 重建这个 Unity VR Lab。
Lab ID：lab2_magnetostatic
Lab 名称：Magnetostatics

这是盲重建任务。请由你自动创建内部运行副本，成功后给出完整绝对路径让我
切换；不要让我运行 PowerShell 或手动复制文件夹。

请从 Gate 0 开始，严格一次对话只完成一个 Gate。完成当前 Gate 后汇报结果并
等待我审批。需要我操作 Unity 时，请给出完整菜单路径、点击顺序、通过标准和
失败截图要求，不要假设我使用过 Unity。
```

如果 AI 尚未初始化运行，它会执行类似下面的命令：

```powershell
python Tools/labflow/labflow.py new --mode blind-rebuild --lab-id <lab_id> --title "<实验名称>" --domain <领域名称> --scene Assets/Scenes/<场景名称>.unity
```

这些命令通常由 AI 执行，用户不需要手动输入。

初始化时 AI 还会生成
`LabSpecs/<lab_id>/source-references.json`，把原始可复用代码和资源以“只读引用”
交给当前 Lab。文件中只保存相对于当前 Builder Pack 根目录的路径，不保存
`C:\`、`D:\`、`E:\` 等本机绝对地址。仓库从 GitHub 克隆到另一台电脑后，
AI 会运行下面的解析命令，无需用户改路径：

```powershell
python Tools/labflow/labflow.py source-references --lab-id <lab_id>
```

命令显示的绝对路径只用于当次诊断，不能写回源码、场景、合同或证据文件。

## 6. 每个 Gate 的作用

### Gate 0：边界与历史经验检查

目的：确认这是安全、干净且可用于单次重建的 Builder Pack。

AI 会：

- 运行 Builder Boundary；
- 验证只读源码引用清单、相对路径和清单哈希；
- 检查 Common、本地 Unity 包路径、MCP 禁用状态和单运行边界；
- 记录发布边界 PASS 后，由 AI 执行 `python Tools/labflow/labflow.py capture-gate0-baseline --lab-id <lab_id>`，一次性保存本次运行的受保护文件基线；
- 阅读允许使用的跨 Lab 构建经验；
- 禁止读取受限旧 Lab 总结；
- 填写当前 Lab 的经验审阅记录。

用户需要：检查 AI 的 PASS 证据。如果无问题，回复“继续”或同义表达。

发布包固定哈希只允许工作流维护端更新。Gate 0 基线保存在
`BuildEvidence/<lab_id>/gate0-protected-baseline.json`，不能覆盖或重新生成；Gate 1–10 发现受保护文件相对基线有新增、删除或变化时会停止。

Gate 4 尚未生成脚手架时，用户授权的维护方可登记逐文件旧／新哈希维护凭据，
保留原基线和 Gate 0–3 审批。Builder 不得签发、修改、删除或重新登记凭据，
也不得修改 state 中的 `protected_maintenance`。维护完成后重新执行
Boundary、source-references 和 status，完整性通过即可在原 Gate 4 继续；
仍须生成脚手架并取得 Unity 零错误证据。规则见 `Policies/reuse-policy.md`。

### Gate 1：确认实验 Brief

目的：把用户文档转化成完整、无歧义的实验需求。

AI 会整理：

- 学习目标与实验任务；
- 公式、单位、参数范围和预期结果；
- 实验对象、初始状态和第一步操作；
- 桌面操作，以及重置、生命周期、遥测和 Assist 要求；
- 房间、UI、可见反馈与验收证据。

桌面核心交互默认自动获得功能等价的 XR 路径：操作同一对象、产生同一状态变化、
反馈和成功结果，具体鼠标与 Trigger/Select/Grab 输入由 Common/XRI 处理。用户
不需要在 Gate 1 设计普通鼠标到 XR 映射；只有需求明确要求不同语义或 Common
无法表达该动作时，AI 才能集中询问并记录例外。

用户需要：回答 AI 集中提出的缺失问题，审核 Brief 是否符合原始实验要求。

### Gate 2：建立五份接口合同

目的：把需求变成可自动校验的结构化合同。

五份合同分别描述：

- Lab 总体规格；
- 对象和唯一 ID；
- 实验步骤；
- 验收标准；
- 遥测事件映射。

AI 会运行合同校验，确保对象、步骤、验收项、遥测和 Assist 引用互相对应。
用户主要审核合同表达是否与 Brief 一致。

### Gate 3：设计与 API 审核

目的：在写业务代码前确认技术方案。

AI 会说明：

- 哪些 Common、XR 和房间组件必须复用；
- 哪些代码属于当前 Lab 新实现；
- 使用的 Unity/Common/XRI API 版本、程序集和准确签名；
- 场景结构、对象可见性、交互、相机和 UI 布局；
- Builder 如何在错误时停止，避免保存半成品场景；
- 测试、证据和延期计划。

用户需要：审核功能与视觉设计，确认后允许生成脚手架。

### Gate 4：生成并编译纯脚手架

目的：先证明最小工程结构可以编译，再加入领域逻辑。

AI 会生成 Runtime 和 Editor Builder 的空结构，但不会提前加入完整领域算法。
随后 AI 会给出详细 Unity 操作，例如：

1. 打开 Unity 项目的 `UnityProject` 文件夹；
2. 等待右下角编译图标停止；
3. 通过 `Window > General > Console` 打开 Console；
4. 点击 Console 左上角 **Clear**；
5. 点击顶部菜单 `Assets > Refresh`；
6. 等待重新编译，确认红色编译错误为 0。

失败时，用户应选中第一条红色错误并同时截取完整 Console。

### Gate 5：可复用基础和最小领域闭环

目的：先验证所有 Lab 都依赖的通用能力，再只实现一条最小领域闭环。

Gate 5 分为两个有顺序的检查点。

#### Gate 5A：通用基础验证

AI 会：

- 导入必须复用的房间、XR Interaction Setup、XR Device Simulator Prefab 及依赖；
- 原样复制受保护的 Gate 5 基础代码；
- 接入 Common 的桌面拖拽、XR 射线、对象状态、重置、遥测、Assist 和生命周期；
- 搭建中性测试单元，包括按钮、EventSystem、生成模板、可见生成物和状态显示；
- 要求运行固定的 3 个基础 EditMode 测试；
- 检查按钮能生成可见且有 Collider 的物体、桌面拖拽有效、重置有效、XR
  Rig/射线存在且 Console 干净。

Test Runner 的标准位置是 Unity 顶部菜单：

```text
Window > General > Test Runner
```

进入 **EditMode** 后，运行程序集：

```text
EMVR.ReusableFoundation.Gate5.Tests
```

固定预期为 3 个测试全部绿色通过、Failed 为 0。AI 必须在让用户操作前再次列出
准确测试名、数量和 **Run Selected** 点击位置。

#### Gate 5B：当前 Lab 最小领域切片

AI 只加入一条端到端领域链路：

- 一个领域对象；
- 一个确定性计算；
- 一个可见结果；
- 一个桌面交互和默认功能等价的 XR 适配；
- 状态保存/重置；
- 一个遥测事件；
- 一个 Assist grounding 结果；
- 聚焦该链路的 EditMode 测试。

加入领域代码后，之前的基础证据会自动失效。AI 必须在同一份最终代码上重新运行
领域测试、干净编译、3 个基础测试和中性场景检查，才能提交 Gate 5 审批。

### Gate 6：构建和验证完整 Unity 场景

目的：生成最终场景并检查 Game View 的实际效果。

AI 必须告诉用户准确的 Builder 菜单，例如：

```text
Tools > EMVR > Labs > Build <Lab Title>
```

然后执行 Common 场景验证：

```text
Tools > EMVR > Common > Validate Active Scene
```

用户会检查：

- Console 无红色错误；
- Builder 出错时停止且不保存半成品；
- 房间、XR Setup/Simulator、Common、领域对象和状态接线正确；
- Game View 中 UI 清晰且不遮挡实验区；
- 初始状态及第一次操作后的画面符合 Brief。

Game View 通常是中央 **Scene** 标签旁的 **Game** 标签；若没有，使用
`Window > General > Game` 打开。进入或退出 Play Mode 使用 Unity 顶部中央的
三角形按钮。

### Gate 7：交互与生命周期测试

目的：验证用户实际操作流程。

通常包括：

- 鼠标和键盘桌面操作；
- XR Device Simulator；
- 真实 VR 手柄，如果当前有设备；
- Capture、Restore 和 Reset；
- Start、Lab、Back 等场景生命周期。

没有真实手柄时，可以明确记录为延期，但桌面和模拟器路径仍应完成。用户按照
AI 的逐步指令操作，并提供成功结果或失败截图。

### Gate 8：整理验收材料

目的：把本次运行的真实证据整理成可审核资料。

AI 会完善：

- 复用报告；
- 验证报告；
- ApprovedAssets 导入清单；
- Console、Test Runner、Game View 和交互证据；
- 无法测试项目的延期说明。

不得使用其他 Lab 的报告代替当前 Lab 证据，也不能保留 `TODO` 或 `pending`。

### Gate 9：用户最终验收

目的：由用户决定当前 Lab 是否可以接受。

AI 会展示已完成功能、验证结果和延期项。只有用户可以作出最终验收决定。
Gate 9 通过后流程还没有结束；AI 必须提醒用户接下来会进入 Gate 10 复盘。

### Gate 10：错误复盘与经验归纳

目的：把本轮出现的问题转化成下一次重建可参考的安全经验。

AI 会整理：

- 错误出现的顺序；
- 根因和责任归类；
- 实际修复及验证证据；
- 被拒绝的临时绕过方案；
- 下次应在哪个 Gate 预防；
- 可跨 Lab 使用的 API 和生命周期约束。

完整运行记录保存到 `BuildEvidence/retrospectives/`；经过清理的跨 Lab 经验保存
到 `KnowledgeBase/rebuild-lessons/`，供下一次 Gate 0 阅读。经验文件不得包含
当前 Lab 的答案、坐标、类名、完整实现、隐私路径或未经验证的猜测。

User 审批 Gate 10 后，整个工作流才正式完成。

## 7. AI 每个 Gate 应使用的汇报格式

AI 完成当前 Gate 后，至少应包含：

```text
Current gate: Gate N — <当前阶段名称>
Purpose: <本阶段目的>
Verified: <已完成并验证的内容>
Remaining/blockers: <未完成内容或“无”>
Need from user: <需要用户审核、操作或回复的内容>
```

如果 AI 只说“完成了”却没有证据，用户可以回复：

```text
请按工作流汇报当前 Gate 的 Purpose、Verified、Remaining/blockers 和
Need from user，并给出实际验证证据。不要进入下一个 Gate。
```

## 8. 出错时怎么做

### Unity 出现编译错误

不要继续点击 Builder 或进入下一个 Gate。按照 AI 指令：

1. 打开 `Window > General > Console`；
2. 点击 **Clear**；
3. 点击 `Assets > Refresh`；
4. 等待重新编译；
5. 选中第一条红色错误；
6. 截取包含完整错误和 Console 区域的图片发给 AI。

AI 应优先分析最早的根错误，而不是逐个修改后续连锁错误。

### 按钮、模型或生成物看不到

向 AI 提供：

- Game View 全图；
- Hierarchy 全图；
- Console；
- 点击前后的状态；
- 使用的是鼠标、模拟器还是真实手柄。

AI 应在当前 Gate 内检查按钮事件、EventSystem、模板激活状态、Collider、相机
可见性和生成位置，不能用“成功日志”代替画面验收。

### AI 想修改工作流或可复用基础

要求 AI 停止并汇报问题。Builder Agent 不得修改：

```text
.agents/
Policies/
Tools/
KnowledgeBase/
ApprovedAssets/
ReusableFoundation/
UnityProject/LocalPackages/com.emvr.lab-common/
```

这些问题应交给工作流维护者处理，修复后再恢复当前 Gate。

## 9. 常用状态命令

查看当前 Gate、阻塞项和下一步：

```powershell
python Tools/labflow/labflow.py status --lab-id <lab_id>
```

如果状态提示 schema 过旧，只能执行迁移：

```powershell
python Tools/labflow/labflow.py migrate-state --lab-id <lab_id>
```

迁移会保留文件，但会清除旧审批和旧证据，并从 Gate 0 重新核验。

## 10. 完成后的结果在哪里

当前 Lab 的主要输出位于运行副本中：

```text
LabSpecs/<lab_id>/
LabInterfaceContracts/
UnityProject/Assets/
BuildEvidence/<lab_id>/
BuildEvidence/retrospectives/<lab_id>.md
KnowledgeBase/rebuild-lessons/<lab_id>.md
```

保留整个 `RebuildWorkspaces/EMVR_Blind_Rebuild_<lab_id>` 文件夹即可保存本次
Lab、规格、代码、场景、
测试和证据。下一次重建其他 Lab 时，应重新从主 Builder Pack 创建新的独立副本，
不要清空或复用这次运行目录。
