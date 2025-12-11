using UnityEngine;
using System;
using System.Collections.Generic;

public class AutoTurret : MonoBehaviour, ICanBeHit, IConfigurable
{
    #region IConfigurable Implementation
    public string GetConfigTitle()
    {
        return $"Auto Turret ({gameObject.name})";
    }

    public List<ConfigItem> GetConfigItems()
    {
        var items = new List<ConfigItem>();

        items.Add(new ConfigItem("Health", health, ConfigType.Int, (v) => health = Convert.ToInt32(v)));
        items.Add(new ConfigItem("Fire Interval", fireInterval, ConfigType.Float, (v) => fireInterval = Convert.ToSingle(v)));
        items.Add(new ConfigItem("Rotation Speed", rotationSpeed, ConfigType.Float, (v) => rotationSpeed = Convert.ToSingle(v)));

        return items;
    }
    #endregion

    [Header("Stats")]
    public int health = 100;
    public float fireInterval = 2.0f;
    public float rotationSpeed = 5.0f;

    [Header("Parts")]
    public Transform turretHead; // The part that rotates
    public Transform firePoint;  // Where bullets spawn
    public Transform body;       // The main body (for bullet reflection target)
    public GameObject bulletPrefab;
    public GameObject deathEffect;

    private Transform target;
    private float lastFireTime;
    private bool isDead = false;

    private void Start()
    {
        GlobalEvent.CheckPointReset.AddListener(OnCheckPointReset);
        if (body == null) body = transform;
    }

    private void OnDestroy()
    {
        GlobalEvent.CheckPointReset.RemoveListener(OnCheckPointReset);
    }

    private void Update()
    {
        if (isDead) return;

        if (target != null)
        {
            // Rotate towards target
            if (turretHead != null)
            {
                Vector3 direction = target.position - turretHead.position;
                // direction.y = 0; // Optional: Keep rotation flat
                if (direction != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    turretHead.rotation = Quaternion.Slerp(turretHead.rotation, lookRotation, Time.deltaTime * rotationSpeed);
                }
            }

            // Fire
            if (Time.time - lastFireTime >= fireInterval)
            {
                Fire();
                lastFireTime = Time.time;
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Fire()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            TurretBullet turretBullet = bullet.GetComponent<TurretBullet>();
            if (turretBullet != null)
            {
                turretBullet.target = target;
                turretBullet.createTurret = this;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            OnDeath();
        }
    }

    public void OnDeath()
    {
        isDead = true;
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }
        
        // Disable children to hide turret but keep gameobject active for logic if needed, 
        // or just deactivate whole object. Enemy.cs deactivates children.
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
        
        // Optional: Reactivate after delay like Enemy.cs
        StartCoroutine(ReactivateAfterDelay());
    }

    private System.Collections.IEnumerator ReactivateAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        OnCheckPointReset();
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    private void OnCheckPointReset()
    {
        target = null;
        health = 100;
        isDead = false;
        lastFireTime = Time.time; // Reset fire timer
        GetComponentInChildren<TurretCheckBox>()?.ResetDetection();
    }
}
