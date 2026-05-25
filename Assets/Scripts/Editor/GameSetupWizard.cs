using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// 游戏一键配置向导
/// 菜单：Tools → 游戏配置向导
/// 自动完成：白色正方形Sprite生成、子弹Prefab、武器Prefab、
///           玩家组件挂载、AimPivot、武器实例化、Canvas UI搭建
/// 只需拖入玩家GameObject和Canvas，点一个按钮即可。
/// </summary>
public class GameSetupWizard : EditorWindow
{
    // ══════════════════════════════════════════════════
    //  窗口状态
    // ══════════════════════════════════════════════════
    private GameObject playerGO;
    private Canvas     targetCanvas;

    // 槽位武器选择（枚举索引对应 WeaponDefs 数组）
    private int slot1Index = 1; // 默认：突击步枪
    private int slot2Index = 0; // 默认：冲锋枪
    private int meleeIndex = 6; // 默认：刀

    // UI尺寸
    private float slotSize    = 80f;
    private float slotSpacing = 8f;
    private float padding     = 12f;

    // 颜色
    private Color colSelected = new Color(1f, 0.85f, 0f, 0.9f);
    private Color colNormal   = new Color(0.15f, 0.15f, 0.15f, 0.75f);
    private Color colEmpty    = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    private Color colPanel    = new Color(0f, 0f, 0f, 0.45f);

    private Vector2 scroll;
    private bool showAdvanced = false;

    // 路径常量
    private const string WEAPON_DIR = "Assets/Prefabs/Weapons";
    private const string BULLET_DIR = "Assets/Prefabs/Bullets";
    private const string SPRITE_PATH = "Assets/Prefabs/WhiteSquare.png";

    // ══════════════════════════════════════════════════
    //  武器定义表
    // ══════════════════════════════════════════════════
    private struct WeaponDef
    {
        public string      label;
        public System.Type script;
        public Color       bodyCol;
        public Color       barrelCol;
        public Vector2     bodySize;
        public Vector2     barrelSize;
        public Vector2     barrelOffset;
        public Vector2     firePointOffset;
        public AmmoType    ammo;
    }

    private static readonly WeaponDef[] WeaponDefs = new WeaponDef[]
    {
        new WeaponDef { label="冲锋枪",     script=typeof(SMG),           ammo=AmmoType.SMG,
            bodyCol=new Color(0f,0.85f,0.85f),  barrelCol=new Color(0f,0.5f,0.5f),
            bodySize=new Vector2(0.18f,0.28f),  barrelSize=new Vector2(0.22f,0.08f),
            barrelOffset=new Vector2(0.18f,0f), firePointOffset=new Vector2(0.30f,0f) },

        new WeaponDef { label="突击步枪",   script=typeof(AssaultRifle),  ammo=AmmoType.Rifle,
            bodyCol=new Color(0.2f,0.4f,1f),    barrelCol=new Color(0.1f,0.2f,0.7f),
            bodySize=new Vector2(0.16f,0.30f),  barrelSize=new Vector2(0.28f,0.08f),
            barrelOffset=new Vector2(0.20f,0f), firePointOffset=new Vector2(0.35f,0f) },

        new WeaponDef { label="射手步枪",   script=typeof(MarksmanRifle), ammo=AmmoType.Rifle,
            bodyCol=new Color(0.1f,0.6f,0.2f),  barrelCol=new Color(0.05f,0.35f,0.1f),
            bodySize=new Vector2(0.14f,0.28f),  barrelSize=new Vector2(0.36f,0.07f),
            barrelOffset=new Vector2(0.22f,0f), firePointOffset=new Vector2(0.42f,0f) },

        new WeaponDef { label="连发霰弹枪", script=typeof(AutoShotgun),   ammo=AmmoType.Shotgun,
            bodyCol=new Color(1f,0.5f,0f),      barrelCol=new Color(0.7f,0.3f,0f),
            bodySize=new Vector2(0.20f,0.34f),  barrelSize=new Vector2(0.18f,0.14f),
            barrelOffset=new Vector2(0.17f,0f), firePointOffset=new Vector2(0.28f,0f) },

        new WeaponDef { label="轻机枪",     script=typeof(LMG),           ammo=AmmoType.LMG,
            bodyCol=new Color(1f,0.9f,0f),      barrelCol=new Color(0.7f,0.6f,0f),
            bodySize=new Vector2(0.22f,0.40f),  barrelSize=new Vector2(0.32f,0.10f),
            barrelOffset=new Vector2(0.24f,0f), firePointOffset=new Vector2(0.40f,0f) },

        new WeaponDef { label="自动手枪",   script=typeof(AutoPistol),    ammo=AmmoType.SMG,
            bodyCol=new Color(0.7f,0.2f,1f),    barrelCol=new Color(0.45f,0.1f,0.7f),
            bodySize=new Vector2(0.16f,0.22f),  barrelSize=new Vector2(0.18f,0.07f),
            barrelOffset=new Vector2(0.15f,0f), firePointOffset=new Vector2(0.25f,0f) },

        new WeaponDef { label="刀",         script=typeof(Knife),         ammo=AmmoType.None,
            bodyCol=new Color(0.75f,0.75f,0.75f), barrelCol=new Color(0.95f,0.95f,0.95f),
            bodySize=new Vector2(0.30f,0.10f),  barrelSize=new Vector2(0.20f,0.06f),
            barrelOffset=new Vector2(0.26f,0f), firePointOffset=new Vector2(0.38f,0f) },
    };

    // 子弹定义
    private struct BulletDef
    {
        public string   name;
        public AmmoType ammo;
        public Color    color;
        public Vector2  size;
    }

    private static readonly BulletDef[] BulletDefs = new BulletDef[]
    {
        new BulletDef { name="Bullet_SMG",     ammo=AmmoType.SMG,
            color=new Color(0f,0.9f,0.9f),     size=new Vector2(0.06f,0.12f) },
        new BulletDef { name="Bullet_Rifle",   ammo=AmmoType.Rifle,
            color=new Color(0.3f,0.5f,1f),     size=new Vector2(0.06f,0.14f) },
        new BulletDef { name="Bullet_Shotgun", ammo=AmmoType.Shotgun,
            color=new Color(1f,0.6f,0.1f),     size=new Vector2(0.08f,0.08f) },
        new BulletDef { name="Bullet_LMG",     ammo=AmmoType.LMG,
            color=new Color(1f,0.95f,0.1f),    size=new Vector2(0.07f,0.15f) },
    };

    // ══════════════════════════════════════════════════
    //  菜单入口
    // ══════════════════════════════════════════════════
    [MenuItem("Tools/游戏配置向导")]
    public static void Open()
    {
        var w = GetWindow<GameSetupWizard>("游戏配置向导");
        w.minSize = new Vector2(380, 500);
        w.Show();
        // 自动查找场景中的玩家和Canvas
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

        // 标题
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 15, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("⚙  游戏一键配置向导", titleStyle);
        EditorGUILayout.Space(2);
        EditorGUILayout.HelpBox(
            "只需指定玩家和Canvas，点击「一键配置」即可自动完成所有设置。",
            MessageType.Info);
        EditorGUILayout.Space(4);
        Line();

        // ── 场景对象 ──────────────────────────────────
        Section("场景对象");
        using (new EditorGUILayout.HorizontalScope())
        {
            playerGO = (GameObject)EditorGUILayout.ObjectField(
                "玩家 GameObject", playerGO, typeof(GameObject), true);
            if (GUILayout.Button("自动查找", GUILayout.Width(72)))
                AutoDetect();
        }
        targetCanvas = (Canvas)EditorGUILayout.ObjectField(
            "UI Canvas", targetCanvas, typeof(Canvas), true);

        // 状态指示
        bool hasPlayer = playerGO != null;
        bool hasCanvas = targetCanvas != null;
        if (!hasPlayer) Warn("未指定玩家 GameObject");
        if (!hasCanvas) Warn("未指定 Canvas");
        EditorGUILayout.Space(4);
        Line();

        // ── 武器槽配置 ────────────────────────────────
        Section("武器槽配置");
        string[] labels = System.Array.ConvertAll(WeaponDefs, d => d.label);
        slot1Index = EditorGUILayout.Popup("槽位1（键1）主武器", slot1Index, labels);
        slot2Index = EditorGUILayout.Popup("槽位2（键2）主武器", slot2Index, labels);
        meleeIndex = EditorGUILayout.Popup("槽位3（键3）近战",   meleeIndex, labels);
        EditorGUILayout.Space(4);
        Line();

        // ── 高级选项（折叠）──────────────────────────
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

        // ── 执行按钮 ──────────────────────────────────
        bool canRun = hasPlayer && hasCanvas;
        GUI.enabled = canRun;
        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
        if (GUILayout.Button("▶  一键配置全部", btnStyle, GUILayout.Height(40)))
            RunAll();
        GUI.enabled = true;

        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();
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
    private static void Warn(string msg)
    {
        EditorGUILayout.HelpBox(msg, MessageType.Warning);
    }

    // ══════════════════════════════════════════════════
    //  主流程
    // ══════════════════════════════════════════════════
    private void RunAll()
    {
        Undo.SetCurrentGroupName("游戏一键配置");
        int group = Undo.GetCurrentGroup();

        try
        {
            EditorUtility.DisplayProgressBar("配置中...", "生成白色正方形Sprite", 0.05f);
            Sprite whiteSprite = EnsureWhiteSprite();

            EditorUtility.DisplayProgressBar("配置中...", "生成子弹Prefab", 0.20f);
            var bulletMap = BuildBulletPrefabs(whiteSprite);

            EditorUtility.DisplayProgressBar("配置中...", "生成武器Prefab", 0.45f);
            var weaponPrefabs = BuildWeaponPrefabs(whiteSprite, bulletMap);

            EditorUtility.DisplayProgressBar("配置中...", "配置玩家组件", 0.65f);
            var (slotSys, aimPivot) = SetupPlayer(weaponPrefabs);

            EditorUtility.DisplayProgressBar("配置中...", "搭建武器栏UI", 0.85f);
            SetupHUD(slotSys);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Undo.CollapseUndoOperations(group);

            EditorUtility.DisplayProgressBar("配置中...", "完成", 1f);
            Debug.Log("[GameSetupWizard] 全部配置完成！");
            EditorUtility.DisplayDialog("✅ 配置完成",
                "所有组件已自动配置完毕！\n\n" +
                "• 子弹Prefab → Assets/Prefabs/Bullets/\n" +
                "• 武器Prefab → Assets/Prefabs/Weapons/\n" +
                "• 玩家已挂载 WeaponSlotSystem\n" +
                "• Canvas 已生成武器栏 UI\n\n" +
                "直接按 Play 即可测试。", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ══════════════════════════════════════════════════
    //  Step 1：白色正方形 Sprite
    // ══════════════════════════════════════════════════
    private static Sprite EnsureWhiteSprite()
    {
        // 优先复用已有资源
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        if (existing != null) return existing;

        EnsureDir("Assets/Prefabs");

        // 生成 4×4 纯白 PNG
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        string absPath = Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", SPRITE_PATH));
        File.WriteAllBytes(absPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(SPRITE_PATH);

        var ti = (TextureImporter)AssetImporter.GetAtPath(SPRITE_PATH);
        ti.textureType         = TextureImporterType.Sprite;
        ti.spritePixelsPerUnit = 4;
        ti.filterMode          = FilterMode.Point;
        ti.textureCompression  = TextureImporterCompression.Uncompressed;
        AssetDatabase.ImportAsset(SPRITE_PATH);

        return AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
    }

    // ══════════════════════════════════════════════════
    //  Step 2：子弹 Prefab
    //  返回 AmmoType → Prefab 的映射
    // ══════════════════════════════════════════════════
    private static System.Collections.Generic.Dictionary<AmmoType, GameObject>
        BuildBulletPrefabs(Sprite white)
    {
        EnsureDir(BULLET_DIR);
        var map = new System.Collections.Generic.Dictionary<AmmoType, GameObject>();

        foreach (var def in BulletDefs)
        {
            string path = $"{BULLET_DIR}/{def.name}.prefab";

            // 已存在则复用，不重复创建
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { map[def.ammo] = existing; continue; }

            var root = new GameObject(def.name);

            // Bullet 脚本（RequireComponent 会自动添加 Rigidbody2D）
            var b = root.AddComponent<Bullet>();
            b.damage   = 10f;
            b.maxRange = 20f;

            // 外观
            MakeSquareChild(root.transform, "Visual", white, def.color, def.size);

            // 物理（Bullet 的 RequireComponent 已自动添加 Rigidbody2D）
            var rb2 = root.GetComponent<Rigidbody2D>();
            rb2.gravityScale = 0f;
            rb2.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = root.AddComponent<CircleCollider2D>();
            col.radius    = Mathf.Max(def.size.x, def.size.y) * 0.5f;
            col.isTrigger = true;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            map[def.ammo] = prefab;
        }
        return map;
    }

    // ══════════════════════════════════════════════════
    //  Step 3：武器 Prefab
    //  返回 WeaponDef.label → Prefab 的映射
    // ══════════════════════════════════════════════════
    private static System.Collections.Generic.Dictionary<string, GameObject>
        BuildWeaponPrefabs(Sprite white,
            System.Collections.Generic.Dictionary<AmmoType, GameObject> bulletMap)
    {
        EnsureDir(WEAPON_DIR);
        var map = new System.Collections.Generic.Dictionary<string, GameObject>();

        foreach (var def in WeaponDefs)
        {
            string path = $"{WEAPON_DIR}/{def.script.Name}.prefab";

            // 已存在则复用（不覆盖，避免丢失场景引用）
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { map[def.label] = existing; continue; }

            var root = new GameObject(def.script.Name);

            // 武器脚本
            var weapon = (WeaponBase)root.AddComponent(def.script);

            // 主体 + 枪管
            MakeSquareChild(root.transform, "Body",   white, def.bodyCol,   def.bodySize);
            var barrel = MakeSquareChild(root.transform, "Barrel", white, def.barrelCol, def.barrelSize);
            barrel.transform.localPosition = def.barrelOffset;

            // FirePoint
            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(root.transform, false);
            fp.transform.localPosition = def.firePointOffset;
            weapon.firePoint = fp.transform;

            // 子弹
            if (bulletMap.TryGetValue(def.ammo, out var bp))
                weapon.bulletPrefab = bp;

            // 碰撞体（拾取用）
            var col = root.AddComponent<BoxCollider2D>();
            col.size      = def.bodySize + new Vector2(0.06f, 0.06f);
            col.isTrigger = true;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            map[def.label] = prefab;
        }
        return map;
    }

    // ══════════════════════════════════════════════════
    //  Step 4：玩家组件
    // ══════════════════════════════════════════════════
    private (WeaponSlotSystem, Transform) SetupPlayer(
        System.Collections.Generic.Dictionary<string, GameObject> weaponPrefabs)
    {
        Undo.RecordObject(playerGO, "Setup Player");

        // PlayerController
        var pc = GetOrAdd<PlayerController>(playerGO);

        // AimPivot
        Transform aimPivot = playerGO.transform.Find("AimPivot");
        if (aimPivot == null)
        {
            var ap = new GameObject("AimPivot");
            Undo.RegisterCreatedObjectUndo(ap, "Create AimPivot");
            ap.transform.SetParent(playerGO.transform, false);
            aimPivot = ap.transform;
        }
        if (pc.aimPivot == null)
        {
            Undo.RecordObject(pc, "Set AimPivot");
            pc.aimPivot = aimPivot;
        }

        // WeaponSlotSystem
        var slotSys = GetOrAdd<WeaponSlotSystem>(playerGO);
        Undo.RecordObject(slotSys, "Setup WeaponSlotSystem");

        // 实例化三把武器到 AimPivot 下
        slotSys.primary1 = InstantiateWeapon(weaponPrefabs, slot1Index, aimPivot, "Weapon_Slot1");
        slotSys.primary2 = InstantiateWeapon(weaponPrefabs, slot2Index, aimPivot, "Weapon_Slot2");
        slotSys.melee    = InstantiateWeapon(weaponPrefabs, meleeIndex,  aimPivot, "Weapon_Melee");

        EditorUtility.SetDirty(playerGO);
        return (slotSys, aimPivot);
    }

    private WeaponBase InstantiateWeapon(
        System.Collections.Generic.Dictionary<string, GameObject> map,
        int defIndex, Transform parent, string slotName)
    {
        if (defIndex < 0 || defIndex >= WeaponDefs.Length) return null;
        var def = WeaponDefs[defIndex];
        if (!map.TryGetValue(def.label, out var prefab) || prefab == null) return null;

        // 复用已存在的同名子物体
        var existing = parent.Find(slotName);
        if (existing != null)
        {
            var wb = existing.GetComponent<WeaponBase>();
            if (wb != null) return wb;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = slotName;
        inst.SetActive(false); // WeaponSlotSystem 控制显隐
        Undo.RegisterCreatedObjectUndo(inst, "Instantiate " + slotName);
        return inst.GetComponent<WeaponBase>();
    }

    // ══════════════════════════════════════════════════
    //  Step 5：武器栏 HUD
    // ══════════════════════════════════════════════════
    private void SetupHUD(WeaponSlotSystem slotSys)
    {
        var canvasTf = targetCanvas.transform;

        // 面板根节点
        var panelGO   = FindOrMakeChild(canvasTf, "WeaponSlotPanel");
        var panelRect = GetOrAdd<RectTransform>(panelGO);
        panelRect.anchorMin        = Vector2.zero;
        panelRect.anchorMax        = Vector2.zero;
        panelRect.pivot            = Vector2.zero;
        float totalH = slotSize * 3 + slotSpacing * 2 + padding * 2;
        float totalW = slotSize + padding * 2;
        panelRect.sizeDelta        = new Vector2(totalW, totalH);
        panelRect.anchoredPosition = new Vector2(16f, 16f);

        var panelImg = GetOrAdd<Image>(panelGO);
        panelImg.color = colPanel;

        var hud = GetOrAdd<WeaponSlotHUD>(panelGO);
        Undo.RecordObject(hud, "Setup HUD");
        hud.selectedColor   = colSelected;
        hud.normalColor     = colNormal;
        hud.emptySlotColor  = colEmpty;
        hud.slotSystemRef   = slotSys;   // 直接赋引用，不依赖Tag查找
        if (hud.slots == null || hud.slots.Length != 3)
            hud.slots = new WeaponSlotHUD.SlotUI[3];

        string[] slotNames  = { "Slot_Primary1", "Slot_Primary2", "Slot_Melee" };
        string[] keyHints   = { "1", "2", "3" };
        // 武器名从槽位选择中读取
        string[] weaponLabels =
        {
            WeaponDefs[slot1Index].label,
            WeaponDefs[slot2Index].label,
            WeaponDefs[meleeIndex].label,
        };

        for (int i = 0; i < 3; i++)
        {
            int visualOrder = 2 - i; // 索引0在最下，索引2在最上
            var slotGO   = FindOrMakeChild(panelRect.transform, slotNames[i]);
            var slotRect = GetOrAdd<RectTransform>(slotGO);
            slotRect.anchorMin        = Vector2.zero;
            slotRect.anchorMax        = Vector2.zero;
            slotRect.pivot            = Vector2.zero;
            slotRect.sizeDelta        = new Vector2(slotSize, slotSize);
            slotRect.anchoredPosition = new Vector2(padding,
                padding + visualOrder * (slotSize + slotSpacing));

            var bg = GetOrAdd<Image>(slotGO);
            bg.color = (i == 0) ? colSelected : colNormal;

            // 图标
            var iconGO   = FindOrMakeChild(slotRect.transform, "WeaponIcon");
            var iconRect = GetOrAdd<RectTransform>(iconGO);
            iconRect.anchorMin = new Vector2(0.1f, 0.22f);
            iconRect.anchorMax = new Vector2(0.9f, 0.88f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            var iconImg = GetOrAdd<Image>(iconGO);
            iconImg.enabled = false;

            // 按键提示
            var keyGO   = FindOrMakeChild(slotRect.transform, "KeyHint");
            var keyRect = GetOrAdd<RectTransform>(keyGO);
            keyRect.anchorMin = new Vector2(0f, 0.72f);
            keyRect.anchorMax = new Vector2(0.45f, 1f);
            keyRect.offsetMin = new Vector2(4f, -2f);
            keyRect.offsetMax = new Vector2(0f, -2f);
            var keyTMP = GetOrAdd<TextMeshProUGUI>(keyGO);
            keyTMP.text      = keyHints[i];
            keyTMP.fontSize  = 14;
            keyTMP.fontStyle = FontStyles.Bold;
            keyTMP.color     = Color.white;
            keyTMP.alignment = TextAlignmentOptions.TopLeft;

            // 武器名
            var nameGO   = FindOrMakeChild(slotRect.transform, "WeaponName");
            var nameRect = GetOrAdd<RectTransform>(nameGO);
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.28f);
            nameRect.offsetMin = new Vector2(2f, 2f);
            nameRect.offsetMax = new Vector2(-2f, 0f);
            var nameTMP = GetOrAdd<TextMeshProUGUI>(nameGO);
            nameTMP.text             = weaponLabels[i];
            nameTMP.fontSize         = 11;
            nameTMP.color            = Color.white;
            nameTMP.alignment        = TextAlignmentOptions.Center;
            nameTMP.enableWordWrapping = false;
            nameTMP.overflowMode     = TextOverflowModes.Ellipsis;

            // 填入 HUD slots
            if (hud.slots[i] == null) hud.slots[i] = new WeaponSlotHUD.SlotUI();
            hud.slots[i].root           = slotGO;
            hud.slots[i].background     = bg;
            hud.slots[i].weaponIcon     = iconImg;
            hud.slots[i].weaponNameText = nameTMP;
            hud.slots[i].keyHintText    = keyTMP;

            EditorUtility.SetDirty(slotGO);
        }

        EditorUtility.SetDirty(panelGO);
        EditorUtility.SetDirty(hud);
    }

    // ══════════════════════════════════════════════════
    //  通用工具方法
    // ══════════════════════════════════════════════════

    private static GameObject MakeSquareChild(
        Transform parent, string name, Sprite white, Color color, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = white;
        sr.color  = color;
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    private static GameObject FindOrMakeChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
