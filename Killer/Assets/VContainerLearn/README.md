# VContainer 简单示例使用说明

我已经为你创建了以下脚本：

1.  **IGameService.cs**: 定义服务的接口。
2.  **GameService.cs**: 服务的具体实现逻辑。
3.  **BasicExampleController.cs**: 使用服务的消费者（通过 `[Inject]` 注入）。
4.  **GameLifetimeScope.cs**: 依赖注入的配置入口（Composition Root）。

## 如何在 Unity 场景中设置

请按照以下步骤在 `Assets/VContainerLearn/VContainerLearn.unity` 场景中进行配置：

1.  **创建 Scope 对象**:
    *   在场景中创建一个空的 GameObject，命名为 `GameLifetimeScope`。
    *   将 `GameLifetimeScope.cs` 脚本挂载到这个物体上。

2.  **创建消费者对象**:
    *   在场景中创建一个空的 GameObject，命名为 `GameController`。
    *   将 `BasicExampleController.cs` 脚本挂载到这个物体上。

3.  **关联注入 (重要)**:
    *   为了让 VContainer 知道要给 `GameController` 注入依赖，你有两种简单的选择：
        *   **方法 A (推荐)**: 将 `GameController` 物体拖拽成为 `GameLifetimeScope` 物体的**子物体**。VContainer 会自动扫描 Scope 的子物体进行注入。
        *   **方法 B**: 选中 `GameLifetimeScope` 物体，在 Inspector 面板中找到 `Auto Inject Game Objects` 列表，将 `GameController` 拖进去。

4.  **运行**:
    *   点击 Play 运行游戏。
    *   查看 Console 控制台，你应该能看到：
        *   `[GameService] Game Started! Service is working.`
        *   `[BasicExampleController] Current Score: 100`

## 代码解释

*   **GameLifetimeScope.cs**:
    ```csharp
    builder.Register<GameService>(Lifetime.Singleton).As<IGameService>();
    ```
    这行代码告诉 VContainer：当有人需要 `IGameService` 时，请给他一个 `GameService` 的实例（而且是单例，全局共享一份）。

*   **BasicExampleController.cs**:
    ```csharp
    [Inject]
    public void Construct(IGameService gameService) { ... }
    ```
    这告诉 VContainer：这个组件初始化时，请自动把 `IGameService` 塞进来。