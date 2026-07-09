using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Prefab 生成器
/// 负责：白色正方形Sprite、子弹Prefab、武器Prefab 的创建
/// </summary>
public static class PrefabBuilder
{
    public const string WEAPON_DIR  = "Assets/Prefabs/Weapons";
    public const string BULLET_DIR  = "Assets/Prefabs/Bullets";
    public const string SPRITE_PATH = "Assets/Prefabs/WhiteSquare.png";

    // ── 武器定义 ──────────────────────────────────────
    public struct WeaponDef
    {
        public string      label;
        public System.Type script;
        public Color       bodyCol, barrelCol;
        public Vector2     bodySize, barrelSize;
        public Vector2     barrelOffset, firePointOffset;
        public AmmoType    ammo;
    }

    public static readonly WeaponDef[] WeaponDefs =
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

    // ── 子弹定义 ──────────────────────────────────────
    private struct BulletDef
    {
        public string name; public AmmoType ammo; public Color color; public Vector2 size;
    }

    private static readonly BulletDef[] BulletDefs =
    {
        new BulletDef { name="Bullet_SMG",     ammo=AmmoType.SMG,     color=new Color(0f,0.9f,0.9f),   size=new Vector2(0.06f,0.12f) },
        new BulletDef { name="Bullet_Rifle",   ammo=AmmoType.Rifle,   color=new Color(0.3f,0.5f,1f),   size=new Vector2(0.06f,0.14f) },
        new BulletDef { name="Bullet_Shotgun", ammo=AmmoType.Shotgun, color=new Color(1f,0.6f,0.1f),   size=new Vector2(0.08f,0.08f) },
        new BulletDef { name="Bullet_LMG",     ammo=AmmoType.LMG,     color=new Color(1f,0.95f,0.1f),  size=new Vector2(0.07f,0.15f) },
    };

    // ══════════════════════════════════════════════════
    //  公开接口
    // ══════════════════════════════════════════════════

    public static Sprite EnsureWhiteSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        if (existing != null) return existing;

        EditorHelper.EnsureDir("Assets/Prefabs");

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels); tex.Apply();

        string absPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SPRITE_PATH));
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

    public static Dictionary<AmmoType, GameObject> BuildBulletPrefabs(Sprite white)
    {
        EditorHelper.EnsureDir(BULLET_DIR);
        var map = new Dictionary<AmmoType, GameObject>();

        foreach (var def in BulletDefs)
        {
            string path = $"{BULLET_DIR}/{def.name}.prefab";

            // 如果已存在则复用（清理模式下不会走到这里）
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { map[def.ammo] = existing; continue; }

            var root = new GameObject(def.name);
            var b = root.AddComponent<Bullet>();
            b.damage = 10f; b.maxRange = 20f;

            EditorHelper.MakeSquareChild(root.transform, "Visual", white, def.color, def.size);

            var rb2 = root.GetComponent<Rigidbody2D>();
            rb2.gravityScale = 0f;
            rb2.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = root.AddComponent<CircleCollider2D>();
            col.radius = Mathf.Max(def.size.x, def.size.y) * 0.5f;
            col.isTrigger = true;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            map[def.ammo] = prefab;
        }
        return map;
    }

    public static Dictionary<string, GameObject> BuildWeaponPrefabs(
        Sprite white, Dictionary<AmmoType, GameObject> bulletMap)
    {
        EditorHelper.EnsureDir(WEAPON_DIR);
        var map = new Dictionary<string, GameObject>();

        foreach (var def in WeaponDefs)
        {
            string path = $"{WEAPON_DIR}/{def.script.Name}.prefab";

            // 如果已存在则复用（清理模式下不会走到这里）
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) { map[def.label] = existing; continue; }

            var root = new GameObject(def.script.Name);
            var weapon = (WeaponBase)root.AddComponent(def.script);

            EditorHelper.MakeSquareChild(root.transform, "Body", white, def.bodyCol, def.bodySize);
            var barrel = EditorHelper.MakeSquareChild(root.transform, "Barrel", white, def.barrelCol, def.barrelSize);
            barrel.transform.localPosition = def.barrelOffset;

            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(root.transform, false);
            fp.transform.localPosition = def.firePointOffset;
            weapon.firePoint = fp.transform;

            if (bulletMap.TryGetValue(def.ammo, out var bp))
                weapon.bulletPrefab = bp;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = def.bodySize + new Vector2(0.06f, 0.06f);
            col.isTrigger = true;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            map[def.label] = prefab;
        }
        return map;
    }
}
