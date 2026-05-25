using UnityEngine;

/// <summary>
/// 子弹 v3
/// 新增：弹药数据注入、护甲穿透计算、射程限制
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("由武器在生成时赋值")]
    public float    damage   = 10f;
    public float    maxRange = 20f;
    public AmmoData ammoData;           // 弹药数据（含穿透等级）

    [HideInInspector]
    public Vector2 origin;

    [Header("碰撞")]
    public LayerMask hitLayers;

    [Header("效果")]
    public GameObject hitEffectPrefab;

    private const float MaxLifetime = 10f;

    private void Start()
    {
        origin = transform.position;
        Destroy(gameObject, MaxLifetime);
    }

    private void Update()
    {
        if (Vector2.Distance(transform.position, origin) >= maxRange)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) == 0) return;

        float finalDamage = CalculateFinalDamage(other.gameObject);

        IDamageable damageable = other.GetComponent<IDamageable>();
        damageable?.TakeDamage(finalDamage);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    /// <summary>
    /// 计算最终人体伤害：
    /// 若目标有 ArmorComponent，走护甲穿透逻辑；否则全额伤害
    /// </summary>
    private float CalculateFinalDamage(GameObject target)
    {
        var armor = target.GetComponent<ArmorComponent>();
        if (armor != null)
            return armor.ProcessHit(damage, ammoData);

        // 无护甲：弹药数据的基础倍率仍然生效（对无甲目标全额）
        if (ammoData != null)
            return damage * ammoData.baseDamageMultiplier;

        return damage;
    }
}

/// <summary>可伤害接口</summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}
