# 命令模式撤销/重做系统

这是一个基于命令模式实现的Unity关卡编辑器撤销/重做功能演示。

## 文件结构

- `ICommand.cs` - 命令接口，定义了Execute()和Undo()方法
- `MoveCommand.cs` - 移动物体的命令实现
- `CreateObjectCommand.cs` - 创建物体的命令实现
- `DeleteObjectCommand.cs` - 删除物体的命令实现
- `CommandHistory.cs` - 命令历史管理器，使用两个栈管理撤销/重做操作
- `LevelEditorManager.cs` - 关卡编辑器主控制器
- `CommandTestDemo.cs` - 简单的测试演示脚本
- `TestCube.prefab` - 测试用的立方体预制体
- `TestSphere.prefab` - 测试用的球体预制体

## 使用方法

### 1. 基本设置

1. 在场景中创建一个空的GameObject
2. 添加`LevelEditorManager`组件
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
2. 添加`CommandTestDemo`组件
3. 设置预制体引用
4. 运行场景，系统会自动执行一系列测试操作
5. 使用U键撤销，R键重做，C键清空历史

## 核心原理

### 命令模式

每个用户操作都被封装成一个命令对象，实现了`ICommand`接口：

```csharp
public interface ICommand
{
    void Execute(); // 执行操作
    void Undo();    // 撤销操作
}
```

### 双栈管理

- **撤销栈(Undo Stack)**: 存储已执行的命令
- **重做栈(Redo Stack)**: 存储已撤销的命令

当执行新命令时：
1. 执行命令
2. 压入撤销栈
3. 清空重做栈

当撤销时：
1. 从撤销栈弹出命令
2. 执行撤销操作
3. 压入重做栈

当重做时：
1. 从重做栈弹出命令
2. 重新执行命令
3. 压入撤销栈

## 扩展功能

### 添加新的命令类型

1. 实现`ICommand`接口
2. 在构造函数中记录必要的状态信息
3. 实现`Execute()`和`Undo()`方法

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
        _oldColor = renderer.material.color;
        _newColor = newColor;
    }
    
    public void Execute()
    {
        _renderer.material.color = _newColor;
    }
    
    public void Undo()
    {
        _renderer.material.color = _oldColor;
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

## 注意事项

1. **性能优化**: 对于创建/删除操作，使用`SetActive(true/false)`而不是`Instantiate/Destroy`
2. **状态记录**: 确保命令对象记录了足够的状态信息来执行和撤销操作
3. **引用管理**: 注意避免对已销毁对象的引用
4. **内存管理**: 对于大量操作，考虑限制历史记录的大小

## 适用场景

- 关卡编辑器
- 地图编辑工具
- 动画编辑器
- 任何需要撤销/重做功能的编辑工具

这个系统提供了清晰的架构和良好的扩展性，可以根据具体需求进行定制和扩展。