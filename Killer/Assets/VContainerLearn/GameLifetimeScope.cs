using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册 MapManager
        builder.Register<MapManager>(Lifetime.Singleton);
        
        // 注册 EnemyManager
        builder.Register<EnemyManager>(Lifetime.Singleton);
    }
}
