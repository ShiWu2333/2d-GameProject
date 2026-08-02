using UnityEngine;

/// <summary>
/// 运行时游戏初始化
/// 自动配置：墙壁碰撞、敌人AI、玩家组件、物理层
/// 放在场景中一个空物体上（推荐命名 GameSetup）
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameSetupHelper : MonoBehaviour
{
    [Header("自动配置")]
    public bool autoSetupEnemies = true;
    public bool autoSetupPlayer = true;
    public bool autoSetupPhysics = true;
    public bool autoSetupPathfinding = true;
    public bool createMvpArenaIfMissing = true;
    public bool createTestEnemyIfMissing = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapMvpSetup()
    {
        if (FindObjectOfType<GameSetupHelper>() != null)
            return;

        var setupGO = new GameObject("MVP_GameSetup");
        setupGO.AddComponent<GameSetupHelper>();
    }

    void Awake()
    {
        if (autoSetupPhysics)
            SetupPhysicsLayers();

        if (autoSetupPlayer)
            SetupPlayer();

        if (autoSetupEnemies)
            SetupEnemies();

        if (autoSetupPathfinding)
            EnsurePathfindingGrid();

        // 确保有 GameManager
        if (GameManager.Instance == null && GetComponent<GameManager>() == null)
            gameObject.AddComponent<GameManager>();
    }

    private void SetupPhysicsLayers()
    {
        int defaultLayer = LayerMask.NameToLayer("Default");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        int playerBulletLayer = LayerMask.NameToLayer("PlayerBullet");
        int wallLayer = LayerMask.NameToLayer("Wall");

        if (enemyBulletLayer >= 0 && enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyBulletLayer, enemyLayer, true);

        if (enemyBulletLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyBulletLayer, enemyBulletLayer, true);

        if (playerBulletLayer >= 0)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                Physics2D.IgnoreLayerCollision(playerBulletLayer, player.layer, true);
            Physics2D.IgnoreLayerCollision(playerBulletLayer, playerBulletLayer, true);
        }

        if (wallLayer >= 0)
        {
            if (defaultLayer >= 0)
                Physics2D.IgnoreLayerCollision(defaultLayer, wallLayer, false);
            if (enemyLayer >= 0)
                Physics2D.IgnoreLayerCollision(enemyLayer, wallLayer, false);
            if (playerBulletLayer >= 0)
                Physics2D.IgnoreLayerCollision(playerBulletLayer, wallLayer, false);
            if (enemyBulletLayer >= 0)
                Physics2D.IgnoreLayerCollision(enemyBulletLayer, wallLayer, false);
        }
    }

    private void SetupPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (player.GetComponent<EnemyAlertSystem>() == null)
            player.AddComponent<EnemyAlertSystem>();

        if (player.GetComponent<DamageIndicator>() == null)
            player.AddComponent<DamageIndicator>();

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        EnsureSolidCollider(player, new Vector2(0.7f, 0.9f));
        EnsureWallGuard(player);
    }

    private void SetupEnemies()
    {
        // 墙壁碰撞
        if (createMvpArenaIfMissing)
            CreateMvpArenaIfMissing();

        SetupWallColliders();

        // 在"敌人组"下查找所有子物体并配置为敌人
        var enemyGroup = GameObject.Find("敌人组");
        if (enemyGroup != null)
        {
            foreach (Transform child in enemyGroup.transform)
            {
                if (child == null) continue;
                var go = child.gameObject;
                if (go.GetComponent<EnemyStats>() != null) continue;
                if (go.GetComponent<EnemySetup>() != null) continue;

                var setup = go.AddComponent<EnemySetup>();
                setup.preset = EnemySetup.EnemyPreset.Normal;
                Debug.Log($"[GameSetupHelper] 配置敌人：{go.name}");
            }
        }

        // 也处理散落在外面的敌人（名字含 Enemy）
        int configuredEnemies = 0;
        var allEnemyCandidates = FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in allEnemyCandidates)
        {
            var go = sr.gameObject;
            bool looksLikeEnemy = go.CompareTag("Enemy") || go.name.Contains("Enemy") || go.name.Contains("敌人");
            if (!looksLikeEnemy || go.name.Contains("Bullet")) continue;

            if (go.GetComponent<EnemyStats>() != null || go.GetComponent<EnemySetup>() != null)
            {
                configuredEnemies++;
                continue;
            }

            var setup = go.AddComponent<EnemySetup>();
            setup.preset = EnemySetup.EnemyPreset.Normal;
            configuredEnemies++;
            Debug.Log($"[GameSetupHelper] 配置敌人：{go.name}");
        }

        if (createTestEnemyIfMissing && configuredEnemies == 0 && FindObjectOfType<EnemyStats>() == null)
            CreateTestEnemy();
    }

    private void SetupWallColliders()
    {
        var generator = FindObjectOfType<WallColliderGenerator>();
        if (generator == null)
            generator = gameObject.AddComponent<WallColliderGenerator>();

        if (generator.wallParent == null)
            generator.wallParent = FindWallParent();

        generator.Generate();
    }

    private void CreateMvpArenaIfMissing()
    {
        if (FindWallParent() != null || HasWallLayerObjects())
            return;

        var parent = new GameObject("墙壁组");
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
            parent.layer = wallLayer;

        CreateWall(parent.transform, "Wall_Top", new Vector2(0f, 5f), new Vector2(16f, 0.5f));
        CreateWall(parent.transform, "Wall_Bottom", new Vector2(0f, -5f), new Vector2(16f, 0.5f));
        CreateWall(parent.transform, "Wall_Left", new Vector2(-8f, 0f), new Vector2(0.5f, 10f));
        CreateWall(parent.transform, "Wall_Right", new Vector2(8f, 0f), new Vector2(0.5f, 10f));
        CreateWall(parent.transform, "Wall_Cover", new Vector2(1.5f, 0f), new Vector2(0.6f, 3f));

        Debug.Log("[GameSetupHelper] 已创建 MVP 测试墙体");
    }

    private void CreateWall(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
            go.layer = wallLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite();
        sr.color = Color.white;
        sr.sortingOrder = 5;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        box.isTrigger = false;
    }

    private void CreateTestEnemy()
    {
        var enemy = new GameObject("Enemy_MVP");
        enemy.transform.position = new Vector3(4f, 0f, 0f);
        enemy.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            enemy.layer = enemyLayer;

        var sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite();
        sr.color = new Color(0.9f, 0.2f, 0.2f);
        sr.sortingOrder = 1;

        var setup = enemy.AddComponent<EnemySetup>();
        setup.preset = EnemySetup.EnemyPreset.Normal;
        setup.initialState = EnemyAI.AIState.Idle;
        setup.ApplyPreset();

        Debug.Log("[GameSetupHelper] 已创建 MVP 测试敌人");
    }

    private void EnsureSolidCollider(GameObject go, Vector2 fallbackSize)
    {
        if (go == null) return;

        var collider = go.GetComponent<Collider2D>();
        if (collider == null)
        {
            var capsule = go.AddComponent<CapsuleCollider2D>();
            capsule.size = fallbackSize;
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.isTrigger = false;
            return;
        }

        collider.isTrigger = false;
    }

    private void EnsureWallGuard(GameObject go)
    {
        if (go == null) return;

        var guard = go.GetComponent<WallCollisionGuard>();
        if (guard == null)
            guard = go.AddComponent<WallCollisionGuard>();

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
            guard.wallLayer = 1 << wallLayer;
    }

    private Transform FindWallParent()
    {
        string[] names =
        {
            "墙壁组",
            "Walls",
            "WallGroup",
            "Wall Group",
            "MVP_Walls"
        };

        foreach (string name in names)
        {
            var go = GameObject.Find(name);
            if (go != null)
                return go.transform;
        }

        return null;
    }

    private bool HasWallLayerObjects()
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer < 0)
            return false;

        var colliders = FindObjectsOfType<Collider2D>();
        foreach (var col in colliders)
        {
            if (col != null && col.gameObject.layer == wallLayer)
                return true;
        }

        return false;
    }

    private Sprite CreateSolidSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void EnsurePathfindingGrid()
    {
        if (Grid2D.Instance != null) return;
        if (FindObjectOfType<Grid2D>() != null) return;

        // PathfindingSetup 会通过 RuntimeInitializeOnLoadMethod 自动创建
        // 这里做保险检查
        Debug.Log("[GameSetupHelper] 寻路网格将由 PathfindingSetup 自动创建");
    }
}
