using UnityEngine;

/// <summary>
/// 子弹脚本
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("子弹属性")]
    public float damage = 10f;
    public float lifetime = 3f;                  // 自动销毁时间
    public LayerMask hitLayers;                  // 可命中的层

    [Header("效果")]
    public GameObject hitEffectPrefab;           // 命中特效
    public GameObject trailEffect;               // 拖尾特效（可选）

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否命中可伤害物体
        if (((1 << other.gameObject.layer) & hitLayers) != 0)
        {
            // 尝试造成伤害
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // 生成命中特效
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // 销毁子弹
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// 可伤害接口
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}
