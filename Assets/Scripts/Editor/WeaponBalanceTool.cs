using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 游戏数值调整工具
/// 菜单：Tools → 数值调整
/// 一个窗口内总览和修改所有游戏核心数值（玩家属性、武器属性）
/// </summary>
public class WeaponBalanceTool : EditorWindow
{
    private Vector2 scroll;

    // 数据源
    private PlayerController playerController;
    private PlayerStats playerStats;
    private List<WeaponBase> sceneWeapons = new List<WeaponBase>();
    private List<WeaponBase> prefabWeapons = new List<WeaponBase>();

    // 折叠状态
    private bool showPlayer = true;
    private bool showSceneWeapons = true;
    private bool showPrefabWeapons = true;
    private bool editPrefabs = false;

    // 页签
    private int tab = 0;
    private static readonly string[] tabs = { "全部", "玩家属性", "武器数值" };

    [MenuItem("Tools/数值调整")]
    public static void Open()
    {
        var w = GetWindow<WeaponBalanceTool>("数值调整");
        w.minSize = new Vector2(620, 450);
        w.Refresh();
    }

    private void OnEnable() => Refresh();
    private void OnFocus()  => Refresh();

    private void Refresh()
    {
        // 玩家
#if UNITY_2023_1_OR_NEWER
        playerController = Object.FindFirstObjectByType<PlayerController>();
#else
        playerController = Object.FindObjectOfType<PlayerController>();
#endif
        playerStats = playerController != null ? playerController.GetComponent<PlayerStats>() : null;

        // 场景武器（包含隐藏的武器）
        sceneWeapons.Clear();
        var allWeaponsInScene = Resources.FindObjectsOfTypeAll<WeaponBase>();
        foreach (var w in allWeaponsInScene)
        {
            // 排除Prefab资产，只要场景中的实例
            if (w == null) continue;
            if (EditorUtility.IsPersistent(w.gameObject)) continue;
            if (w.gameObject.scene.name == null) continue;
            sceneWeapons.Add(w);
        }

        // Prefab武器（手动调用Awake获取子类赋值的真实数值）
        prefabWeapons.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Weapons" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var wb = prefab.GetComponent<WeaponBase>();
            if (wb != null)
            {
                var tempGO = Object.Instantiate(prefab);
                tempGO.hideFlags = HideFlags.HideAndDontSave;
                var tempWb = tempGO.GetComponent<WeaponBase>();
                if (tempWb != null)
                {
                    // 通过反射调用protected Awake
                    var awakeMethod = tempWb.GetType().GetMethod("Awake",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public);
                    if (awakeMethod != null)
                        awakeMethod.Invoke(tempWb, null);

                    CopyWeaponValues(tempWb, wb);
                    EditorUtility.SetDirty(prefab);
                }
                Object.DestroyImmediate(tempGO);
                prefabWeapons.Add(wb);
            }
        }
        AssetDatabase.SaveAssets();
    }

    // ══════════════════════════════════════════════════
    //  GUI
    // ══════════════════════════════════════════════════

    private void OnGUI()
    {
        // 工具栏
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                Refresh();
            GUILayout.Space(10);
            tab = GUILayout.Toolbar(tab, tabs, EditorStyles.toolbarButton, GUILayout.Width(260));
            GUILayout.FlexibleSpace();
            editPrefabs = GUILayout.Toggle(editPrefabs, "允许改Prefab", EditorStyles.toolbarButton);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (tab == 0 || tab == 1)
            DrawPlayerSection();

        if (tab == 0 || tab == 2)
            DrawWeaponSection();

        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════════════════
    //  玩家属性
    // ══════════════════════════════════════════════════

    private void DrawPlayerSection()
    {
        showPlayer = EditorGUILayout.Foldout(showPlayer, "▶ 玩家属性", true, EditorStyles.foldoutHeader);
        if (!showPlayer) return;

        if (playerController == null || playerStats == null)
        {
            EditorGUILayout.HelpBox("场景中未找到玩家（需要PlayerController + PlayerStats）", MessageType.Warning);
            return;
        }

        EditorGUI.indentLevel++;

        // ── 移动 ──
        EditorGUILayout.LabelField("移动", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        playerController.walkSpeed      = EditorGUILayout.FloatField("步行速度", playerController.walkSpeed);
        playerController.sprintSpeed    = EditorGUILayout.FloatField("冲刺速度", playerController.sprintSpeed);
        playerController.knifeSpeedBonus= EditorGUILayout.FloatField("持刀加速倍率", playerController.knifeSpeedBonus);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(playerController);

        EditorGUILayout.Space(4);

        // ── 血量 ──
        EditorGUILayout.LabelField("血量", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        playerStats.maxHealth = EditorGUILayout.FloatField("最大血量", playerStats.maxHealth);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(playerStats);

        EditorGUILayout.Space(4);

        // ── 体力 ──
        EditorGUILayout.LabelField("体力", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        playerStats.maxStamina       = EditorGUILayout.FloatField("最大体力", playerStats.maxStamina);
        playerStats.staminaDrainRate = EditorGUILayout.FloatField("体力消耗速率 (/秒)", playerStats.staminaDrainRate);
        playerStats.staminaRegenRate = EditorGUILayout.FloatField("体力恢复速率 (/秒)", playerStats.staminaRegenRate);
        playerStats.staminaRegenDelay= EditorGUILayout.FloatField("恢复延迟 (秒)", playerStats.staminaRegenDelay);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(playerStats);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(8);
        DrawLine();
    }

    // ══════════════════════════════════════════════════
    //  武器数值
    // ══════════════════════════════════════════════════

    private void DrawWeaponSection()
    {
        // 场景武器
        showSceneWeapons = EditorGUILayout.Foldout(showSceneWeapons,
            $"▶ 场景武器 ({sceneWeapons.Count})", true, EditorStyles.foldoutHeader);
        if (showSceneWeapons && sceneWeapons.Count > 0)
        {
            DrawWeaponTable(sceneWeapons, false);
            EditorGUILayout.Space(8);
        }

        // Prefab武器
        showPrefabWeapons = EditorGUILayout.Foldout(showPrefabWeapons,
            $"▶ Prefab武器 ({prefabWeapons.Count})", true, EditorStyles.foldoutHeader);
        if (showPrefabWeapons && prefabWeapons.Count > 0)
        {
            DrawWeaponTable(prefabWeapons, !editPrefabs);
            EditorGUILayout.Space(8);
        }

        DrawLine();
    }

    // ── 武器表格 ──────────────────────────────────────

    private void DrawWeaponTable(List<WeaponBase> weapons, bool readOnly)
    {
        // 表头
        using (new EditorGUILayout.HorizontalScope())
        {
            Col("名称",    85);
            Col("伤害",    45);
            Col("射速",    45);
            Col("弹匣",    38);
            Col("换弹(s)", 48);
            Col("射程",    40);
            Col("后坐力",  45);
            Col("基础散射", 50);
            Col("移动散射", 50);
            Col("移速倍率", 50);
            Col("半自动",  38);
        }
        DrawThinLine();

        // 数据行
        foreach (var w in weapons)
        {
            if (w == null) continue;
            DrawWeaponRow(w, readOnly);
        }
    }

    private void DrawWeaponRow(WeaponBase w, bool readOnly)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !readOnly;
            EditorGUI.BeginChangeCheck();

            GUILayout.Label(w.weaponName, GUILayout.Width(85));
            w.damage          = EditorGUILayout.FloatField(w.damage,          GUILayout.Width(45));
            w.fireRate        = EditorGUILayout.FloatField(w.fireRate,        GUILayout.Width(45));
            w.maxAmmo         = EditorGUILayout.IntField(w.maxAmmo,           GUILayout.Width(38));
            w.reloadTime      = EditorGUILayout.FloatField(w.reloadTime,      GUILayout.Width(48));
            w.range           = EditorGUILayout.FloatField(w.range,           GUILayout.Width(40));
            w.recoil          = EditorGUILayout.FloatField(w.recoil,          GUILayout.Width(45));
            w.baseSpread      = EditorGUILayout.FloatField(w.baseSpread,      GUILayout.Width(50));
            w.moveSpreadBonus = EditorGUILayout.FloatField(w.moveSpreadBonus, GUILayout.Width(50));
            w.moveSpeedMult   = EditorGUILayout.FloatField(w.moveSpeedMult,   GUILayout.Width(50));
            w.isSemiAuto      = EditorGUILayout.Toggle(w.isSemiAuto,          GUILayout.Width(38));

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(w);

            GUI.enabled = true;
        }
    }

    // ── 工具方法 ──────────────────────────────────────

    private static void Col(string text, int width)
    {
        GUILayout.Label(text, EditorStyles.miniLabel, GUILayout.Width(width));
    }

    private static void DrawLine()
    {
        var r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0.4f, 0.4f, 0.4f, 0.6f));
        EditorGUILayout.Space(4);
    }

    private static void DrawThinLine()
    {
        var r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }

    /// <summary>
    /// 把临时实例化的武器数值复制回Prefab资产
    /// </summary>
    private static void CopyWeaponValues(WeaponBase from, WeaponBase to)
    {
        to.weaponName      = from.weaponName;
        to.ammoType        = from.ammoType;
        to.damage          = from.damage;
        to.fireRate        = from.fireRate;
        to.maxAmmo         = from.maxAmmo;
        to.reloadTime      = from.reloadTime;
        to.range           = from.range;
        to.recoil          = from.recoil;
        to.baseSpread      = from.baseSpread;
        to.moveSpreadBonus = from.moveSpreadBonus;
        to.moveSpeedMult   = from.moveSpeedMult;
        to.aimSpreadMult   = from.aimSpreadMult;
        to.aimMoveSpeedMult= from.aimMoveSpeedMult;
        to.isSemiAuto      = from.isSemiAuto;
        to.bulletSpeed     = from.bulletSpeed;
        to.pelletsPerShot  = from.pelletsPerShot;
    }
}
