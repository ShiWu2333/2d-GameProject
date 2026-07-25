using UnityEngine;

/// <summary>
/// 敌人子弹
/// 碰到玩家造成伤害（经过护甲计算）
/// 碰到墙壁销毁
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [Header("属性")]
    public float damage = 8f;
    public float speed = 12f;
    public float maxLifetime = 5f;
    public float maxRange = 20f;

    [HideInInspector]
    public Vector2 direction;

    [Header("穿透")]
    [Tooltip("敌人子弹的穿透等级（用于护甲计算）")]
    public ArmorPenetrationLevel penetrationLevel = ArmorPenetrationLevel.Low;

    private Vector3 origin;
    private Rigidbody2D rb;
    private Vector2 previousPosition;
    private bool hasHit;

    void Start()
    {
        origin = transform.position;
        previousPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (direction.sqrMagnitude > 0.01f)
                rb.velocity = direction.normalized * speed;
            else
                rb.velocity = transform.right * speed;
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (enemyBulletLayer >= 0)
            gameObject.layer = enemyBulletLayer;

        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        SweepForHit();

        // 超出射程销毁
        if (Vector2.Distance(transform.position, origin) >= maxRange)
            Destroy(gameObject);

        previousPosition = transform.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHandleHit(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider != null)
            TryHandleHit(collision.collider);
    }

    private void SweepForHit()
    {
        if (hasHit) return;

        Vector2 currentPosition = transform.position;
        Vector2 delta = currentPosition - previousPosition;
        float distance = delta.magnitude;
        if (distance <= 0.001f) return;

        int mask = 0;
        int wallLayer = LayerMask.NameToLayer("Wall");
        int defaultLayer = LayerMask.NameToLayer("Default");
        if (wallLayer >= 0) mask |= 1 << wallLayer;
        if (defaultLayer >= 0) mask |= 1 << defaultLayer;
        if (mask == 0) mask = ~0;

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
            TryHandleHit(closest.collider);
    }

    private bool TryHandleHit(Collider2D other)
    {
        if (hasHit || other == null) return false;

        // 不伤害其他敌人
        if (other.GetComponentInParent<EnemyStats>() != null) return false;
        if (other.GetComponentInParent<EnemyAI>() != null) return false;
        // 不被地面物品阻挡
        if (other.GetComponentInParent<GroundItem>() != null) return false;
        // 不被容器阻挡（直接穿过）
        if (other.GetComponentInParent<LootContainer>() != null) return false;

        // 命中玩家
        var playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats != null)
        {
            float finalDamage = CalculatePlayerDamage(playerStats.gameObject);
            playerStats.TakeDamage(finalDamage);
            hasHit = true;
            Destroy(gameObject);
            return true;
        }

        // 命中墙壁/障碍物（任何不是触发器的碰撞体）
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
        if (other.GetComponentInParent<EnemyStats>() != null) return false;
        if (other.GetComponentInParent<EnemyAI>() != null) return false;
        if (other.GetComponentInParent<GroundItem>() != null) return false;
        if (other.GetComponentInParent<LootContainer>() != null) return false;
        if (other.GetComponentInParent<PlayerStats>() != null) return true;
        return !other.isTrigger;
    }

    /// <summary>
    /// 计算对玩家的最终伤害（考虑护甲）
    /// </summary>
    private float CalculatePlayerDamage(GameObject playerObj)
    {
        var armor = playerObj.GetComponent<ArmorComponent>();
        if (armor == null || !armor.HasArmor)
            return damage;

        // 使用ArmorComponent的穿透计算方法
        return armor.ProcessHitWithPenetration(damage, penetrationLevel);
    }
}
