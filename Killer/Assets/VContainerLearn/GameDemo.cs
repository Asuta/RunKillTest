using UnityEngine;
using VContainer;

public class GameDemo : MonoBehaviour
{
    private AudioManager _audioManager;
    private MapManager _mapManager;
    private EnemyManager _enemyManager;

    [Inject]
    public void Construct(AudioManager audioManager, MapManager mapManager, EnemyManager enemyManager)
    {
        _audioManager = audioManager;
        _mapManager = mapManager;
        _enemyManager = enemyManager;
    }

    void Start()
    {
        Debug.Log("--- Game Demo Start ---");
        _audioManager.PlaySound("BattleBGM");
        _mapManager.LoadMap("Forest");
        _enemyManager.SpawnEnemy("Goblin");
    }
}