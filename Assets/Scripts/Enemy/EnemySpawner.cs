using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人刷新点
/// 负责在指定位置生成敌人，敌人死亡后可重新刷新
/// 
/// 使用方式：
/// 1. 在场景中放置空物体挂此脚本
/// 2. 设置敌人预制体和刷新参数
/// 3. 关联巡逻路线（可选）
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("生成配置")]
    [Tooltip("敌人预制体（留空则使用默认EnemySetup创建）")]
    public GameObject enemyPrefab;

    [Tooltip("敌人预设类型")]
    public EnemySetup.EnemyPreset preset = EnemySetup.EnemyPreset.Normal;

    [Tooltip("每次生成的数量")]
    public int spawnCount = 1;

    [Tooltip("生成半径（多敌人时分散）")]
    public float spawnRadius = 1f;

    [Header("刷新机制")]
    [Tooltip("是否在敌人死亡后刷新")]
    public bool respawnEnabled = true;

    [Tooltip("刷新延迟（秒）")]
    public float respawnDelay = 30f;

    [Tooltip("最大同时存在数量（0=无限制）")]
    public int maxAliveCount = 0;

    [Tooltip("是否在游戏开始时自动生成")]
    public bool spawnOnStart = true;

    [Header("巡逻路线")]
    [Tooltip("分配给生成敌人的巡逻路线")]
    public PatrolRoute patrolRoute;

    [Header("初始AI状态")]
    public EnemyAI.AIState initialState = EnemyAI.AIState.Patrol;

    // 运行时数据
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private float respawnTimer;
    private int totalSpawned;
    private bool isWaitingRespawn;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnWave();
        }
    }

    void Update()
    {
        // 清理已死亡的引用
        spawnedEnemies.RemoveAll(e => e == null);

        // 刷新计时
        if (isWaitingRespawn && respawnEnabled)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                isWaitingRespawn = false;
                SpawnWave();
            }
        }

        // 检查是否需要触发刷新
        if (respawnEnabled && !isWaitingRespawn && spawnedEnemies.Count == 0 && totalSpawned > 0)
        {
            isWaitingRespawn = true;
            respawnTimer = respawnDelay;
        }
    }

    /// <summary>立即生成一波敌人</summary>
    public void SpawnWave()
    {
        int toSpawn = spawnCount;

        // 检查存活上限
        if (maxAliveCount > 0)
        {
            int alive = spawnedEnemies.Count;
            toSpawn = Mathf.Min(toSpawn, maxAliveCount - alive);
        }

        for (int i = 0; i < toSpawn; i++)
        {
            SpawnSingleEnemy();
        }
    }

    private void SpawnSingleEnemy()
    {
        Vector2 spawnPos = (Vector2)transform.position;

        // 多个敌人时分散生成位置
        if (spawnCount > 1)
        {
            spawnPos += Random.insideUnitCircle * spawnRadius;
        }

        GameObject enemy;

        if (enemyPrefab != null)
        {
            enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // 无预制体时动态创建
            enemy = CreateDefaultEnemy(spawnPos);
        }

        // 配置敌人
        ConfigureEnemy(enemy);

        spawnedEnemies.Add(enemy);
        totalSpawned++;

        // 注册死亡回调
        var stats = enemy.GetComponent<EnemyStats>();
        if (stats != null)
        {
            stats.onDeath.AddListener(() => OnEnemyDeath(enemy));
        }
    }

    private void ConfigureEnemy(GameObject enemy)
    {
        var setup = enemy.GetComponent<EnemySetup>();
        if (setup == null)
            setup = enemy.AddComponent<EnemySetup>();

        setup.preset = preset;
        setup.initialState = initialState;

        // 分配巡逻路线
        if (patrolRoute != null && patrolRoute.PointCount > 0)
        {
            // 转换PatrolRoute的子物体为Transform数组给EnemySetup用
            var points = new Transform[patrolRoute.PointCount];
            for (int i = 0; i < patrolRoute.PointCount; i++)
            {
                points[i] = patrolRoute.transform.GetChild(i);
            }
            setup.patrolPoints = points;
            setup.initialState = EnemyAI.AIState.Patrol;
        }
    }

    private GameObject CreateDefaultEnemy(Vector2 position)
    {
        var go = new GameObject($"Enemy_Spawned_{totalSpawned}");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.color = new Color(0.9f, 0.2f, 0.2f);
        sr.sortingOrder = 1;

        return go;
    }

    private void OnEnemyDeath(GameObject enemy)
    {
        // 从列表中移除会在Update中自动处理（null检查）
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 刷新点标记
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.3f);

        // 生成范围
        if (spawnCount > 1)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }

        // 连接到巡逻路线
        if (patrolRoute != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawLine(transform.position, patrolRoute.transform.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
#endif
}
