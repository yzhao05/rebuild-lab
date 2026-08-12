# Refactor Phases

本文档把暑期重构拆成可回滚的小阶段。默认工作流是：从 `Summer26` 切 feature 分支，完成一小组文档/配置/代码变更，人工确认后再 commit，不直接 merge 回 `Summer26`。

## Phase 0：仓库健康检查

目标：确认项目可以被安全维护。

已检查事项：

- 当前分支不是 `master`。
- Unity 核心目录存在：`Assets/`、`Packages/`、`ProjectSettings/`。
- Unity 版本：2022.3.62f3c1。
- `.gitignore` 原先只包含 `Temp/UnityLockfile`，需要补强。
- 当前已有部分 Unity/IDE 生成文件被 Git 跟踪，暂不删除，后续单独处理。

退出标准：

- `.gitignore` 覆盖 Unity 常见生成目录和 IDE 文件。
- 已跟踪生成文件有清单和处理方案。
- 工作区修改清楚、风险可解释。

风险等级：安全到低风险。

## Phase 1：项目理解和文档化

目标：先建立共同地图，再动代码。

交付物：

- `docs/refactor/current-script-to-target-module-map.md`
- `docs/refactor/refactor-phases.md`
- `docs/github-workflow-for-beginners.md`

退出标准：

- 能说清每类脚本现在在哪里、未来属于哪个模块。
- 能给 Git 初学者解释当前分支、哪些文件该提交、哪些文件暂不处理。

风险等级：低风险。只新增/修改文档，不影响 Unity 运行。

## Phase 2：模块化目录骨架

目标：创建目标结构，但不移动旧脚本。

目标目录：

- `Assets/Scripts/EMVR/Runtime/`
- `Assets/Scripts/EMVR/Pedagogy/`
- `Assets/Scripts/EMVR/Assist/`
- `Assets/Scripts/EMVR/Telemetry/`
- `Assets/Scripts/EMVR/Domain/`
- `Assets/Scripts/EMVR/Research/`
- `Assets/Labs/Electrostatics/`
- `Assets/Labs/Magnetostatics/`
- `Assets/Labs/WavePolarization/`

退出标准：

- 每个目录有 README 说明边界。
- `Assets` 下新增文件和目录都有 `.meta`。
- 没有移动 scene、prefab、C# legacy 文件。

风险等级：低风险。新增文本资产，不改变场景引用。

## Phase 3：静电场配置抽离

目标：以 `lab1_electrostatics` 作为 pilot lab，先抽象配置合同。

交付物：

- `lab1_electrostatics_lab_spec.example.json`
- `lab1_electrostatics_steps.example.json`
- `lab1_electrostatics_objects.example.json`
- `lab1_electrostatics_rubric.example.json`
- `lab1_electrostatics_telemetry_map.example.json`

退出标准：

- `lab_id`、`step_id`、`object_id` 命名稳定。
- Assist payload 可以从配置中找到 step/rubric/object grounding 来源。
- 仍不强制 Unity 运行时读取这些文件；先作为合同样板。

风险等级：低风险。配置示例不会改变运行时。

## Phase 4：Assist 合同统一

目标：稳定 Unity 到 Gateway/Dify 的字段。

重点字段：

- `session_id`
- `user_id`
- `lab_id`
- `step_id`
- `mode`
- `student_input_text`
- `selected_object_json`
- `objects_json`
- `recent_actions_json`
- `diagnostic_hints_json`
- `trigger_reason_json`
- `step_rubric_json`
- `session_memory_json`

退出标准：

- 避免 `scene_id` 和 `lab_id` 概念漂移。
- 避免 `trigger_reason` 和 `trigger_reason_json` 字段漂移。
- Dify End 节点输出和 Unity `AssistResponseModels` 对齐。

风险等级：中低风险。会触及 C# 和 Gateway/Dify 合同，需测试一条完整闭环。

## Phase 5：低风险代码迁移

目标：只移动最容易验证、最少 scene 绑定的脚本。

迁移顺序建议：

1. 数据模型和纯合同类。
2. Runtime session/object registry。
3. Telemetry event/outcome。
4. Pedagogy step/rubric/validation。
5. Lab-specific adapters。

暂缓事项：

- 不移动 Unity scene。
- 不移动 prefab。
- 不重写 `ElectricField`、`MagneticField`、`WaveField`。
- 不批量改旧 `Game_*` 脚本。

风险等级：中风险。移动 C# 需要保留 `.meta` 并验证 Unity 编译和 scene 引用。

## Commit 策略

推荐小步提交：

1. `docs: add refactor map and beginner workflow`
2. `chore: add emvr module skeleton`
3. `config: add electrostatics lab example specs`
4. `chore: update unity gitignore`

实际提交前应先输出：

- 当前分支。
- 将要提交的文件。
- 不会提交的文件。
- 风险说明。

