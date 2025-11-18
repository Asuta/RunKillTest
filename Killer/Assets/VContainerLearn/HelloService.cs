using UnityEngine;

namespace VContainerLearn
{
    public class HelloService : IHelloService
    {
        public void SayHello(string message)
        {
            Debug.Log($"HelloService says: {message}");
        }
    }
}
