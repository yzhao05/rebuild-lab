# EMVRLABS-Summer26 使用说明

这是一个 Unity VR 电磁学实验项目。当前目录新增了一套面向 AI agent 的知识库、接口契约、MCP 工具和共同代码说明，用于后续从头构建或扩展 lab。

## 本次新增内容

### 1. Agent 知识库

路径：

```text
AI_AGENT_CONTEXT_BUNDLE/
```

内容包括：

- `Agents.md`：Unity 项目规则
- `WHAT_TO_SHOW_AI_FOR_AGENT_LAB.md`：给 AI agent 的上下文顺序
- `.agents/knowledge/common_lessons.md`：Lab 1-3 的共同经验
- `.agents/knowledge/lab_issue_catalog.md`：历史问题和最终解决办法
- `.agents/playbooks/`：构建、调试、写 README 的流程
- `.agents/evals/`：验收和回归检查清单
- `summary/lab1`、`summary/lab2`、`summary/lab3`：三个已有实验的总结

使用新 agent 构建实验前，应先让 agent 阅读这个知识库。

### 2. Lab 接口契约

路径：

```text
LabInterfaceContracts/
```

里面已经整理了 lab1、lab2、lab3 的接口 JSON：

- `lab1_electrostatics_*`
- `lab2_static_magnetic_field_*`
- `lab3_wave_polarization_*`

每个 lab 都包含：

- `lab_spec`
- `objects`
- `steps`
- `rubric`
- `telemetry_map`

后续构建 lab 时，应先根据这些 JSON 复用场景、物体、步骤、rubric、telemetry 和 AI assist grounding 信息。

### 3. Lab MCP Server

路径：

```text
LabMCP/
```

主要文件：

- `LabMCP/lab_mcp_server.py`
- `LabMCP/mcp_config.example.json`
- `LabMCP/README.md`

这个 MCP server 读取 `LabInterfaceContracts`，提供列出 lab、读取 spec/steps/objects/rubric/telemetry、组合 step context、生成 assist grounding template、校验接口一致性等工具。

注意：这个 MCP 使用 stdio，不占用 TCP 端口。如果你要接入 Unity MCP 插件、Dify 或其他外部服务，需要把实际 host 和 port 告诉 agent，不要让 agent 猜。

### 4. 共同代码总结

路径：

```text
common/
```

主要文件：

- `common/README.md`
- `common/common_code_inventory.json`

这里总结了三个 lab 共同使用的 Unity 场景、入口脚本、XR 交互、步骤 UI、AI Assist 框架、物体模型和场可视化代码。

### 5. Agent 接入说明

路径：

```text
README_FOR_AGENT.md
```

这是给接入 agent 看的核心说明。它告诉 agent：

- 从头构建前必须先看知识库
- 根据历史问题先向用户提问
- 复用 `LabInterfaceContracts` 和 `common` 中的代码
- Unity 编译和 Play Mode 测试由用户完成
- 构建时间过长时必须停止、查知识库、修正流程

## 如何使用

### 给 AI Agent 使用

把下面这些信息给 agent：

```text
先阅读 README_FOR_AGENT.md。
再阅读 AI_AGENT_CONTEXT_BUNDLE/ 中的知识库。
使用 LabInterfaceContracts/ 作为 lab 接口契约。
使用 LabMCP/ 中的 MCP server 读取接口。
使用 common/ 中的共同代码说明复用 Unity 场景、物体和脚本。
Unity 编译和 Play Mode 测试由我在 Unity Editor 中完成。
```

如果使用 MCP client，可以参考：

```text
LabMCP/mcp_config.example.json
```

### 构建或扩展 Lab 的推荐流程

1. 让 agent 先读 `README_FOR_AGENT.md` 和 `AI_AGENT_CONTEXT_BUNDLE/`。
2. 说明要构建的是新 lab，还是修改 lab1/lab2/lab3。
3. 提供实验要求、公式、物体、步骤、交互方式和验收标准。
4. 回答 agent 根据知识库提出的问题，例如 Unity 版本、是否需要桌面测试、哪些物体可拖拽等。
5. 让 agent 先检查或创建 JSON 接口契约。
6. 让 agent 根据 `common/` 复用现有场景、物体、XR、步骤 UI、Assist 和可视化代码。
7. agent 写完代码后，你在 Unity Editor 中等待编译。
8. 你运行场景，提供 Console/Game/Scene 截图或错误信息。
9. agent 根据反馈修复。

### Unity 编译注意事项

本项目默认规则：

```text
不要让 agent 主动跑 Unity batchmode。
不要让 agent 手写 .unity 场景 YAML，除非你明确要求。
优先使用 Unity Editor 菜单生成场景。
Unity 编译和 Play Mode 测试由使用者完成。
```

如果 Unity 编译、生成或运行时间过长，应让 agent 停止当前流程，并检查：

```text
AI_AGENT_CONTEXT_BUNDLE/.agents/knowledge/lab_issue_catalog.md
```

历史经验表明，长时间不可见的 batchmode 构建容易导致调试困难。正确做法是拆分任务、用 Editor 菜单生成、让用户提供 Console 和截图反馈。

## 三个已有 Lab

| Lab | 场景 | 主题 |
|---|---|---|
| lab1_electrostatics | `Assets/Scenes/Static_Electric_Field.unity` | 静电场、电荷和电场线 |
| lab2_static_magnetic_field | `Assets/Scenes/Static_Magnetic_Field.unity` | 静磁场、电流导线和螺线管 |
| lab3_wave_polarization | `Assets/Scenes/Wave_Polarization.unity` | 电磁波偏振、线偏振片和四分之一波片 |

共享启动场景：

```text
Assets/Scenes/start.unity
Assets/Scenes/startVR.unity
```

