using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 弹药数据资产一键生成工具
/// 菜单：Tools → 生成弹药数据资产
/// 在 Assets/Data/AmmoData/ 下生成所有弹药的 ScriptableObject
/// 并自动将默认弹药赋值给场景中对应武器
/// </summary>
public static class AmmoDataCreator
{
    private const string AMMO_DIR = "Assets/Data/AmmoData";

    // ── 弹药定义表 ────────────────────────────────────
    private struct AmmoDef
    {
        public string                ammoName;
        public AmmoType              ammoType;
        public bool                  isHighGrade;
        public ArmorPenetrationLevel penetration;
        public float                 damageMultiplier;
        // 对应的武器脚本类型（用于自动赋值）
        public System.Type[]         weaponTypes;
    }

    private static readonly AmmoDef[] Defs = new AmmoDef[]
    {
        // ── 冲锋枪弹 ──────────────────────────────────
        new AmmoDef {
            ammoName         = "冲锋枪弹药（低级）",
            ammoType         = AmmoType.SMG,
            isHighGrade      = false,
            penetration      = ArmorPenetrationLevel.Low,
            damageMultiplier = 1.0f,
            weaponTypes      = new[]{ typeof(SMG), typeof(AutoPistol) },
        },
        new AmmoDef {
            ammoName         = "冲锋枪弹药（高级）",
            ammoType         = AmmoType.SMG,
            isHighGrade      = true,
            penetration      = ArmorPenetrationLevel.Medium,
            damageMultiplier = 1.1f,
            weaponTypes      = new[]{ typeof(SMG), typeof(AutoPistol) },
        },

        // ── 突击步枪弹 ────────────────────────────────
        new AmmoDef {
            ammoName         = "突击步枪弹药（低级）",
            ammoType         = AmmoType.Rifle,
            isHighGrade      = false,
            penetration      = ArmorPenetrationLevel.Medium,
            damageMultiplier = 1.0f,
            weaponTypes      = new[]{ typeof(AssaultRifle) },
        },
        new AmmoDef {
            ammoName         = "突击步枪弹药（高级）",
            ammoType         = AmmoType.Rifle,
            isHighGrade      = true,
            penetration      = ArmorPenetrationLevel.High,
            damageMultiplier = 1.15f,
            weaponTypes      = new[]{ typeof(AssaultRifle) },
        },

        // ── 射手步枪弹 ────────────────────────────────
        new AmmoDef {
            ammoName         = "射手步枪弹药（低级）",
            ammoType         = AmmoType.Rifle,
            isHighGrade      = false,
            penetration      = ArmorPenetrationLevel.High,
            damageMultiplier = 1.0f,
            weaponTypes      = new[]{ typeof(MarksmanRifle) },
        },
        new AmmoDef {
            ammoName         = "射手步枪弹药（高级）",
            ammoType         = AmmoType.Rifle,
            isHighGrade      = true,
            penetration      = ArmorPenetrationLevel.Elite,
            damageMultiplier = 1.2f,
            weaponTypes      = new[]{ typeof(MarksmanRifle) },
        },

        // ── 霰弹 ──────────────────────────────────────
        new AmmoDef {
            ammoName         = "连发霰弹枪弹药（低级）",
            ammoType         = AmmoType.Shotgun,
            isHighGrade      = false,
            penetration      = ArmorPenetrationLevel.Low,
            damageMultiplier = 1.0f,
            weaponTypes      = new[]{ typeof(AutoShotgun) },
        },
        new AmmoDef {
            ammoName         = "连发霰弹枪弹药（高级）",
            ammoType         = AmmoType.Shotgun,
            isHighGrade      = true,
            penetration      = ArmorPenetrationLevel.Medium,
            damageMultiplier = 1.1f,
            weaponTypes      = new[]{ typeof(AutoShotgun) },
        },

        // ── 机枪弹 ────────────────────────────────────
        new AmmoDef {
            ammoName         = "轻机枪弹药（低级）",
            ammoType         = AmmoType.LMG,
            isHighGrade      = false,
            penetration      = ArmorPenetrationLevel.Medium,
            damageMultiplier = 1.0f,
            weaponTypes      = new[]{ typeof(LMG) },
        },
        new AmmoDef {
            ammoName         = "轻机枪弹药（高级）",
            ammoType         = AmmoType.LMG,
            isHighGrade      = true,
            penetration      = ArmorPenetrationLevel.High,
            damageMultiplier = 1.15f,
            weaponTypes      = new[]{ typeof(LMG) },
        },

        // ── 自动手枪弹 ────────────────────────────────
        new AmmoDef {
            ammoName         = "自动手枪弹药（低级）",
            ammoType         = AmmoType.SMG,
            isHighGrade      = false,
            penetration      = ArmorPenetrationLevel.Low,
            damageMultiplier = 1.0f,
            weaponTypes      = new System.Type[0],   // 手枪弹与SMG弹共用，不单独赋值
        },
        new AmmoDef {
            ammoName         = "自动手枪弹药（高级）",
            ammoType         = AmmoType.SMG,
            isHighGrade      = true,
            penetration      = ArmorPenetrationLevel.Medium,
            damageMultiplier = 1.05f,
            weaponTypes      = new System.Type[0],
        },
    };

    // ─────────────────────────────────────────────────
    [MenuItem("Tools/生成弹药数据资产")]
    public static void CreateAll()
    {
        EnsureDir(AMMO_DIR);

        foreach (var def in Defs)
        {
            string safeName = def.ammoName
                .Replace("（", "_").Replace("）", "")
                .Replace(" ", "_");
            string path = $"{AMMO_DIR}/{safeName}.asset";

            // 已存在则跳过（不覆盖，保留手动调整）
            if (AssetDatabase.LoadAssetAtPath<AmmoData>(path) != null)
            {
                Debug.Log($"  已存在，跳过：{path}");
                continue;
            }

            var asset = ScriptableObject.CreateInstance<AmmoData>();
            asset.ammoName         = def.ammoName;
            asset.ammoType         = def.ammoType;
            asset.isHighGrade      = def.isHighGrade;
            asset.penetrationLevel = def.penetration;
            asset.baseDamageMultiplier = def.damageMultiplier;

            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"  ✓ 创建：{path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 自动赋值给场景中的武器（赋低级弹药为默认）
        AutoAssignToSceneWeapons();

        EditorUtility.DisplayDialog("✅ 弹药数据生成完成",
            $"已在 {AMMO_DIR} 下生成所有弹药数据资产。\n\n" +
            "场景中的武器已自动赋值默认（低级）弹药。\n\n" +
            "可在武器 Inspector 的「Current Ammo Data」字段手动切换为高级弹药。",
            "OK");
    }

    // ── 自动赋值给场景中的武器 ────────────────────────
    private static void AutoAssignToSceneWeapons()
    {
        // 找场景中所有 WeaponBase
        var allWeapons = Object.FindObjectsOfType<WeaponBase>(true);
        if (allWeapons.Length == 0) return;

        foreach (var def in Defs)
        {
            if (def.isHighGrade) continue;           // 只赋低级弹药为默认
            if (def.weaponTypes.Length == 0) continue;

            string safeName = def.ammoName
                .Replace("（", "_").Replace("）", "")
                .Replace(" ", "_");
            string path = $"{AMMO_DIR}/{safeName}.asset";
            var ammoAsset = AssetDatabase.LoadAssetAtPath<AmmoData>(path);
            if (ammoAsset == null) continue;

            foreach (var weapon in allWeapons)
            {
                foreach (var wt in def.weaponTypes)
                {
                    if (weapon.GetType() == wt && weapon.currentAmmoData == null)
                    {
                        Undo.RecordObject(weapon, "Auto Assign AmmoData");
                        weapon.currentAmmoData = ammoAsset;
                        EditorUtility.SetDirty(weapon);
                        Debug.Log($"  → {weapon.gameObject.name} 赋值弹药：{def.ammoName}");
                    }
                }
            }
        }
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
