using UnityEngine;

/// <summary>
/// 敌人初始化组件
/// 挂在场景中已有的敌人 GameObject 上，自动添加所有必要组件
/// 在 Awake 时自动配置（无需手动添加每个组件）
/// </summary>
public class EnemySetup : MonoBehaviour
{
    [Header("敌人配置预设")]
    public EnemyPreset preset = EnemyPreset.Normal;

    [Header("自定义覆盖（留0则使用预设值）")]
    public float customHealth = 0f;
    public float customSightRange = 0f;
    public float customDamage = 0f;
    public float customFireRate = 0f;

    [Header("行为")]
    [Tooltip("初始AI状态")]
    public EnemyAI.AIState initialState = EnemyAI.AIState.Idle;

    [Header("巡逻路径（可选）")]
    public Transform[] patrolPoints;

    public enum EnemyPreset
    {
        Weak,       // 弱小：低血量、低伤害、反应慢
        Normal,     // 普通：中等各项
        Elite,      // 精英：高血量、高伤害、反应快
        Sniper,     // 狙击手：远距离、高伤害、低射速
        Rusher,     // 冲锋手：高移速、近距离、高射速
    }

    void Awake()
    {
        SetupComponents();
        ApplyPreset();
    }

    private void SetupComponents()
    {
        // 确保有 Rigidbody2D
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 确保有 Collider2D
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var boxCol = gameObject.AddComponent<BoxCollider2D>();
            // 基于sprite大小自动设置
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                boxCol.size = sr.sprite.bounds.size;
            }
            else
            {
                boxCol.size = new Vector2(0.8f, 0.8f);
            }
        }
        else
        {
            col.isTrigger = false;
        }

        // 确保有 EnemyStats
        if (GetComponent<EnemyStats>() == null)
            gameObject.AddComponent<EnemyStats>();

        // 确保有 EnemyAI
        if (GetComponent<EnemyAI>() == null)
            gameObject.AddComponent<EnemyAI>();

        // 确保有 EnemyHealthBar
        if (GetComponent<EnemyHealthBar>() == null)
            gameObject.AddComponent<EnemyHealthBar>();

        var wallGuard = GetComponent<WallCollisionGuard>();
        if (wallGuard == null)
            wallGuard = gameObject.AddComponent<WallCollisionGuard>();

        // 确保有 SpriteRenderer
        if (GetComponent<SpriteRenderer>() == null)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sr.color = Color.red;
        }
    }

    public void ApplyPreset()
    {
        var stats = GetComponent<EnemyStats>();
        var ai = GetComponent<EnemyAI>();

        if (stats == null || ai == null) return;

        // 先应用预设
        switch (preset)
        {
            case EnemyPreset.Weak:
                stats.maxHealth = 50f;
                ai.sightRange = 6f;
                ai.attackRange = 5f;
                ai.bulletDamage = 5f;
                ai.fireInterval = 0.7f;
                ai.burstCount = 2;
                ai.burstCooldown = 2f;
                ai.chaseSpeed = 2f;
                ai.shootSpread = 8f;
                stats.dropAmmoAmount = 10;
                break;

            case EnemyPreset.Normal:
                stats.maxHealth = 100f;
                ai.sightRange = 8f;
                ai.attackRange = 6f;
                ai.bulletDamage = 8f;
                ai.fireInterval = 0.5f;
                ai.burstCount = 3;
                ai.burstCooldown = 1.5f;
                ai.chaseSpeed = 3f;
                ai.shootSpread = 5f;
                stats.dropAmmoAmount = 20;
                break;

            case EnemyPreset.Elite:
                stats.maxHealth = 180f;
                ai.sightRange = 10f;
                ai.attackRange = 7f;
                ai.bulletDamage = 12f;
                ai.fireInterval = 0.35f;
                ai.burstCount = 5;
                ai.burstCooldown = 1.2f;
                ai.chaseSpeed = 3.5f;
                ai.shootSpread = 3f;
                stats.dropAmmoAmount = 30;
                stats.dropMedical = true;
                break;

            case EnemyPreset.Sniper:
                stats.maxHealth = 70f;
                ai.sightRange = 14f;
                ai.attackRange = 12f;
                ai.bulletDamage = 25f;
                ai.fireInterval = 1.5f;
                ai.burstCount = 1;
                ai.burstCooldown = 2.5f;
                ai.chaseSpeed = 2f;
                ai.shootSpread = 1.5f;
                ai.bulletSpeed = 18f;
                stats.dropAmmoAmount = 15;
                break;

            case EnemyPreset.Rusher:
                stats.maxHealth = 80f;
                ai.sightRange = 7f;
                ai.attackRange = 3.5f;
                ai.bulletDamage = 6f;
                ai.fireInterval = 0.2f;
                ai.burstCount = 6;
                ai.burstCooldown = 1f;
                ai.chaseSpeed = 4.5f;
                ai.shootSpread = 10f;
                ai.standStillWhileAttacking = false;
                stats.dropAmmoAmount = 25;
                break;
        }

        // 应用自定义覆盖
        if (customHealth > 0f) stats.maxHealth = customHealth;
        if (customSightRange > 0f) ai.sightRange = customSightRange;
        if (customDamage > 0f) ai.bulletDamage = customDamage;
        if (customFireRate > 0f) ai.fireInterval = customFireRate;

        // 重新初始化血量（因为Awake顺序可能先于此脚本修改maxHealth）
        stats.ResetHealth();

        // 设置AI初始状态和巡逻路径
        ai.defaultState = initialState;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            ai.patrolPoints = patrolPoints;
            if (initialState == EnemyAI.AIState.Idle)
                ai.defaultState = EnemyAI.AIState.Patrol;
        }

        // 设置墙壁检测层。MVP 优先只使用 Wall，避免 Default 上的玩家/道具误挡视线。
        int wallLayerIndex = LayerMask.NameToLayer("Wall");
        int defaultLayer = LayerMask.NameToLayer("Default");
        int mask = 0;
        if (wallLayerIndex >= 0)
            mask = 1 << wallLayerIndex;
        else if (defaultLayer >= 0)
            mask = 1 << defaultLayer;
        if (mask == 0) mask = ~0; // 全部层
        ai.wallLayer = mask;

        var wallGuard = GetComponent<WallCollisionGuard>();
        if (wallGuard != null)
            wallGuard.wallLayer = mask;

        // 设置敌人自身层
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            gameObject.layer = enemyLayer;

        if (!gameObject.CompareTag("Enemy"))
            gameObject.tag = "Enemy";
    }
}
