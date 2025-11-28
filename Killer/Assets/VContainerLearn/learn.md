我已经完成了 VContainer 学习示例代码的编写，所有文件都已创建在 `Assets/VContainerLearn/` 目录下。

**已创建的文件清单：**

1.  **服务类 (Services):**
    *   `AudioManager.cs`: 全局音频服务。
    *   `UserData.cs`: 全局用户数据服务。
    *   `MenuController.cs`: 主菜单专用服务。
    *   `MapManager.cs`: 战斗场景地图服务。
    *   `EnemyManager.cs`: 战斗场景敌人服务。

2.  **生命周期作用域 (LifetimeScopes):**
    *   `RootLifetimeScope.cs`: 注册了全局单例 `AudioManager` 和 `UserData`。
    *   `MenuLifetimeScope.cs`: 注册了 `MenuController`。
    *   `GameLifetimeScope.cs`: 注册了 `MapManager` 和 `EnemyManager`。

3.  **演示组件 (Demo Components):**
    *   `MenuDemo.cs`: 演示如何在菜单场景注入并使用 `AudioManager` (来自父级) 和 `MenuController` (来自本级)。
    *   `GameDemo.cs`: 演示如何在战斗场景注入并使用 `AudioManager` (来自父级) 以及 `MapManager` 和 `EnemyManager` (来自本级)。

**使用说明 (在 Unity 编辑器中):**

1.  **Root Scope 设置:**
    *   创建一个 GameObject (例如命名为 "RootScope")。
    *   挂载 `RootLifetimeScope` 脚本。
    *   (可选) 将其设置为 Project Context 或者在场景加载时作为父级 Scope。

2.  **Menu Scene 设置:**
    *   创建一个 GameObject (例如 "MenuScope")。
    *   挂载 `MenuLifetimeScope` 脚本。
    *   在 `MenuLifetimeScope` 的 Inspector 中，将 `Parent` 设置为 `RootLifetimeScope` (如果是场景切换架构，通常由 VContainer 的 Project Context 自动处理父子关系，或者手动指定)。
    *   创建一个 GameObject 挂载 `MenuDemo` 脚本，观察控制台输出。

3.  **Game Scene 设置:**
    *   创建一个 GameObject (例如 "GameScope")。
    *   挂载 `GameLifetimeScope` 脚本。
    *   同样设置 Parent 关系。
    *   创建一个 GameObject 挂载 `GameDemo` 脚本，观察控制台输出。

代码已准备就绪，请手动编译 Unity 项目以解决引用问题。