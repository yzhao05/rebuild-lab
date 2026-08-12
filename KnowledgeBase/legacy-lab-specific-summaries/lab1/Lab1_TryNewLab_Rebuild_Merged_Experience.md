# Lab1 TryNewLab Rebuild 合并经验总结

本文合并两部分经验：

- `rebuildlab1-2` 中关于 TryNewLab 构建方式的第二次经验。
- 本轮关于 Rebuild Lab1 观察区、拖动、充电、Step 3 和电场线调试的经验。

后续 agent 在 Summer26 项目中构建或修复 Lab1/TryNewLab 时，应优先阅读本文，再看源代码和 Unity 截图反馈。

## 1. Rebuild 的正确目标

TryNewLab/Rebuild Lab1 的目标不是证明 agent 能从零重写完整 Lab1，而是验证：

- 哪些 Summer26 代码必须复用。
- 哪些 Unity 场景、XR、交互、assist、step 系统可以作为 common 基础设施。
- 哪些内容可以由 JSON 接口和 agent 装配生成。
- 哪些问题必须进入知识库，供下一次构建前主动询问用户。

正确标准是：

- 复用 Summer26 房间、背景、灯光、XR setup 和 assist framework。
- 复用 Lab1 中稳定的核心物理代码，例如 `ElectricField.cs`、`Sphere.cs`、`Wire.cs`、`Plane.cs`。
- 新实验内容放在独立 adapter/runtime layer 中。
- 不优先手改 `.unity` YAML。
- Unity 编译和 Play Mode 测试由用户在可见 Editor 中完成。
- agent 根据 Console、Game view、Scene view 截图迭代。

## 2. 第二次经验中的关键原则

### 2.1 不要把 TryNewLab 当成 Lab1 复制品

第二次经验明确指出，TryNewLab 应该以 Summer26 项目为基础，搭建一个新的实验层，而不是完整复制旧 Lab1。

推荐流程：

1. 选定一个 Summer26 场景作为 environment donor。
2. 保留 room/background、lighting、XR origin、camera convention。
3. 添加新的实验根节点，例如 `NewLab_Generated`。
4. 将新对象、新步骤、新接口放在新 layer。
5. 用文档记录哪些是复用，哪些是新建。

### 2.2 先写接口，再做运行时接线

新 lab 应先准备五类 contract：

- lab spec
- objects
- steps
- rubric
- telemetry map

这些 JSON 不一定立即被 runtime 完整加载，但能约束 object id、step id、telemetry 和 assist grounding，避免 agent 在 Unity 中自由发挥。

### 2.3 不要依赖不可见 batchmode

Summer26/Unity 项目容易因为编译、Package、XR、Editor state 卡住。后续 agent 不应静默运行长时间 Unity batchmode。

应让用户在 Unity Editor 中完成：

- 编译。
- 菜单生成。
- Play Mode。
- 截图和 Console 反馈。

agent 应主动索要：

- Console 截图。
- Game view 截图。
- Scene view 截图。
- 当前是否处于 Play Mode。

## 3. 本轮新增经验：拖动、充电、观察区与 Step 3

### 3.1 拖动代码可复用

本轮确认右侧点电荷、线电荷、面电荷的拖动逻辑可以沉淀为可复用代码。

当前可复用脚本：

```text
Assets/Scripts/EMVR/NewLab/Lab1Summer26RayDragRuntime.cs
```

核心经验：

- 鼠标位置不能等同于 XR 激光中心。
- 优先使用 `XRRayInteractor` 的 ray 做 sphere cast。
- 鼠标只作为 debug fallback。
- 选择对象时从 collider 向父级查找：

```csharp
GetComponentInParent<Sphere>()
GetComponentInParent<Wire>()
GetComponentInParent<ChargedObjects.Plane>()
```

这能保证点、线、面三种电荷都能被选中和拖动。

### 3.2 充电逻辑要和选中对象绑定

本轮新增/使用：

```text
Assets/Scripts/EMVR/NewLab/Lab1Summer26ChargeWorkflowRuntime.cs
```

经验：

- 用户先用 controller ray 或鼠标左键选中点/线/面电荷。
- 再点击充电区，给当前选中对象修改 `electric_mag`。
- 不要只依赖 trigger，因为 rebuilt 场景中 collider、rigidbody 和父物体关系可能不同。
- 每次选择或充电时都应主动补齐 `ExperimentObject`，保证 assist、step、telemetry 能识别对象类型。

## 4. 本轮关键错误与修正

### 4.1 错误：把大门/门窗误认为观察区

用户指出：

- 最远处是大门。
- 浅蓝区域是大门的窗户。
- 不是观察区。

经验：

- 观察区必须是独立可见的实验区域。
- 不能仅凭颜色或远处透明区域猜测。
- Unity 视觉问题必须以用户 Game view 截图为准。

### 4.2 错误：重复创建 runtime observation zone

TryNewLab 场景中本来已有：

```text
NewLab_ObservationZone
```

之前额外创建：

```text
Lab1_Summer26_Runtime_Observation_Zone
```

会导致多个观察区、多个 `ElectricField`、多个 trigger/assist chain 并存，造成视觉和事件混乱。

最终策略：

- 优先查找并接管 `NewLab_ObservationZone`。
- 如果不存在，才创建 runtime fallback zone。

### 4.3 错误：只靠 Camera.main.forward 放置观察区

XR/Device Simulator 下 `Camera.main.forward` 不稳定，且 Game view 的视觉远近不一定符合 agent 对世界坐标的直觉。

本轮曾误把：

```text
x = -24 -> x = -21.6
```

判断为推远，实际用户反馈是拉近。

最终用户确认效果较好的位置：

```csharp
private static readonly Vector3 TryNewLabObservationPosition = new Vector3(-26.2f, 1.25f, 4f);
private static readonly Vector3 TryNewLabObservationScale = new Vector3(2.6f, 1.9f, 0.7f);
```

经验：

- 远近方向必须结合截图确认。
- 不要只凭坐标正负判断。
- 位置修改后应让 Console 打印实际坐标。

### 4.4 错误：只看脚本是否运行，不看 Step 事件链

Console 中出现：

```text
[Lab1Summer26ChargeWorkflowRuntime] selected Wire, q = 0
```

只说明 runtime 脚本在运行，不代表 Step 3 会推进。

Step 3 依赖：

```text
expectedAction = "enter_observation_zone"
```

因此必须确保已充电对象进入观察区时记录：

```csharp
InteractionEventLogger.Instance.LogAction("enter_observation_zone", experimentObject);
```

### 4.5 错误：只依赖 OnTriggerEnter

`ElectricField.OnTriggerEnter` 在原 Lab1 中可以触发对象加入观察区，但 rebuilt 场景里可能失败。

原因包括：

- collider/rigidbody 不匹配。
- 父子对象层级变化。
- 观察区位置不对。
- trigger 初始化顺序问题。
- 对象已经在区域内，未发生新的 enter 事件。

最终补了双保险：

1. `ElectricField.SyncOverlappingCharges()` 主动扫描观察区 bounds 内对象。
2. `Lab1Summer26ChargeWorkflowRuntime.ReportChargedObjectsInObservationZone()` 每帧检测已充电对象是否进入观察区。

## 5. 最终有效解决方案

### 5.1 观察区接管与显示

修改文件：

```text
Assets/Scripts/EMVR/NewLab/Lab1Summer26ChargeWorkflowRuntime.cs
Assets/Scripts/ElectricField.cs
```

最终行为：

- 接管 `NewLab_ObservationZone`。
- 临时脱离父物体，使用世界坐标定位：

```csharp
zone.transform.SetParent(null, true);
```

- 保留绿色半透明体积。
- 保留黄色边框。
- 添加小号 `OBSERVATION ZONE` 标签。

用户确认较好的视觉参数：

```csharp
material.color = new Color(0f, 1f, 0.25f, 0.28f);
label.localScale = new Vector3(0.055f, 0.055f, 0.055f);
text.characterSize = 0.16f;
text.fontSize = 72;
```

注意：之前标签文字曾左右反，需要：

```csharp
label.localRotation = Quaternion.Euler(0f, 180f, 0f);
```

### 5.2 Step 3 推进

`ElectricField.LogObservationEnter()` 需要：

- 只对已充电对象生效。
- 自动补齐 `ExperimentObject`。
- 上报 `enter_observation_zone`。

未充电对象不应推进 Step 3：

```csharp
if (!HasNonZeroCharge(target)) return;
```

runtime 主动检测也要跳过未充电对象：

```csharp
if (Mathf.Approximately(GetCharge(charge), 0f)) continue;
```

### 5.3 电场线显示

电场线不显示时，按顺序检查：

1. 观察区是否存在且可见。
2. 观察区 collider 是否 enabled 且 isTrigger。
3. 已充电对象是否真的进入观察区 bounds。
4. `enter_observation_zone` 是否上报。
5. `ElectricField.ChargedWires/ChargedPlanes/ChargedSpheres` 是否加入对象。
6. `Wire_density/Plane_density/Sphere_density` 是否非零。
7. `GetObservationBounds()` 是否返回正确观察区 bounds。
8. 点/线/面的 seed/reference transform 是否缺失。

## 6. 当前用户确认有效的状态

用户最终确认：

- 观察区已经出现。
- 透明度很好。
- 字体缩小是对的。
- 把观察区推到 `x=-26.2` 后距离效果很好。

这组参数应作为 TryNewLab Rebuild Lab1 的当前推荐默认值。

## 7. 后续 agent 排查清单

如果以后用户反馈“没有观察区”：

1. 看 Console 是否有：

```text
[Lab1Summer26ChargeWorkflowRuntime] ensured visible observation zone ...
```

2. 如果没有，先检查脚本是否编译通过、场景名是否命中。
3. 如果有，确认打印的 position/scale 是否合理。
4. 不要把门窗或大门当成观察区。
5. 确认场景里不是多个观察区叠加。

如果用户反馈“Step 3 卡住”：

1. 确认对象 `electric_mag != 0`。
2. 确认对象进入观察区 bounds。
3. 确认 `enter_observation_zone` 被上报。
4. 确认 `StepProgressManager` 当前 step 是 `step_3`。

如果用户反馈“没有电场线”：

1. 先查观察区和 Step 3。
2. 再查 `ElectricField` 对象列表。
3. 最后查物理计算、seed 点和 LineRenderer。

## 8. 构建新 Lab 时应提前询问用户

没有源代码核心的新 lab 不能保证一次生成成熟物理算法。agent 应先问：

- 是否有源代码核心可复用。
- 是否有实验公式、手册、论文或参考实现。
- 哪些对象必须可抓取、可拖动、可点击。
- 是否必须支持真实 VR 手柄。
- 鼠标 debug 如何映射到 VR 操作。
- 观察区、instruction、操作对象的视觉优先级。
- 是否先做 mock/近似物理原型，再逐步替换为可信算法。

推荐策略：

```text
接口和验收标准 -> 复用 common 场景和交互 -> 最小可运行原型 -> 用户 Unity 编译测试 -> 根据截图和 Console 迭代 -> 沉淀稳定代码
```
