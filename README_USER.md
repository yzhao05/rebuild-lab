# EMVR Unity VR Lab 自动重建：用户使用说明

本文面向使用 AI 重建 EMVR Unity VR Lab 的用户。即使你没有使用过 Unity，
也可以按本文操作。AI 必须在每次需要你操作 Unity 时给出准确的菜单位置、点击
顺序、成功标准和失败截图要求。

> 重要：不要直接让 AI 在 `EMVR_Blind_BuilderPack` 主目录中搭建 Lab。
> 每个 Lab 都必须先创建一个独立、干净的运行副本，再把该副本作为 AI 的工作区。

## 1. 项目介绍

本项目是一套用于“盲重建”Unity VR 教学实验的 Builder Pack。所谓盲重建，是指
AI 根据用户提供的实验文档、图片和要求重新实现 Lab，但不能读取旧 Lab 的完整
实现代码、答案、坐标、类名或场景接线。

Builder Pack 为 AI 提供以下允许使用的基础：

- Unity 2022.3 空白工程；
- 已验证的 EMVR Common 通用包；
- XR 手柄、射线、桌面拖拽、状态保存、重置、遥测和 Assist 等通用能力；
- 经过批准的模型、材质、UI、音频和 XRI 资源；
- 必须复用的房间 Prefab；
- Gate 5 受保护的按钮、生成物可见性、EventSystem 和回归测试基础；
- 历次重建中提炼出的跨 Lab 构建经验；
- Gate 0–10 状态机、验证脚本和证据模板。

AI 可以复制或调用已批准的通用代码，但不能修改 Common、受保护的 Gate 5
基础、ApprovedAssets、工作流、Skill、Policy 或知识库。发现这些基础存在问题时，
AI 必须暂停当前 Gate 并向用户汇报，不能自行修改基础设施或写替代代码。

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
lab2_magnetostatics
lab4_optics
induction_demo
```

## 4. 创建干净运行副本

### 第一步：关闭 Unity

创建运行副本前，先关闭正在打开 Builder Pack 的 Unity 窗口。

### 第二步：打开 PowerShell

在 Windows 开始菜单中搜索并打开 **PowerShell**，复制下面的命令。把目标路径
中的 `<lab_id>` 换成实际 Lab ID。目标文件夹必须尚不存在。

```powershell
powershell -ExecutionPolicy Bypass -File "E:\暑研\EMVR_Blind_BuilderPack\Tools\create_blind_run_pack.ps1" -Destination "E:\暑研\EMVR_Blind_Run_<lab_id>"
```

例如：

```powershell
powershell -ExecutionPolicy Bypass -File "E:\暑研\EMVR_Blind_BuilderPack\Tools\create_blind_run_pack.ps1" -Destination "E:\暑研\EMVR_Blind_Run_lab2_magnetostatics"
```

成功时 PowerShell 会显示：

```text
[EMVR Builder Boundary] PASS
[EMVR Blind Run Pack] CREATED
```

如果显示 `FAIL` 或红色错误，不要调用 Lab Builder Agent，也不要自己改工作流；
把完整错误发给工作流维护者。

### 第三步：把运行副本作为 AI 工作区

在 Codex 中选择打开文件夹，打开刚生成的目录，例如：

```text
E:\暑研\EMVR_Blind_Run_lab2_magnetostatics
```

不要打开它的父目录 `E:\暑研`，也不要打开主目录
`E:\暑研\EMVR_Blind_BuilderPack` 作为本次 Lab 的构建工作区。

## 5. 调用 AI 的格式

打开运行副本后，把需求文档上传到当前对话，然后发送以下提示词：

```text
$build-unity-vr-lab-blind

请根据我在当前对话中上传的需求文档重建这个 Unity VR Lab。
Lab ID：<lab_id>
Lab 名称：<实验名称>

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
Lab ID：lab2_magnetostatics
Lab 名称：Magnetostatics

请从 Gate 0 开始，严格一次对话只完成一个 Gate。完成当前 Gate 后汇报结果并
等待我审批。需要我操作 Unity 时，请给出完整菜单路径、点击顺序、通过标准和
失败截图要求，不要假设我使用过 Unity。
```

如果 AI 尚未初始化运行，它会执行类似下面的命令：

```powershell
python Tools/labflow/labflow.py new --lab-id <lab_id> --title "<实验名称>" --domain <领域名称> --scene Assets/Scenes/<场景名称>.unity
```

这些命令通常由 AI 执行，用户不需要手动输入。

## 6. 每个 Gate 的作用

### Gate 0：边界与历史经验检查

目的：确认这是安全、干净且可用于单次重建的 Builder Pack。

AI 会：

- 运行 Builder Boundary；
- 检查 Common、本地 Unity 包路径、MCP 禁用状态和单运行边界；
- 阅读允许使用的跨 Lab 构建经验；
- 禁止读取受限旧 Lab 总结；
- 填写当前 Lab 的经验审阅记录。

用户需要：检查 AI 的 PASS 证据。如果无问题，回复“继续”或同义表达。

### Gate 1：确认实验 Brief

目的：把用户文档转化成完整、无歧义的实验需求。

AI 会整理：

- 学习目标与实验任务；
- 公式、单位、参数范围和预期结果；
- 实验对象、初始状态和第一步操作；
- 桌面、XR、重置、生命周期、遥测和 Assist 要求；
- 房间、UI、可见反馈与验收证据。

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

- 导入必须复用的房间 Prefab 及依赖；
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
- 一个桌面交互和对应 XR 适配；
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
- 房间、Common、领域对象和状态接线正确；
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

保留整个 `EMVR_Blind_Run_<lab_id>` 文件夹即可保存本次 Lab、规格、代码、场景、
测试和证据。下一次重建其他 Lab 时，应重新从主 Builder Pack 创建新的独立副本，
不要清空或复用这次运行目录。
