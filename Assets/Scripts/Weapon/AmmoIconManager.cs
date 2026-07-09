using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 弹药图标管理器
/// 运行时动态生成并缓存弹药图标Sprite
/// 图标在首次请求时生成，之后从缓存返回
/// </summary>
public static class AmmoIconManager
{
    // ── 颜色定义 ──────────────────────────────────────
    private static readonly Color PistolColor  = new Color(1.0f, 0.80f, 0.0f);   // 金色
    private static readonly Color SMGColor     = new Color(0.0f, 0.85f, 1.0f);   // 青色
    private static readonly Color RifleColor   = new Color(0.2f, 0.90f, 0.3f);   // 绿色
    private static readonly Color ShotgunColor = new Color(1.0f, 0.45f, 0.1f);   // 橙色
    private static readonly Color LMGColor     = new Color(0.9f, 0.20f, 0.7f);   // 粉红色

    private static readonly Color LowBorder    = new Color(0.55f, 0.55f, 0.55f); // 灰色边框
    private static readonly Color HighBorder   = new Color(1.0f, 0.85f, 0.2f);   // 金色边框

    // ── 图标缓存（key = "AmmoType_Grade"） ───────────
    private static Dictionary<string, Sprite> _cache;

    // ══════════════════════════════════════════════════
    //  公开接口
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 获取弹药图标Sprite（运行时安全，缓存保证不重复生成）
    /// </summary>
    public static Sprite GetAmmoIcon(AmmoType ammoType, bool isHighGrade)
    {
        if (ammoType == AmmoType.None) return null;

        EnsureCache();

        string key = MakeKey(ammoType, isHighGrade);

        if (_cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        // 尝试从Resources加载（如果有美术资源优先用）
        Sprite loaded = TryLoadFromResources(ammoType);
        if (loaded != null)
        {
            _cache[key] = loaded;
            return loaded;
        }

        // 动态生成
        Sprite generated = GenerateIcon(ammoType, isHighGrade);
        _cache[key] = generated;
        return generated;
    }

    /// <summary>
    /// 获取弹药基础颜色
    /// </summary>
    public static Color GetAmmoBaseColor(AmmoType ammoType)
    {
        switch (ammoType)
        {
            case AmmoType.Pistol:  return PistolColor;
            case AmmoType.SMG:     return SMGColor;
            case AmmoType.Rifle:   return RifleColor;
            case AmmoType.Shotgun: return ShotgunColor;
            case AmmoType.LMG:     return LMGColor;
            default:               return Color.white;
        }
    }

    /// <summary>
    /// 获取弹药颜色名称
    /// </summary>
    public static string GetAmmoColorName(AmmoType ammoType)
    {
        switch (ammoType)
        {
            case AmmoType.Pistol:  return "金色";
            case AmmoType.SMG:     return "青色";
            case AmmoType.Rifle:   return "绿色";
            case AmmoType.Shotgun: return "橙色";
            case AmmoType.LMG:     return "粉红色";
            default:               return "白色";
        }
    }

    /// <summary>
    /// 为AmmoItem设置图标
    /// </summary>
    public static void SetIconForAmmoItem(AmmoItem ammoItem)
    {
        if (ammoItem == null) return;
        ammoItem.icon = GetAmmoIcon(ammoItem.ammoType, ammoItem.isHighGrade);
    }

    /// <summary>
    /// 预加载所有图标
    /// </summary>
    public static void PreloadAllIcons()
    {
        AmmoType[] types = { AmmoType.Pistol, AmmoType.SMG, AmmoType.Rifle, AmmoType.Shotgun, AmmoType.LMG };
        foreach (var t in types)
        {
            GetAmmoIcon(t, false);
            GetAmmoIcon(t, true);
        }
    }

    /// <summary>
    /// 清理缓存
    /// </summary>
    public static void ClearCache()
    {
        _cache = null;
    }

    // ══════════════════════════════════════════════════
    //  内部实现
    // ══════════════════════════════════════════════════

    private static void EnsureCache()
    {
        if (_cache == null)
            _cache = new Dictionary<string, Sprite>(16);
    }

    private static string MakeKey(AmmoType type, bool high)
    {
        return $"{type}_{(high ? 1 : 0)}";
    }

    private static Sprite TryLoadFromResources(AmmoType ammoType)
    {
        string typeName = ammoType.ToString().ToLower();
        return Resources.Load<Sprite>($"Sprites/Ammo/ammo_{typeName}");
    }

    // ── 图标生成 ──────────────────────────────────────

    private static Sprite GenerateIcon(AmmoType ammoType, bool isHighGrade)
    {
        const int size = 64;

        // 创建Texture2D（RGBA32确保透明通道正确）
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color baseColor = GetAmmoBaseColor(ammoType);
        Color border = isHighGrade ? HighBorder : LowBorder;

        Color[] pixels = new Color[size * size];

        int cx = size / 2;
        int cy = size / 2;
        float outerR = size / 2f - 1f;
        float innerR = outerR - 3f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color pixel = Color.clear;

                if (dist <= innerR)
                {
                    // 内部填充
                    pixel = baseColor;

                    // 高级弹药中心绘制菱形标记
                    if (isHighGrade)
                    {
                        float absDx = Mathf.Abs(dx);
                        float absDy = Mathf.Abs(dy);
                        if (absDx + absDy < innerR * 0.45f && absDx + absDy > innerR * 0.25f)
                        {
                            pixel = Color.white;
                        }
                    }
                    else
                    {
                        // 低级弹药中心绘制圆点
                        if (dist < innerR * 0.25f)
                        {
                            pixel = Color.Lerp(baseColor, Color.white, 0.6f);
                        }
                    }
                }
                else if (dist <= outerR)
                {
                    // 边框
                    pixel = border;
                }

                pixels[y * size + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false); // updateMipmaps=false, makeNoLongerReadable=false

        // 创建Sprite（pivot在中心，pixelsPerUnit=100）
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
        sprite.name = $"Ammo_{ammoType}_{(isHighGrade ? "High" : "Low")}";

        return sprite;
    }
}
