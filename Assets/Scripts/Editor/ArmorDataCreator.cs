using UnityEngine;
using UnityEditor;

/// <summary>
/// 护甲数据 + 修维包数据一键生成工具
/// 菜单：Tools → 生成护甲数据资产
/// 在 Assets/Data/ArmorData/ 和 Assets/Data/RepairKits/ 下生成所有资产
/// 并自动将 ArmorComponent 挂载到场景玩家身上
/// </summary>
public static class ArmorDataCreator
{
    private const string ARMOR_DIR  = "Assets/Data/ArmorData";
    private const string REPAIR_DIR = "Assets/Data/RepairKits";

    // ══════════════════════════════════════════════════
    //  护甲定义表
    // ══════════════════════════════════════════════════
    private struct ArmorDef
    {
        public string        name;
        public ArmorType     type;
        public ArmorClass    armorClass;
        public float         maxDurability;
        public float         maxAbsorption;
        public float         walkPenalty;    // 步行速度倍率（1=无惩罚）
        public float         sprintPenalty;  // 奔跑速度倍率
        public int           maxRepairs;     // -1=无限
        public float         repairRatio;    // 每次修复恢复比例
        public RepairKitTier minKitTier;
    }

    private static readonly ArmorDef[] ArmorDefs =
    {
        // 低级护甲（轻型）：低防护，耐久低，负面低，可修少
        new ArmorDef {
            name="低级护甲（轻型）", type=ArmorType.LightArmor_Light,
            armorClass=ArmorClass.Light,
            maxDurability=60f,  maxAbsorption=0.45f,
            walkPenalty=0.97f,  sprintPenalty=0.97f,
            maxRepairs=2,       repairRatio=0.5f,
            minKitTier=RepairKitTier.Basic },

        // 低级护甲（重型）：低防护，耐久中，负面中，可修中
        new ArmorDef {
            name="低级护甲（重型）", type=ArmorType.LightArmor_Heavy,
            armorClass=ArmorClass.Light,
            maxDurability=100f, maxAbsorption=0.50f,
            walkPenalty=0.92f,  sprintPenalty=0.90f,
            maxRepairs=4,       repairRatio=0.5f,
            minKitTier=RepairKitTier.Basic },

        // 中级护甲（轻型）：中防护，耐久较低，负面较低，可修较少
        new ArmorDef {
            name="中级护甲（轻型）", type=ArmorType.MediumArmor_Light,
            armorClass=ArmorClass.Medium,
            maxDurability=80f,  maxAbsorption=0.60f,
            walkPenalty=0.94f,  sprintPenalty=0.92f,
            maxRepairs=3,       repairRatio=0.5f,
            minKitTier=RepairKitTier.Advanced },

        // 中级护甲（重型）：中防护，耐久较高，负面较高，可修较多
        new ArmorDef {
            name="中级护甲（重型）", type=ArmorType.MediumArmor_Heavy,
            armorClass=ArmorClass.Medium,
            maxDurability=130f, maxAbsorption=0.65f,
            walkPenalty=0.87f,  sprintPenalty=0.83f,
            maxRepairs=6,       repairRatio=0.5f,
            minKitTier=RepairKitTier.Advanced },

        // 高级护甲（轻型）：高防护，耐久中，负面中，可修中
        new ArmorDef {
            name="高级护甲（轻型）", type=ArmorType.HeavyArmor_Light,
            armorClass=ArmorClass.Heavy,
            maxDurability=110f, maxAbsorption=0.72f,
            walkPenalty=0.88f,  sprintPenalty=0.85f,
            maxRepairs=4,       repairRatio=0.5f,
            minKitTier=RepairKitTier.Elite },

        // 高级护甲（重型）：高防护，耐久高，负面高，可修多
        new ArmorDef {
            name="高级护甲（重型）", type=ArmorType.HeavyArmor_Heavy,
            armorClass=ArmorClass.Heavy,
            maxDurability=180f, maxAbsorption=0.78f,
            walkPenalty=0.78f,  sprintPenalty=0.72f,
            maxRepairs=8,       repairRatio=0.5f,
            minKitTier=RepairKitTier.Elite },

        // 复合重装：特级防护，耐久极高，负面极高，可修无限
        new ArmorDef {
            name="复合重装", type=ArmorType.CompoundHeavy,
            armorClass=ArmorClass.Elite,
            maxDurability=280f, maxAbsorption=0.88f,
            walkPenalty=0.62f,  sprintPenalty=0.55f,
            maxRepairs=-1,      repairRatio=0.4f,
            minKitTier=RepairKitTier.Special },

        // 定制轻装：特级防护，耐久较高，负面极低，可修无限
        new ArmorDef {
            name="定制轻装", type=ArmorType.CustomLight,
            armorClass=ArmorClass.Elite,
            maxDurability=150f, maxAbsorption=0.82f,
            walkPenalty=0.98f,  sprintPenalty=0.97f,
            maxRepairs=-1,      repairRatio=0.4f,
            minKitTier=RepairKitTier.Special },
    };

    // ══════════════════════════════════════════════════
    //  修维包定义表
    // ══════════════════════════════════════════════════
    private struct RepairDef
    {
        public string        name;
        public RepairKitTier tier;
        public ArmorClass    maxArmorClass;
        public float         useTime;
    }

    private static readonly RepairDef[] RepairDefs =
    {
        new RepairDef { name="基础修维包",   tier=RepairKitTier.Basic,
            maxArmorClass=ArmorClass.Light,  useTime=3f },
        new RepairDef { name="高级修维包",   tier=RepairKitTier.Advanced,
            maxArmorClass=ArmorClass.Medium, useTime=4f },
        new RepairDef { name="精英修维包",   tier=RepairKitTier.Elite,
            maxArmorClass=ArmorClass.Heavy,  useTime=5f },
        // 特种修维套件：不可用于任何护甲（CanRepair 始终返回 false）
        new RepairDef { name="特种修维套件", tier=RepairKitTier.Special,
            maxArmorClass=ArmorClass.None,   useTime=0f },
    };

    // ══════════════════════════════════════════════════
    [MenuItem("Tools/生成护甲数据资产")]
    public static void CreateAll()
    {
        EnsureDir(ARMOR_DIR);
        EnsureDir(REPAIR_DIR);

        // 生成护甲资产
        var armorAssets = new ArmorData[ArmorDefs.Length];
        for (int i = 0; i < ArmorDefs.Length; i++)
            armorAssets[i] = CreateArmorAsset(ArmorDefs[i]);

        // 生成修维包资产
        for (int i = 0; i < RepairDefs.Length; i++)
            CreateRepairKitAsset(RepairDefs[i]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 自动配置场景玩家
        AutoSetupPlayer(armorAssets);

        EditorUtility.DisplayDialog("✅ 护甲数据生成完成",
            $"护甲资产（{ArmorDefs.Length}个）→ {ARMOR_DIR}\n" +
            $"修维包资产（{RepairDefs.Length}个）→ {REPAIR_DIR}\n\n" +
            "场景玩家已自动挂载 ArmorComponent。\n" +
            "可在 Inspector 的「Equipped Armor」字段选择护甲。", "OK");
    }

    // ── 创建护甲资产 ──────────────────────────────────
    private static ArmorData CreateArmorAsset(ArmorDef def)
    {
        string safeName = def.name.Replace("（", "_").Replace("）", "");
        string path     = $"{ARMOR_DIR}/{safeName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<ArmorData>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<ArmorData>();
        asset.armorName         = def.name;
        asset.armorType         = def.type;
        asset.armorClass        = def.armorClass;
        asset.maxDurability     = def.maxDurability;
        asset.maxAbsorption     = def.maxAbsorption;
        asset.walkSpeedPenalty  = def.walkPenalty;
        asset.sprintSpeedPenalty= def.sprintPenalty;
        asset.maxRepairCount    = def.maxRepairs;
        asset.repairRestoreRatio= def.repairRatio;
        asset.minRepairKitTier  = def.minKitTier;

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"  ✓ 护甲：{path}");
        return asset;
    }

    // ── 创建修维包资产 ────────────────────────────────
    private static void CreateRepairKitAsset(RepairDef def)
    {
        string path = $"{REPAIR_DIR}/{def.name}.asset";
        if (AssetDatabase.LoadAssetAtPath<RepairKit>(path) != null) return;

        var asset = ScriptableObject.CreateInstance<RepairKit>();
        asset.kitName      = def.name;
        asset.tier         = def.tier;
        asset.maxArmorClass= def.maxArmorClass;
        asset.useTime      = def.useTime;

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"  ✓ 修维包：{path}");
    }

    // ── 自动配置场景玩家 ──────────────────────────────
    private static void AutoSetupPlayer(ArmorData[] armorAssets)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // 找不到Tag时尝试按名字查找
#if UNITY_2023_1_OR_NEWER
            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
            var allGOs = Object.FindObjectsOfType<GameObject>();
#endif
            foreach (var go in allGOs)
            {
                if (go.name.ToLower().Contains("player") && go.GetComponent<PlayerController>() != null)
                {
                    player = go;
                    break;
                }
            }
        }
        if (player == null)
        {
            Debug.LogWarning("[ArmorDataCreator] 场景中未找到玩家对象，请手动挂载 ArmorComponent");
            return;
        }

        var comp = player.GetComponent<ArmorComponent>();
        if (comp == null)
        {
            comp = Undo.AddComponent<ArmorComponent>(player);
            Debug.Log("  ✓ 已为玩家添加 ArmorComponent");
        }

        EditorUtility.SetDirty(player);
        Debug.Log($"  ✓ 玩家 ArmorComponent 配置完成（默认无护甲，可在 Inspector 中选择）");
    }

    private static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
