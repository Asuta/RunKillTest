using VContainer.Unity;

namespace VContainerLearn
{
    // IStartable 是 VContainer 提供的一个接口
    // 实现了这个接口的类，在容器构建完成后会自动调用其 Start 方法
    public class GamePresenter : IStartable
    {
        private readonly IHelloService _helloService;

        // 通过构造函数注入 IHelloService
        public GamePresenter(IHelloService helloService)
        {
            _helloService = helloService;
        }

        public void Start()
        {
            // 在启动时调用服务
            _helloService.SayHello("VContainer is working!");
        }
    }
}
