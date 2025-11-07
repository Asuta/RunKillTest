using UnityEngine;
using VContainer.Unity;

namespace VContainerLearn
{
    // 演示父容器解析 scoped 服务的类 (IStartable)
    public class ParentScopedUser : IStartable
    {
        private readonly ScopedCounter _counter;

        public ParentScopedUser(ScopedCounter counter)
        {
            _counter = counter;
            Debug.Log($"ParentScopedUser constructed. Counter scope id: {_counter.ScopeId}");
        }

        public void Start()
        {
            var newCount = _counter.Increment();
            Debug.Log($"ParentScopedUser Start -> counter[{_counter.ScopeId}] = {newCount}");
        }
    }
}
