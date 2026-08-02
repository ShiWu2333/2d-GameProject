using UnityEngine;
using UnityEditor;

/// <summary>
/// 编辑器工具：生成基础敌人预制体
/// 菜单：Tools → Enemy Setup → 创建敌人预制体
/// </summary>
public class EnemyPrefabCreator : EditorWindow
{
    private EnemySetup.EnemyPreset selectedPreset = EnemySetup.EnemyPreset.Normal;
    private string prefabName = "Enemy_Base";
    private Color enemyColor = new Color(0.9f, 0.2f, 0.2f);

    [MenuItem("Tools/Enemy Setup/创建敌人预制体")]
    public static void ShowWindow()
    {
        GetWindow<EnemyPrefabCreator>("创建敌人预制体");
    }

    void OnGUI()
    {
        GUILayout.Label("敌人预制体创建器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        prefabName = EditorGUILayout.TextField("预制体名称", prefabName);
        selectedPreset = (EnemySetup.EnemyPreset)EditorGUILayout.EnumPopup("预设类型", selectedPreset);
        enemyColor = EditorGUILayout.ColorField("颜色", enemyColor);

        GUILayout.Space(20);

        if (GUILayout.Button("创建预制体", GUILayout.Height(40)))
        {
            CreateEnemyPrefab();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("批量创建所有预设预制体", GUILayout.Height(30)))
        {
            CreateAllPresets();
        }
    }

    private void CreateEnemyPrefab()
    {
        var go = BuildEnemyGameObject(prefabName, selectedPreset, enemyColor);

        // 保存为预制体
        string path = $"Assets/Prefabs/Enemies/{prefabName}.prefab";
        EnsureDirectory("Assets/Prefabs/Enemies");

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        if (prefab != null)
        {
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[EnemyPrefabCreator] 已创建预制体: {path}");
        }
    }

    private void CreateAllPresets()
    {
        EnsureDirectory("Assets/Prefabs/Enemies");

        var presets = new[]
        {
            (EnemySetup.EnemyPreset.Weak, "Enemy_Weak", new Color(0.6f, 0.8f, 0.2f)),
            (EnemySetup.EnemyPreset.Normal, "Enemy_Normal", new Color(0.9f, 0.2f, 0.2f)),
            (EnemySetup.EnemyPreset.Elite, "Enemy_Elite", new Color(0.8f, 0.1f, 0.6f)),
            (EnemySetup.EnemyPreset.Sniper, "Enemy_Sniper", new Color(0.2f, 0.4f, 0.9f)),
            (EnemySetup.EnemyPreset.Rusher, "Enemy_Rusher", new Color(1f, 0.5f, 0f)),
        };

        foreach (var (preset, name, color) in presets)
        {
            var go = BuildEnemyGameObject(name, preset, color);
            string path = $"Assets/Prefabs/Enemies/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
            Debug.Log($"[EnemyPrefabCreator] 已创建: {path}");
        }

        AssetDatabase.Refresh();
        Debug.Log("[EnemyPrefabCreator] 所有预设预制体创建完毕！");
    }

    private static GameObject BuildEnemyGameObject(string name, EnemySetup.EnemyPreset preset, Color color)
    {
        var go = new GameObject(name);
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

        // 设置层和标签
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            go.layer = enemyLayer;
        go.tag = "Enemy";

        // SpriteRenderer
        var sr = go.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.color = color;
        sr.sortingOrder = 1;

        // Rigidbody2D
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        // Collider
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.8f);
        box.isTrigger = false;

        // 核心组件
        var stats = go.AddComponent<EnemyStats>();
        var ai = go.AddComponent<EnemyAI>();
        go.AddComponent<EnemyHealthBar>();

        // EnemySetup（应用预设）
        var setup = go.AddComponent<EnemySetup>();
        setup.preset = preset;
        setup.initialState = EnemyAI.AIState.Patrol;

        return go;
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
