using UnityEngine;

namespace VContainerLearn.Services
{
    public class GameService : IGameService
    {
        public void LogStatus(string message)
        {
            Debug.Log($"[GameService] {message}");
        }
    }
}
