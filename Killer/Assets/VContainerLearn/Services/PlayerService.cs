using UnityEngine;

namespace VContainerLearn.Services
{
    public class PlayerService : IPlayerService
    {
        readonly IGameService gameService;

        public PlayerService(IGameService gameService)
        {
            this.gameService = gameService;
        }

        public void DoPlayerAction()
        {
            gameService.LogStatus("PlayerService: 执行玩家动作");
            Debug.Log("PlayerService: DoPlayerAction called");
        }
    }
}
