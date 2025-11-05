using UnityEngine;
using VContainer;
using VContainer.Unity;
using VContainerLearn.Services;

namespace VContainerLearn.Install
{
    // 将此脚本添加到场景中的一个 GameObject（例如名为 "LifetimeScope" 的对象）
    // VContainer 会在该对象的容器中注册下面的服务
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 单例服务
            builder.Register<IGameService, GameService>(Lifetime.Singleton);
            builder.Register<IPlayerService, PlayerService>(Lifetime.Singleton);

            // 敌人服务注册为瞬时，每次解析会得到新的实例
            builder.Register<IEnemyService, EnemyService>(Lifetime.Transient);

            // 如果你想要在容器创建时执行某些初始化代码，可以注册一个 EntryPoint
            // builder.RegisterEntryPoint<MyEntryPoint>();
        }
    }
}
