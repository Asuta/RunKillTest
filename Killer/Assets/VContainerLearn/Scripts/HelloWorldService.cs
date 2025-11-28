// 文件路径: HelloWorldService.cs
namespace MyGame
{
    /// <summary>
    /// 一个简单的服务，用于演示核心业务逻辑。
    /// 它不依赖于任何框架。
    /// </summary>
    public class HelloWorldService
    {
        public void Hello()
        {
            UnityEngine.Debug.Log("Hello world");
        }
    }
}
