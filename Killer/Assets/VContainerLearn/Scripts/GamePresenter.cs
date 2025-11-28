// 文件路径: GamePresenter.cs
using VContainer;
using VContainer.Unity;

namespace MyGame
{
    /// <summary>
    /// 负责协调服务和视图的 Presenter。
    /// 它实现了 IStartable，以便在游戏开始时执行初始化逻辑。
    /// </summary>
    public class GamePresenter : IStartable
    {
        // 通过构造函数注入依赖项
        readonly HelloWorldService helloWorldService;
        readonly HelloScreen helloScreen;
        readonly IObjectResolver _container;


        public GamePresenter(
            HelloWorldService helloWorldService,
            HelloScreen helloScreen,
            IObjectResolver container)
        {
            this.helloWorldService = helloWorldService;
            this.helloScreen = helloScreen;
            this._container = container;
        }

        /// <summary>
        /// 当 VContainer 容器构建完成后，此方法会在 Unity 的 Start 阶段被调用。
        /// </summary>
        void IStartable.Start()
        {
            // 将按钮的点击事件连接到服务
            helloScreen.HelloButton.onClick.AddListener(() => helloWorldService.Hello());
        }
    }
}
