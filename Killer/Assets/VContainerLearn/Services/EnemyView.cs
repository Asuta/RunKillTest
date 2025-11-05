using UnityEngine;

namespace VContainerLearn.Services
{
    public class EnemyView : MonoBehaviour
    {
        void Awake()
        {
            Debug.Log("EnemyView Awake - 敌人已创建");
        }

        void OnEnable()
        {
            Debug.Log("EnemyView OnEnable");
        }
    }
}
