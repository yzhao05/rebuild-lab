# Lab 3 Wave Polarization 开发错误与解决方法总结

本文总结 Lab 3 Wave Polarization 从 handbook 设计、Unity 场景生成、交互调试到可视化修正过程中出现的问题，以及最终采用的有效解决方法。

本文只记录最终有效方案，供后续 Lab 或 Lab 3 继续迭代时复用。

## 1. 没有先沿用 Lab 1/Lab 2 已验证流程

### 问题表现

Lab 3 开始时已经明确要求：

- 根据 handbook 构建 Lab 3。
- 第一步先询问设计问题。
- 如果问题在 `summary` 文件夹中已有解决方法，则沿用旧方案。

过程中虽然先读取了 Lab 1/Lab 2 summary，但仍短暂尝试运行：

```powershell
& 'E:\unity\2022\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\rebuild\rebuild lab' -logFile 'D:\rebuild\rebuild lab\tmp\lab3-unity-compile.log'
```

该行为违背了 Lab 2 summary 中已经确定的结论：本项目不应主动依赖 batchmode 生成或检查 Unity 编译，Unity 生成和 Console 反馈应交给用户在 Editor 中手动执行。

### 最终解决方法

- 停止主动运行 Unity batchmode。
- Lab 3 后续只提供 Editor 菜单入口：

```text
Tools > Wave Polarization Lab > Build Lab 3 Scene
```

- 由用户在 Unity Editor 中手动执行生成、进入 Play Mode、提供 Game/Console 截图。
- Codex 只根据用户截图和 Console 反馈继续修正代码。

## 2. 第一版画面语义不清晰

### 问题表现

用户看到第一版画面后无法判断：

- 中间半透明箱子是什么。
- 两边蓝色和橘色方块是什么。
- 两条正弦波是否可以拖动。
- 按 1/2 后场景差异在哪里。

画面虽然包含了偏振实验所需对象，但缺少明确的视觉层级和操作语义。

### 原因

- 正弦波、观察区、可拖拽元件、预设按钮都在同一画面中，缺少角色区分。
- 文本标签过多且部分重叠。
- `Wave polarization observation stage` 这类说明偏工程化，不如直接提示用户操作。
- 可拖动对象和显示对象没有明显区分。

### 最终解决方法

- 将观察区标题改为：

```text
Drop films here
```

- 给输入/输出波加说明：

```text
input wave display
output wave display
```

- 给左右元件架加说明：

```text
drag blue LP films
drag orange QWP films
```

- 将元件文字压缩：

```text
Linear polarizer -> LP
Quarter-wave plate -> QWP
axis 45 deg -> axis 45
fast 45 deg -> fast 45
```

- 将备用元件架下移，顶部说明板上移，结果面板移到右侧上方。
- 在 README 中明确写出：正弦波只是显示，不可拖动；蓝色/橘色方块才是可拖拽元件。

## 3. 可拖拽对象与观察区判定不一致

### 问题表现

用户反馈：把方块拖进中间箱子后，出射光没有变化。

### 原因

Lab 3 初始实现使用 3D `Bounds.Contains` 判断偏振片是否进入观察区。正交相机下容易出现：

- 屏幕上看起来已经进入箱子。
- 但实际 3D `z` 深度或 Bounds 判定仍未进入。

这个问题与 Lab 2 中“螺线管拖进透明可视化区后不显示磁场”的深度错觉属于同类问题。

### 最终解决方法

改为只按屏幕平面的 `x/y` 坐标判断元件是否进入观察区，不再依赖隐藏的 3D 深度。

涉及文件：

```text
Assets/Scripts/Lab3_WavePolarization/PolarizationWaveVisualizer.cs
```

核心处理：

```csharp
static bool IsInsideStageOnScreenPlane(Vector3 point, Bounds bounds)
{
    return point.x >= bounds.min.x && point.x <= bounds.max.x
        && point.y >= bounds.min.y && point.y <= bounds.max.y;
}
```

同时状态栏改为显示当前被识别的元件：

```text
active films: LP 45, QWP 0 -> LHCP
```

这样用户可以立即判断拖入是否被系统识别。

## 4. 拖拽命中不稳定

### 问题表现

用户尝试拖动蓝色/橘色方块时，可能感觉拖不动或不好选中。

### 原因

偏振片本体较小，初始 BoxCollider 与视觉对象几乎等大，鼠标射线稍微偏离就可能无法命中。

### 最终解决方法

放大可拖拽命中盒：

```csharp
box.size = new Vector3(1.35f, 1.35f, 0.48f);
```

涉及文件：

```text
Assets/Scripts/Lab3_WavePolarization/PolarizationDesktopDraggable.cs
```

同时把桌面拖拽灵敏度从 `2f` 改为用户要求的：

```csharp
dragSensitivity = 3f;
```

## 5. 按 1-5 后出射光变化不明显

### 问题表现

用户反馈：按 `1/2/3/4/5` 时，出射光变化不明显。

### 原因

第一版出射光主要用一条黄色正弦曲线表达。对偏振实验来说，单条正弦线只能显示某个投影分量，很难直观看出：

- 线偏振方向变化。
- 圆偏振旋转。
- 椭圆偏振长短轴差异。
- 左旋/右旋差别。

即使 Jones 向量计算结果已经改变，视觉上仍可能像“同一条正弦波”。

### 最终解决方法

在输出区增加 E-field 矢量采样线：

- 线偏振：白色矢量方向基本固定。
- 圆偏振：白色矢量沿传播方向旋转。
- 椭圆偏振：白色矢量旋转且长度变化明显。

同时在右侧偏振轨迹中加入即时 E 矢量和端点标记。

涉及文件：

```text
Assets/Scripts/Lab3_WavePolarization/PolarizationWaveVisualizer.cs
```

新增逻辑：

```text
BuildOutputVectorSamples(...)
BuildOrbitInstantVector(...)
```

## 6. 1-5 默认预设差异不足

### 问题表现

即使按下不同预设，用户仍不容易从画面看出差异。

### 原因

第一版预设配置更偏向 handbook 的抽象问题，而不是“按下按钮立刻展示明显差异”。例如 Q1 和 Q2 的黄色输出波形在投影视图中差别不够强。

### 最终解决方法

调整 1-5 默认配置，使每个预设加载后立即呈现明显不同的输出：

- `1`: `LP 45`，线偏振，高强度。
- `2`: `QWP 0`，圆偏振。
- `3`: `QWP 0 + LP 90`，线偏振，强度约为 0.5。
- `4`: `QWP 90`，与 2 相反手性的圆偏振。
- `5`: `LP 0 + QWP 30`，明显椭圆偏振。

涉及文件：

```text
Assets/Scripts/Lab3_WavePolarization/PolarizationLabController.cs
```

## 7. 波形动画速度过快

### 问题表现

用户希望白色矢量转速慢一点，慢一半，并在终点加箭头。

### 最终解决方法

将动画速度从：

```csharp
animationSpeed = 3.8f;
```

改为：

```csharp
animationSpeed = 1.9f;
```

并给白色 E-field 矢量末端增加箭头：

```text
CreateArrowHead(...)
```

涉及文件：

```text
Assets/Scripts/Lab3_WavePolarization/PolarizationWaveVisualizer.cs
```

## 8. 场景说明文档需要同步更新

### 问题表现

当画面语义不断调整后，如果 README 不同步更新，后续使用者仍会误解：

- 哪些对象可拖动。
- 中间箱子是否只是装饰。
- 正弦波是否代表可交互对象。

### 最终解决方法

同步更新：

```text
Assets/Scripts/Lab3_WavePolarization/README.md
Assets/Scripts/Lab3_WavePolarization/Lab3_Experiment_Guide_README.md
```

明确写入：

- 青色/黄色正弦波只是显示，不可拖动。
- 中间半透明箱子是 active observation stage。
- 只有箱子内的 films 会参与 Jones-vector cascade。
- 蓝色方块是 linear polarizer。
- 橘色方块是 quarter-wave plate。

## 9. 当前推荐运行与测试方式

### 生成场景

在 Unity Editor 中手动执行：

```text
Tools > Wave Polarization Lab > Build Lab 3 Scene
```

不要通过 Codex 主动运行 Unity batchmode。

### Play Mode 测试

1. 打开：

```text
Assets/Scenes/Lab3_WavePolarization.unity
```

2. 点击 Play。
3. 按 `1-5` 观察预设差异。
4. 拖动蓝色 LP 或橘色 QWP 到 `Drop films here` 区域。
5. 观察状态栏是否显示：

```text
active films: ...
```

6. 按住 `R` 拖动进行连续旋转。
7. 使用 `Q/E` 调整当前选中元件角度。
8. 观察黄色输出波、白色 E-field 矢量、右侧偏振轨迹和结果标签。

## 10. 当前 Lab 3 主要文件结构

```text
Assets/
  Editor/
    Lab3_WavePolarization/
      WavePolarizationLabSceneBuilder.cs

  Scripts/
    Lab3_WavePolarization/
      PolarizationElementType.cs
      PolarizationElement.cs
      PolarizationElementFactory.cs
      PolarizationDesktopDraggable.cs
      PolarizationWaveVisualizer.cs
      PolarizationLabController.cs
      PolarizationLabButton.cs
      PolarizationScenePresentation.cs
      README.md
      Lab3_Experiment_Guide_README.md
```

## 11. 后续 Lab 复用结论

- 不主动使用 Unity batchmode；使用 Editor 菜单和用户截图反馈。
- 对正交相机下的拖拽/进入区域判定，优先使用屏幕平面的 `x/y`，避免隐藏深度造成误判。
- 可视化不能只显示物理量的单一投影；应显示能区分核心现象的几何特征。
- 可交互对象必须与显示对象有明确视觉区别。
- 状态栏应显示系统实际识别到的对象，方便判断“拖进去是否生效”。
- README 必须随交互语义变化同步更新。
