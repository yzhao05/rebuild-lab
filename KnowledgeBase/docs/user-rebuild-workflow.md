# Unity VR Lab 盲重建工作流使用指南

本指南只说明当前 Gate 0–10 工作流。Skill 和
`.agents/skills/build-unity-vr-lab-blind/references/workflow-gates.md` 是最终
权威来源。

## 基本规则

- 一个 Builder Pack 只运行一个 Lab。新 Lab 必须使用干净的 Pack 副本。
- Agent 每条实质性回复只完成一个 Gate，完成后等待 User 审批。
- `继续`、`可以`、`下一步`、`通过`、`OK` 等明确推进意图都可视为审批；
  不要求固定回复格式。
- Agent 不读取 Gold、受限旧总结、旧 Agent bundle、旧示例合同或其他运行
  的 LabSpec/代码/证据。
- Unity MCP、Lab MCP 和对应依赖均禁用。
- User 上传到当前对话的准确文档路径属于允许输入；不得枚举其父目录。
- 工作流基础设施、Common、ApprovedAssets、Policies、Tools 和 KnowledgeBase
  在 Lab 重建期间只读。发现问题时 Agent 只汇报，由 User 修复。

## 创建或恢复运行

本节中的命令均由 Agent 执行，User 不需要打开 PowerShell 或手动复制 Pack。

盲重建、复刻或效果对比任务中，重建 Agent 必须先在主 Pack 内执行：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/create_blind_run_pack.ps1 -LabId <lab_id>
```

固定路径为
`RebuildWorkspaces/EMVR_Blind_Rebuild_<lab_id>`。Agent 核对 PASS 后给出该目录的
完整绝对路径；User 只负责把该子目录本身切换为 Agent workspace。不得要求
User 执行复制命令，也不得在主 Pack、父目录、其他名称目录或旧运行副本中
重建。

如果当前根目录已经是固定子工作区且 `.emvr-workspace.json` 与请求的 Lab ID
一致，Agent 直接初始化或恢复运行，不得再次调用主 Pack 复制工具。

切换到子工作区后，由 Agent 新建运行：

```powershell
python Tools/labflow/labflow.py new --mode blind-rebuild --lab-id <lab_id> --title "<title>" --domain <domain> --scene Assets/Scenes/<Output>.unity
```

查看状态：

```powershell
python Tools/labflow/labflow.py status --lab-id <lab_id>
```

如果 Pack 中已有运行，`new` 会拒绝覆盖。Agent 必须恢复匹配的现有运行或向
User 报告冲突，不能覆盖、删除，也不能让 User 负责运行复制命令。

如果 Builder Pack 已被集成到另一个项目，任务是开发一个真正的新 Lab 而不是
重建/对比旧 Lab，则不限制工作区必须位于 `RebuildWorkspaces`；使用
`labflow new --mode integrated-development`，其他 Gate 和复用规则仍然有效。

## 统一审批命令

每个 Gate 完成且 User 表达推进意图后，都使用同一命令记录原话：

```powershell
python Tools/labflow/labflow.py approve-gate --lab-id <lab_id> --gate <N> --note "<user wording>"
```

不得提前审批、跳 Gate 或由 Agent 代替 User 审批。

## Gate 0：边界与经验检查

Agent 运行 Builder Boundary，验证本地 UPM 路径、Common、MCP 禁用状态、
受限资料隔离和单运行边界；递归阅读
`KnowledgeBase/rebuild-lessons/` 中除 `README.md` 外的安全经验，并完成：

```text
LabSpecs/<lab_id>/prior-lessons-review.md
```

受限目录 `KnowledgeBase/legacy-lab-specific-summaries/` 不得读取。

边界脚本实际 PASS 后记录：

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --boundary-verifier passed --note "<observed PASS output>"
python Tools/labflow/labflow.py capture-gate0-baseline --lab-id <lab_id>
```

第二条命令也由 Agent 执行，只能在 Gate 0 成功创建一次。它把本次运行开始时的受保护文件状态保存到
`BuildEvidence/<lab_id>/gate0-protected-baseline.json`。Gate 1–10 会持续与该文件比较；Agent 不能通过重建基线或修改发布哈希来接受后续变化。

## Gate 1：Brief 确认

Agent 先从 User 上传的文档提取事实，再集中询问缺失的公式、单位、参数范围、
任务、预期结果、对象行为、桌面操作、初始状态、第一步可见变化、重置、
遥测、Assist、房间和验收证据。User 审批完整 Brief 后记录 Gate 1。

每个桌面核心交互默认由 Common/XRI 获得功能等价的 XR 路径，即同一对象、状态
变化、可见反馈和成功结果。Gate 1 不要求 User 设计普通鼠标到 XR 按键映射；
只有文档要求不同语义、User 主动要求例外或 Common 无法表达动作时才询问，并
在 Brief 的 `desktop_to_xr_mapping.exceptions` 中记录。

## Gate 2：五份合同

完成当前 Lab 的五份 canonical JSON：

- `lab_spec`
- `objects`
- `steps`
- `rubric`
- `telemetry_map`

校验：

```powershell
python Tools/labflow/labflow.py validate --lab-id <lab_id>
```

旧 `.example.json` 不在活动合同目录，也不能作为回退输入。

## Gate 3：设计与 API 审核

`design.md` 必须写明：

- Mandatory Common 复用矩阵及场景接线；
- 默认桌面/XR 等价路径及已确认例外（通常为无）；
- 每个 Common、Unity、XRI API 的实际版本、程序集和准确签名；
- 房间 Prefab、交互、状态、场景 Builder 失败策略和 Game View 布局；
- 初始状态、第一步操作和操作后的准确可见状态；
- 当前 Lab 的代码输出路径、测试与证据计划。

将 `Approval status` 改为 `READY FOR APPROVAL` 后提交 User 审批。

## Gate 5 执行的 ApprovedAssets 导入

`ApprovedAssets/` 位于 Unity import root 之外，不能直接实例化。使用固定工具
把选定资产及其 ApprovedAssets GUID 依赖闭包复制到当前 Lab 输出。Gate 5
必须依次导入房间、完整 XR Interaction Setup 和 XR Device Simulator：

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py --lab-id <lab_id> --source "EMVRRoom/Prefabs/Room_Big_Part_01.prefab"
python .agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py --lab-id <lab_id> --source "XRInteractionToolkitSamples/Starter Assets/Prefabs/XR Interaction Setup.prefab"
python .agents/skills/build-unity-vr-lab-blind/scripts/import_approved_asset_closure.py --lab-id <lab_id> --source "XRInteractionToolkitSamples/XR Device Simulator/XR Device Simulator.prefab"
```

目标固定为：

```text
UnityProject/Assets/Generated/<lab_id>/ApprovedDependencies/
```

导入 manifest 固定保存到
`BuildEvidence/<lab_id>/approved-asset-imports/`。不得在运行中写入共享的
`UnityProject/Assets/ApprovedReuse/`。Gate 1 和 Gate 3 只记录选择与路径；必须
等 Gate 4 审批完成、进入 Gate 5 后才执行上述导入命令。

## Gate 4：纯脚手架编译

生成：

```powershell
python Tools/labflow/labflow.py scaffold --lab-id <lab_id>
```

脚手架只包含 runtime/editor 结构，不调用未经 Gate 3 审核的 Common 或领域
API。Agent 必须把 User 当作第一次使用 Unity，直接给出完整点击步骤，不能只写
“Clear Console”：确认顶部中央三角形 Play 按钮为灰色，等待右下角编译转圈消失，
通过 `Window > General > Console` 打开 Console（通常停靠在底部），点击 Console
左上角 **Clear**，再点顶部菜单 `Assets > Refresh`，等待编译结束。成功标准是
Console 中红色编译错误为 0；失败时应让 User 选中第一条红色错误并截取完整
Console。然后记录：

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --scaffold-compile passed --note "<observed evidence>"
```

## Gate 5：最小领域切片

先生成并验证受保护的 Gate 5 基础层：

```powershell
python .agents/skills/build-unity-vr-lab-blind/scripts/materialize_gate5_foundation.py --lab-id <lab_id>
```

该工具将 `ReusableFoundation/Gate5MinimalSlice/Source/` 原样复制到
`UnityProject/Assets/Generated/<lab_id>/Gate5Foundation/`。源文件与生成副本都
不允许 Builder Agent 修改；Agent 只能在其他当前 Lab 路径中调用或组合它们。

在 Unity 中打开 `Window > General > Test Runner`，点击 **EditMode**，运行精确
程序集 `EMVR.ReusableFoundation.Gate5.Tests` 的 3 个测试；再检查中性按钮能生成
可见且有 Collider 的物体、桌面拖拽和重置有效、Console 干净。领域代码还未加入
之前先记录：

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --gate5-foundation passed --note "<3/3 tests and neutral-scene evidence>"
```

只实现一个端到端切片：一个领域对象/结果、一个桌面交互、一个状态/重置路径、
一个遥测事件和一个 Assist grounding 结果。要求运行 EditMode 测试时，Agent
必须先检查当前测试 `.asmdef` 和 `[Test]` 方法，再把精确程序集名、测试名和数量
写入指令。Unity 2022.3 的 Test Runner 主路径是顶部菜单
`Window > General > Test Runner`；窗口通常停靠在底部 Project/Console 区域。
User 点击窗口顶部 **EditMode**，在右上角搜索精确程序集，展开测试树，选中程序集
或测试组，然后点击工具栏 **Run Selected**。成功标准是所有指定测试显示绿色、
Passed 数量等于预期且 Failed 为 0。失败时附 Test Runner 和 Console 两张截图。
此步骤不要进入 Play Mode，也不要运行场景 Builder。通过测试和干净编译后记录：

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --domain-slice passed --domain-compile passed --note "<observed evidence>"
```

加入领域代码后，前一次基础层观察会自动过期。对当前最终代码重新运行领域测试、
干净 Console、上述 3 个基础测试和中性场景检查；在不再修改文件的情况下，再次
单独记录 `--gate5-foundation passed`。只有基础与领域证据摘要都对应同一份当前
代码时才能审批 Gate 5。当前 Gate 有编译错误时不得继续添加代码。

## Gate 6：场景构建与 Game View

Agent 必须给出当前 Lab 的完整 Builder 菜单名，例如
`Tools > EMVR > Labs > Build <Lab Title>`，而不是只说“运行 Builder”。User 从
Unity 顶部菜单逐级点击该路径，然后点击
`Tools > EMVR > Common > Validate Active Scene`。Game View 位于中央 **Scene**
旁的 **Game** 标签；若没有该标签，用 `Window > General > Game` 打开。进入
Play Mode 时点击顶部中央三角形按钮并确认变蓝，退出时再次点击并确认变灰。
检查：

- Console 清空后编译无错误；
- Builder 遇到 Error/Exception/Assert 会停止且不保存部分场景；
- 既有场景只有包含当前运行 marker root 才能重开；
- Mandatory room、XR Setup/Simulator、Common、对象和状态接线正确；
- Game View UI 可读、不遮挡实验区；
- 初始状态和第一步后的可见状态符合 Brief。

记录：

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --compile passed --scene-built passed --note "<observed evidence>"
```

## Gate 7：交互与生命周期

分别测试桌面、XR Device Simulator、真实手柄（可明确延期）、Capture/Restore/
Reset 和 Start/Lab/Back。记录：

```powershell
python Tools/labflow/labflow.py record --lab-id <lab_id> --playtest passed --note "<observed evidence>"
```

## Gate 8：验收资料

完善当前 Lab 专属文件，不能复用其他运行的报告：

```text
BuildEvidence/<lab_id>/reuse-report.md
BuildEvidence/<lab_id>/validation-report.md
BuildEvidence/<lab_id>/approved-asset-imports/*.json
```

移除所有 `pending`/`TODO`，附上真实 Console、Game View、交互和延期证据。

## Gate 9：User 最终验收

Agent 展示完整结果和延期项，由 User 决定是否接受。提交 Gate 9 时必须提醒：
审批后还会执行 Gate 10 经验归纳，Gate 9 不是流程终点。

## Gate 10：复盘与安全知识归纳

Gate 9 审批后运行：

```powershell
python Tools/labflow/labflow.py retrospective --lab-id <lab_id>
```

完善：

```text
BuildEvidence/retrospectives/<lab_id>.md
KnowledgeBase/rebuild-lessons/<lab_id>.md
```

第一份保存本轮完整时间线；第二份只保留跨 Lab、经过验证的错误模式、API/
生命周期约束和预防检查。不得写入旧/当前 Lab 答案、坐标、类名、场景接线、
大段代码、隐私路径或未经验证的猜测。User 审批 Gate 10 后流程完成。
