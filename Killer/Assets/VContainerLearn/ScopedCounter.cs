using UnityEngine;

namespace VContainerLearn
{
    // 一个简单的计数器，演示 scoped 实例在不同 LifetimeScope 中是不同的
    public class ScopedCounter
    {
        private static int s_nextScopeId = 0;
        private readonly int _scopeId;
        private int _count;

        public ScopedCounter()
        {
            _scopeId = System.Threading.Interlocked.Increment(ref s_nextScopeId);
            Debug.Log($"ScopedCounter created for scope #{_scopeId}");
        }

        public int Increment()
        {
            _count++;
            Debug.Log($"ScopedCounter[#{_scopeId}] increment -> {_count}");
            return _count;
        }

        public int Count => _count;
        public int ScopeId => _scopeId;
    }
}
