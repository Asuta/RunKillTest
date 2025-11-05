using UnityEngine;
using VContainer;
using VContainerLearn.Services;

namespace VContainerLearn.Presenters
{
    // 演示如何在 MonoBehaviour 中注入并调用服务
    public class EnemySpawner : MonoBehaviour
    {
        [Inject]
        IEnemyService enemyService;

        public int spawnOnStart = 2;

        void Start()
        {
            if (enemyService != null)
            {
                for (int i = 0; i < spawnOnStart; i++)
                {
                    enemyService.SpawnEnemy();
                }
            }
            else
            {
                Debug.LogWarning("EnemyService 未注入 — 请确认这个 GameObject 在 LifetimeScope 的管理下，或使用 Container.Instantiate(prefab)");
            }
        }
    }
}
