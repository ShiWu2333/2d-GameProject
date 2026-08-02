using UnityEngine;
using UnityEditor;

/// <summary>
/// 巡逻路线编辑器工具
/// 快速创建和编辑巡逻路线
/// </summary>
[CustomEditor(typeof(PatrolRoute))]
public class PatrolRouteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var route = (PatrolRoute)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("巡逻点管理", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"当前巡逻点数: {route.transform.childCount}");

        GUILayout.Space(5);

        if (GUILayout.Button("添加巡逻点（在末尾）"))
        {
            AddPatrolPoint(route, route.transform.childCount);
        }

        if (GUILayout.Button("在场景视图位置添加巡逻点"))
        {
            AddPatrolPointAtSceneView(route);
        }

        if (route.transform.childCount > 0 && GUILayout.Button("删除最后一个巡逻点"))
        {
            Undo.DestroyObjectImmediate(route.transform.GetChild(route.transform.childCount - 1).gameObject);
        }
    }

    private void AddPatrolPoint(PatrolRoute route, int index)
    {
        Vector3 pos;
        if (route.transform.childCount > 0)
        {
            var last = route.transform.GetChild(route.transform.childCount - 1);
            pos = last.position + Vector3.right * 2f;
        }
        else
        {
            pos = route.transform.position;
        }

        CreatePoint(route, pos, index);
    }

    private void AddPatrolPointAtSceneView(PatrolRoute route)
    {
        Vector3 pos = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.camera.transform.position
            : route.transform.position;
        pos.z = 0f;

        CreatePoint(route, pos, route.transform.childCount);
    }

    private void CreatePoint(PatrolRoute route, Vector3 pos, int index)
    {
        var point = new GameObject($"PatrolPoint_{index}");
        point.transform.SetParent(route.transform, false);
        point.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(point, "Add Patrol Point");
        Selection.activeGameObject = point;
    }
}

/// <summary>
/// 菜单工具：快速创建巡逻路线和刷新点
/// </summary>
public class EnemySceneTools
{
    [MenuItem("Tools/Enemy Setup/创建巡逻路线")]
    public static void CreatePatrolRoute()
    {
        var routeGO = new GameObject("PatrolRoute_新路线");
        routeGO.AddComponent<PatrolRoute>();

        // 创建默认4个巡逻点
        Vector3 basePos = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.camera.transform.position
            : Vector3.zero;
        basePos.z = 0f;

        CreatePatrolPoint(routeGO.transform, basePos + new Vector3(-2, 2, 0), 0);
        CreatePatrolPoint(routeGO.transform, basePos + new Vector3(2, 2, 0), 1);
        CreatePatrolPoint(routeGO.transform, basePos + new Vector3(2, -2, 0), 2);
        CreatePatrolPoint(routeGO.transform, basePos + new Vector3(-2, -2, 0), 3);

        Undo.RegisterCreatedObjectUndo(routeGO, "Create Patrol Route");
        Selection.activeGameObject = routeGO;
        Debug.Log("[EnemySceneTools] 已创建巡逻路线（4个巡逻点）");
    }

    [MenuItem("Tools/Enemy Setup/创建敌人刷新点")]
    public static void CreateSpawner()
    {
        Vector3 pos = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.camera.transform.position
            : Vector3.zero;
        pos.z = 0f;

        var spawnerGO = new GameObject("EnemySpawner_新刷新点");
        spawnerGO.transform.position = pos;
        spawnerGO.AddComponent<EnemySpawner>();

        Undo.RegisterCreatedObjectUndo(spawnerGO, "Create Enemy Spawner");
        Selection.activeGameObject = spawnerGO;
        Debug.Log("[EnemySceneTools] 已创建敌人刷新点");
    }

    private static void CreatePatrolPoint(Transform parent, Vector3 pos, int index)
    {
        var point = new GameObject($"PatrolPoint_{index}");
        point.transform.SetParent(parent, false);
        point.transform.position = pos;
    }
}
