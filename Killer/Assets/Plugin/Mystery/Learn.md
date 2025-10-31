
# Squiggle 文档（中文翻译）

## 概述
Squiggle 的目的是帮助调试代码。你可以跟踪变量以查看它们随时间的变化，使用 `DebugGraph` 类将数据输出到 Squiggle 窗口，或将 `ComponentMonitor` 附加到一个 GameObject 上并选择要监视的变量。

通过点击菜单 “Window -> Squiggle” 打开 Graph Console 窗口。示例图表可以通过在 Squiggle 窗口右上角点击 “?” 按钮查看，或运行随附的示例场景。

---

## 绘制图表（Graphing Values）

- 快速可视化数据：`DebugGraph.Log(Value)`
  随着数据随时间变化会形成图表。支持的类型包括浮点数（Floating Point）、整数（Integer）、Vector2、Vector3、Color、Color32、布尔（Boolean）和枚举（Enum）。

- 创建多个图表：`DebugGraph.Log(Name, Value)`
  匿名的 Log 由其堆栈跟踪（文件和行号）标识；带名称的日志会记录到该名称的图表中。

- 为数据着色：`DebugGraph.Log(Name, Value, Color)`
  每个记录的值可以有不同颜色，从而可以创建多色图表，更容易定位需要关注的数据。

- 标记时间戳：`DebugGraph.Log(Name, Value, Color, Time)`
  默认使用 `Time.realtimeSinceStartup` 作为时间戳，但你可以指定自己的时间值（当需要在同一时间步内采样多次时很有用）。例如，可用于发现函数中最耗时的部分或可视化代码执行顺序。提示：使用颜色来标识代码的不同部分。

- 创建字符串图表：`DebugGraph.Write(...)`
  使用 `DebugGraph.Write` 而不是 `DebugGraph.Log` 可以将任何对象输出为字符串。每个输出字符串将在图上以竖线表示。

- 跟踪路径：`DebugGraph.Draw(...)`
  使用 `DebugGraph.Draw` 而不是 `DebugGraph.Log` 可以跟踪一个随时间变化的 `Vector2` 值形成的线条。

- 创建颜色渐变：
  任何记录的 `Color` 或 `Color32` 值都会自动以颜色渐变和其 RGBA 值的图形两种形式显示。

---

## 比较数值（Comparing Values）

- 快速比较数据：`DebugGraph.MultiLog(Color, Value)`
  每种颜色会形成自己的图表，随着数据变化可用于在同一图表上比较多个值。

- 多重比较：`DebugGraph.MultiLog(Name, Color, Value)`
  匿名的 MultiLog 会按调用它们的方法分组；带名称的会记录到该名称的图表中。

- 命名值：`DebugGraph.MultiLog(Name, Color, Value, ValueName)`
  在包含多个图线的图表中可以为每条图线单独命名。

- 标记时间戳：`DebugGraph.MultiLog(Name, Color, Value, Time)`
  默认使用 `Time.realtimeSinceStartup` 作为时间戳，但可指定自定义时间以便在同一时间步内多次采样。

---

## 导出数据（Exporting Values）

- 访问原始数据：`DebugGraph.ExportData(Name)`
  返回一个 IPlottableGraphPoint 数组列表（List of IPlottableGraphPoint[]），代表该命名图表记录的所有数据。

- 保存到文件：`DebugGraph.Save(Name, OutputFolder)`
  将指定名称的图表保存到输出文件夹中的文件。

- 保存所有图表到文件夹：`DebugGraph.SaveAllToFolder(OutputFolder)`
  所有图表将分别保存为输出文件夹中的单独文件。

- 保存所有图表到单个文件：`DebugGraph.SaveAllToFile(Filename)`
  所有图表将保存到指定文件名的单一文件中。

---

## Squiggle 窗口（Squiggle Window）

- 查看记录值：将鼠标沿图表移动以查看数值，点击图表可以冻结/解冻时间线采样器。
- 查看最小值、最大值和中间值：这些信息显示在图形控制台窗口的左侧。
- 锁定最小值和最大值：点击值旁边的锁图标进行锁定。
- 缩放（Zoom）：按住 Ctrl（在 macOS 上为 Cmd），在图表上按左键然后上下拖动进行缩放。（按住 Shift 将同时缩放所有图表）
- 平移（Pan）：按住 Ctrl / Cmd，在图表上按左键然后左右拖动进行平移。（按住 Shift 将同时平移所有图表）
- 拉伸（Stretch）：通过拖动图表下方的灰色条来改变图表高度。

---

## 工具栏按钮（Toolbar Buttons）

- Record：启用时会记住所有图表数据。禁用时仅显示图表数据的短预览。
- Clear：从图形控制台删除所有记录的数据。
- Shrink：将所有图表缩至最小高度。
- Restore：将所有图表恢复到初始的缩放/平移状态。
- Snap To End：将时间线采样器移动到图表末端，以查看最新输入的图形值。
- Lock All：锁定所有最小值和最大值。
- Unlock All：解锁所有最小值和最大值。
- Settings：打开 Squiggle 设置窗口。
- Export Data：打开图表导出窗口，提供将图表数据导出到文件的选项。
- ?：打开帮助窗口，其中包含示例图表。

---

## 组件监视器（Component Monitor）

- 监视组件变量：将 `ComponentMonitor` 附加到一个 GameObject，然后点击 “[ + ]” 按钮并选择要跟踪的变量。
- 停止监视组件变量：点击要停止监视的变量左侧的 “[ - ]” 按钮。
- 选择采样时间：从 SampleTime 下拉菜单中选择 `Update`、`FixedUpdate` 或 `LateUpdate`。

---

## Debug Graph Renderer

- 在游戏中渲染图表：将 `DebugGraphRenderer` 附加到一个 GameObject，然后在 Graph Name 字段中输入要渲染的图表名称。
- 自定义图表材质：可以为 `DebugGraphRenderer` 指定自定义材质。共有三个可用的纹理坐标：
  - 0: Graph Point (X, Y)
  - 1: Graph Distance (X, Y)
  - 2: Normalized Point (X, Y)

---

## 高级用法（Mystery.Graphing 命名空间）

- 自定义绘图：`DebugGraph.AddCustomGraph(Name, IDebugGraph)`
  你可以通过继承 `LinearDebugGraph<X, Y>` 或实现 `IDebugGraph` 接口来创建自定义图表。然后使用 `AddCustomGraph` 将其显示在图形控制台窗口中。

- 自定义图表控制：`DebugGraph.AddCustomGraph(IGraphConsole)`
  你可以通过创建 `SingleGraphConsole<X, Y>`、`MultiGraphConsole<X, Y>` 的实例，或继承 `GraphConsole`，或实现 `IGraphConsole` 接口来以编程方式控制自定义图表。然后使用 `AddCustomGraph` 将其显示在图形控制台窗口中。

---

## 问题与建议
- 邮箱：mark@mysterytech.net
- 推特：@mysterytechs

--- 

（翻译保留了原文中的方法名和术语，以便于在代码中直接使用。）