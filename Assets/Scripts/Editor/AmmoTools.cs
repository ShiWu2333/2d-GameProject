using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 弹药编辑器工具集
/// 菜单：Tools → 弹药工具
/// 包含：弹药数据资产生成 + 弹药图标生成
/// </summary>
public static class AmmoTools
{
    // ══════════════════════════════════════════════════
    //  常量 & 路径
    // ══════════════════════════════════════════════════

    private const string AMMO_DIR = "Assets/Data/AmmoData";
    private const string ICON_DIR = "Assets/Resources/Sprites/Ammo";

    // ══════════════════════════════════════════════════
    //  弹药定义表
    // ══════════════════════════════════════════════════

    private struct AmmoDef
    {
        public string                ammoName;
        public AmmoType              ammoType;
        public bool                  isHighGrade;
        public ArmorPenetrationLevel penetration;
        public float                 damageMultiplier;
        public System.Type[]         weaponTypes;
    }

    private static readonly AmmoDef[] Defs =
    {
        // ── 冲锋枪弹 ──
        new AmmoDef { ammoName="冲锋枪弹药（低级）", ammoType=AmmoType.SMG, isHighGrade=false,
            penetration=ArmorPenetrationLevel.Low, damageMultiplier=1.0f,
            weaponTypes=new[]{ typeof(SMG), typeof(AutoPistol) } },
        new AmmoDef { ammoName="冲锋枪弹药（高级）", ammoType=AmmoType.SMG, isHighGrade=true,
            penetration=ArmorPenetrationLevel.Medium, damageMultiplier=1.1f,
            weaponTypes=new[]{ typeof(SMG), typeof(AutoPistol) } },

        // ── 突击步枪弹 ──
        new AmmoDef { ammoName="突击步枪弹药（低级）", ammoType=AmmoType.Rifle, isHighGrade=false,
            penetration=ArmorPenetrationLevel.Medium, damageMultiplier=1.0f,
            weaponTypes=new[]{ typeof(AssaultRifle) } },
        new AmmoDef { ammoName="突击步枪弹药（高级）", ammoType=AmmoType.Rifle, isHighGrade=true,
            penetration=ArmorPenetrationLevel.High, damageMultiplier=1.15f,
            weaponTypes=new[]{ typeof(AssaultRifle) } },

        // ── 射手步枪弹 ──
        new AmmoDef { ammoName="射手步枪弹药（低级）", ammoType=AmmoType.Rifle, isHighGrade=false,
            penetration=ArmorPenetrationLevel.High, damageMultiplier=1.0f,
            weaponTypes=new[]{ typeof(MarksmanRifle) } },
        new AmmoDef { ammoName="射手步枪弹药（高级）", ammoType=AmmoType.Rifle, isHighGrade=true,
            penetration=ArmorPenetrationLevel.Elite, damageMultiplier=1.2f,
            weaponTypes=new[]{ typeof(MarksmanRifle) } },

        // ── 霰弹 ──
        new AmmoDef { ammoName="连发霰弹枪弹药（低级）", ammoType=AmmoType.Shotgun, isHighGrade=false,
            penetration=ArmorPenetrationLevel.Low, damageMultiplier=1.0f,
            weaponTypes=new[]{ typeof(AutoShotgun) } },
        new AmmoDef { ammoName="连发霰弹枪弹药（高级）", ammoType=AmmoType.Shotgun, isHighGrade=true,
            penetration=ArmorPenetrationLevel.Medium, damageMultiplier=1.1f,
            weaponTypes=new[]{ typeof(AutoShotgun) } },

        // ── 机枪弹 ──
        new AmmoDef { ammoName="轻机枪弹药（低级）", ammoType=AmmoType.LMG, isHighGrade=false,
            penetration=ArmorPenetrationLevel.Medium, damageMultiplier=1.0f,
            weaponTypes=new[]{ typeof(LMG) } },
        new AmmoDef { ammoName="轻机枪弹药（高级）", ammoType=AmmoType.LMG, isHighGrade=true,
            penetration=ArmorPenetrationLevel.High, damageMultiplier=1.15f,
            weaponTypes=new[]{ typeof(LMG) } },

        // ── 自动手枪弹（共用SMG弹） ──
        new AmmoDef { ammoName="自动手枪弹药（低级）", ammoType=AmmoType.SMG, isHighGrade=false,
            penetration=ArmorPenetrationLevel.Low, damageMultiplier=1.0f,
            weaponTypes=new System.Type[0] },
        new AmmoDef { ammoName="自动手枪弹药（高级）", ammoType=AmmoType.SMG, isHighGrade=true,
            penetration=ArmorPenetrationLevel.Medium, damageMultiplier=1.05f,
            weaponTypes=new System.Type[0] },
    };

    // ══════════════════════════════════════════════════
    //  菜单入口
    // ══════════════════════════════════════════════════

    [MenuItem("Tools/弹药工具/生成弹药数据资产")]
    public static void CreateAmmoData()
    {
        EnsureDir(AMMO_DIR);

        int created = 0;
        foreach (var def in Defs)
        {
            string safeName = def.ammoName.Replace("（", "_").Replace("）", "").Replace(" ", "_");
            string path = $"{AMMO_DIR}/{safeName}.asset";

            if (AssetDatabase.LoadAssetAtPath<AmmoData>(path) != null)
                continue;

            var asset = ScriptableObject.CreateInstance<AmmoData>();
            asset.ammoName             = def.ammoName;
            asset.ammoType             = def.ammoType;
            asset.isHighGrade          = def.isHighGrade;
            asset.penetrationLevel     = def.penetration;
            asset.baseDamageMultiplier = def.damageMultiplier;

            AssetDatabase.CreateAsset(asset, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AutoAssignToSceneWeapons();

        EditorUtility.DisplayDialog("弹药数据",
            $"生成完成：新建 {created} 个，路径 {AMMO_DIR}\n场景武器已自动赋值默认弹药。", "OK");
    }

    [MenuItem("Tools/弹药工具/生成弹药图标")]
    public static void GenerateIcons()
    {
        EnsureDir(ICON_DIR);

        GenerateIcon(AmmoType.Pistol,  new Color(1f, 0.8f, 0f));
        GenerateIcon(AmmoType.SMG,     new Color(0f, 0.8f, 1f));
        GenerateIcon(AmmoType.Rifle,   new Color(0f, 1f, 0.4f));
        GenerateIcon(AmmoType.Shotgun, new Color(1f, 0.4f, 0f));
        GenerateIcon(AmmoType.LMG,     new Color(1f, 0f, 0.8f));

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("弹药图标", $"图标已生成到 {ICON_DIR}", "OK");
    }

    [MenuItem("Tools/弹药工具/一键全部生成")]
    public static void CreateAll()
    {
        CreateAmmoData();
        GenerateIcons();
    }

    // ══════════════════════════════════════════════════
    //  弹药数据 - 内部方法
    // ══════════════════════════════════════════════════

    private static void AutoAssignToSceneWeapons()
    {
#if UNITY_2023_1_OR_NEWER
        var allWeapons = Object.FindObjectsByType<WeaponBase>(FindObjectsSortMode.None);
#else
        var allWeapons = Object.FindObjectsOfType<WeaponBase>();
#endif
        if (allWeapons.Length == 0) return;

        foreach (var def in Defs)
        {
            if (def.isHighGrade || def.weaponTypes.Length == 0) continue;

            string safeName = def.ammoName.Replace("（", "_").Replace("）", "").Replace(" ", "_");
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
                    }
                }
            }
        }
    }

    // ══════════════════════════════════════════════════
    //  图标生成 - 内部方法
    // ══════════════════════════════════════════════════

    private static void GenerateIcon(AmmoType type, Color color)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        int cx = size / 2, cy = size / 2;
        int radius = size / 2 - 4;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                if (dist <= radius)
                {
                    float alpha = dist > radius * 0.8f
                        ? 1f - (dist - radius * 0.8f) / (radius * 0.2f)
                        : 1f;
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                else if (dist <= radius + 2)
                {
                    tex.SetPixel(x, y, Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();

        string fileName = $"ammo_{type.ToString().ToLower()}.png";
        string filePath = $"{ICON_DIR}/{fileName}";
        File.WriteAllBytes(filePath, tex.EncodeToPNG());

        AssetDatabase.ImportAsset(filePath);
        var importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }

    // ══════════════════════════════════════════════════
    //  工具
    // ══════════════════════════════════════════════════

    private static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
