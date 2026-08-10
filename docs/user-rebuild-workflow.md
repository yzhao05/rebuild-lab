# Unity VR Lab Rebuild 工作流使用指南

## 一、工作流概况

这套工作流用于使用 Agent rebuild 现有 Unity VR Lab，或以已有 Lab 为参考开发新 Lab。它的目标不是让 Agent 直接一次性生成所有代码，而是把需求、实验设计、代码生成和 Unity 测试拆成可检查、可回退、可记录的阶段。

完整流程如下：

```text
创建工作流记录
    ↓
填写 Lab Brief
    ↓
完善五份接口契约
    ↓
契约自动校验
    ↓
场景与交互设计审核
    ↓
生成 Unity 代码骨架
    ↓
实现最小可运行实验
    ↓
Unity 编译与场景生成
    ↓
Play Mode 测试
    ↓
记录证据并验收
```

对应的状态变化为：

```text
brief_ready
→ contracts_ready
→ design_approved
→ scaffold_ready
→ unity_compiled
→ scene_built
→ playtest_passed
→ accepted
```

### 工作流解决什么问题

- 防止 Agent 在实验需求不明确时直接编写大量代码。
- 防止场景对象、实验步骤、评分标准和遥测事件互相不一致。
- 防止新 Lab 重复实现 `com.emvr.lab-common` 已经提供的公共功能。
- 防止直接修改 `.unity` YAML 或生成一个丢失原有 XR Rig、灯光和房间的空场景。
- 明确区分“代码已生成”“Unity 已编译”和“实验已经通过测试”。
- 为每轮 rebuild 留下需求、设计、测试结果和问题记录，方便比较不同迭代。

### 三类参与者

| 参与者 | 主要职责 |
| --- | --- |
| User | 提供实验材料和目标、审核设计、操作 Unity、完成体验测试并确认验收 |
| Agent | 整理需求、生成和校验契约、设计方案、实现代码、分析错误、维护工作流状态 |
| Unity Editor | 编译 C#、运行场景 Builder、执行场景验证、运行 Play Mode |

### Agent 的固定回复格式

进入本工作流后，Agent 每次进行实质性回复时都应先显示：

```text
当前阶段：<workflow stage>
本阶段目的：<本阶段为什么存在>
已完成：<已经验证的文件或证据>
待完成/阻塞：<尚缺内容>
需要 User：<下一步需要提供的信息、批准或 Unity 操作>
```

一个关卡完成后，Agent 应报告新的阶段并暂停，等待 User 回复“继续”，再修改下一阶段的文件。

### 每轮 rebuild 的主要产出

每个 Lab 最终会形成以下内容：

```text
LabSpecs/<lab_id>/
├── brief.yaml              # 实验需求
├── design.md               # 场景与交互设计
├── workflow_state.json     # 当前阶段和测试证据
└── implementation.md       # Unity 操作说明

LabInterfaceContracts/
├── <lab_id>_lab_spec.json
├── <lab_id>_objects.json
├── <lab_id>_steps.json
├── <lab_id>_rubric.json
└── <lab_id>_telemetry_map.json

Assets/Scripts/EMVR/Labs/<LabName>/
└── <LabName>Controller.cs

Assets/Editor/<LabName>/
└── <LabName>SceneBuilder.cs

evals/
└── 本轮 rebuild 的测试与验收记录
```

### 开始前的基本规则

- 所有命令都应在 Unity 项目根目录运行。
- 使用 `$build-unity-vr-lab` 让 Agent 按本工作流工作。
- 不手动编辑 `.unity` 场景 YAML。
- 不使用 Unity batchmode 代替用户在 Editor 中的编译和 Play Mode 测试。
- 优先复用 `com.emvr.lab-common`，Lab 文件夹只保存领域公式、实验参数和特有交互。
- 除非设计明确要求替换，否则保留 donor scene 的房间、灯光、相机、XR Rig 和导航配置。
- 任何阶段失败时停留在当前阶段，修复并重新验证，不应跳过关卡。

---

## 二、具体步骤

## 步骤 1：创建本轮 Lab 工作流

### 目的

为本轮 rebuild 创建唯一的 `lab_id`，确定 donor scene、目标场景、实验名称和领域，并生成后续工作所需的目录与初始文件。

### 操作

在项目根目录运行：

```powershell
python tools/labflow/labflow.py new `
  --lab-id <lab_id> `
  --title "<Lab 名称>" `
  --domain <实验领域> `
  --scene Assets/Scenes/<目标场景>.unity `
  --donor-scene Assets/Scenes/<参考场景>.unity
```

例如：

```powershell
python tools/labflow/labflow.py new `
  --lab-id rebuild_lab1_v3 `
  --title "Lab 1 Rebuild V3" `
  --domain electrostatics `
  --scene Assets/Scenes/Rebuild_Lab1_V3.unity `
  --donor-scene Assets/Scenes/Static_Electric_Field.unity
```

如果让 Agent 执行，可以输入：

```text
使用 $build-unity-vr-lab 创建 rebuild_lab1_v3。
目标场景是 Assets/Scenes/Rebuild_Lab1_V3.unity，
donor scene 是 Assets/Scenes/Static_Electric_Field.unity。
```

### 产出

- `LabSpecs/<lab_id>/brief.yaml`
- `LabSpecs/<lab_id>/design.md`
- `LabSpecs/<lab_id>/workflow_state.json`
- 五份带占位内容的 JSON 契约
- 工作流状态：`brief_ready`

### 完成条件

`status` 命令能够识别该 `lab_id`，并提示下一步填写 Brief 和契约。

---

## 步骤 2：填写 Lab Brief

### 目的

把用户脑中的实验要求转化为结构化需求。Brief 决定后续设计和代码实现的边界，是 Agent 生成 Lab 时最重要的输入之一。

### User 需要提供的信息

- 实验名称和所属领域。
- 教材、实验手册、截图或已有 Lab 参考资料。
- 学习目标。
- 学生需要完成的操作步骤。
- 公式、单位、参数范围和物理假设。
- 预设参数及其预期结果。
- 需要出现的实验对象。
- 鼠标、XR Device Simulator 和真实 VR 的交互要求。
- 需要保留的 donor scene 内容。
- 判断实验成功的标准及所需截图。

如果某项还没有确定，应明确告诉 Agent“暂未决定”，不要让 Agent 靠猜测写入正式需求。

### Agent 操作

Agent 将这些信息整理到：

```text
LabSpecs/<lab_id>/brief.yaml
```

并清除其中所有 `Replace with...` 或 `replace_with...` 占位内容。

Agent 必须先读取已有材料，再按每次最多三个相关问题分批访谈 User。Agent 不应要求 User 亲自编辑 YAML；User 回答和确认实验要求，Agent 负责更新文件。所有内容应标记为已确认、需要确认的推断或尚未解决，不得用 Agent 自行猜测的实验要求消除占位内容。

### 产出

- 一份可追踪的实验需求说明。
- 明确的场景、对象、步骤、交互和验收边界。
- 后续五份契约的内容来源。

### 完成条件

User 能够通过 Brief 回答以下问题：

1. 学生在这个 Lab 中要学会什么？
2. 学生要操作哪些对象？
3. 正确的实验结果是什么？
4. 如何通过画面或数据确认任务完成？
5. 桌面和 VR 模式分别如何完成核心流程？

Agent 还需要向 User 展示 Brief 摘要，并获得明确确认，之后才能生成最终 contracts。

User 明确确认完整 Brief 后，由 Agent 记录确认：

```powershell
python tools/labflow/labflow.py confirm-brief `
  --lab-id <lab_id> `
  --note "User confirmed the complete Brief summary."
```

如果没有这条确认记录，后续 `validate` 不会把工作流推进到 `contracts_ready`。

---

## 步骤 3：完善五份接口契约

### 目的

把 Lab 拆分成 Agent、Unity 代码、AI Assist 和研究数据都能共同理解的稳定接口，避免不同模块使用不同的对象名称、步骤编号或事件名称。

### 操作与文件

Agent 根据 Brief 完善以下文件：

| 文件 | 内容 | 主要用途 |
| --- | --- | --- |
| `<lab_id>_lab_spec.json` | Lab 名称、领域、场景、学习目标、入口步骤 | 定义 Lab 总体身份和入口 |
| `<lab_id>_objects.json` | 对象 ID、类型、属性、Unity 名称提示 | 统一场景对象和实验对象身份 |
| `<lab_id>_steps.json` | 实验步骤、学生目标、预期动作、成功条件 | 定义完整学习流程 |
| `<lab_id>_rubric.json` | 每一步的掌握标准和常见误解 | 支持评估与 AI Assist |
| `<lab_id>_telemetry_map.json` | 事件名称和必填字段 | 统一研究日志和行为记录 |

文件位于：

```text
LabInterfaceContracts/
```

### 产出

- 五份相互关联的 canonical JSON contracts。
- 稳定的对象 ID、步骤 ID、评分标准和遥测事件名称。
- Agent 后续设计和实现代码时使用的正式接口。

### 完成条件

- 所有生成的占位内容都已替换。
- 每个步骤引用的对象真实存在于 objects contract。
- 入口步骤真实存在。
- 每个 rubric 引用的步骤真实存在。
- 实验关键动作具有对应 telemetry event。

---

## 步骤 4：运行契约校验

### 目的

在编写 Unity C# 之前自动发现需求层面的错误。此步骤通过只代表契约内部一致，不代表 Unity Lab 已经完成。

### 操作

```powershell
python tools/labflow/labflow.py validate --lab-id <lab_id>
```

校验器会检查：

- 五份 JSON 是否存在且能够解析。
- 五份文件的 `lab_id` 是否一致。
- 是否还存在生成时的占位内容。
- 对象 ID 和步骤 ID 是否唯一。
- 入口步骤是否存在。
- 步骤引用的对象是否存在。
- rubric 引用的步骤是否存在。
- 基础 AI Assist 和 telemetry event 是否齐全。

### 处理结果

- `ERROR`：必须修复，不能进入下一阶段。
- `WARNING`：需要检查并说明是否接受。
- `PASSED`：可以开始场景与交互设计。

### 产出

- 契约校验结果。
- 更新后的 `workflow_state.json`。
- 工作流状态：`contracts_ready`。

### 完成条件

校验结果为 `0 errors`。

---

## 步骤 5：设计场景与交互方案

### 目的

在生成 C# 和修改场景之前，让 User 确认最终体验将如何工作，减少“代码可以运行，但实验体验不符合预期”的返工。

### Agent 操作

Agent 在以下文件中完成设计：

```text
LabSpecs/<lab_id>/design.md
```

设计至少应说明：

- 学生从看到说明到获得实验结果的最短路径。
- donor scene 中需要保留的对象。
- 需要生成或修改的对象。
- Instruction、Experiment、Status、Result 四个可见区域。
- 鼠标操作方式。
- XR Device Simulator 和真实 VR 操作方式。
- 每个重要操作后的可见反馈。
- 公式、参数范围、预设和预期结果。
- 初始状态、完成状态和错误状态。
- 测试截图及 Console 证据要求。

### User 操作

检查设计是否符合实验目标。尤其确认：

- 对象位置和实验流程是否合理。
- 是否保留了需要的旧场景内容。
- 核心任务是否能用鼠标完成。
- VR 操作是否与鼠标操作表达同一个实验行为。
- 实验结果是否足够清晰、可观察。

如果不满意，直接提出修改意见。此时还没有生成正式场景，修改成本最低。

### 产出

- User 审核过的 `design.md`。
- 明确的场景和交互实现方案。

### 完成条件

User 明确表示批准该设计。沉默或契约校验通过都不等于设计批准。

---

## 步骤 6：记录设计批准

### 目的

把 User 的设计确认变成机器可读的工作流关卡，防止 Agent 未经确认直接生成实现。

### 操作

先将 `design.md` 中：

```text
Approval status: NOT APPROVED
```

修改为：

```text
Approval status: APPROVED
```

然后运行：

```powershell
python tools/labflow/labflow.py approve `
  --lab-id <lab_id> `
  --note "User approved the scene and interaction design."
```

### 产出

- 设计批准时间和说明被写入 `workflow_state.json`。
- 工作流状态：`design_approved`。

### 完成条件

`status` 显示：

```text
design_approved: true
```

---

## 步骤 7：生成 Unity 实现骨架

### 目的

创建统一、可检查的 Lab 代码入口和 Editor Scene Builder，避免 Agent 直接写场景 YAML 或从空场景重新搭建公共环境。

### 操作

```powershell
python tools/labflow/labflow.py scaffold --lab-id <lab_id>
```

如果契约未通过或设计没有批准，该命令会拒绝执行。

### 生成内容

```text
Assets/Scripts/EMVR/Labs/<LabName>/<LabName>Controller.cs
Assets/Editor/<LabName>/<LabName>SceneBuilder.cs
LabSpecs/<lab_id>/implementation.md
```

Scene Builder 的基础行为为：

1. 检查 Unity 是否处于 Play Mode。
2. 检查 donor scene 是否存在。
3. 在目标场景不存在时复制 donor scene。
4. 打开目标场景。
5. 创建 Lab 的 Generated Root。
6. 添加 Lab Controller。
7. 保存场景。

### 产出

- Lab 专用运行时代码入口。
- donor-scene-based Editor Builder。
- Unity 内下一步操作说明。
- 工作流状态：`scaffold_ready`。

### 完成条件

上述文件生成成功，并显示对应的 Unity Builder 菜单路径。

注意：scaffold 只是安全骨架，不是完整实验，也不代表 C# 已经在 Unity 中编译通过。

---

## 步骤 8：实现最小可运行实验

### 目的

先实现一条可以完整测试的核心实验路径，用较小范围验证设计、公共组件和工作流，再增加高级功能。

### Agent 操作

Agent 根据批准后的 `design.md` 和五份 contracts：

- 在 Lab 专用目录中实现公式、参数和领域规则。
- 扩展 Scene Builder，生成实验对象和 UI。
- 使用 `com.emvr.lab-common` 的公共状态显示、交互、遥测、Assist grounding 和验证组件。
- 确保核心任务可以使用桌面鼠标完成。
- 为 XR 交互使用相同的对象身份和实验规则。
- 为重要操作提供可见状态或结果反馈。
- 保持 exactly one active `AudioListener`，并避免重复 `EventSystem`。

推荐最小实现顺序：

```text
实验对象可见
→ 参数或预设可设置
→ 核心交互可执行
→ 公式结果可计算
→ 结果与状态可见
→ reset/repeat 可用
→ telemetry 可记录
```

### 产出

- Lab 特有的领域代码。
- 扩展后的 Scene Builder。
- 可以交给 Unity 编译和生成场景的最小实验版本。

### 完成条件

Agent 完成代码级检查，并明确告诉 User 接下来需要在 Unity 中执行哪些操作。此阶段不能由 Agent 自行宣布 Playtest 通过。

---

## 步骤 9：在 Unity 中编译代码

### 目的

确认生成和实现的 C# 与当前 Unity 版本、包依赖和项目代码兼容。

### User 操作

1. 返回 Unity Editor。
2. 等待右下角编译或资源导入完成。
3. 打开 `Window > General > Console`。
4. 点击 `Clear`。
5. 观察是否出现新的红色错误。

没有出现关于 package 或 Lab 代码的错误，通常表示相关代码已成功导入和编译。Console 中没有 package 的专门成功消息是正常现象。

### 记录命令

编译通过：

```powershell
python tools/labflow/labflow.py record `
  --lab-id <lab_id> `
  --compile passed `
  --note "Unity compilation completed with no relevant Console errors after Clear."
```

编译失败：

```powershell
python tools/labflow/labflow.py record `
  --lab-id <lab_id> `
  --compile failed `
  --note "Paste or summarize the first relevant Console error here."
```

### 产出

- Unity 编译结果和 Console 证据。
- 编译通过时，工作流状态：`unity_compiled`。

### 完成条件

清空 Console 后没有与本 Lab 或 `com.emvr.lab-common` 有关的编译错误。

---

## 步骤 10：生成并验证目标场景

### 目的

通过 Editor Builder 将代码定义的设计实际写入目标场景，同时保留 donor scene 中的公共环境和 XR 配置。

### User 操作

1. 确保 Unity 不在 Play Mode。
2. 在 Unity 顶部菜单运行：

   ```text
   Tools > EMVR > Labs > Build <Lab Name>
   ```

3. 等待场景生成和保存完成。
4. 运行：

   ```text
   Tools > EMVR > Common > Validate Active Scene
   ```

5. 检查 Scene View、Hierarchy 和 Console。

### 重点检查

- 打开的是否是正确目标场景。
- donor scene 的房间、灯光、相机和 XR Rig 是否保留。
- Generated Root 和实验对象是否存在。
- 是否只有一个 active `AudioListener`。
- 是否没有重复 EventSystem。
- 场景验证器是否报告阻塞问题。

### 记录命令

```powershell
python tools/labflow/labflow.py record `
  --lab-id <lab_id> `
  --scene-built passed `
  --note "Builder completed and active-scene validation has no blocking issues."
```

### 产出

- 目标 `.unity` 场景。
- 场景验证结果。
- Scene View 或 Hierarchy 证据。
- 工作流状态：`scene_built`。

### 完成条件

Builder 正常完成，目标场景正确打开，并且场景验证没有阻塞问题。

---

## 步骤 11：运行 Play Mode 测试

### 目的

验证学生实际看到和操作的 Lab 是否符合设计，而不仅仅是场景对象存在或代码能够编译。

### User 操作

先执行桌面测试，再执行 XR 测试。

#### 桌面核心流程

1. 点击 Play。
2. 确认 Instruction、Experiment、Status、Result 区域可见。
3. 按照实验说明完成核心任务。
4. 确认每个重要操作都有可见反馈。
5. 对比实际结果与 Brief 中的预期结果。
6. 测试 reset 和重复实验。

#### XR 流程

1. 使用 XR Device Simulator 或真实设备进入实验。
2. 确认射线、选择、抓取和移动行为正常。
3. 使用与桌面模式相同的核心对象完成实验。
4. 检查 UI、状态和实验结果在 VR 中是否清晰可见。

### 必需证据

- 初始状态 Game View。
- 核心任务完成后的 Game View。
- 执行关键交互后的状态或结果显示。
- 点击 Clear 后的 Console。
- 如果测试 XR，保存 XR 视角或操作结果。

### 记录命令

测试通过：

```powershell
python tools/labflow/labflow.py record `
  --lab-id <lab_id> `
  --playtest passed `
  --note "Desktop core flow passed; XR result and evidence recorded."
```

测试失败：

```powershell
python tools/labflow/labflow.py record `
  --lab-id <lab_id> `
  --playtest failed `
  --note "Describe the exact action, expected result, actual result, and Console message."
```

### 产出

- Play Mode 测试结果。
- Game View 和 Console 证据。
- 可复现的问题描述。
- 测试通过时，工作流状态：`playtest_passed`。

### 完成条件

核心实验可以从头到尾完成，结果正确、反馈清晰、无阻塞错误，并且所需证据已经保存。

---

## 步骤 12：完成 Eval 和 User 验收

### 目的

判断本轮 rebuild 是否真正达到了实验目标，并留下可用于下一轮迭代的记录。`playtest_passed` 不会自动变成 `accepted`。

### 操作

根据以下模板创建本轮 Eval：

```text
evals/templates/rebuild-eval.template.json
```

记录：

- 本轮 rebuild 的目标。
- 使用的 donor scene 和目标场景。
- 契约校验结果。
- 设计批准说明。
- Unity 编译结果。
- 场景验证结果。
- 桌面和 XR Playtest 结果。
- 截图或其他证据路径。
- 已知问题和下一轮改进项。

User 最后确认：

- 实验内容是否正确。
- 学习目标是否真正可完成。
- 场景和交互是否达到预期。
- 是否接受本轮结果，或进入下一次 rebuild。

### 产出

- 本轮 rebuild 的 Eval 记录。
- User 的验收结论。
- 下一轮 rebuild 的具体输入，而不是笼统的“继续优化”。
- User 验收结论：`accepted`。该结论记录在 Eval 中，当前 CLI 不会自动把工作流提升为 `accepted`。

### 完成条件

User 明确接受本轮 Lab，并且 Eval、证据和已知问题均已记录。

---

## 三、查看当前进度

任何时候都可以运行：

```powershell
python tools/labflow/labflow.py status --lab-id <lab_id>
```

输出会显示：

- 当前工作流阶段。
- contracts 是否通过。
- design 是否批准。
- compile、scene-built 和 playtest 证据状态。
- 当前应该执行的下一步。

当不确定接下来做什么时，不要自行跳到后续阶段，先运行 `status`，再让 Agent 只完成其中显示的 `next_action`。

## 四、推荐的 Agent 指令

开始新一轮 rebuild 后，可以直接对 Agent 说：

```text
使用 $build-unity-vr-lab 继续 <lab_id>。
先读取 workflow_state.json 并告诉我当前阶段。
只完成当前阶段要求的内容，不跳过契约验证和设计批准。
需要我在 Unity 中操作时，给我明确的菜单路径、测试动作和预期结果。
```

如果 Unity 测试失败，可以说：

```text
使用 $build-unity-vr-lab 诊断 <lab_id> 的这次测试失败。
操作步骤是：<实际步骤>。
预期结果是：<预期结果>。
实际结果是：<实际结果>。
Console 第一条相关错误是：<错误内容>。
请先分析原因，不要跳过当前工作流阶段。
```
