using UnityEngine;

/// <summary>
/// 敌人AI：状态机驱动
/// 状态：Idle(驻守) → Patrol(巡逻) → Chase(追击) → Attack(攻击) → Dead(死亡)
/// 
/// 行为逻辑：
/// - 视野范围内检测到玩家 → 进入追击
/// - 追击到射程内 → 开火攻击
/// - 玩家脱离视野 → 回到巡逻/驻守
/// - 有墙壁遮挡（射线检测）→ 不算看到玩家
/// </summary>
[RequireComponent(typeof(EnemyStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    // ══════════════════════════════════════════════════
    //  状态枚举
    // ══════════════════════════════════════════════════
    public enum AIState
    {
        Idle,       // 驻守（不移动，但检测玩家）
        Patrol,     // 巡逻（在巡逻点之间移动）
        Chase,      // 追击（朝玩家移动）
        Attack,     // 攻击（停下来射击）
        Dead,       // 死亡
    }

    // ══════════════════════════════════════════════════
    //  Inspector 配置
    // ══════════════════════════════════════════════════

    [Header("AI类型")]
    [Tooltip("初始行为：Idle=驻守不动，Patrol=来回巡逻")]
    public AIState defaultState = AIState.Idle;

    [Header("视野检测")]
    [Tooltip("视野半径（能看到玩家的最大距离）")]
    public float sightRange = 8f;

    [Tooltip("听觉半径（玩家开枪时的感知距离，无视遮挡）")]
    public float hearRange = 12f;

    [Tooltip("失去目标后的追踪记忆时间")]
    public float memoryDuration = 3f;

    [Tooltip("墙壁遮挡检测层（射线穿过这些层则视为遮挡）")]
    public LayerMask wallLayer;

    [Header("移动")]
    [Tooltip("巡逻移速")]
    public float patrolSpeed = 1.5f;

    [Tooltip("追击移速")]
    public float chaseSpeed = 3f;

    [Tooltip("无NavMesh时的墙壁探测距离")]
    public float obstacleProbeDistance = 0.8f;

    [Tooltip("无NavMesh时绕墙转向强度")]
    public float obstacleAvoidanceStrength = 0.75f;

    [Tooltip("追击时与玩家保持的最小距离（到达后开火）")]
    public float attackRange = 6f;

    [Tooltip("攻击时是否站定（true=不动，false=缓慢靠近）")]
    public bool standStillWhileAttacking = true;

    [Header("巡逻路径")]
    [Tooltip("巡逻点（留空则在出生点附近随机巡逻）")]
    public Transform[] patrolPoints;

    [Tooltip("到达巡逻点后等待时间")]
    public float patrolWaitTime = 2f;

    [Tooltip("随机巡逻半径（无巡逻点时使用）")]
    public float randomPatrolRadius = 4f;

    [Header("攻击")]
    [Tooltip("射击间隔（秒）")]
    public float fireInterval = 0.5f;

    [Tooltip("每发伤害")]
    public float bulletDamage = 8f;

    [Tooltip("子弹速度")]
    public float bulletSpeed = 12f;

    [Tooltip("射击散射角度")]
    public float shootSpread = 5f;

    [Tooltip("连射次数（射完后暂停）")]
    public int burstCount = 3;

    [Tooltip("连射间歇时间")]
    public float burstCooldown = 1.5f;

    [Tooltip("子弹预制体（留空则自动创建）")]
    public GameObject enemyBulletPrefab;

    [Header("警戒")]
    [Tooltip("被攻击后是否立即进入追击（即使看不到玩家）")]
    public bool aggroOnHit = true;

    // ══════════════════════════════════════════════════
    //  运行时状态
    // ══════════════════════════════════════════════════

    public AIState CurrentState { get; private set; }

    private EnemyStats stats;
    private Rigidbody2D rb;
    private Transform player;
    private Vector3 spawnPosition;

    // 视野
    private bool canSeePlayer;
    private float lastSeenTime;
    private Vector3 lastKnownPlayerPos;

    // 巡逻
    private int currentPatrolIndex;
    private float patrolWaitTimer;
    private Vector3 randomPatrolTarget;
    private bool hasRandomTarget;

    // 攻击
    private float fireTimer;
    private int burstShotsFired;
    private float burstCooldownTimer;

    // ══════════════════════════════════════════════════
    //  生命周期
    // ══════════════════════════════════════════════════

    void Awake()
    {
        stats = GetComponent<EnemyStats>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        spawnPosition = transform.position;
        CurrentState = defaultState;
    }

    void Start()
    {
        // 查找玩家
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;

        // 注册被击事件
        if (stats != null)
        {
            if (aggroOnHit && stats.onHit != null)
                stats.onHit.AddListener(OnTakeHit);

            if (stats.onDeath != null)
                stats.onDeath.AddListener(OnDie);
        }
    }

    void Update()
    {
        if (stats.IsDead) return;
        if (player == null) return;

        UpdateVision();
        UpdateState();
    }

    void FixedUpdate()
    {
        if (stats.IsDead) return;
        if (player == null) return;

        UpdateMovement();
    }

    // ══════════════════════════════════════════════════
    //  视野检测
    // ══════════════════════════════════════════════════

    private void UpdateVision()
    {
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        canSeePlayer = false;

        if (distToPlayer <= sightRange)
        {
            // 射线检测遮挡
            Vector2 dir = (player.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position, dir, distToPlayer, wallLayer);

            if (hit.collider == null)
            {
                // 没有墙壁遮挡，能看到玩家
                canSeePlayer = true;
                lastSeenTime = Time.time;
                lastKnownPlayerPos = player.position;
            }
        }
    }

    /// <summary>是否在记忆时间内（刚丢失视野但还记得玩家位置）</summary>
    private bool HasMemoryOfPlayer()
    {
        return (Time.time - lastSeenTime) < memoryDuration;
    }

    // ══════════════════════════════════════════════════
    //  状态切换
    // ══════════════════════════════════════════════════

    private void UpdateState()
    {
        switch (CurrentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
        }
    }

    private void SwitchState(AIState newState)
    {
        CurrentState = newState;

        // 进入新状态的初始化
        switch (newState)
        {
            case AIState.Chase:
                break;
            case AIState.Attack:
                burstShotsFired = 0;
                burstCooldownTimer = 0f;
                fireTimer = 0f;
                break;
            case AIState.Patrol:
                patrolWaitTimer = 0f;
                hasRandomTarget = false;
                break;
        }
    }

    // ══════════════════════════════════════════════════
    //  各状态逻辑
    // ══════════════════════════════════════════════════

    private void UpdateIdle()
    {
        // 看到玩家 → 追击
        if (canSeePlayer)
        {
            SwitchState(AIState.Chase);
            return;
        }

        // 被打后通过OnTakeHit也会切换
    }

    private void UpdatePatrol()
    {
        // 看到玩家 → 追击
        if (canSeePlayer)
        {
            SwitchState(AIState.Chase);
            return;
        }

        // 巡逻等待
        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer -= Time.deltaTime;
            return;
        }
    }

    private void UpdateChase()
    {
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // 能看到 + 在攻击范围内 → 攻击
        if (canSeePlayer && distToPlayer <= attackRange)
        {
            SwitchState(AIState.Attack);
            return;
        }

        // 失去视野且记忆时间过 → 回到默认状态
        if (!canSeePlayer && !HasMemoryOfPlayer())
        {
            SwitchState(defaultState);
            return;
        }
    }

    private void UpdateAttack()
    {
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // 丢失视野 → 追击
        if (!canSeePlayer)
        {
            if (HasMemoryOfPlayer())
                SwitchState(AIState.Chase);
            else
                SwitchState(defaultState);
            return;
        }

        // 玩家跑出攻击范围 → 追击
        if (distToPlayer > attackRange * 1.2f)
        {
            SwitchState(AIState.Chase);
            return;
        }

        // 射击逻辑
        HandleShooting();
    }

    // ══════════════════════════════════════════════════
    //  移动
    // ══════════════════════════════════════════════════

    private void UpdateMovement()
    {
        Vector2 velocity = Vector2.zero;

        switch (CurrentState)
        {
            case AIState.Idle:
                velocity = Vector2.zero;
                break;

            case AIState.Patrol:
                velocity = GetPatrolVelocity();
                break;

            case AIState.Chase:
                velocity = GetChaseVelocity();
                break;

            case AIState.Attack:
                if (!standStillWhileAttacking)
                {
                    // 攻击时缓慢保持距离
                    float dist = Vector2.Distance(transform.position, player.position);
                    if (dist > attackRange * 0.8f)
                    {
                        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                        velocity = dir * patrolSpeed * 0.5f;
                    }
                }
                break;
        }

        rb.velocity = AvoidWalls(velocity);

        // 面向移动方向/玩家
        UpdateFacing();
    }

    private Vector2 GetPatrolVelocity()
    {
        if (patrolWaitTimer > 0f) return Vector2.zero;

        Vector3 target;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            target = patrolPoints[currentPatrolIndex].position;
        }
        else
        {
            // 随机巡逻
            if (!hasRandomTarget)
            {
                randomPatrolTarget = spawnPosition + (Vector3)(Random.insideUnitCircle * randomPatrolRadius);
                hasRandomTarget = true;
            }
            target = randomPatrolTarget;
        }

        float dist = Vector2.Distance(transform.position, target);
        if (dist < 0.3f)
        {
            // 到达巡逻点
            patrolWaitTimer = patrolWaitTime;
            hasRandomTarget = false;

            if (patrolPoints != null && patrolPoints.Length > 0)
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

            return Vector2.zero;
        }

        Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;
        return dir * patrolSpeed;
    }

    private Vector2 GetChaseVelocity()
    {
        Vector3 target = canSeePlayer ? player.position : lastKnownPlayerPos;
        float dist = Vector2.Distance(transform.position, target);

        // 到达攻击范围就停
        if (dist <= attackRange * 0.8f && canSeePlayer)
            return Vector2.zero;

        Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;
        return dir * chaseSpeed;
    }

    private Vector2 AvoidWalls(Vector2 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude < 0.01f || wallLayer.value == 0)
            return desiredVelocity;

        Vector2 origin = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 dir = desiredVelocity.normalized;
        float speed = desiredVelocity.magnitude;

        RaycastHit2D frontHit = Physics2D.Raycast(origin, dir, obstacleProbeDistance, wallLayer);
        if (frontHit.collider == null)
            return desiredVelocity;

        Vector2 left = new Vector2(-dir.y, dir.x);
        Vector2 right = new Vector2(dir.y, -dir.x);

        float leftClearance = ProbeClearance(origin, left);
        float rightClearance = ProbeClearance(origin, right);
        Vector2 sideDir = leftClearance >= rightClearance ? left : right;

        Vector2 steered = (dir + sideDir * obstacleAvoidanceStrength).normalized;
        RaycastHit2D steeredHit = Physics2D.Raycast(origin, steered, obstacleProbeDistance * 0.6f, wallLayer);
        if (steeredHit.collider != null)
            steered = sideDir;

        return steered * speed;
    }

    private float ProbeClearance(Vector2 origin, Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, obstacleProbeDistance, wallLayer);
        return hit.collider == null ? obstacleProbeDistance : hit.distance;
    }

    private void UpdateFacing()
    {
        Vector2 lookDir = Vector2.zero;

        if (CurrentState == AIState.Attack || CurrentState == AIState.Chase)
        {
            if (player != null)
                lookDir = (player.position - transform.position).normalized;
        }
        else if (rb.velocity.sqrMagnitude > 0.01f)
        {
            lookDir = rb.velocity.normalized;
        }

        if (lookDir.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // ══════════════════════════════════════════════════
    //  射击
    // ══════════════════════════════════════════════════

    private void HandleShooting()
    {
        // 连射冷却
        if (burstCooldownTimer > 0f)
        {
            burstCooldownTimer -= Time.deltaTime;
            return;
        }

        // 射击间隔
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        // 开火
        FireBullet();
        fireTimer = fireInterval;
        burstShotsFired++;

        // 达到连射上限 → 进入冷却
        if (burstShotsFired >= burstCount)
        {
            burstShotsFired = 0;
            burstCooldownTimer = burstCooldown;
        }
    }

    private void FireBullet()
    {
        if (player == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        // 加散射
        float spread = Random.Range(-shootSpread, shootSpread);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spread;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        Vector3 spawnPos = transform.position + (Vector3)(dir * 0.5f);

        GameObject bulletGO;
        if (enemyBulletPrefab != null)
        {
            bulletGO = Instantiate(enemyBulletPrefab, spawnPos, rot);
        }
        else
        {
            // 自动创建简易子弹
            bulletGO = CreateDefaultBullet(spawnPos, rot);
        }

        var eb = bulletGO.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            eb.damage = bulletDamage;
            eb.speed = bulletSpeed;
            eb.direction = rot * Vector2.right;
        }
    }

    private GameObject CreateDefaultBullet(Vector3 pos, Quaternion rot)
    {
        var go = new GameObject("EnemyBullet");
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.layer = LayerMask.NameToLayer("Default"); // 会在setup中改

        var sr = go.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.color = new Color(1f, 0.4f, 0.2f);
        go.transform.localScale = new Vector3(0.15f, 0.08f, 1f);

        var rb2 = go.AddComponent<Rigidbody2D>();
        rb2.gravityScale = 0f;
        rb2.freezeRotation = true;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.05f;
        col.isTrigger = true;

        go.AddComponent<EnemyBullet>();

        return go;
    }

    // ══════════════════════════════════════════════════
    //  事件回调
    // ══════════════════════════════════════════════════

    private void OnTakeHit()
    {
        // 被打后立即进入追击（即使看不到玩家，往声音方向走）
        if (CurrentState == AIState.Idle || CurrentState == AIState.Patrol)
        {
            lastKnownPlayerPos = player != null ? player.position : transform.position;
            lastSeenTime = Time.time;
            SwitchState(AIState.Chase);
        }
    }

    private void OnDie()
    {
        CurrentState = AIState.Dead;
        rb.velocity = Vector2.zero;
    }

    // ══════════════════════════════════════════════════
    //  外部接口
    // ══════════════════════════════════════════════════

    /// <summary>外部通知有声响（如玩家开枪）</summary>
    public void AlertSound(Vector3 soundPosition)
    {
        float dist = Vector2.Distance(transform.position, soundPosition);
        if (dist <= hearRange && (CurrentState == AIState.Idle || CurrentState == AIState.Patrol))
        {
            lastKnownPlayerPos = soundPosition;
            lastSeenTime = Time.time;
            SwitchState(AIState.Chase);
        }
    }

    // ══════════════════════════════════════════════════
    //  Gizmos
    // ══════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 视野范围
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.15f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, sightRange);

        // 攻击范围
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 巡逻路径
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                int next = (i + 1) % patrolPoints.Length;
                if (patrolPoints[next] == null) continue;
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
            }
        }
    }
#endif
}
