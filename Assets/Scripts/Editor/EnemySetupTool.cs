using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 编辑器工具：快速配置场景中的敌人
/// 菜单：Tools → Enemy Setup → 初始化场景敌人
/// 会自动给场景中没有 EnemySetup 的红色物体添加敌人组件
/// </summary>
public class EnemySetupTool : EditorWindow
{
    [MenuItem("Tools/Enemy Setup/修复墙壁碰撞与GameSetup")]
    public static void FixWallsAndGameSetup()
    {
        var setup = Object.FindObjectOfType<GameSetupHelper>();
        if (setup == null)
        {
            var setupGO = new GameObject("GameSetup");
            setup = setupGO.AddComponent<GameSetupHelper>();
            Undo.RegisterCreatedObjectUndo(setupGO, "Create GameSetup");
            Debug.Log("[EnemySetupTool] 已创建 GameSetup");
        }

        var wallParent = FindWallParent();
        if (wallParent == null)
        {
            wallParent = CreateDefaultWallGroup();
            Debug.Log("[EnemySetupTool] 未找到墙壁组，已创建默认墙壁组");
        }

        var generator = Object.FindObjectOfType<WallColliderGenerator>();
        if (generator == null)
            generator = setup.gameObject.AddComponent<WallColliderGenerator>();

        generator.wallParent = wallParent;
        generator.Regenerate();
        int wallCount = NormalizeWallColliders(wallParent);

        EditorUtility.SetDirty(setup.gameObject);
        EditorUtility.SetDirty(wallParent.gameObject);
        EditorSceneManager.MarkSceneDirty(wallParent.gameObject.scene);
        Debug.Log($"[EnemySetupTool] 墙壁碰撞已修复：{wallCount} 个墙体已设为 Wall 层 + 非Trigger BoxCollider2D");
    }

    [MenuItem("Tools/Enemy Setup/初始化场景敌人")]
    public static void InitSceneEnemies()
    {
        // 找到所有已有 EnemySetup 的物体
        var existingSetups = Object.FindObjectsOfType<EnemySetup>();
        int configured = existingSetups.Length;

        // 检查场景中是否有 GameManager
        if (Object.FindObjectOfType<GameManager>() == null)
        {
            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gmGO, "Create GameManager");
            Debug.Log("[EnemySetupTool] 已创建 GameManager");
        }

        // 检查玩家是否有 EnemyAlertSystem
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<EnemyAlertSystem>() == null)
        {
            Undo.AddComponent<EnemyAlertSystem>(player);
            Debug.Log("[EnemySetupTool] 已为玩家添加 EnemyAlertSystem");
        }

        Debug.Log($"[EnemySetupTool] 场景中已有 {configured} 个敌人配置完毕");
        Debug.Log("[EnemySetupTool] 提示：给场景中的敌人物体添加 EnemySetup 组件即可自动配置AI");
    }

    [MenuItem("Tools/Enemy Setup/创建测试敌人")]
    public static void CreateTestEnemy()
    {
        // 在场景视图中心创建一个测试敌人
        Vector3 pos = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.camera.transform.position
            : Vector3.zero;
        pos.z = 0f;

        var go = new GameObject("Enemy_Test");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.color = Color.red;
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        var setup = go.AddComponent<EnemySetup>();
        setup.preset = EnemySetup.EnemyPreset.Normal;

        Undo.RegisterCreatedObjectUndo(go, "Create Test Enemy");
        Selection.activeGameObject = go;

        Debug.Log("[EnemySetupTool] 已创建测试敌人，可在Inspector中调整预设");
    }

    [MenuItem("Tools/Enemy Setup/为选中物体添加敌人组件")]
    public static void AddEnemyToSelected()
    {
        var selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选中要配置为敌人的物体", "确定");
            return;
        }

        int count = 0;
        foreach (var go in selected)
        {
            if (go.GetComponent<EnemySetup>() == null)
            {
                Undo.AddComponent<EnemySetup>(go);
                count++;
            }
        }

        Debug.Log($"[EnemySetupTool] 已为 {count} 个物体添加敌人组件");
    }

    private static Transform FindWallParent()
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

    private static int NormalizeWallColliders(Transform wallParent)
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        int count = 0;

        foreach (Transform child in wallParent.GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == wallParent) continue;

            var go = child.gameObject;
            var sr = go.GetComponent<SpriteRenderer>();
            var collider2D = go.GetComponent<Collider2D>();
            if (sr == null && collider2D == null)
                continue;

            Undo.RecordObject(go, "Normalize Wall Layer");
            if (wallLayer >= 0)
                go.layer = wallLayer;

            var box = collider2D as BoxCollider2D;
            if (box == null)
            {
                box = Undo.AddComponent<BoxCollider2D>(go);
                if (sr != null && sr.sprite != null)
                    box.size = sr.sprite.bounds.size;
                else
                    box.size = Vector2.one;
                box.offset = Vector2.zero;
            }
            else
            {
                Undo.RecordObject(box, "Normalize Wall Collider");
            }

            box.isTrigger = false;
            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(box);
            count++;
        }

        return count;
    }

    private static Transform CreateDefaultWallGroup()
    {
        var parent = new GameObject("墙壁组");
        Undo.RegisterCreatedObjectUndo(parent, "Create Wall Group");

        CreateWall(parent.transform, "Wall_Top", new Vector2(0f, 5f), new Vector2(16f, 0.5f));
        CreateWall(parent.transform, "Wall_Bottom", new Vector2(0f, -5f), new Vector2(16f, 0.5f));
        CreateWall(parent.transform, "Wall_Left", new Vector2(-8f, 0f), new Vector2(0.5f, 10f));
        CreateWall(parent.transform, "Wall_Right", new Vector2(8f, 0f), new Vector2(0.5f, 10f));
        CreateWall(parent.transform, "Wall_Cover", new Vector2(1.5f, 0f), new Vector2(0.6f, 3f));

        return parent.transform;
    }

    private static void CreateWall(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Wall");
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

    private static Sprite CreateSolidSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
