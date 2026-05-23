using UnityEngine;

/// <summary>
/// 步枪武器示例
/// </summary>
public class RifleWeapon : WeaponBase
{
    [Header("步枪特有属性")]
    public float bulletSpeed = 20f;
    public float spreadAngle = 2f;               // 散射角度

    protected override void Shoot()
    {
        base.Shoot();

        // 添加散射
        Quaternion spreadRotation = firePoint.rotation * Quaternion.Euler(0f, 0f, Random.Range(-spreadAngle, spreadAngle));

        // 生成子弹并设置速度
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, spreadRotation);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = bullet.transform.up * bulletSpeed;
            }
        }
    }
}
