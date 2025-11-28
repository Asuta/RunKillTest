using VContainer;
using VContainer.Unity;

public class MenuLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 注册 MenuController
        builder.Register<MenuController>(Lifetime.Singleton);

        // 注册场景中的 MenuDemo，确保它能被注入依赖
        builder.RegisterComponentInHierarchy<MenuDemo>();
    }
}
