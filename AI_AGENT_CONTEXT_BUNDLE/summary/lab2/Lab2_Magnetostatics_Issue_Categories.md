# Lab 2 Magnetostatics 搭建问题归类

本文基于 `Lab2_Magnetostatics_Issue_Summary.md` 进一步归纳 Lab 2 Magnetostatics 开发过程中出现的问题。问题分为两大类：

1. 技术型问题：Unity 生成、脚本 API、场景交互、文字显示、磁场可视化、快捷键等实现层面的具体问题。
2. 设计/沟通型问题：实验操作方式、用户测试反馈、文档转译、交互流程选择等和需求表达或体验设计相关的问题。

本文只记录最终奏效的解决方法，不保留中间失败方案。

## 1. 技术型问题

技术型问题是指实验目标已经明确后，仍然需要通过代码、Unity 配置或运行时调试解决的问题。

### 1.1 Unity 生成流程问题

问题表现：

- 曾尝试使用 batchmode 自动生成 Lab 2 场景：

```powershell
& 'E:\unity\2022\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\rebuild\rebuild lab' -executeMethod MagnetostaticsLabSceneBuilder.BuildScene -logFile 'D:\rebuild\rebuild lab\tmp\lab2-unity-build.log'
```

- 该方式和 Lab 1 一样不适合当前项目调试。
- 用户难以判断 Unity 是否卡在编译、资源导入、项目加载或 Editor 状态锁定。
- Lab 2 的问题大量依赖 Game 视图截图和 Console 反馈，静默 batchmode 不能提供足够信息。

改正措施：

- 不再使用 batchmode 生成 Lab 2。
- 改为用户在 Unity Editor 中手动执行：

```text
Tools > Magnetostatics Lab > Build Lab 2 Scene
```

- 执行前退出 Play Mode，等待 Unity 编译完成。
- 如果 Console 有红色编译错误，先修复后再生成。
- 生成日志保留在：

```text
tmp/magnetostatics-lab-build-progress.log
```

### 1.2 Unity API 使用问题

问题表现：

- 执行 Lab 2 场景生成时出现弹窗：

```text
Magnetostatics Lab Build Failed
Value cannot be null.
Parameter name: dest
```

- 日志定位到：

```text
MagneticCurrentSource.UpdateVisuals()
Renderer.GetPropertyBlock(...)
```

原因：

- `MaterialPropertyBlock` 在 Editor 生成期间可能尚未初始化。
- 直接传入 `Renderer.GetPropertyBlock()` 会触发 `dest` 为空的异常。

改正措施：

- 在 `MagneticCurrentSource` 中懒加载 `MaterialPropertyBlock`。
- 使用前保证 `propertyBlock != null`。
- 跳过 `TextMesh` 的 renderer，避免把电流源颜色逻辑应用到文字 renderer。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticCurrentSource.cs
```

### 1.3 文本显示问题

问题表现：

- 顶部说明、按钮文字、底部提示文字又小又糊。
- 文字与背景板或按钮重叠。
- 修改字体分辨率后，视觉大小反而不符合预期。

改正措施：

- 沿用 Lab 1 最终奏效的文字清晰化方案。
- 使用 `TextMesh fontSize = 384`。
- 保持字体扩大前的视觉大小，不用 `768 + 缩小 characterSize`。
- 先调整场景与相机尺度，再设置文字大小。
- 将相机正交视野收紧到 `legibleOrthographicSize = 5.6`。
- 在运行时由 `MagneticScenePresentation` 修正旧场景残留文本参数。
- 调用 `RequestCharactersInTexture(...)`。
- 字体贴图使用 `FilterMode.Trilinear` 和 `anisoLevel = 8`。
- 顶部说明改为短句和分区布局，底部按钮分层摆放，减少遮挡。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticScenePresentation.cs
Assets/Editor/Lab2_Magnetostatics/MagnetostaticsLabSceneBuilder.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticLabController.cs
```

### 1.4 透明可视化区域遮挡拖动

问题表现：

- 直导线或螺线管在屏幕边缘可以二次拖动。
- 拖到屏幕中央透明可视化区域后无法再次选中和拖动。

原因：

- 透明的 `Magnetic Field Visualization Cube` 带有 collider。
- 鼠标点击屏幕中央时优先命中透明 cube，后方电流源收不到拖动输入。

改正措施：

- 禁用透明可视化 cube 的 collider。
- 新生成场景时由 builder 直接禁用 stage collider。
- 已生成旧场景在 Play Mode 中由 `MagneticScenePresentation` 自动禁用该 collider。

涉及文件：

```text
Assets/Editor/Lab2_Magnetostatics/MagnetostaticsLabSceneBuilder.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticScenePresentation.cs
```

### 1.5 磁场可视化深度问题

问题表现：

- 右侧螺线管拖入透明区域后，屏幕上看起来已经进入实验区，但不显示磁场。

原因：

- 正交相机下，屏幕位置进入区域并不代表真实 3D 深度进入区域。
- 螺线管可能仍停留在右侧源架的 `z=-4.65`，没有进入磁场可视化平面。

改正措施：

- 桌面拖拽开始时寻找 `MagneticFieldVisualizer`。
- 将被拖动物体的 `z` 坐标固定到可视化平面。
- 后续拖动只改变屏幕平面上的位置。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticDesktopDraggable.cs
```

### 1.6 鼠标拖动手感问题

问题表现：

- 棍子拖动一开始过于灵敏，鼠标动一点，物体移动很远。
- 调低后又过于迟钝。

原因：

- 旧方案使用射线和平面相交计算拖动点，从源架深度切到可视化深度时容易产生坐标跳变。

改正措施：

- 改为基于正交相机的屏幕位移拖拽。
- 鼠标屏幕位移转换为世界坐标位移。
- 不再用旧深度平面计算初始命中点。
- 最终拖拽灵敏度设为：

```csharp
dragSensitivity = 2f
```

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticDesktopDraggable.cs
```

### 1.7 螺线管选中和拖动问题

问题表现：

- 直导线可以拖动。
- 螺线管不能稳定选中，或者完全不能拖动。

原因：

- 螺线管由 `LineRenderer` 绘制，线圈结构细，鼠标命中区域不稳定。

改正措施：

- 为螺线管补稳定碰撞体。
- 运行时自动识别 solenoid 并添加较大的 `BoxCollider`。
- 同时放大原有 `CapsuleCollider`。
- 新生成场景时 builder 也直接创建这些碰撞体。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticDesktopDraggable.cs
Assets/Editor/Lab2_Magnetostatics/MagnetostaticsLabSceneBuilder.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticLabController.cs
```

### 1.8 电流赋值交互问题

问题表现：

- 拖动物体经过 current loader 时，视觉上经过红/蓝电流条，但电流没有设置成功。
- 螺线管可拖动后，仍然无法稳定设置电流。

原因：

- 被拖动物体吸附到可视化平面，而 current loader 位于前景深度。
- 如果使用完整 3D 距离判断，视觉重合也可能因为 `z` 不同而距离很远。

改正措施：

- 将赋予电流的方式改为点击式：
  - 先拖动或选中电流源。
  - 将电流源放到屏幕中央附近。
  - 点击左侧红/蓝 current loader。
  - current loader 给当前选中对象或屏幕中央最近的电流源赋值。
- `MagneticDesktopDraggable` 记录当前选中的 `MagneticCurrentSource`。
- `MagneticCurrentEnergizer.OnMouseDown()` 调用赋值逻辑。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticDesktopDraggable.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticCurrentEnergizer.cs
```

### 1.9 快捷键输入问题

问题表现：

- `C` 有效。
- `F`、`V`、`X` 无效或看起来无效。

原因：

- `F` 和 `V` 同时在 `MagneticLabController` 和 `MagneticFieldVisualizer` 中处理，按一次切换两次，看起来没有变化。
- `X` 只走旧输入接口，在启用新 Input System 的项目中不稳定。

改正措施：

- 快捷键统一由 `MagneticLabController` 处理。
- `MagneticFieldVisualizer` 不再读取键盘输入。
- `MagneticLabController` 处理 `C/F/V/X/+/-`。
- 同时兼容旧 Input 和新 Input System。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticFieldVisualizer.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticLabController.cs
```

### 1.10 文档与文件组织问题

问题表现：

- 原 `README.md` 更偏脚本接口说明，不适合作为学生或测试者直接阅读的实验流程。
- Lab 2 需要单独说明如何操作 Q1-Q6、如何截图、如何使用 current loader。

改正措施：

- 新增实验操作文档：

```text
Assets/Scripts/Lab2_Magnetostatics/Lab2_Experiment_Guide_README.md
```

- 文档包括打开场景、桌面鼠标测试、VR 操作、current loader 使用、Scene 1-5 与 Q1-Q6 对应关系、Q7 bonus、截图建议和常见问题。

## 2. 设计/沟通型问题

设计/沟通型问题主要和用户输入信息、实验目标描述、操作方式约定、视觉反馈有关。这类问题不一定是代码错误，但会影响实现方向和最终体验。

### 2.1 初始任务范围较大

问题表现：

- 用户要求从 Lab 2 PDF 从头实现 Unity/VR 实验。
- 任务同时包含文档解析、物理建模、场景生成、交互、磁场可视化、README、调试总结等多层内容。

影响：

- 如果一次性完成所有内容，容易出现等待时间长、问题定位困难、用户难以判断卡点的问题。

改进建议：

- 后续 Lab 建议继续按阶段推进：
  - 先解析实验文档。
  - 再生成最小可运行场景。
  - 再补交互和可视化。
  - 最后做视觉、操作和文档整理。

### 2.2 需要复用 Lab 1 的已知结论

问题表现：

- 用户指出如果问题在 Lab 1 总结中已有答案，应直接采用 Lab 1 的解决方法，不要重复提问。
- Lab 2 的 batchmode、字体清晰度、透明 cube 遮挡等问题与 Lab 1 存在相似性。

影响：

- 如果忽略 Lab 1 总结，会重复走已经验证无效的路线。

改进建议：

- 后续新 Lab 开始前先阅读对应前序 Lab 的 summary。
- 已在 Lab 1 验证过的处理方式直接沿用：
  - 手动 Unity Editor 菜单生成。
  - TextMesh 清晰化策略。
  - 禁用遮挡交互的透明 collider。
  - 保留桌面鼠标测试路径。

### 2.3 操作方式需要根据用户测试反馈收敛

问题表现：

- 初期电流赋值按“拖过 current loader”理解。
- 用户实际测试后希望改成“拖到屏幕中央，点击 current loader 赋电流”。

影响：

- 文档中的实验流程和 Unity 中实际可操作体验并不总是一一对应。
- 需要根据用户实际截图和手感反馈调整交互。

改进建议：

- 对关键交互优先采用用户可稳定执行的方式。
- 对深度相关交互，尽量避免依赖看不见的 3D 距离触发。
- 将重要状态变化改成明确点击或按钮触发。

### 2.4 视觉问题必须以用户截图为准

问题表现：

- 字体糊、小、重叠等问题单靠代码很难完全判断。
- 多次修改后，用户截图才确认当前方案可用。

影响：

- Unity Game 视图中的实际显示会受相机、场景尺度、TextMesh 参数、字体贴图和分辨率共同影响。

改进建议：

- 视觉类问题以 Game 视图截图为最终判断依据。
- 字体、布局、按钮位置、可视化区域遮挡等问题应结合用户截图迭代。
- 对已经确认可用的视觉方案写入 summary，后续 Lab 直接复用。

### 2.5 可交互对象需要明确可选中区域

问题表现：

- 用户看到螺线管模型在屏幕中，但鼠标无法拖动。
- 直观视觉对象和实际 collider 命中区域不一致。

影响：

- 用户会认为模型“不能交互”，即使代码逻辑本身存在。

改进建议：

- 对 `LineRenderer`、细线、透明对象等不适合鼠标命中的模型，主动补可交互 collider。
- 可交互范围应符合用户视觉预期，而不是只贴合几何线条。

### 2.6 实验说明需要面向操作而不是脚本

问题表现：

- 用户需要的是“Lab 2 如何进行实验”的 README。
- 脚本接口说明不足以指导学生或测试者完成实验。

影响：

- 如果 README 偏工程实现，用户仍需要额外询问如何操作场景。

改进建议：

- 每个 Lab 都应提供独立实验操作文档。
- 文档重点写：
  - 打开哪个场景。
  - 如何拖动对象。
  - 如何设置物理量。
  - 每个预设对应哪道题。
  - 如何观察和截图。

### 2.7 多 Lab 开发应持续沉淀问题库

问题表现：

- Lab 2 中多处问题与 Lab 1 相似，但也出现了磁场可视化、螺线管 collider、电流赋值方式等新问题。

影响：

- 如果不分类沉淀，Lab 3 以后容易重复调试同类问题。

改进建议：

- 每个 Lab 完成后同时保留两类 summary：
  - 过程型 summary：按问题出现顺序记录。
  - 分类型 summary：按技术型和设计/沟通型归纳。
- 只记录最终奏效的解决方法，方便后续直接复用。

## 3. 简要结论

Lab 2 的技术型问题主要集中在 Unity 生成流程、TextMesh 清晰度、透明 collider 遮挡、正交相机深度、螺线管命中区域、拖拽手感、电流赋值和快捷键输入上。

Lab 2 的设计/沟通型问题主要集中在如何复用 Lab 1 经验、如何把 PDF 实验转译成稳定可操作的 Unity 交互、如何根据用户截图调整视觉、以及如何把工程 README 改写成实验操作 README。

后续开发 Lab 3 时，建议优先复用以下结论：

- 不依赖 batchmode，优先让用户在 Unity Editor 菜单手动生成。
- 文字显示直接使用 Lab 1/Lab 2 已验证的清晰化方案。
- 透明可视化区域默认不参与鼠标命中。
- 细线或 `LineRenderer` 模型必须补稳定 collider。
- 关键物理量赋值优先使用明确点击触发，而不是依赖隐藏深度距离。
- 每个 Lab 都单独提供面向实验操作的 README。
