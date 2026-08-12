# Lab 1 Electrostatics VR Lab 搭建对话总结

本文总结本次从零搭建 Unity 物理 3D/VR Lab 的对话过程，分为两个部分：

1. 为了完成“从头开始搭建实验”这个目标，用户输入了什么，以及后续搭建同类实验时用户需要提供什么。
2. 搭建过程中用户提出的所有修改点，以及对应采取的改正措施。

## 1. 用户需要提供的内容

### 1.1 实验目标

用户最初提出的目标是：

> 根据实验要求文档，从头构建一个用 Unity 实现的物理 3D Lab，并完成实验场景。

该目标决定了工作内容不是单纯写脚本，而是包括：

- 阅读实验手册。
- 提炼实验物理内容。
- 设计 Unity 场景结构。
- 生成可运行的实验场景。
- 支持普通桌面测试和 VR 操作。
- 根据 Play Mode 实测结果持续修正体验问题。

### 1.2 用户提供的实验文档

为了搭建 Lab 1，用户提供了实验要求文档：

- `E:/暑研/EM_VR_Labs/Docs/Handbook/Lab 1 Electrostatics.pdf`

该文档用于确定 Lab 1 的核心内容：

- 点电荷、线电荷、面电荷的静电场模型。
- 电场线、矢量场、叠加原理。
- 正负电荷和导体板之间的关系。
- 需要学生完成的 Q1-Q7 实验观察任务。
- 需要在场景中提供的操作流程提示、模型公式提示、实验预设按钮等。

后续为了整理三个 Lab 的共性代码，用户又提供了：

- `E:/暑研/EM_VR_Labs/Docs/Handbook/Lab 2 Magnetostatic.pdf`
- `E:/暑研/EM_VR_Labs/Docs/Handbook/Lab 3 Wave Polarization.pdf`

这两个文档用于总结 Lab 1、Lab 2、Lab 3 的共性结构，并将公共代码与实验特有代码拆分。

### 1.3 用户提供的 Unity 环境信息

用户明确提供了 Unity Editor 路径：

- `E:\unity\2022\Editor\Unity.exe`

并说明当时已有 Unity 进程在运行。

这个信息用于尝试通过 Unity batchmode 或现有 Editor 状态来生成场景和检查编译状态。后续发现静默 batchmode 方式容易耗时过长，因此改成以 Editor 菜单和日志排查为主。

### 1.4 用户提供的运行截图与现象反馈

用户在不同阶段提供了多张 Unity 截图，用来反馈实际运行状态，包括：

- 场景是否生成成功。
- 字体是否过大、遮挡、重叠。
- Play Mode 后文字是否左右镜像。
- Unity Console 中的 warning/error。
- 鼠标悬停提示是否清晰。
- 1-5 按钮是否能响应。
- VR/鼠标模拟拖动物体是否有效。
- 手柄射线是否可见。
- 哪些按钮实际可以拖动。

这些截图非常关键，因为 Unity 场景的很多问题无法只靠代码判断，需要通过 Game 视图和 Scene 视图观察实际效果。

### 1.5 用户后续搭建同类实验时应提供的内容

如果之后继续搭建新的 VR Lab，用户最好提供：

- 实验手册 PDF。
- Unity 项目根目录。
- Unity Editor 版本和安装路径。
- 项目中已安装的 XR 插件信息。
- 希望支持的平台：桌面测试、VR 头显、手柄、鼠标模拟等。
- 实验中必须出现的对象、公式、步骤、报告问题。
- 运行截图，尤其是 Play Mode 之后的 Game 视图。
- Console 报错或 warning 截图。
- 对交互方式的明确要求，例如“鼠标左键代表 VR 手柄抓取”。

## 2. 修改需求与改正措施

### 2.1 生成过程耗时过长

用户反馈：

- 静默生成或 batchmode 调用时间过长。
- 某一步持续了很久，不应该卡半小时。

改正措施：

- 先解释生成过程的目的：尝试让 Unity 在后台执行 Editor 生成器，自动创建场景和资源。
- 增加日志思路，便于定位卡在哪一步。
- 将任务拆成几个阶段：
  - 阶段 1：读取实验文档，整理实验内容。
  - 阶段 2：生成 Unity 脚本和场景生成器。
  - 阶段 3：运行场景生成器并检查场景。
  - 阶段 4：进入 Play Mode 测试交互。
  - 阶段 5：根据截图和 Console 修正问题。
- 后续建议减少静默 batchmode，改用 Unity Editor 菜单触发生成，便于用户观察和排错。

### 2.2 先给出实现说明

用户要求：

- 先生成一份关于如何实现这个 Lab 的说明。
- 明确需要增加哪些文件，以及场景包含哪些内容。

改正措施：

- 梳理 Lab 1 的实现结构。
- 说明需要增加：
  - 场景生成器。
  - 电荷脚本。
  - 电场可视化脚本。
  - 预设按钮脚本。
  - 桌面拖拽脚本。
  - VR/鼠标交互辅助脚本。
- 说明场景中应包含：
  - 实验房间/玻璃墙。
  - 正负充电区。
  - 可拖拽电荷。
  - 电场可视化区域。
  - 1-5 预设按钮。
  - 说明文字和公式提示板。
  - 摄像机和 XR rig。

### 2.3 场景生成是否成功

用户提供生成后截图，询问是否成功。

观察到：

- 场景中已经出现了实验结构。
- 但文字非常大，并且互相遮挡。

改正措施：

- 判断“场景对象生成成功，但视觉布局还没有成功”。
- 后续重点转向文字大小、位置、摄像机角度和交互逻辑修正。

### 2.4 如何运行查看效果

用户询问：

- 现在能不能运行查看效果。
- 如果能，应该怎么运行。

改正措施：

- 说明可以在 Unity 中打开 `Assets/Scenes/Lab1_Electrostatics.unity`。
- 点击顶部 Play 按钮进入 Play Mode。
- 使用 Game 视图查看运行效果。
- 桌面测试可使用键盘和鼠标，VR 测试可使用 XR 设备或 Device Simulator。

### 2.5 Audio Listener 数量警告

用户反馈 Console 警告：

> There are 2 audio listeners in the scene. Please ensure there is always exactly one audio listener in the scene.

改正措施：

- 判断原因是场景里同时存在桌面预览相机和 XR 相机，两者都带有 Audio Listener。
- 修改生成/场景脚本，确保最终场景中只保留一个 Audio Listener。
- 保留主要运行相机的 Audio Listener，禁用或移除另一个。

### 2.6 Play Mode 中调用 Editor API 报错

用户截图显示：

> This cannot be used during play mode, please use SceneManager.CreateScene() instead.

改正措施：

- 判断原因是场景生成器使用了 Editor-only 的新建场景 API，并且用户在 Play Mode 中触发了生成。
- 修改生成器逻辑：
  - 如果当前处于 Play Mode，则拒绝执行生成。
  - 给出明确提示，要求先退出 Play Mode。
- 向用户说明退出 Play Mode 的方法：再次点击 Unity 顶部蓝色 Play 按钮。

### 2.7 renderer.material 在编辑模式泄漏材质 warning

用户反馈：

> Instantiating material due to calling renderer.material during edit mode.

改正措施：

- 判断原因是在编辑模式下访问 `renderer.material`，Unity 会复制材质实例，导致材质泄漏 warning。
- 将相关代码改为使用：
  - `renderer.sharedMaterial`
  - 或 `MaterialPropertyBlock`
- 对动态颜色变化优先使用 `MaterialPropertyBlock`，避免运行时和编辑时不必要地复制材质。

### 2.8 Curl error 60 证书 warning

用户反馈：

> Curl error 60: Cert verify failed. Certificate Common Name(CN) does not match with the expected CN.

改正措施：

- 判断这不是 Lab 代码本身的 bug。
- 说明它通常来自 Unity 服务、Muse、Package Manager 或联网证书校验。
- 该 warning 不影响当前本地实验场景运行。
- 如果需要彻底处理，应检查网络代理、证书、Unity 登录服务或关闭相关在线服务。

### 2.9 说明 Play Mode 下如何测试代码

用户要求：

- 用文字讲解代码在 Play Mode 下如何运行。

改正措施：

- 说明进入 Play Mode 后：
  - 场景初始化控制器。
  - 电荷对象注册到场景。
  - 电场可视化脚本根据电荷计算电场线和矢量。
  - 1-5 按钮切换不同实验预设。
  - 鼠标或 VR 指针可选择/拖动对象。
  - C/F/V 等控制键用于清除、显示电场线、显示矢量。

### 2.10 点击物体后 XR Device Simulator 面板太大

用户反馈：

- 点击一个物体后出现巨大 XR Device Simulator 面板。
- 要求扩大场景或减小提示。
- 要求整个场景正对镜头，不要斜对。

改正措施：

- 调整场景尺寸和摄像机位置，使实验主体正对镜头。
- 对 Device Simulator 面板进行隐藏或缩小处理，避免遮挡实验主体。
- 调整运行时摄像机，使 Game 视图看到的是正面实验台，而不是斜视角。

### 2.11 1-5 按钮响应异常

用户反馈：

- 按 2 有反应。
- 按 1/3/4/5 没反应。

改正措施：

- 检查键盘输入和预设按钮绑定逻辑。
- 修改控制器，让数字键 1-5 都调用 `ApplyPreset(1-5)`。
- 为场景中的 1-5 黄色预设按钮绑定对应的 `ElectrostaticPresetPad`。
- 增加点击/选择的 debounce，避免一次点击触发多次。

### 2.12 Play Mode 后文字左右镜像

用户多次反馈：

- 编辑模式下文字正常。
- 进入 Play Mode 后文字左右镜像。
- 单独改摄像机或文字方向后仍然不对。
- 同时改文字和摄像头会互相抵消，字体看起来不变化。

问题分析：

- 编辑模式 Scene 视图和 Play Mode Game 视图使用的视角不同。
- 运行时摄像机可能站在文字背面看提示板。
- TextMesh 或提示板朝向与摄像机方向不一致。
- XR rig 或 preview camera 在 Play Mode 中覆盖了原本设计好的视角。

改正措施：

- 先区分编辑视图和运行视图的摄像机。
- 让运行时主摄像机固定在实验台正面。
- 调整文字朝向，使它面向运行时摄像机。
- 只保留一套最终朝向逻辑，避免同时翻转文字和摄像机造成抵消。
- 最终使 Play Mode 下主体文字恢复正常方向。

### 2.13 场景文字重叠

用户反馈：

- 虽然 Play Mode 下文字方向正常了，但仍存在重叠。

改正措施：

- 重新整理提示板布局。
- 将说明文字分区：
  - Workflow。
  - Report。
  - Models。
- 减少单块文字密度。
- 调整字体大小、行距、位置和背景板尺寸。
- 将过长的说明拆成更短的行。

### 2.14 悬停提示文字反向

用户反馈：

- 鼠标移到下方 1-5 黄色方块上时，显示的悬停文字仍然是反的。

改正措施：

- 检查 hover annotation 的生成方向。
- 让悬停提示也面向运行时摄像机。
- 将 hover 提示逻辑统一到公共组件 `ElectrostaticHoverAnnotation` 中。
- 避免每个按钮各自使用不同朝向规则。

### 2.15 场景长宽高扩大两倍

用户要求：

- 把整个场景长宽高扩大两倍。

改正措施：

- 扩大实验台、玻璃墙、可视化区域和按钮之间的间距。
- 调整摄像机距离和正交/透视参数，使扩大后的场景仍然完整进入画面。
- 避免仅仅放大模型而不移动摄像机导致视野被裁切。

### 2.16 字体分辨率和清晰度问题

用户多次反馈：

- 部分字体看不清。
- 悬停字体看不清。
- 修改后仍然存在同样问题。

改正措施：

- 调整 TextMesh 字体大小、缩放和显示距离。
- 增大文字背景板对比度。
- 减少一块面板上的文字数量。
- 将提示文字拆成三块面板。
- 增加文字和背景的间距。
- 调整摄像机与文字平面的距离，减少过小文字在 Game 视图中的采样模糊。

### 2.17 改成 VR 手柄操作

用户指出：

- 这个实验是 VR Lab。
- XR 插件已经搭载好。
- 需要把对应操作改成 VR 手柄操作。

改正措施：

- 加入 XR 交互支持。
- 让预设按钮支持 VR 射线选择和激活。
- 让可拖拽物体支持 VR 选择/抓取。
- 保留桌面鼠标测试入口，方便没有头显时调试。
- 增加 XR fallback ray，用于在原始 XR 线渲染异常时仍然看到手柄射线。

### 2.18 检测上一组对话为什么进行了很久

用户要求：

- 检测为什么上一组对话进行了这么久。
- 接着修改成 VR Lab。

分析结论：

- 耗时主要来自 Unity 静默生成、等待编译、Play Mode 与 Editor Mode API 混用、视觉问题多轮截图修正。
- Unity 场景类任务需要不断进入 Play Mode 观察，单靠代码很难一次到位。

改正措施：

- 减少不透明的后台 batchmode。
- 明确阶段和日志。
- 优先用可观察的 Editor 菜单和 Console 反馈。
- 继续将实验改成 VR 和桌面双模式。

### 2.19 手柄射线不可见，充电没反应

用户反馈：

- 没看到手柄射线。
- 充电代码好像不太对，没反应。
- 当前测试用鼠标代表手柄，按住鼠标左键代表拖动。

改正措施：

- 增加桌面鼠标模拟 VR 指针：
  - 鼠标移动代表指针方向。
  - 鼠标左键按住代表抓取/拖动。
- 增加可见的鼠标/VR 指针射线。
- 鼠标点击时使用 `Physics.RaycastAll`，避免透明 Field Cube 挡住后面的可拖拽物体。
- 将拖动接口抽象为 `IEmVrMouseDraggable`。
- 修改充电逻辑：
  - 红色充电区赋予正电。
  - 蓝色充电区赋予负电。
  - 拖动物体进入充电区时立即充电。
  - 增加距离 fallback，不完全依赖 trigger 碰撞。

### 2.20 对照当前场景讲解如何测试功能

用户提供截图并要求：

- 对着场景文字讲解如何测试功能。

改正措施：

- 说明当前场景中的三个说明区：
  - Workflow：实验步骤。
  - Report：Q1-Q7 对应任务。
  - Models：公式参考。
- 说明下方 1-5 按钮：
  - 可用数字键或 VR/鼠标指针选择。
  - 用于切换不同电荷场景预设。
- 说明红蓝方块：
  - 红色用于正电荷。
  - 蓝色用于负电荷。
- 说明透明玻璃区域：
  - 用来观察电场线和矢量变化。
- 说明桌面测试：
  - 鼠标指向物体。
  - 按住左键拖动。
  - 拖入红/蓝区域充电。

### 2.21 用户询问电荷在哪里

用户问：

- 电荷在哪里？

改正措施：

- 解释场景中的电荷包括：
  - 预设生成在玻璃区域内的点/线/面电荷。
  - 可拖动的测试电荷。
  - 左右红蓝充电区不是电荷本身，而是给可拖动物体充正/负电的区域。
- 说明如果看不清，可使用 C 清除预设，再拖动可充电物体测试。

### 2.22 只有 1、2、5 按钮可以拖动，要求突出显示

用户实验后反馈：

- 只有 1、2、5 按钮可以拖动。
- 要把 1、2、5 标红突出。

改正措施：

- 将 1、2、5 号按钮高亮。
- 使用红/白/更明显的视觉样式突出它们。
- 保持 3、4 的普通预设按钮样式。
- 让用户更容易知道当前重点可测试对象。

### 2.23 根据 Lab 1 总结三个 Lab 的共性代码并重构目录

用户提供 Lab 2 和 Lab 3 手册后要求：

- 阅读两个 Lab 实现手册。
- 根据 Lab 1 总结三个 Lab 的共性代码。
- 新建文件夹存储共性代码。
- 将每个 Lab 特有代码分离到各自文件夹。
- 修改现有代码存储位置以优化结构。

改正措施：

- 阅读 Lab 2 Magnetostatic 和 Lab 3 Wave Polarization 手册。
- 总结三类 Lab 的共性：
  - VR/鼠标指针。
  - 悬停英文注释。
  - 可拖拽物体接口。
  - XR 射线 fallback。
  - 场景说明/实验步骤/报告问题面板的组织思路。
- 新建公共代码目录：
  - `Assets/Scripts/Common/`
- 将公共脚本移动到 Common：
  - `ElectrostaticHoverAnnotation.cs`
  - `ElectrostaticMouseVrPointer.cs`
  - `ElectrostaticXrRayFallback.cs`
  - `IEmVrMouseDraggable.cs`
- 新建 Lab 1 专用目录：
  - `Assets/Scripts/Lab1_Electrostatics/`
- 将 Lab 1 专用脚本移动到该目录：
  - `ChargeDistribution.cs`
  - `ElectrostaticCharge.cs`
  - `ElectrostaticCharger.cs`
  - `ElectrostaticDesktopDraggable.cs`
  - `ElectrostaticFieldVisualizer.cs`
  - `ElectrostaticLabButton.cs`
  - `ElectrostaticLabController.cs`
  - `ElectrostaticPresetPad.cs`
  - `ElectrostaticScenePresentation.cs`
- 新建 Lab 2 和 Lab 3 目录并放置说明：
  - `Assets/Scripts/Lab2_Magnetostatics/README.md`
  - `Assets/Scripts/Lab3_WavePolarization/README.md`
- 将场景生成器移动到：
  - `Assets/Editor/Lab1_Electrostatics/ElectrostaticsLabSceneBuilder.cs`
- 尽量保留 `.meta` 文件，以降低 Unity MonoBehaviour 引用丢失风险。

### 2.24 为 Lab 1 写 README

用户要求：

- 对 Lab 1 写一个 README。
- 明确各种接口以及实验如何操作。

改正措施：

- 新增/完善：
  - `Assets/Scripts/Lab1_Electrostatics/README.md`
- README 内容包括：
  - 场景入口。
  - 桌面鼠标测试方式。
  - VR 操作方式。
  - 1-5 预设按钮说明。
  - Lab 1 专用脚本接口说明。
  - Common 公共脚本接口说明。
  - 推荐测试流程。
  - 注意事项。

## 3. 当前项目结构总结

经过整理后，Lab 1 相关代码大致分布如下：

```text
Assets/
  Editor/
    Lab1_Electrostatics/
      ElectrostaticsLabSceneBuilder.cs

  Scripts/
    Common/
      ElectrostaticHoverAnnotation.cs
      ElectrostaticMouseVrPointer.cs
      ElectrostaticXrRayFallback.cs
      IEmVrMouseDraggable.cs
      README.md

    Lab1_Electrostatics/
      ChargeDistribution.cs
      ElectrostaticCharge.cs
      ElectrostaticCharger.cs
      ElectrostaticDesktopDraggable.cs
      ElectrostaticFieldVisualizer.cs
      ElectrostaticLabButton.cs
      ElectrostaticLabController.cs
      ElectrostaticPresetPad.cs
      ElectrostaticScenePresentation.cs
      README.md

    Lab2_Magnetostatics/
      README.md

    Lab3_WavePolarization/
      README.md
```

## 4. 当前 Lab 1 操作摘要

### 4.1 生成场景

在 Unity 顶部菜单中选择：

```text
Tools > Electrostatics Lab > Build Lab 1 Scene
```

注意：不要在 Play Mode 中执行生成器。

### 4.2 运行实验

1. 打开 `Assets/Scenes/Lab1_Electrostatics.unity`。
2. 点击 Unity 顶部 Play 按钮进入 Play Mode。
3. 在 Game 视图中观察实验台。
4. 使用数字键 `1-5` 或点击下方黄色按钮切换预设。
5. 用鼠标指向物体，按住左键模拟 VR 手柄拖动。
6. 将可拖动物体拖到红色区域充正电，拖到蓝色区域充负电。
7. 观察玻璃区域内电场线、矢量和电荷分布变化。

### 4.3 VR 操作

- 使用 VR 手柄射线指向按钮或物体。
- 选择 1-5 预设按钮切换实验场景。
- 抓取可拖动物体并移动到红/蓝充电区。
- 指针悬停到对象上时显示对应英文注释。

### 4.4 常见 Console 信息

- `Curl error 60`：通常来自 Unity 在线服务或证书问题，不是 Lab 1 代码错误。
- `There are 2 audio listeners`：应保证场景中只有一个 Audio Listener。
- `This cannot be used during play mode`：说明在 Play Mode 中误触发了 Editor 场景生成器，需要先退出 Play Mode。
- `renderer.material during edit mode`：应避免编辑模式下访问 `renderer.material`，改用 `sharedMaterial` 或 `MaterialPropertyBlock`。

