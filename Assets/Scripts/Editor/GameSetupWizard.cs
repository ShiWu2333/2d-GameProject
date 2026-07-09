using UnityEngine;
using UnityEditor;

/// <summary>
/// 游戏配置向导
/// 菜单：Tools → 游戏配置向导
/// 支持一键全部配置，也支持各步骤单独执行
/// </summary>
public class GameSetupWizard : EditorWindow
{
    // ── 窗口状态 ──
    private GameObject playerGO;
    private Canvas     targetCanvas;
    private int slot1Index = 1; // 突击步枪
    private int slot2Index = 0; // 冲锋枪
    private int meleeIndex = 6; // 刀

    // ── UI选项 ──
    private float slotSize    = 80f;
    private float slotSpacing = 8f;
    private float padding     = 12f;
    private Color colSelected = new Color(1f, 0.85f, 0f, 0.9f);
    private Color colNormal   = new Color(0.15f, 0.15f, 0.15f, 0.75f);
    private Color colEmpty    = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    private Color colPanel    = new Color(0f, 0f, 0f, 0.45f);

    private Vector2 scroll;
    private bool showAdvanced;

    [MenuItem("Tools/游戏配置向导")]
    public static void Open()
    {
        var w = GetWindow<GameSetupWizard>("游戏配置向导");
        w.minSize = new Vector2(400, 580);
        w.Show();
        w.AutoDetect();
    }

    private void AutoDetect()
    {
        if (playerGO == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerGO = go;
        }
        if (targetCanvas == null)
        {
#if UNITY_2023_1_OR_NEWER
            targetCanvas = Object.FindFirstObjectByType<Canvas>();
#else
            targetCanvas = Object.FindObjectOfType<Canvas>();
#endif
        }
    }

    // ══════════════════════════════════════════════════
    //  GUI
    // ══════════════════════════════════════════════════

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawTitle();
        DrawSceneObjects();
        DrawWeaponSlots();
        DrawAdvancedOptions();
        DrawStepButtons();
        DrawRunAllButton();

        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();
    }

    private void DrawTitle()
    {
        var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("⚙  游戏配置向导", style);
        EditorGUILayout.Space(2);
        EditorGUILayout.HelpBox("可一键全部配置，也可分步单独执行。", MessageType.Info);
        EditorGUILayout.Space(4);
        Line();
    }

    private void DrawSceneObjects()
    {
        Section("场景对象");
        using (new EditorGUILayout.HorizontalScope())
        {
            playerGO = (GameObject)EditorGUILayout.ObjectField("玩家 GameObject", playerGO, typeof(GameObject), true);
            if (GUILayout.Button("自动查找", GUILayout.Width(72))) AutoDetect();
        }
        targetCanvas = (Canvas)EditorGUILayout.ObjectField("UI Canvas", targetCanvas, typeof(Canvas), true);

        if (playerGO == null) EditorGUILayout.HelpBox("未指定玩家 GameObject", MessageType.Warning);
        if (targetCanvas == null) EditorGUILayout.HelpBox("未指定 Canvas", MessageType.Warning);
        EditorGUILayout.Space(4);
        Line();
    }

    private void DrawWeaponSlots()
    {
        Section("武器槽配置");
        string[] labels = System.Array.ConvertAll(PrefabBuilder.WeaponDefs, d => d.label);
        slot1Index = EditorGUILayout.Popup("槽位1（键1）主武器", slot1Index, labels);
        slot2Index = EditorGUILayout.Popup("槽位2（键2）主武器", slot2Index, labels);
        meleeIndex = EditorGUILayout.Popup("槽位3（键3）近战",   meleeIndex, labels);
        EditorGUILayout.Space(4);
        Line();
    }

    private void DrawAdvancedOptions()
    {
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "高级选项（UI尺寸 / 颜色）", true);
        if (showAdvanced)
        {
            EditorGUI.indentLevel++;
            slotSize    = EditorGUILayout.FloatField("格子尺寸",   slotSize);
            slotSpacing = EditorGUILayout.FloatField("格子间距",   slotSpacing);
            padding     = EditorGUILayout.FloatField("面板内边距", padding);
            EditorGUILayout.Space(2);
            colSelected = EditorGUILayout.ColorField("选中高亮色", colSelected);
            colNormal   = EditorGUILayout.ColorField("普通格子色", colNormal);
            colEmpty    = EditorGUILayout.ColorField("空槽颜色",   colEmpty);
            colPanel    = EditorGUILayout.ColorField("面板背景色", colPanel);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(4);
        Line();
    }

    private void DrawStepButtons()
    {
        Section("分步执行");
        EditorGUILayout.HelpBox("每个按钮独立执行对应步骤，会先清除该步骤的旧数据再重新生成。", MessageType.None);
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("① 生成Prefab\n（子弹+武器）", GUILayout.Height(38)))
                StepPrefabs();

            GUI.enabled = playerGO != null;
            if (GUILayout.Button("② 配置玩家\n（组件+武器挂载）", GUILayout.Height(38)))
                StepPlayer();
            GUI.enabled = true;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = playerGO != null && targetCanvas != null;
            if (GUILayout.Button("③ 搭建HUD\n（武器栏UI）", GUILayout.Height(38)))
                StepHUD();
            GUI.enabled = true;

            GUI.color = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("⚠ 清除全部\n（删除旧数据）", GUILayout.Height(38)))
                StepClean();
            GUI.color = Color.white;
        }

        EditorGUILayout.Space(4);
        Line();
    }

    private void DrawRunAllButton()
    {
        bool canRun = playerGO != null && targetCanvas != null;
        GUI.enabled = canRun;
        var style = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
        if (GUILayout.Button("▶  一键配置全部（清除旧数据 + 重新生成）", style, GUILayout.Height(42)))
            RunAll();
        GUI.enabled = true;
    }

    // ══════════════════════════════════════════════════
    //  分步执行
    // ══════════════════════════════════════════════════

    private void StepPrefabs()
    {
        SetupCleaner.CleanPrefabs();
        var white = PrefabBuilder.EnsureWhiteSprite();
        var bulletMap = PrefabBuilder.BuildBulletPrefabs(white);
        PrefabBuilder.BuildWeaponPrefabs(white, bulletMap);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "子弹和武器Prefab已重新生成。", "OK");
    }

    private void StepPlayer()
    {
        SetupCleaner.CleanPlayer(playerGO);

        // 确保Prefab存在
        var white = PrefabBuilder.EnsureWhiteSprite();
        var bulletMap = PrefabBuilder.BuildBulletPrefabs(white);
        var weaponMap = PrefabBuilder.BuildWeaponPrefabs(white, bulletMap);

        PlayerSetup.Run(playerGO, weaponMap, slot1Index, slot2Index, meleeIndex);
        EditorUtility.DisplayDialog("完成", "玩家组件和武器已重新配置。", "OK");
    }

    private void StepHUD()
    {
        SetupCleaner.CleanHUD(targetCanvas);

        // 需要slotSys引用
        var slotSys = playerGO.GetComponent<WeaponSlotSystem>();
        if (slotSys == null)
        {
            EditorUtility.DisplayDialog("错误", "玩家没有 WeaponSlotSystem，请先执行步骤②。", "OK");
            return;
        }

        WeaponHUDSetup.Run(targetCanvas, slotSys, slot1Index, slot2Index, meleeIndex, GetHUDOptions());
        EditorUtility.DisplayDialog("完成", "武器栏HUD已重新搭建。", "OK");
    }

    private void StepClean()
    {
        if (!EditorUtility.DisplayDialog("确认清除",
            "将删除：\n• 所有子弹/武器Prefab\n• 玩家武器实例\n• 武器栏HUD面板\n\n确定清除？",
            "清除", "取消"))
            return;

        SetupCleaner.CleanAll(playerGO, targetCanvas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "旧数据已全部清除。", "OK");
    }

    // ══════════════════════════════════════════════════
    //  一键全部
    // ══════════════════════════════════════════════════

    private void RunAll()
    {
        Undo.SetCurrentGroupName("游戏一键配置");
        int group = Undo.GetCurrentGroup();

        try
        {
            EditorUtility.DisplayProgressBar("配置中...", "清除旧数据", 0.02f);
            SetupCleaner.CleanAll(playerGO, targetCanvas);

            EditorUtility.DisplayProgressBar("配置中...", "生成Sprite", 0.10f);
            var white = PrefabBuilder.EnsureWhiteSprite();

            EditorUtility.DisplayProgressBar("配置中...", "生成子弹Prefab", 0.25f);
            var bulletMap = PrefabBuilder.BuildBulletPrefabs(white);

            EditorUtility.DisplayProgressBar("配置中...", "生成武器Prefab", 0.50f);
            var weaponMap = PrefabBuilder.BuildWeaponPrefabs(white, bulletMap);

            EditorUtility.DisplayProgressBar("配置中...", "配置玩家组件", 0.70f);
            var (slotSys, _) = PlayerSetup.Run(playerGO, weaponMap, slot1Index, slot2Index, meleeIndex);

            EditorUtility.DisplayProgressBar("配置中...", "搭建武器栏UI", 0.85f);
            WeaponHUDSetup.Run(targetCanvas, slotSys, slot1Index, slot2Index, meleeIndex, GetHUDOptions());

            EditorUtility.DisplayProgressBar("配置中...", "生成地面弹药", 0.95f);
            AmmoSpawner.SpawnAmmoForWeapons(playerGO, slot1Index, slot2Index, meleeIndex);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Undo.CollapseUndoOperations(group);

            EditorUtility.DisplayDialog("✅ 配置完成",
                "旧数据已清除，所有组件已重新配置！\n\n" +
                "• 子弹Prefab → Assets/Prefabs/Bullets/\n" +
                "• 武器Prefab → Assets/Prefabs/Weapons/\n" +
                "• 玩家已挂载 WeaponSlotSystem\n" +
                "• Canvas 已生成武器栏 UI\n" +
                "• 对应弹药已放置在玩家附近（各30发）\n\n" +
                "直接按 Play 即可测试。", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ══════════════════════════════════════════════════
    //  工具
    // ══════════════════════════════════════════════════

    private WeaponHUDSetup.Options GetHUDOptions()
    {
        return new WeaponHUDSetup.Options
        {
            slotSize = slotSize, slotSpacing = slotSpacing, padding = padding,
            colSelected = colSelected, colNormal = colNormal,
            colEmpty = colEmpty, colPanel = colPanel,
        };
    }

    private static void Section(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private static void Line()
    {
        var r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        EditorGUILayout.Space(3);
    }
}
