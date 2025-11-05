using UnityEngine;

namespace VContainerLearn.Services
{
    public class EnemyService : IEnemyService
    {
        readonly IGameService gameService;

        public EnemyService(IGameService gameService)
        {
            this.gameService = gameService;
        }

        public void SpawnEnemy()
        {
            var go = new GameObject("Enemy");
            var view = go.AddComponent<EnemyView>();
            gameService.LogStatus("EnemyService: 生成了一个敌人");
            Debug.Log("EnemyService: SpawnEnemy created Enemy GameObject");
        }
    }
}
