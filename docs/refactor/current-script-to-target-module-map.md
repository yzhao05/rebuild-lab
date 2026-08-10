# Current Script To Target Module Map

更新日期：2026-06-30

本文档记录当前脚本位置与暑期目标模块之间的对应关系。第一阶段只做识别和映射，不直接移动 C# 脚本，避免破坏 Unity 场景、Prefab 与 `.meta` 引用。

## 当前项目快照

- Unity 版本：2022.3.62f3c1。
- 核心 Unity 目录已存在：`Assets/`、`Packages/`、`ProjectSettings/`。
- 主要运行场景在 `Assets/Scenes/`：
  - `Static_Electric_Field.unity`
  - `Static_Magnetic_Field.unity`
  - `Wave_Polarization.unity`
  - `start.unity`
  - `startVR.unity`
- 现有 EMVR Assist 层位于 `Assets/Scripts/EMVR/Assist/`。
- 旧实验和物理脚本仍主要位于 `Assets/Scripts/` 根目录。

## 模块边界

| 目标模块 | 负责内容 | 不应该负责 |
|---|---|---|
| `Runtime` | session、lab context、对象注册、服务初始化 | 具体静电学教学规则 |
| `Pedagogy` | step、rubric、validation、scaffolding policy | 直接调用 Dify 或 Gateway |
| `Assist` | AssistRequest、Gateway client、AssistResponse、字幕、高亮、TTS/STT 适配 | 写死某个具体实验逻辑 |
| `Telemetry` | InteractionEvent、recent actions、outcome、log/export | 依赖 Dify 具体实现 |
| `Domain` | 电磁学物理对象、物理计算、场线/波/磁场模型 | AI 干预、研究分组 |
| `Research` | study config、evaluation、data export、anonymization | 污染 Domain 计算逻辑 |
| `Labs` | 具体实验内容包、scene/prefab/config/knowledge/checklist | 被 EMVR 平台层反向依赖 |

## 当前脚本映射

| 当前脚本 | 当前位置 | 建议目标 | 当前判断 |
|---|---|---|---|
| `LabSessionManager.cs` | `Assets/Scripts/EMVR/Assist/Core/` | `Runtime/Session` | 负责 session/user/lab/scene，上游应逐步从 Assist 移出。 |
| `AssistConfig.cs` | `Assets/Scripts/EMVR/Assist/Core/` | `Assist` + `Runtime` 配置拆分 | 目前同时保存 Gateway、Dify、默认 lab/step。后续可拆成平台配置和 Assist provider 配置。 |
| `StepProgressManager.cs` | `Assets/Scripts/EMVR/Assist/Core/` | `Pedagogy/Steps` | 当前硬编码默认 step，后续应由 `StepSpec` 或 lab config 驱动。 |
| `AssistPayloadBuilder.cs` | `Assets/Scripts/EMVR/Assist/Core/` | `Assist/Contracts` | 当前统一组装 payload，是合同稳定化的核心入口。 |
| `AssistGatewayClient.cs` | `Assets/Scripts/EMVR/Assist/Network/` | `Assist/ProviderGateway` | 保持在 Assist，后续只依赖 `AssistRequest` 合同。 |
| `AssistResponseExecutor.cs` | `Assets/Scripts/EMVR/Assist/UI/` | `Assist/ResponseExecution` | 负责 subtitle/highlight/outcome 启动，后续可拆出高亮适配器。 |
| `AssistUIController.cs` | `Assets/Scripts/EMVR/Assist/UI/` | `Assist/UI` | 学生输入入口，保持 Assist 层。 |
| `ExperimentObject.cs` | `Assets/Scripts/EMVR/Assist/Objects/` | `Runtime/ObjectModel` | 场景对象状态快照来源；后续保持 lab-agnostic。 |
| `ExperimentObjectRegistry.cs` | `Assets/Scripts/EMVR/Assist/Objects/` | `Runtime/ObjectRegistry` | 对象注册/查找应作为 Runtime 服务。 |
| `SelectionTracker.cs` | `Assets/Scripts/EMVR/Assist/Objects/` | `Runtime/InteractionContext` | 选中对象属于运行时上下文，同时服务 Assist 和 Telemetry。 |
| `AssistSelectionReporter.cs` | `Assets/Scripts/EMVR/Assist/Objects/` | `Telemetry` 或 `Runtime` adapter | 把 Unity 交互转成选择/日志事件，可保持为桥接脚本。 |
| `ElectrostaticsAssistBinder.cs` | `Assets/Scripts/EMVR/Assist/Objects/` | `Labs/Electrostatics/Adapters` | 名称已表明是静电场专用，不应留在平台 Assist 层。 |
| `InteractionEventLogger.cs` | `Assets/Scripts/EMVR/Assist/Logging/` | `Telemetry/EventLogging` | recent actions 的来源，应归入统一 Telemetry。 |
| `InteractionEventModels.cs` | `Assets/Scripts/EMVR/Assist/Data/` | `Telemetry/Contracts` | 统一事件合同。 |
| `OutcomeTracker.cs` | `Assets/Scripts/EMVR/Assist/Outcome/` | `Telemetry/Outcome` | 干预后的 outcome 记录，应与研究数据导出对齐。 |
| `DiagnosticHintsModels.cs` | `Assets/Scripts/EMVR/Assist/Data/` | `Pedagogy` + `Telemetry` | `DiagnosticHints` 属于教学诊断，`OutcomeRecord` 更接近 Telemetry。 |
| `LocalValidationManager.cs` | `Assets/Scripts/EMVR/Assist/Validation/` | `Pedagogy/Validation` | 当前规则面向静电场，后续要由 rubric/config 驱动。 |
| `BehaviorMonitor.cs` | `Assets/Scripts/EMVR/Assist/Proactive/` | `Pedagogy/ScaffoldingPolicy` | 识别 idle/repeat/deviation，触发 Assist，但策略本身应与 provider 解耦。 |
| `AssistPayload.cs` | `Assets/Scripts/EMVR/Assist/Data/` | `Assist/Contracts` | 当前字段基本符合 MVP grounding 需求。 |
| `AssistResponseModels.cs` | `Assets/Scripts/EMVR/Assist/Data/` | `Assist/Contracts` | Unity 可执行 JSON 的模型。 |
| `ExperimentObjectModels.cs` | `Assets/Scripts/EMVR/Assist/Data/` | `Runtime/ObjectModel` | 对象 snapshot 合同。 |
| `ElectricField.cs` | `Assets/Scripts/` | `Domain/Electrostatics` | 第一阶段不重写，先作为 legacy physics backend。 |
| `MagneticField.cs` | `Assets/Scripts/` | `Domain/Magnetostatics` | 第一阶段不移动。 |
| `WaveField.cs` | `Assets/Scripts/` | `Domain/Waves` | 第一阶段不移动。 |
| `Sphere.cs`、`Wire.cs`、`Plane.cs`、`Solenoid.cs` | `Assets/Scripts/` | `Domain/Objects` 或 lab adapters | 旧 ChargedObjects 体系，先 wrapper/映射，不直接重写。 |
| `Polarizer.cs`、`LinearPolarizer.cs`、`QuarterPlate.cs` | `Assets/Scripts/` | `Domain/Waves` | 波偏振相关，暂不参与静电场 pilot。 |
| `Game_1_P.cs`、`Game_1_S.cs`、`Game_1_W.cs` | `Assets/Scripts/` | `Labs/Electrostatics/LegacyFlow` | 疑似静电场流程脚本，迁移前需查 scene 引用。 |
| `Game_2_*` | `Assets/Scripts/` | `Labs/Magnetostatics/LegacyFlow` | 暂不迁移。 |
| `Game_3_*` | `Assets/Scripts/` | `Labs/WavePolarization/LegacyFlow` | 暂不迁移。 |
| `AssistantRequest.cs`、`AssistantResponse.cs`、`BridgeResponse.cs` | `Assets/Scripts/` | `Assist/LegacyBridge` | 可能是旧 AI/bridge 脚本，后续确认是否仍被场景引用。 |
| `Start_Game.cs`、`Step.cs`、`Back.cs`、`Respawn.cs`、`Test.cs` | `Assets/Scripts/` | `Labs/*/LegacyUIOrFlow` | 未确认归属前只记录，不移动。 |

## 第一阶段迁移原则

1. 不移动 scene、prefab、legacy physics 脚本。
2. 不重写 `ElectricField`、`MagneticField`、`WaveField`。
3. 优先冻结合同：`LabSpec`、`StepSpec`、`ObjectSpec`、`RubricSpec`、`TelemetryEvent`、`AssistRequest`、`AssistResponse`、`OutcomeRecord`。
4. 先让 `lab1_electrostatics` 成为样板，再扩展其他 lab。
5. 每次移动 C# 前，必须确认 `.meta` 与 scene/prefab 引用风险。

