using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 弹药生成器
/// 配置时在玩家附近生成各种武器对应的弹药地面物品
/// </summary>
public static class AmmoSpawner
{
    private const int AMMO_PER_PACK = 30;
    private const string AMMO_PARENT_NAME = "GroundAmmo";

    /// <summary>
    /// 根据已配置的武器槽位，在玩家附近生成对应弹药
    /// </summary>
    public static void SpawnAmmoForWeapons(GameObject playerGO, int slot1Index, int slot2Index, int meleeIndex)
    {
        if (playerGO == null) return;

        // 清理旧弹药
        CleanOldAmmo();

        // 收集需要的弹药类型（去重）
        var ammoTypes = new HashSet<AmmoType>();
        AddAmmoType(ammoTypes, slot1Index);
        AddAmmoType(ammoTypes, slot2Index);
        AddAmmoType(ammoTypes, meleeIndex);

        if (ammoTypes.Count == 0) return;

        // 创建父节点
        var parent = GameObject.Find(AMMO_PARENT_NAME);
        if (parent == null)
        {
            parent = new GameObject(AMMO_PARENT_NAME);
            Undo.RegisterCreatedObjectUndo(parent, "Create AmmoParent");
        }

        // 在玩家附近生成弹药
        Vector3 playerPos = playerGO.transform.position;
        int index = 0;

        foreach (var ammoType in ammoTypes)
        {
            // 环形分布在玩家周围
            float angle = (360f / ammoTypes.Count) * index * Mathf.Deg2Rad;
            float radius = 2.5f;
            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f);

            CreateAmmoPickup(parent.transform, spawnPos, ammoType);
            index++;
        }

        EditorUtility.SetDirty(parent);
        Debug.Log($"[AmmoSpawner] 已生成 {ammoTypes.Count} 种弹药在玩家附近");
    }

    /// <summary>
    /// 清理场景中旧的弹药地面物品
    /// </summary>
    public static void CleanOldAmmo()
    {
        var parent = GameObject.Find(AMMO_PARENT_NAME);
        if (parent != null)
            Undo.DestroyObjectImmediate(parent);
    }

    private static void AddAmmoType(HashSet<AmmoType> set, int defIndex)
    {
        if (defIndex < 0 || defIndex >= PrefabBuilder.WeaponDefs.Length) return;
        var ammo = PrefabBuilder.WeaponDefs[defIndex].ammo;
        if (ammo != AmmoType.None)
            set.Add(ammo);
    }

    private static void CreateAmmoPickup(Transform parent, Vector3 position, AmmoType ammoType)
    {
        string typeName = GetAmmoDisplayName(ammoType);
        var go = new GameObject($"Ammo_{ammoType}");
        Undo.RegisterCreatedObjectUndo(go, "Spawn Ammo");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        // SpriteRenderer（用颜色方块表示）
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = GetAmmoColor(ammoType);
        // 尝试加载白色Sprite
        var white = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(PrefabBuilder.SPRITE_PATH);
        if (white != null) sr.sprite = white;
        go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        // 碰撞体（拾取检测）
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        // GroundItem组件
        var gi = go.AddComponent<GroundItem>();
        gi.itemType = GroundItem.GroundItemType.Ammo;
        gi.displayName = $"{typeName}弹药 ×{AMMO_PER_PACK}";

        // AmmoItem数据
        gi.ammoItem = new AmmoItem
        {
            itemName    = $"{typeName}弹药（低级）",
            ammoType    = ammoType,
            ammoAmount  = AMMO_PER_PACK,
            quantity    = AMMO_PER_PACK,
            isHighGrade = false,
        };

        EditorUtility.SetDirty(go);
    }

    private static string GetAmmoDisplayName(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:  return "手枪";
            case AmmoType.SMG:     return "冲锋枪";
            case AmmoType.Rifle:   return "步枪";
            case AmmoType.Shotgun: return "霰弹枪";
            case AmmoType.LMG:     return "轻机枪";
            default:               return "未知";
        }
    }

    private static Color GetAmmoColor(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:  return new Color(1f, 0.8f, 0f);
            case AmmoType.SMG:     return new Color(0f, 0.85f, 1f);
            case AmmoType.Rifle:   return new Color(0.2f, 0.9f, 0.3f);
            case AmmoType.Shotgun: return new Color(1f, 0.45f, 0.1f);
            case AmmoType.LMG:     return new Color(0.9f, 0.2f, 0.7f);
            default:               return Color.white;
        }
    }
}
