using VContainer;
using VContainer.Unity;

public class RootLifetimeScope : LifetimeScope
{
    public int Value;
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册全局单例 AudioManager
        builder.Register<AudioManager>(Lifetime.Singleton);
        
        // 注册全局单例 UserData
        builder.Register<UserData>(Lifetime.Singleton);
    }
}
