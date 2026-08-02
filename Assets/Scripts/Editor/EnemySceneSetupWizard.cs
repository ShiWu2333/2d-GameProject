using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 一键配置完整敌人系统的向导
/// 菜单：Tools → Enemy Setup → 一键配置完整敌人系统
/// 
/// 会创建：
/// 1. 寻路网格 (Grid2D)
/// 2. 巡逻路线 (PatrolRoute)
/// 3. 敌人刷新点 (EnemySpawner)
/// 4. 连接所有引用
/// </summary>
public class EnemySceneSetupWizard
{
    [MenuItem("Tools/Enemy Setup/一键配置完整敌人系统")]
    public static void SetupCompleteEnemySystem()
    {
        // 1. 确保有寻路网格
        var grid = Object.FindObjectOfType<Grid2D>();
        if (grid == null)
        {
            var gridGO = new GameObject("Pathfinding_Grid2D");
            grid = gridGO.AddComponent<Grid2D>();
            grid.gridCenter = Vector2.zero;
            grid.gridSize = new Vector2(40f, 40f);
            grid.nodeSize = 0.5f;
            grid.obstacleCheckRadius = 0.35f;

            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0)
                grid.obstacleLayer = 1 << wallLayer;

            Undo.RegisterCreatedObjectUndo(gridGO, "Create Grid2D");
            Debug.Log("[EnemySetupWizard] ✓ 已创建寻路网格");
        }
        else
        {
            Debug.Log("[EnemySetupWizard] ✓ 寻路网格已存在");
        }

        // 2. 创建示例巡逻路线
        var routeGO = new GameObject("PatrolRoute_示例");
        var route = routeGO.AddComponent<PatrolRoute>();
        route.mode = PatrolRoute.PatrolMode.Loop;
        route.waitTimePerPoint = 2f;

        // 四个巡逻点形成方形路线
        CreatePoint(routeGO.transform, new Vector3(-3, 3, 0), 0);
        CreatePoint(routeGO.transform, new Vector3(3, 3, 0), 1);
        CreatePoint(routeGO.transform, new Vector3(3, -3, 0), 2);
        CreatePoint(routeGO.transform, new Vector3(-3, -3, 0), 3);

        Undo.RegisterCreatedObjectUndo(routeGO, "Create Patrol Route");
        Debug.Log("[EnemySetupWizard] ✓ 已创建示例巡逻路线（4点方形）");

        // 3. 创建敌人刷新点
        var spawnerGO = new GameObject("EnemySpawner_示例");
        spawnerGO.transform.position = new Vector3(4f, 2f, 0f);
        var spawner = spawnerGO.AddComponent<EnemySpawner>();
        spawner.preset = EnemySetup.EnemyPreset.Normal;
        spawner.spawnCount = 2;
        spawner.respawnEnabled = true;
        spawner.respawnDelay = 30f;
        spawner.patrolRoute = route;
        spawner.initialState = EnemyAI.AIState.Patrol;

        Undo.RegisterCreatedObjectUndo(spawnerGO, "Create Spawner");
        Debug.Log("[EnemySetupWizard] ✓ 已创建敌人刷新点（关联巡逻路线）");

        // 4. 确保有 GameSetupHelper
        if (Object.FindObjectOfType<GameSetupHelper>() == null)
        {
            var setupGO = new GameObject("GameSetup");
            setupGO.AddComponent<GameSetupHelper>();
            Undo.RegisterCreatedObjectUndo(setupGO, "Create GameSetup");
            Debug.Log("[EnemySetupWizard] ✓ 已创建 GameSetup");
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[EnemySetupWizard] 完整敌人系统配置完毕！");
        Debug.Log("  - Grid2D: A*寻路网格");
        Debug.Log("  - PatrolRoute: 巡逻路线（可自由编辑巡逻点）");
        Debug.Log("  - EnemySpawner: 敌人刷新点（死亡30秒后刷新）");
        Debug.Log("  - AI行为: 巡逻→追踪→攻击→脱战（5秒超范围）→返回巡逻");
        Debug.Log("═══════════════════════════════════════════");

        Selection.activeGameObject = spawnerGO;
    }

    private static void CreatePoint(Transform parent, Vector3 pos, int index)
    {
        var point = new GameObject($"PatrolPoint_{index}");
        point.transform.SetParent(parent, false);
        point.transform.position = pos;
    }
}
