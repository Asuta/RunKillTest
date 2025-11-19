using UnityEngine;
using System;
using System.Collections.Generic;

public class BounceCube : MonoBehaviour, IConfigurable
{
    #region 变量声明
    public float bounceForceMultiplier = 10f;
    #endregion

    #region IConfigurable 实现
    public string GetConfigTitle()
    {
        return $"BounceCube ({gameObject.name})";
    }

    public List<ConfigItem> GetConfigItems()
    {
        var items = new List<ConfigItem>();

        // 配置 bounceForceMultiplier
        items.Add(new ConfigItem(
            "bounceForceMultiplier",
            bounceForceMultiplier,
            ConfigType.Float,
            (newValue) =>
            {
                bounceForceMultiplier = Convert.ToSingle(newValue);
                Debug.Log($"[BounceCube Config] bounceForceMultiplier updated to: {bounceForceMultiplier}");
            }
        ));

        return items;
    }
    #endregion

    #region Unity生命周期方法
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion

    #region 碰撞处理
    private void OnCollisionEnter(Collision collision)
    {
        // 检查碰撞对象的layer是否为Player
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("Player collided with BounceCube!");
            
            // 获取碰撞法线
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 normal = contact.normal;
                
                // 绘制法线用于调试（红色）
                Debug.DrawRay(contact.point, normal * 2f, Color.red, 2f);
                Debug.Log("BounceCube碰撞法线: " + normal);
                
                // 通过碰撞体附加的Rigidbody来查找IBounceCubeProvider接口
                if (collision.rigidbody != null)
                {
                    IBounceCubeProvider bounceProvider = collision.rigidbody.GetComponent<IBounceCubeProvider>();
                    if (bounceProvider != null)
                    {
                        bounceProvider.OutHandleBounceCube(normal * -bounceForceMultiplier);
                    }
                    else
                    {
                        // 如果在Rigidbody上找不到，尝试在其父对象中查找
                        bounceProvider = collision.rigidbody.GetComponentInParent<IBounceCubeProvider>();
                        if (bounceProvider != null)
                        {
                            bounceProvider.OutHandleBounceCube(normal * -bounceForceMultiplier);
                        }
                    }
                    #endregion
                }
            }
        }
    }
}
