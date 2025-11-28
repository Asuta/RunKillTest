// 文件路径: GameLifetimeScope.cs
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MyGame
{
    /// <summary>
    /// 这是项目的组合根。
    /// 它负责配置依赖注入容器，注册所有需要的服务和组件。
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {


        [SerializeField] SaveLoadManager _saveLoadManagerPrefab;


        /// <summary>
        /// VContainer 在构建容器时会调用此方法。
        /// 我们在这里注册所有的依赖项。
        /// </summary>
        protected override void Configure(IContainerBuilder builder)
        {
            // // 注册 HelloWorldService，并指定其生命周期为单例。
            // // 整个应用程序中只会有一个 HelloWorldService 实例。
            // builder.Register<HelloWorldService>(Lifetime.Singleton);

            // // 注册 GamePresenter 作为入口点。
            // // VContainer 会自动管理它的生命周期（如调用 Start()）。
            // // 它默认也是 Singleton 生命周期。
            // builder.RegisterEntryPoint<GamePresenter>();

            // 注册一个已经在场景中存在的 MonoBehaviour 组件。
            // 这使得 VContainer 可以将其注入到其他类中（例如 GamePresenter）。
            // builder.RegisterComponent(helloScreen);

            builder.RegisterComponentInNewPrefab(_saveLoadManagerPrefab, Lifetime.Singleton);

            // 因为是第一次获取，VContainer 就会立刻去实例化那个 Prefab。
            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<SaveLoadManager>();
            });



        }
    }
}
