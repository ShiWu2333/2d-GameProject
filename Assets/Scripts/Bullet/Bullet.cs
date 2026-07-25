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
    private Vector2 previousPosition;
    private Rigidbody2D rb;
    private bool hasHit;

    private void Start()
    {
        previousPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        int playerBulletLayer = LayerMask.NameToLayer("PlayerBullet");
        if (playerBulletLayer >= 0)
            gameObject.layer = playerBulletLayer;

        // 若武器未赋值 origin，则以生成位置为起点
        if (origin == Vector2.zero)
            origin = transform.position;
        Destroy(gameObject, MaxLifetime);

        // 如果 hitLayers 未设置，自动包含 Enemy 层
        if (hitLayers.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                hitLayers = 1 << enemyLayer;
            else
                hitLayers = ~0; // 所有层
        }
    }

    private void Update()
    {
        SweepForHit();

        if (Vector2.Distance(transform.position, origin) >= maxRange)
            Destroy(gameObject);

        previousPosition = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleHit(other, transform.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 碰到实体墙壁直接销毁
        if (collision.collider != null)
            TryHandleHit(collision.collider, transform.position);
    }

    private void SweepForHit()
    {
        if (hasHit) return;

        Vector2 currentPosition = transform.position;
        Vector2 delta = currentPosition - previousPosition;
        float distance = delta.magnitude;
        if (distance <= 0.001f) return;

        int wallLayer = LayerMask.NameToLayer("Wall");
        int mask = hitLayers.value;
        if (wallLayer >= 0)
            mask |= 1 << wallLayer;

        RaycastHit2D[] hits = Physics2D.RaycastAll(previousPosition, delta.normalized, distance, mask);
        RaycastHit2D closest = default;
        bool hasClosest = false;

        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
            if (!IsRelevantHit(hit.collider)) continue;

            if (!hasClosest || hit.distance < closest.distance)
            {
                closest = hit;
                hasClosest = true;
            }
        }

        if (hasClosest)
            TryHandleHit(closest.collider, closest.point);
    }

    private bool TryHandleHit(Collider2D other, Vector2 hitPosition)
    {
        if (hasHit || other == null) return false;

        // 不伤害玩家（玩家子弹不打玩家）
        if (other.GetComponentInParent<PlayerStats>() != null) return false;
        if (other.GetComponentInParent<PlayerController>() != null) return false;

        // 不被地面物品/容器阻挡
        if (other.GetComponentInParent<GroundItem>() != null) return false;
        if (other.GetComponentInParent<LootContainer>() != null) return false;

        // 检查是否是可伤害目标
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            var damageTarget = damageable as Component;
            GameObject targetObject = damageTarget != null ? damageTarget.gameObject : other.gameObject;
            float finalDamage = CalculateFinalDamage(targetObject);
            damageable.TakeDamage(finalDamage);

            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);

            hasHit = true;
            Destroy(gameObject);
            return true;
        }

        // 非可伤害目标：如果碰撞体不是触发器（实体墙壁），则子弹销毁
        if (!other.isTrigger)
        {
            hasHit = true;
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private bool IsRelevantHit(Collider2D other)
    {
        if (other == null) return false;
        if (other.GetComponentInParent<PlayerStats>() != null) return false;
        if (other.GetComponentInParent<PlayerController>() != null) return false;
        if (other.GetComponentInParent<GroundItem>() != null) return false;
        if (other.GetComponentInParent<LootContainer>() != null) return false;
        if (other.GetComponentInParent<IDamageable>() != null) return true;
        return !other.isTrigger;
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
