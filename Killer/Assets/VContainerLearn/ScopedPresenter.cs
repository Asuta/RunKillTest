using UnityEngine;
using VContainer.Unity;

namespace VContainerLearn
{
    // 在子作用域中运行的演示类
    // 它通过构造函数注入 ScopedCounter（在父容器中注册为 Lifetime.Scoped）
    // 因为它被注册在子作用域（ChildScopeLifetimeScope）中，所以将为该子作用域获得自己的 ScopedCounter 实例
    public class ScopedPresenter : IStartable
    {
        private readonly ScopedCounter _counter;

        public ScopedPresenter(ScopedCounter counter)
        {
            _counter = counter;
            Debug.Log($"ScopedPresenter constructed. Counter scope id: {_counter.ScopeId}");
        }

        public void Start()
        {
            // 在 Start 时对 counter 进行一次递增并打印
            var newCount = _counter.Increment();
            Debug.Log($"ScopedPresenter Start -> counter[{_counter.ScopeId}] = {newCount}");
        }
    }
}
