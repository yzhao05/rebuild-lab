# Lab 2 Magnetostatics 开发问题总结

本文总结 Lab 2 Magnetostatics 从文档实现到可运行 Unity/VR 实验场景过程中遇到的问题，以及最终奏效的解决方法。

本文只保留最终有效方案，不记录中间尝试失败的方案。

## 1. Unity batchmode 生成不适合本项目调试

### 问题表现

曾尝试使用命令行自动生成 Lab 2 场景：

```powershell
& 'E:\unity\2022\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\rebuild\rebuild lab' -executeMethod MagnetostaticsLabSceneBuilder.BuildScene -logFile 'D:\rebuild\rebuild lab\tmp\lab2-unity-build.log'
```

该方式和 Lab 1 中遇到的问题一致：

- 静默 batchmode 不利于观察 Unity 是否正在编译、导入资源、加载项目或卡住。
- 如果 Unity Editor 已经打开，batchmode 容易受项目锁、编译状态或 Editor 状态影响。
- 场景视觉问题必须通过 Game 视图和 Console 迭代，batchmode 不能提供足够反馈。

### 最终解决方法

不再使用 batchmode 自动生成。

改为让用户在 Unity Editor 中手动执行菜单：

```text
Tools > Magnetostatics Lab > Build Lab 2 Scene
```

执行前要求：

- 退出 Play Mode。
- 等待 Unity 编译完成。
- 如果 Console 有红色编译错误，先修复错误再生成。

生成日志保留在：

```text
tmp/magnetostatics-lab-build-progress.log
```

## 2. 场景生成时报 `Value cannot be null. Parameter name: dest`

### 问题表现

执行 Lab 2 场景生成器时，Unity 弹窗：

```text
Magnetostatics Lab Build Failed
Value cannot be null.
Parameter name: dest
```

日志定位到：

```text
MagneticCurrentSource.UpdateVisuals()
Renderer.GetPropertyBlock(...)
```

### 原因

`MaterialPropertyBlock` 在 Editor 生成期间可能尚未初始化，直接传入 `Renderer.GetPropertyBlock()` 会触发 `dest` 为空的异常。

### 最终解决方法

在 `MagneticCurrentSource` 中改为懒加载 `MaterialPropertyBlock`：

- 使用前先确保 `propertyBlock != null`。
- 跳过 `TextMesh` 的 renderer，避免给文字 renderer 设置电流源颜色。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticCurrentSource.cs
```

## 3. 字体又小又糊

### 问题表现

Lab 2 初期场景中顶部说明文字、按钮文字和底部提示文字出现：

- 字体模糊。
- 字体过小。
- 文字与背景板或按钮重叠。
- 修改字体分辨率后，视觉大小反而变小。

### 最终解决方法

沿用 Lab 1 最终奏效的文字清晰化策略：

- 使用 `TextMesh fontSize = 384`。
- 不使用 `768 + 缩小 characterSize` 的方案。
- 保持较大的视觉字号。
- Play Mode 中由 `MagneticScenePresentation` 强制修正旧场景残留参数。
- 将相机正交视野收紧到 `legibleOrthographicSize = 5.6`。
- 对字体贴图调用 `RequestCharactersInTexture(...)`。
- 字体贴图使用 `FilterMode.Trilinear` 和 `anisoLevel = 8`。
- 顶部说明板采用少字、分区、短句。
- 底部预设按钮和控制按钮分层摆放，避免重叠。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticScenePresentation.cs
Assets/Editor/Lab2_Magnetostatics/MagnetostaticsLabSceneBuilder.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticLabController.cs
```

## 4. 透明可视化区域遮挡二次拖动

### 问题表现

电流源在屏幕边缘可以二次拖动，但拖到屏幕中央透明可视化区域后无法再次拖动。

### 原因

透明的 `Magnetic Field Visualization Cube` 带有 collider。  
在屏幕中央区域，鼠标点击优先命中透明 cube，导致后方的直导线或螺线管无法收到二次拖动输入。

### 最终解决方法

禁用透明可视化 cube 的 collider：

- 新生成场景时，builder 直接将 stage collider 设为 disabled。
- 已生成的旧场景在 Play Mode 中由 `MagneticScenePresentation` 自动禁用该 collider。

涉及文件：

```text
Assets/Editor/Lab2_Magnetostatics/MagnetostaticsLabSceneBuilder.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticScenePresentation.cs
```

## 5. 右侧螺线管放入区域后不显示磁场

### 问题表现

从右侧拖入螺线管，看起来已经在透明可视化区域内，但磁场线没有按预期更新。

### 原因

正交相机下，物体在屏幕上看起来进入了透明区域，但其真实 3D 深度仍可能停留在右侧源架的 `z=-4.65`，没有进入可视化区域中心平面。

### 最终解决方法

Lab 2 的桌面拖拽统一吸附到 `MagneticFieldVisualizer` 所在的可视化平面：

- 拖拽开始时寻找 `MagneticFieldVisualizer`。
- 将物体的 `z` 坐标固定到可视化平面。
- 后续拖拽只改变屏幕平面上的位置。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticDesktopDraggable.cs
```

## 6. 棍子拖动过灵敏或过迟钝

### 问题表现

最初拖拽时鼠标稍微移动，物体在屏幕中移动很远。  
后来调低后又过于迟钝。

### 原因

原先使用射线和平面相交来计算拖拽位置，在从源架深度切换到可视化区域深度时会产生坐标跳变。

### 最终解决方法

将拖拽逻辑改为基于正交相机的屏幕位移：

- 鼠标屏幕位移转换成世界坐标位移。
- 不再使用旧深度平面计算初始命中点。
- 当前最终灵敏度设置为：

```csharp
dragSensitivity = 2f
```

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticDesktopDraggable.cs
```

## 7. 螺线管不能稳定选中或拖动

### 问题表现

直导线可以拖动，但螺线管无法拖动或很难选中。

### 原因

螺线管由 `LineRenderer` 画出，原始命中区域不够稳定。  
正面点击时，细线圈结构不适合作为可靠鼠标命中对象。

### 最终解决方法

为螺线管补稳定碰撞体：

- 运行时自动识别 solenoid。
- 给 solenoid 添加较大的 `BoxCollider`。
- 同时放大原 `CapsuleCollider`。
- 新生成场景时 builder 也直接创建这些碰撞体。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticDesktopDraggable.cs
Assets/Editor/Lab2_Magnetostatics/MagnetostaticsLabSceneBuilder.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticLabController.cs
```

## 8. 电流加载方式不符合实际操作

### 问题表现

拖动电流源经过 current loader 时，视觉上经过了红/蓝电流条，但电流没有设置成功。

### 原因

拖拽对象已经被吸附到可视化平面，而 current loader 位于前景深度。  
如果用完整 3D 距离判断，视觉上重合也可能因为 `z` 不同而距离很远。

### 最终解决方法

将赋予电流的操作改为：

1. 先拖动或选中电流源。
2. 将电流源放在屏幕中央附近。
3. 点击左侧红/蓝 current loader。
4. current loader 给当前选中或屏幕中央最近的电流源赋值。

实现上：

- `MagneticDesktopDraggable` 记录当前选中的 `MagneticCurrentSource`。
- `MagneticCurrentEnergizer.OnMouseDown()` 调用赋值逻辑。
- 如果没有当前选中对象，则选择屏幕中央最近的电流源。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticDesktopDraggable.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticCurrentEnergizer.cs
```

## 9. F/V/X 快捷键无效

### 问题表现

`C` 有用，但 `F`、`V`、`X` 没有效果。

### 原因

`F` 和 `V` 同时在 `MagneticLabController` 和 `MagneticFieldVisualizer` 中处理，按一次会切换两次，看起来没有变化。  
`X` 只使用旧输入接口，在启用新 Input System 的项目中可能不稳定。

### 最终解决方法

快捷键统一由 `MagneticLabController` 处理：

- `MagneticFieldVisualizer` 不再读取键盘输入。
- `MagneticLabController` 处理 `C/F/V/X/+/-`。
- 同时兼容旧 Input 和新 Input System。

涉及文件：

```text
Assets/Scripts/Lab2_Magnetostatics/MagneticFieldVisualizer.cs
Assets/Scripts/Lab2_Magnetostatics/MagneticLabController.cs
```

## 10. 需要单独的实验操作说明

### 问题表现

原 `README.md` 更偏向脚本接口说明，不适合作为学生或测试者直接阅读的实验流程。

### 最终解决方法

新增实验操作文档：

```text
Assets/Scripts/Lab2_Magnetostatics/Lab2_Experiment_Guide_README.md
```

内容包括：

- 如何打开 Lab 2 场景。
- 桌面鼠标测试方式。
- VR 操作方式。
- current loader 使用方法。
- Scene 1-5 与 Q1-Q6 的对应关系。
- Q7 bonus 提示。
- 截图建议。
- 常见问题。

## 11. 当前 Lab 2 主要文件结构

```text
Assets/
  Editor/
    Lab2_Magnetostatics/
      MagnetostaticsLabSceneBuilder.cs

  Scripts/
    Lab2_Magnetostatics/
      MagneticSourceType.cs
      MagneticCurrentSource.cs
      MagneticCurrentEnergizer.cs
      MagneticDesktopDraggable.cs
      MagneticFieldVisualizer.cs
      MagneticLabController.cs
      MagneticPresetPad.cs
      MagneticLabButton.cs
      MagneticScenePresentation.cs
      README.md
      Lab2_Experiment_Guide_README.md
```

## 12. 当前推荐运行方式

### 生成场景

在 Unity Editor 中手动执行：

```text
Tools > Magnetostatics Lab > Build Lab 2 Scene
```

不要在 Play Mode 中执行生成器。

### 运行实验

1. 打开：

```text
Assets/Scenes/Lab2_Magnetostatics.unity
```

2. 点击 Play。
3. 使用鼠标左键拖动直导线或螺线管。
4. 将电流源放到屏幕中央附近。
5. 点击左侧红/蓝 current loader 赋予电流。
6. 观察透明区域中的磁场线。
7. 使用 `1-5`、`C`、`F`、`V`、`X`、`+/-` 进行实验。
