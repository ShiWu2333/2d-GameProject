using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 容器物品生成器
/// 容器第一次被打开时根据标签生成物品
/// </summary>
public static class ContainerLootGenerator
{
    /// <summary>
    /// 为容器生成物品（只在第一次打开时调用）
    /// </summary>
    public static void GenerateLoot(LootContainer container)
    {
        if (container.hasGenerated) return;
        container.hasGenerated = true;

        var tags = container.GetTags();

        if (tags.Contains(LootTags.Ammo))
            GenerateAmmo(container);

        if (tags.Contains(LootTags.Weapon))
            GenerateWeapons(container);
    }

    // ── 弹药生成 ──────────────────────────────────────

    private static void GenerateAmmo(LootContainer container)
    {
        // 小容器刷2~4个弹药（4个概率较小）
        int count = RollAmmoCount();

        for (int i = 0; i < count; i++)
        {
            AmmoType type = RollAmmoType();
            int amount = Random.Range(24, 57); // 24~56
            bool isHigh = RollHighGrade(type);

            container.ammoLoot.Add(new AmmoLoot
            {
                ammoType = type,
                amount = amount,
                isHighGrade = isHigh
            });
        }
    }

    /// <summary>小容器弹药数量：2~4，4的概率较小（20%）</summary>
    private static int RollAmmoCount()
    {
        float r = Random.value;
        if (r < 0.4f) return 2;      // 40%
        if (r < 0.8f) return 3;      // 40%
        return 4;                      // 20%
    }

    /// <summary>随机弹药类型</summary>
    private static AmmoType RollAmmoType()
    {
        AmmoType[] types = { AmmoType.SMG, AmmoType.Rifle, AmmoType.Shotgun, AmmoType.LMG, AmmoType.Pistol };
        return types[Random.Range(0, types.Length)];
    }

    /// <summary>穿透等级越高刷出概率线性下降</summary>
    private static bool RollHighGrade(AmmoType type)
    {
        // 高级弹药概率25%
        return Random.value < 0.25f;
    }

    // ── 武器生成 ──────────────────────────────────────

    private static void GenerateWeapons(LootContainer container)
    {
        int count;
        if (container.containerSize == ContainerSize.Medium)
        {
            // 中容器：刷1个武器
            count = 1;
        }
        else
        {
            // 大容器：刷1~2个（2个概率大一点60%）
            count = Random.value < 0.6f ? 2 : 1;
        }

        for (int i = 0; i < count; i++)
        {
            var weaponItem = RollWeapon();
            if (weaponItem != null)
                container.lootItems.Add(weaponItem);
        }
    }

    /// <summary>随机一把武器（实例化已有Prefab）</summary>
    private static InventoryItem RollWeapon()
    {
        // 武器Prefab路径（和PrefabBuilder生成的一致）
        string[] prefabPaths = {
            "Assets/Prefabs/Weapons/SMG.prefab",
            "Assets/Prefabs/Weapons/AssaultRifle.prefab",
            "Assets/Prefabs/Weapons/MarksmanRifle.prefab",
            "Assets/Prefabs/Weapons/AutoShotgun.prefab",
            "Assets/Prefabs/Weapons/LMG.prefab",
            "Assets/Prefabs/Weapons/AutoPistol.prefab"
        };
        string[] weaponNames = { "冲锋枪", "突击步枪", "射手步枪", "连发霰弹枪", "轻机枪", "自动手枪" };
        int[] slotCounts     = {    6,        8,         9,          7,         10,        2       };

        int idx = Random.Range(0, prefabPaths.Length);

        // 尝试加载并实例化Prefab（编辑器和运行时都能用RuntimeLoad方式）
        GameObject weaponGO = null;

        #if UNITY_EDITOR
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[idx]);
        if (prefab != null)
        {
            weaponGO = Object.Instantiate(prefab);
        }
        #endif

        // 运行时回退：直接找场景中同名Prefab实例复制，或手动创建
        if (weaponGO == null)
        {
            // 查找场景中是否已有同类型武器可以复制
            WeaponBase[] allWeapons = Object.FindObjectsOfType<WeaponBase>(true);
            foreach (var w in allWeapons)
            {
                if (w.weaponName == weaponNames[idx])
                {
                    weaponGO = Object.Instantiate(w.gameObject);
                    break;
                }
            }
        }

        if (weaponGO != null)
        {
            weaponGO.SetActive(false);
            weaponGO.name = $"{weaponNames[idx]}_Loot_{Random.Range(1000,9999)}";

            var weapon = weaponGO.GetComponent<WeaponBase>();
            if (weapon != null)
            {
                // Awake可能已在Instantiate时执行，确保数据正确
                var item = new InventoryItem
                {
                    itemName = weapon.weaponName,
                    quantity = 1,
                    slotCount = weapon.weaponSlotCount,
                    tags = new List<string> { LootTags.Weapon },
                    weaponRef = weapon,
                };
                return item;
            }
            Object.Destroy(weaponGO);
        }

        // 最终回退：纯数据（无外观）
        return new InventoryItem
        {
            itemName = weaponNames[idx],
            quantity = 1,
            slotCount = slotCounts[idx],
            tags = new List<string> { LootTags.Weapon },
        };
    }
}
