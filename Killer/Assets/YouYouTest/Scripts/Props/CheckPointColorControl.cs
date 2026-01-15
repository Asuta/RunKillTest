using UnityEngine;
using VInspector;
 
public class CheckPointColorControl : MonoBehaviour
{
    [SerializeField]
    private Color targetColor = Color.white;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateParticleSystemColors(targetColor);
    }

    /// <summary>
    /// 设置当前物体及所有子物体的粒子系统颜色
    /// </summary>
    /// <param name="color">目标颜色</param>
    /// <param name="includeInactive">是否包含未激活的子物体</param>
    // [ContextMenu("更新所有子物体粒子系统颜色")]
    [Button("更新所有子物体粒子系统颜色")]
    public void UpdateParticleSystemColors(Color color, bool includeInactive = true)
    {
        // 获取自身及所有子物体中的 ParticleSystem 组件（包括未激活的）
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }
}
