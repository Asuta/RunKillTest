# 命令模式撤销/重做系统

这是一个基于命令模式实现的、高性能且健壮的Unity关卡编辑器撤销/重做功能演示。该系统经过重构，解决了原始设计中的性能瓶颈和架构耦合问题。

## 文件结构

- `ICommand.cs` - 核心命令接口，定义了 `Execute()` 和 `Undo()` 方法
- `IDisposableCommand.cs` - (在ICommand.cs中) 可释放资源的命令接口，用于解耦资源管理
- `MoveCommand.cs` - 移动物体的命令实现
- `CreateObjectCommand.cs` - 创建物体的命令实现，实现了资源释放
- `DeleteObjectCommand.cs` - 删除物体的命令实现，实现了资源释放
- `GrabCommand.cs` - 抓取并移动物体的命令实现
- `CommandHistory.cs` - **[已优化]** 命令历史管理器，使用 `LinkedList` 实现高性能管理，并解耦了资源清理逻辑
- `LevelEditorManager.cs` - 关卡编辑器主控制器
- `CommandTestDemo.cs` - 简单的测试演示脚本
- `TestCube.prefab` - 测试用的立方体预制体
- `TestSphere.prefab` - 测试用的球体预制体

## 使用方法

### 1. 基本设置

1. 在场景中创建一个空的GameObject
2. 添加 `LevelEditorManager` 组件
3. 在Inspector中设置预制体引用（TestCube和TestSphere）
4. 确保场景中有主摄像机

### 2. 编辑器操作

- **左键拖拽**: 选择并移动物体
- **1键**: 在鼠标位置创建立方体
- **2键**: 在鼠标位置创建球体
- **Delete键**: 删除选中的物体
- **Ctrl+Z**: 撤销上一个操作
- **Ctrl+Y**: 重做上一个撤销的操作

### 3. 测试演示

1. 在场景中创建一个空的GameObject
2. 添加 `CommandTestDemo` 组件
3. 设置预制体引用
4. 运行场景，系统会自动执行一系列测试操作
5. 使用U键撤销，R键重做，C键清空历史

## 核心原理

### 命令模式与资源管理

每个用户操作都被封装成一个命令对象，实现了 `ICommand` 接口。对于需要管理资源的命令，还需实现 `IDisposableCommand` 接口。

```csharp
public interface ICommand
{
    void Execute(); // 执行操作
    void Undo();    // 撤销操作
}

public interface IDisposableCommand : ICommand
{
    void Dispose(); // 释放资源
}
```

### 高效的双列表管理

为了解决性能问题，我们不再使用 `Stack`，而是采用 `LinkedList` 来管理历史记录。

- **撤销列表**: 存储已执行的命令
- **重做列表**: 存储已撤销的命令

当执行新命令时：
1. 执行命令
2. 添加到撤销列表末尾
3. 清空重做列表
4. 检查历史记录限制（高效地移除最旧命令）

当撤销时：
1. 从撤销列表末尾取出命令
2. 执行撤销操作
3. 将命令添加到重做列表末尾

当重做时：
1. 从重做列表末尾取出命令
2. 重新执行命令
3. 将命令添加回撤销列表末尾

### 解耦的资源清理

`CommandHistory` 不再关心具体命令的类型。当需要清理资源时，它只检查命令是否实现了 `IDisposableCommand` 接口，如果是，就调用其 `Dispose` 方法。这实现了完美的解耦。

## 扩展功能

### 添加新的命令类型

1. 实现 `ICommand` 接口
2. 如果命令持有需要最终销毁的资源，则同时实现 `IDisposableCommand` 接口
3. 在构造函数中记录必要的状态信息
4. 实现 `Execute()` 和 `Undo()` 方法，并增加健壮性检查

示例：修改颜色的命令

```csharp
public class ChangeColorCommand : ICommand
{
    private Renderer _renderer;
    private Color _oldColor;
    private Color _newColor;
    
    public ChangeColorCommand(Renderer renderer, Color newColor)
    {
        _renderer = renderer;
        if (_renderer != null)
        {
            _oldColor = renderer.material.color;
        }
        _newColor = newColor;
    }
    
    public void Execute()
    {
        if (_renderer != null)
        {
            _renderer.material.color = _newColor;
        }
    }
    
    public void Undo()
    {
        if (_renderer != null)
        {
            _renderer.material.color = _oldColor;
        }
    }
}
```

### 复合命令

对于需要同时执行多个子命令的操作，可以实现复合命令：

```csharp
public class CompositeCommand : ICommand
{
    private List<ICommand> _commands = new List<ICommand>();
    
    public void AddCommand(ICommand command)
    {
        _commands.Add(command);
    }
    
    public void Execute()
    {
        foreach (var command in _commands)
        {
            command.Execute();
        }
    }
    
    public void Undo()
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
```

## 系统优势

1.  **高性能**: 使用 `LinkedList` 使得历史记录的维护操作（特别是移除旧记录）几乎不耗时，即使在历史记录很长的情况下也能保持流畅。
2.  **架构清晰**: 通过 `IDisposableCommand` 接口，将资源管理的责任完全交还给命令本身，`CommandHistory` 无需了解任何具体实现，符合开闭原则。
3.  **健壮性强**: 所有命令都增加了对对象引用的空值检查，有效防止因对象被意外销毁而导致的系统崩溃。
4.  **易于扩展**: 添加新命令无需修改核心管理器，只需实现相应接口即可，系统维护成本极低。

## 注意事项

1.  **性能优化**: 对于创建/删除操作，使用 `SetActive(true/false)` 而不是 `Instantiate/Destroy` 来实现撤销/重做，在命令被永久移除时才真正销毁。
2.  **状态记录**: 确保命令对象记录了足够的状态信息来执行和撤销操作。
3.  **引用管理**: 所有命令都应防御性地处理可能为 `null` 的对象引用。
4.  **内存管理**: 系统已内置历史记录大小限制，并会自动清理超出限制的资源。

## 适用场景

- 关卡编辑器
- 地图编辑工具
- 动画编辑器
- 任何需要撤销/重做功能的编辑工具

这个系统提供了清晰的架构、卓越的性能和良好的扩展性，可以根据具体需求进行定制和扩展，是构建复杂编辑工具的理想选择。