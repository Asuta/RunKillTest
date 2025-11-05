# VContainer 学习示例（简体中文）

此文件夹包含一组简单示例，演示如何在 Unity 中使用 VContainer 进行依赖注入。

主要文件：

- `Install/GameLifetimeScope.cs` — 派生自 `VContainer.Unity.LifetimeScope`，在 `Configure` 中注册服务。
- `Services/IGameService.cs`, `Services/GameService.cs` — 一个用于记录日志的基础服务。
- `Services/IPlayerService.cs`, `Services/PlayerService.cs` — 演示构造函数注入的服务。
- `Services/IEnemyService.cs`, `Services/EnemyService.cs`, `Services/EnemyView.cs` — 演示服务创建 GameObject（简单敌人）的例子。
- `Presenters/PlayerPresenter.cs` — 在 MonoBehaviour 中使用 `[Inject]` 注入 `IPlayerService`。
- `Presenters/EnemySpawner.cs` — 在 MonoBehaviour 中注入 `IEnemyService` 并生成敌人。

如何使用：

1. 在场景中新建一个空的 GameObject，命名为 `LifetimeScope`（或其它名称）。
2. 给该对象添加组件 `GameLifetimeScope`（文件位置：`Assets/VContainerLearn/Install/GameLifetimeScope.cs`）。
3. 在场景中创建两个 GameObject，一个挂载 `PlayerPresenter`，另一个挂载 `EnemySpawner`。
   - 确保这些对象位于 `LifetimeScope` 管理的层级下（一般放在同一场景层级即可）。
4. 运行场景，控制台会输出注入与服务调用的日志，并在场景中创建简单的 `Enemy` GameObject。

说明与注意事项：

- MonoBehaviour 字段注入（使用 `[Inject]`）只在由 VContainer 管理或通过容器实例化的对象上生效。
  如果你直接从 Unity 编辑器手动在场景中放置对象，确保这些对象位于 LifetimeScope 的管理范围内，或改用 `container.Instantiate(prefab)` 创建。
- 服务可以注册为 `Singleton`、`Transient` 等生命周期。示例中 `PlayerService` 为单例，`EnemyService` 为瞬时。
- 这是一个入门级示例，旨在说明常见模式：注册服务、构造注入与字段注入、通过服务创建 GameObject 等。

下一步建议：

- 尝试把 `Enemy` 做成预制体并使用 `container.Instantiate(prefab)` 来确保 MonoBehaviour 中的依赖也被注入。
- 学习 VContainer 的 `RegisterEntryPoint` 与 `IEntryPoint`（或类似生命周期接口）以在容器创建时自动执行初始化逻辑。
