# EMVR Assist Layer

Unity 端与 Dify Workflow 的适配层，按 `Plan/unity_dify_adaptation_plan.md` 规范实现。

## 目录结构

```
EMVR/Assist/
├─ Core/        会话、步骤、配置、Payload 组装器
├─ Data/        数据模型（POCO）
├─ Network/     Gateway HTTP 客户端
├─ UI/          学生输入、AI 回复、高亮
├─ Objects/     ExperimentObject、Registry、SelectionTracker
├─ Logging/     InteractionEventLogger
├─ Validation/  LocalValidationManager（diagnostic_hints）
├─ Proactive/   BehaviorMonitor（idle/repeat/deviation 检测）
└─ Outcome/     OutcomeTracker（干预后结果追踪）
```

## 与 Dify 字段对照

发送字段（与 Dify Start 节点一致）：

```
session_id, user_id, lab_id, step_id, mode,
student_input_text,
selected_object_json, objects_json, recent_actions_json,
diagnostic_hints_json, trigger_reason_json,
step_rubric_json, session_memory_json
```

接收字段：先解析外层 `{"unity_output": {...}}`，再处理 `action_type`：
- `teaching_intervention` / `fallback_response` → 显示 subtitle / voice，处理 highlight，启动 outcome
- `no_action` → 静默返回

## 一次性搭建步骤（在 Static_Electric_Field 场景）

1. 创建空 GameObject `AI_Assist_System`，挂下列组件：
   - `LabSessionManager`
   - `StepProgressManager`
   - `AssistPayloadBuilder`
   - `AssistGatewayClient`
   - `AssistResponseExecutor`
   - `AssistUIController`
   - `SelectionTracker`
   - `InteractionEventLogger`
   - `LocalValidationManager`
   - `BehaviorMonitor`（可选，proactive）
   - `OutcomeTracker`（可选）

2. Project 视图右键 → Create → EMVR → Assist Config，得到 `AssistConfig.asset`，填：
   - `gatewayUrl`：Gateway 模式填 `http://127.0.0.1:8008/v1/assist`；直连 Dify 时勾选 `useDifyDirectMode` 并填 `difyApiKey` + workflow API URL
   - `defaultUserId`、`defaultLabId`、`defaultStepId`
   - `outcomeUrl`：留空则只在 Console 输出

3. 把 `AssistConfig.asset` 拖到 `LabSessionManager.config` 和 `AssistGatewayClient.config`。

4. 把 UI 拖入：
   - `AssistUIController.tmpInputField` 或 `uguiInputField`
   - `AssistUIController.sendButton`
   - `AssistResponseExecutor.tmpReplyText` 或 `uguiReplyText`

5. （阶段 2+）给场景中关键对象挂 `ExperimentObject`：
   - 充电小球（已有 `ChargedObjects.Sphere`）：勾选 `autoSyncChargeFromChargedObject`，设置 `objectId="charge_01"`，`objectType=Sphere`
   - 同理挂 Wire / Plane / 充电区 / 观察区
   - `ExperimentObject` 在 Awake 时自动注册到 Registry

6. （阶段 3+）在 Wire/Plane/Sphere 的抓取/释放脚本里调用：
   - `SelectionTracker.Instance.SetSelected(experimentObject)`
   - `InteractionEventLogger.Instance.LogAction("grab_object", experimentObject)` 等

7. （阶段 4）在 `StepProgressManager` Inspector 中填 `LabStep` 列表，每个 step 配 `expectedAction`：
   - `E1` → `place_object_into_charge_zone`
   - `E2` → `place_charged_object_into_observation_zone`
   - `E3` → `observe_field_lines`

## 阶段验收

| 阶段 | 验收点 |
|---|---|
| 1 | 输入 → 发送 → Console 打印 request JSON → AI subtitle 显示 |
| 2 | `objects_json` 不再为 `[]`，包含挂了 ExperimentObject 的对象 |
| 3 | 抓取对象时 `selected_object_json` 非空，`recent_actions_json` 有事件 |
| 4 | 错误操作时 `diagnostic_hints_json.sequence_deviation=true` |
| 5 | 静止 50s+ 或重复 4 次相同操作时自动发出 `mode=proactive` |
| 6 | 收到 `highlight_objects` 时对象自发光，干预后 outcome 写入 |

## 注意事项

- 不要把 Dify API Key 提交到 Git。`AssistConfig.difyApiKey` 字段建议只在本地 Inspector 里填。
- `DifyDirectMode` 仅供临时调试。正式部署用 Gateway 模式。
- `selected_object_json` 无对象时发送 `"{}"`，不要发送字符串 `"null"`（已在代码里处理）。
- 如果 Dify Cloud 部署，Storage Service 需要公网可达。
