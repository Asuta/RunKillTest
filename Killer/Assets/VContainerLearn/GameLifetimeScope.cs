using VContainer;
using VContainer.Unity;
using VContainerLearn;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册 IHelloService 接口和它的实现 HelloService
        // Lifetime.Singleton 意味着在整个游戏生命周期中，只会有一个 HelloService 实例
        builder.Register<IHelloService, HelloService>(Lifetime.Singleton);

        // 注册 GamePresenter
        // .AsImplementedInterfaces() 会让容器把它注册为它所实现的 IStartable 接口
        // 这使得 VContainer 在启动时能自动调用它的 Start 方法
        builder.Register<GamePresenter>(Lifetime.Singleton).AsImplementedInterfaces();

        // 注册 ScopedCounter 为 Scoped：在不同的 LifetimeScope 中会拥有不同的实例
        builder.Register<ScopedCounter>(Lifetime.Scoped);

        // 注册一个父作用域的使用者，用来演示父作用域解析到的 scoped 实例
        builder.Register<ParentScopedUser>(Lifetime.Singleton).AsImplementedInterfaces();
    }
}
