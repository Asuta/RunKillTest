using UnityEngine;
using VContainer;
using VContainerLearn.Services;

namespace VContainerLearn.Presenters
{
    // 演示如何在 MonoBehaviour 中使用字段注入
    public class PlayerPresenter : MonoBehaviour
    {
        [Inject]
        IPlayerService playerService;

        void Start()
        {
            if (playerService != null)
            {
                playerService.DoPlayerAction();
            }
            else
            {
                Debug.LogWarning("PlayerService 未注入 — 请确认这个 GameObject 在 LifetimeScope 的管理下，或使用 Container.Instantiate(prefab)");
            }
        }
    }
}
