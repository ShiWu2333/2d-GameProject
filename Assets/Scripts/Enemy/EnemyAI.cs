using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 敌人AI：状态机驱动 + A*寻路
/// 状态：Idle(驻守) → Patrol(巡逻) → Chase(追击) → Attack(攻击) → Disengage(脱战) → Dead(死亡)
/// 
/// 核心行为：
/// - 视野范围内检测到玩家 → 进入追击
/// - 追击到攻击范围内 → 开火攻击
/// - 玩家跑出最大攻击距离 → 追踪
/// - 追踪终点不超过最大攻击距离
/// - 离开最大攻击距离超过5秒 → 脱战，返回巡逻路线
/// - 使用A*寻路绕过障碍
/// - 支持PatrolRoute预设路线巡逻
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
        Idle,           // 驻守（不移动，但检测玩家）
        Patrol,         // 巡逻（沿路线或随机巡逻）
        Chase,          // 追击（朝玩家移动）
        Attack,         // 攻击（在攻击范围内射击）
        Disengage,      // 脱战（返回巡逻路线）
        Dead,           // 死亡
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

    [Tooltip("感知半径（无视遮挡，玩家在此范围内一定能感知到）")]
    public float senseRange = 12f;

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

    [Header("攻击距离")]
    [Tooltip("最大攻击距离（开火射程）")]
    public float attackRange = 6f;

    [Tooltip("攻击时是否站定（true=不动，false=缓慢靠近）")]
    public bool standStillWhileAttacking = true;

    [Header("脱战机制")]
    [Tooltip("离开最大攻击距离多少秒后脱战")]
    public float disengageTime = 10f;

    [Header("巡逻路径")]
    [Tooltip("巡逻点（留空则在出生点附近随机巡逻）")]
    public Transform[] patrolPoints;

    [Tooltip("巡逻路线组件（优先级高于patrolPoints）")]
    public PatrolRoute patrolRoute;

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

    [Header("寻路")]
    [Tooltip("寻路刷新间隔（秒）")]
    public float pathUpdateInterval = 0.2f;

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
    private bool patrolReversing; // PingPong模式用

    // 攻击
    private float fireTimer;
    private int burstShotsFired;
    private float burstCooldownTimer;

    // 脱战
    private float outOfRangeTimer;   // 玩家离开攻击范围的累计时间
    private bool playerInAttackRange;

    // 寻路
    private List<Vector2> currentPath;
    private int pathIndex;
    private float pathUpdateTimer;

    // 移动
    private Vector2 lastMoveDir;
    private Vector2 cachedChaseDir;      // 缓存的追击方向
    private float chaseDirUpdateTimer;   // 追击方向刷新计时器
    private const float CHASE_DIR_INTERVAL = 0.2f; // 每0.2秒刷新

    // ══════════════════════════════════════════════════
    //  生命周期
    // ══════════════════════════════════════════════════

    void Awake()
    {
        stats = GetComponent<EnemyStats>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        spawnPosition = transform.position;
        CurrentState = defaultState;
    }

    void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;

        if (stats != null)
        {
            if (aggroOnHit && stats.onHit != null)
                stats.onHit.AddListener(OnTakeHit);

            if (stats.onDeath != null)
                stats.onDeath.AddListener(OnDie);
        }

        if (patrolRoute == null)
        {
            patrolRoute = GetComponentInParent<PatrolRoute>();
        }

        // 确保wallLayer有值
        if (wallLayer.value == 0)
        {
            int wallLayerIndex = LayerMask.NameToLayer("Wall");
            if (wallLayerIndex >= 0)
                wallLayer = 1 << wallLayerIndex;
            else
                wallLayer = LayerMask.GetMask("Default");
        }

        // 确保寻路网格存在并在1秒后生成
        EnsureGrid();
    }

    private void EnsureGrid()
    {
        if (Grid2D.Instance != null && Grid2D.Instance.IsReady) return;

        // 只由第一个敌人创建
        if (FindObjectOfType<Grid2D>() != null) return;

        var go = new GameObject("Pathfinding_Grid2D");
        var grid = go.AddComponent<Grid2D>();
        grid.delayGeneration = true; // Awake里不生成
        grid.nodeSize = 0.5f;
        grid.obstacleCheckRadius = 0.45f;
        grid.obstacleLayer = wallLayer;

        // 用协程延迟生成网格
        StartCoroutine(DelayedGridSetup());
    }

    private System.Collections.IEnumerator DelayedGridSetup()
    {
        yield return new WaitForSeconds(1f);

        var grid = Grid2D.Instance;
        if (grid == null)
        {
            grid = FindObjectOfType<Grid2D>();
            if (grid == null)
            {
                Debug.LogError("[EnemyAI] Grid2D仍然不存在！");
                yield break;
            }
        }

        if (grid.IsReady) yield break;

        // 计算边界
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        if (player != null)
            bounds.Encapsulate(player.position);

        var allCol = FindObjectsOfType<Collider2D>();
        foreach (var c in allCol)
        {
            if (c == null || c.isTrigger) continue;
            if (c.GetComponent<EnemyBullet>() != null) continue;
            bounds.Encapsulate(c.bounds);
        }

        var allEnemies = FindObjectsOfType<EnemyAI>();
        foreach (var e in allEnemies)
            bounds.Encapsulate(e.transform.position);

        grid.gridCenter = bounds.center;
        grid.gridSize = new Vector2(
            Mathf.Max(bounds.size.x + 10f, 40f),
            Mathf.Max(bounds.size.y + 10f, 40f)
        );
        grid.obstacleLayer = wallLayer;

        grid.GenerateGrid();
        Debug.Log($"[EnemyAI] 网格生成完毕: center={grid.gridCenter} size={grid.gridSize} ready={grid.IsReady}");
    }

    void Update()
    {
        if (stats.IsDead) return;
        if (player == null) return;

        UpdateVision();
        UpdateDisengageTimer();
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
        playerInAttackRange = distToPlayer <= attackRange;

        if (distToPlayer <= sightRange)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position, dir, distToPlayer, wallLayer);

            if (hit.collider == null)
            {
                canSeePlayer = true;
                lastSeenTime = Time.time;
                lastKnownPlayerPos = player.position;
            }
        }
    }

    private bool HasMemoryOfPlayer()
    {
        return (Time.time - lastSeenTime) < memoryDuration;
    }

    // ══════════════════════════════════════════════════
    //  脱战计时器
    // ══════════════════════════════════════════════════

    private void UpdateDisengageTimer()
    {
        // 只在Chase或Attack状态下计算脱战
        if (CurrentState != AIState.Chase && CurrentState != AIState.Attack)
        {
            outOfRangeTimer = 0f;
            return;
        }

        if (playerInAttackRange && canSeePlayer)
        {
            // 玩家在攻击范围内，重置脱战计时
            outOfRangeTimer = 0f;
        }
        else
        {
            // 玩家离开攻击范围，开始计时
            outOfRangeTimer += Time.deltaTime;
        }
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
            case AIState.Disengage:
                UpdateDisengage();
                break;
        }
    }

    private void SwitchState(AIState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case AIState.Chase:
                outOfRangeTimer = 0f;
                currentPath = null;
                pathIndex = 0;
                chaseDirUpdateTimer = 0f; // 立即计算方向
                cachedChaseDir = Vector2.zero;
                break;
            case AIState.Attack:
                burstShotsFired = 0;
                burstCooldownTimer = 0f;
                fireTimer = 0f;
                break;
            case AIState.Patrol:
                patrolWaitTimer = 0f;
                hasRandomTarget = false;
                currentPath = null;
                pathIndex = 0;
                break;
            case AIState.Disengage:
                outOfRangeTimer = 0f;
                currentPath = null;
                pathIndex = 0;
                break;
        }
    }

    // ══════════════════════════════════════════════════
    //  各状态逻辑
    // ══════════════════════════════════════════════════

    private void UpdateIdle()
    {
        if (canSeePlayer)
        {
            lastKnownPlayerPos = player.position;
            lastSeenTime = Time.time;
            SwitchState(AIState.Chase);
            return;
        }
    }

    private void UpdatePatrol()
    {
        if (canSeePlayer)
        {
            SwitchState(AIState.Chase);
            return;
        }

        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer -= Time.deltaTime;
            return;
        }
    }

    private void UpdateChase()
    {
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // 脱战检查：离开攻击距离超过5秒
        if (outOfRangeTimer >= disengageTime)
        {
            SwitchState(AIState.Disengage);
            return;
        }

        // 能看到 + 在攻击范围内 → 攻击
        if (canSeePlayer && distToPlayer <= attackRange)
        {
            SwitchState(AIState.Attack);
            return;
        }

        // 能看到但还没到攻击范围 → 继续追
        if (canSeePlayer)
        {
            lastKnownPlayerPos = player.position;
            return;
        }

        // 看不到了，用记忆追到最后已知位置
        if (!HasMemoryOfPlayer())
        {
            SwitchState(AIState.Disengage);
            return;
        }
    }

    private void UpdateAttack()
    {
        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // 脱战检查
        if (outOfRangeTimer >= disengageTime)
        {
            SwitchState(AIState.Disengage);
            return;
        }

        // 丢失视野
        if (!canSeePlayer)
        {
            if (HasMemoryOfPlayer())
                SwitchState(AIState.Chase);
            else
                SwitchState(AIState.Disengage);
            return;
        }

        // 玩家跑出攻击范围 → 追击
        if (distToPlayer > attackRange * 1.2f)
        {
            SwitchState(AIState.Chase);
            return;
        }

        HandleShooting();
    }

    private void UpdateDisengage()
    {
        // 脱战中如果又看到玩家，重新进入追击
        if (canSeePlayer)
        {
            SwitchState(AIState.Chase);
            return;
        }

        // 返回到出生点或巡逻路线起点附近 → 恢复巡逻/驻守
        Vector2 returnTarget = GetReturnTarget();
        float distToReturn = Vector2.Distance(transform.position, returnTarget);

        if (distToReturn < 1f)
        {
            SwitchState(defaultState);
            return;
        }
    }

    /// <summary>脱战后回归目标点</summary>
    private Vector2 GetReturnTarget()
    {
        // 优先回巡逻路线第一个点
        if (patrolRoute != null && patrolRoute.PointCount > 0)
            return patrolRoute.GetPoint(currentPatrolIndex);

        if (patrolPoints != null && patrolPoints.Length > 0)
            return patrolPoints[currentPatrolIndex].position;

        return spawnPosition;
    }

    // ══════════════════════════════════════════════════
    //  移动（带A*寻路）
    // ══════════════════════════════════════════════════

    private void UpdateMovement()
    {
        Vector2 desiredDir = Vector2.zero;
        float speed = 0f;

        switch (CurrentState)
        {
            case AIState.Idle:
                lastMoveDir = Vector2.zero;
                UpdateFacing();
                return;

            case AIState.Patrol:
                desiredDir = GetPatrolDirection(out speed);
                break;

            case AIState.Chase:
                desiredDir = GetChaseDirection(out speed);
                break;

            case AIState.Attack:
                lastMoveDir = Vector2.zero;
                UpdateFacing();
                return;

            case AIState.Disengage:
                desiredDir = GetDisengageDirection(out speed);
                break;
        }

        if (desiredDir.sqrMagnitude > 0.001f && speed > 0.001f)
        {
            float moveDist = speed * Time.fixedDeltaTime;
            Vector2 moveDir = desiredDir.normalized;

            // 尝试朝目标方向移动
            if (CanMoveInDirection(moveDir, moveDist))
            {
                rb.position += moveDir * moveDist;
                lastMoveDir = moveDir;
            }
            else
            {
                // 被墙挡住 → 分轴滑动绕过墙角
                bool moved = false;

                // 尝试水平分量
                if (Mathf.Abs(moveDir.x) > 0.1f)
                {
                    Vector2 hDir = new Vector2(Mathf.Sign(moveDir.x), 0f);
                    if (CanMoveInDirection(hDir, moveDist))
                    {
                        rb.position += hDir * moveDist;
                        lastMoveDir = hDir;
                        moved = true;
                    }
                }

                // 水平不行试垂直
                if (!moved && Mathf.Abs(moveDir.y) > 0.1f)
                {
                    Vector2 vDir = new Vector2(0f, Mathf.Sign(moveDir.y));
                    if (CanMoveInDirection(vDir, moveDist))
                    {
                        rb.position += vDir * moveDist;
                        lastMoveDir = vDir;
                        moved = true;
                    }
                }

                if (!moved)
                {
                    lastMoveDir = Vector2.zero;
                    // 强制立即刷新方向
                    chaseDirUpdateTimer = 0f;
                }
            }
        }
        else
        {
            lastMoveDir = Vector2.zero;
        }

        UpdateFacing();
    }

    /// <summary>检查某方向是否能移动（实际移动用，精确检测）</summary>
    private bool CanMoveInDirection(Vector2 dir, float dist)
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return true;

        Vector2 size = (Vector2)col.bounds.size * 0.9f;
        RaycastHit2D hit = Physics2D.BoxCast(rb.position, size, 0f, dir, dist + 0.03f, wallLayer);
        return hit.collider == null;
    }

    /// <summary>检查某方向是否足够宽敞（路径规划用，离墙远一点）</summary>
    private bool CanMoveInDirectionWide(Vector2 dir, float dist)
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return true;

        Vector2 size = (Vector2)col.bounds.size * 1.3f;
        RaycastHit2D hit = Physics2D.BoxCast(rb.position, size, 0f, dir, dist + 0.1f, wallLayer);
        return hit.collider == null;
    }

    private Vector2 GetPatrolDirection(out float speed)
    {
        speed = patrolSpeed;

        if (patrolWaitTimer > 0f) { speed = 0f; return Vector2.zero; }

        Vector2 target;

        if (patrolRoute != null && patrolRoute.PointCount > 0)
        {
            target = patrolRoute.GetPoint(currentPatrolIndex);
        }
        else if (patrolPoints != null && patrolPoints.Length > 0)
        {
            target = patrolPoints[currentPatrolIndex].position;
        }
        else
        {
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
            patrolWaitTimer = patrolRoute != null ? patrolRoute.waitTimePerPoint : patrolWaitTime;
            hasRandomTarget = false;

            if (patrolRoute != null && patrolRoute.PointCount > 0)
            {
                currentPatrolIndex = patrolRoute.GetNextIndex(currentPatrolIndex, ref patrolReversing);
            }
            else if (patrolPoints != null && patrolPoints.Length > 0)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }

            speed = 0f;
            return Vector2.zero;
        }

        return GetPathDirection(target);
    }

    private Vector2 GetChaseDirection(out float speed)
    {
        speed = chaseSpeed;
        if (player == null) { speed = 0; return Vector2.zero; }

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // 能直接看到玩家
        if (canSeePlayer)
        {
            lastKnownPlayerPos = player.position;

            if (distToPlayer <= attackRange * 0.8f)
            {
                speed = 0f;
                cachedChaseDir = Vector2.zero;
                return Vector2.zero;
            }

            // 直线方向
            Vector2 directDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            
            // 检查直线是否真的能走过去
            float moveDist = chaseSpeed * Time.fixedDeltaTime;
            if (CanMoveInDirection(directDir, moveDist))
            {
                cachedChaseDir = directDir;
                return cachedChaseDir;
            }

            // 直线被挡（视线能过但身体过不去）→ 也走绕行逻辑
        }

        // 每0.2秒刷新绕行方向
        chaseDirUpdateTimer -= Time.fixedDeltaTime;
        if (chaseDirUpdateTimer <= 0f || cachedChaseDir.sqrMagnitude < 0.01f)
        {
            chaseDirUpdateTimer = CHASE_DIR_INTERVAL;
            cachedChaseDir = GetPatrolRouteChaseDirection();
        }

        return cachedChaseDir;
    }

    /// <summary>
    /// 追击时沿巡逻路线走：找到路线上最接近玩家的点，朝那个方向沿路线移动
    /// 这样敌人会通过预设通道绕到玩家那边
    /// </summary>
    private Vector2 GetPatrolRouteChaseDirection()
    {
        // 有巡逻路线时，沿路线走到能看到玩家的位置
        if (patrolRoute != null && patrolRoute.PointCount > 0)
        {
            // 找路线上最接近最后已知玩家位置的点
            int bestIndex = FindClosestRoutePointToTarget(lastKnownPlayerPos);

            // 当前巡逻目标设为该点
            Vector2 routeTarget = patrolRoute.GetPoint(currentPatrolIndex);
            float distToRoutePoint = Vector2.Distance(transform.position, routeTarget);

            // 到达当前路线点，前进到下一个
            if (distToRoutePoint < 0.5f)
            {
                // 朝最佳点方向推进
                if (currentPatrolIndex != bestIndex)
                {
                    currentPatrolIndex = GetNextIndexToward(currentPatrolIndex, bestIndex);
                }
                routeTarget = patrolRoute.GetPoint(currentPatrolIndex);
            }

            Vector2 dir = (routeTarget - (Vector2)transform.position);
            if (dir.sqrMagnitude < 0.01f) return Vector2.zero;
            return dir.normalized;
        }

        // 有patrolPoints时
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            int bestIndex = FindClosestPatrolPointToTarget(lastKnownPlayerPos);
            Vector2 routeTarget = patrolPoints[currentPatrolIndex].position;
            float distToRoutePoint = Vector2.Distance(transform.position, routeTarget);

            if (distToRoutePoint < 0.5f)
            {
                if (currentPatrolIndex != bestIndex)
                {
                    // 简单推进：向bestIndex方向+1或-1
                    if (bestIndex > currentPatrolIndex)
                        currentPatrolIndex++;
                    else if (bestIndex < currentPatrolIndex)
                        currentPatrolIndex--;
                    currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolPoints.Length - 1);
                }
                routeTarget = patrolPoints[currentPatrolIndex].position;
            }

            Vector2 dir = ((Vector2)routeTarget - (Vector2)transform.position);
            if (dir.sqrMagnitude < 0.01f) return Vector2.zero;
            return dir.normalized;
        }

        // 没有路线 → 用扇形扫描绕行
        return FindDetourDirection(lastKnownPlayerPos);
    }

    /// <summary>找PatrolRoute上最接近目标的点的索引</summary>
    private int FindClosestRoutePointToTarget(Vector2 target)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < patrolRoute.PointCount; i++)
        {
            float d = Vector2.Distance(patrolRoute.GetPoint(i), target);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    /// <summary>找patrolPoints上最接近目标的点的索引</summary>
    private int FindClosestPatrolPointToTarget(Vector2 target)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            float d = Vector2.Distance(patrolPoints[i].position, target);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    /// <summary>获取从当前索引朝目标索引前进一步的下一个索引</summary>
    private int GetNextIndexToward(int current, int target)
    {
        if (patrolRoute == null) return current;
        int count = patrolRoute.PointCount;
        if (count <= 1) return current;

        int forwardDist = (target - current + count) % count;
        int backwardDist = (current - target + count) % count;

        if (forwardDist <= backwardDist)
            return (current + 1) % count;
        else
            return (current - 1 + count) % count;
    }

    /// <summary>无预设路线时的后备：扇形扫描找绕行方向</summary>
    private Vector2 FindDetourDirection(Vector2 target)
    {
        Vector2 origin = (Vector2)transform.position;
        Vector2 toTarget = (target - origin).normalized;
        float checkDist = 2f;

        for (int angle = 15; angle <= 180; angle += 15)
        {
            Vector2 rightDir = RotateVector(toTarget, -angle);
            if (CanMoveInDirectionWide(rightDir, checkDist))
            {
                Vector2 futurePos = origin + rightDir * checkDist;
                if (Vector2.Distance(futurePos, target) < Vector2.Distance(origin, target))
                    return rightDir;
            }

            Vector2 leftDir = RotateVector(toTarget, angle);
            if (CanMoveInDirectionWide(leftDir, checkDist))
            {
                Vector2 futurePos = origin + leftDir * checkDist;
                if (Vector2.Distance(futurePos, target) < Vector2.Distance(origin, target))
                    return leftDir;
            }
        }

        for (int angle = 15; angle <= 180; angle += 15)
        {
            Vector2 rightDir = RotateVector(toTarget, -angle);
            if (CanMoveInDirection(rightDir, checkDist)) return rightDir;
            Vector2 leftDir = RotateVector(toTarget, angle);
            if (CanMoveInDirection(leftDir, checkDist)) return leftDir;
        }

        return Vector2.zero;
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private Vector2 GetDisengageDirection(out float speed)
    {
        speed = patrolSpeed;
        Vector2 returnTarget = GetReturnTarget();
        float dist = Vector2.Distance(transform.position, returnTarget);

        if (dist < 0.5f) { speed = 0f; return Vector2.zero; }

        return GetPathDirection(returnTarget);
    }

    /// <summary>
    /// 获取朝目标的移动方向（使用A*寻路）
    /// 寻路失败时直接朝目标方向走，由MoveWithCollision保证不穿墙
    /// </summary>
    private Vector2 GetPathDirection(Vector2 target)
    {
        // 更新寻路路径
        pathUpdateTimer -= Time.fixedDeltaTime;
        if (currentPath == null || pathUpdateTimer <= 0f)
        {
            pathUpdateTimer = pathUpdateInterval;
            RequestNewPath(target);
        }

        // 沿A*路径移动
        if (currentPath != null && pathIndex < currentPath.Count)
        {
            Vector2 nextPoint = currentPath[pathIndex];
            float distToNext = Vector2.Distance(transform.position, nextPoint);

            if (distToNext < 0.4f)
            {
                pathIndex++;
                if (pathIndex >= currentPath.Count)
                    return Vector2.zero;
                nextPoint = currentPath[pathIndex];
            }

            return (nextPoint - (Vector2)transform.position).normalized;
        }

        // A*失败时：直接朝目标走，MoveWithCollision的分轴滑动会沿墙绕行
        return GetFallbackDirection(target);
    }

    /// <summary>
    /// 寻路失败时的回退：直接朝目标走
    /// MoveWithCollision的分轴滑动机制会让敌人沿着墙面滑动绕行
    /// </summary>
    private Vector2 GetFallbackDirection(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position);
        if (dir.sqrMagnitude < 0.01f) return Vector2.zero;
        return dir.normalized;
    }

    private void RequestNewPath(Vector2 target)
    {
        var path = Pathfinder2D.FindPath(transform.position, target);
        if (path != null && path.Count > 0)
        {
            currentPath = Pathfinder2D.SmoothPath(path, wallLayer);
            pathIndex = 0;
        }
        else
        {
            currentPath = null;
            pathIndex = 0;
        }
    }

    private void UpdateFacing()
    {
        Vector2 lookDir = Vector2.zero;

        if (CurrentState == AIState.Attack || CurrentState == AIState.Chase)
        {
            if (player != null)
                lookDir = (player.position - transform.position).normalized;
        }
        else if (lastMoveDir.sqrMagnitude > 0.01f)
        {
            lookDir = lastMoveDir;
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
        if (burstCooldownTimer > 0f)
        {
            burstCooldownTimer -= Time.deltaTime;
            return;
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        FireBullet();
        fireTimer = fireInterval;
        burstShotsFired++;

        if (burstShotsFired >= burstCount)
        {
            burstShotsFired = 0;
            burstCooldownTimer = burstCooldown;
        }
    }

    private void FireBullet()
    {
        if (player == null) return;
        // 必须能看到玩家才开枪（防隔墙射击）
        if (!canSeePlayer) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;

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
        go.layer = LayerMask.NameToLayer("Default");

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
        if (CurrentState == AIState.Idle || CurrentState == AIState.Patrol || CurrentState == AIState.Disengage)
        {
            lastKnownPlayerPos = player != null ? player.position : transform.position;
            lastSeenTime = Time.time;
            SwitchState(AIState.Chase);
        }

        // 被打时重置脱战计时
        outOfRangeTimer = 0f;
    }

    private void OnDie()
    {
        CurrentState = AIState.Dead;
        lastMoveDir = Vector2.zero;
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

    /// <summary>设置巡逻路线</summary>
    public void SetPatrolRoute(PatrolRoute route)
    {
        patrolRoute = route;
        if (route != null && route.PointCount > 0)
        {
            if (CurrentState == AIState.Idle)
                SwitchState(AIState.Patrol);
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

        // 巡逻路径（Transform数组）
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

        // 当前寻路路径
        if (currentPath != null && currentPath.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = pathIndex; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
            if (pathIndex < currentPath.Count)
                Gizmos.DrawLine(transform.position, currentPath[pathIndex]);
        }

        // 脱战计时器（编辑器可视化）
        if (Application.isPlaying && (CurrentState == AIState.Chase || CurrentState == AIState.Attack))
        {
            float ratio = outOfRangeTimer / disengageTime;
            Gizmos.color = Color.Lerp(Color.green, Color.red, ratio);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
#endif
}
