using UnityEngine;

/// <summary>
/// 医疗物品基类
/// </summary>
[System.Serializable]
public class MedicalItem : InventoryItem
{
    [Tooltip("每次使用恢复的血量")]
    public float healAmount = 40f;

    [Tooltip("使用时间（秒）")]
    public float useTime = 1.5f;

    [Tooltip("是否一次性（使用后销毁）")]
    public bool isSingleUse = true;

    [Tooltip("耐久值（非一次性物品使用）。-1 = 无耐久（一次性）")]
    public float maxDurability = -1f;

    [Tooltip("当前耐久")]
    public float currentDurability;

    [Tooltip("每次使用消耗的耐久")]
    public float durabilityPerUse = 10f;

    [Tooltip("物品描述")]
    public string description = "";

    [Tooltip("物品编号")]
    public int itemID = 0;

    /// <summary>是否还能使用</summary>
    public bool CanUse()
    {
        if (isSingleUse) return quantity > 0;
        return currentDurability > 0f;
    }

    public override void Use(PlayerController player)
    {
        if (!CanUse())
        {
            Debug.Log($"[{itemName}] 无法使用（已耗尽）");
            return;
        }

        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.Heal(healAmount);
        Debug.Log($"使用 {itemName}，恢复 {healAmount} 血量");

        if (isSingleUse)
        {
            quantity--;
        }
        else
        {
            currentDurability -= durabilityPerUse;
            if (currentDurability <= 0f)
            {
                currentDurability = 0f;
                Debug.Log($"[{itemName}] 耐久耗尽");
            }
        }
    }
}

/// <summary>
/// 医疗物品工厂：创建预定义的医疗物品实例
/// </summary>
public static class MedicalItemFactory
{
    /// <summary>
    /// 医疗针：快速恢复中等血量，使用一次后销毁
    /// 编号：1001
    /// </summary>
    public static MedicalItem CreateMedicalNeedle()
    {
        return new MedicalItem
        {
            itemName         = "医疗针",
            itemID           = 1001,
            description      = "快速恢复中等血量，使用一次后销毁",
            healAmount       = 40f,
            useTime          = 1.0f,
            isSingleUse      = true,
            maxDurability    = -1f,
            currentDurability= 0f,
            durabilityPerUse = 0f,
            quantity         = 1,
        };
    }

    /// <summary>
    /// 医疗急救套组：可消耗耐久快速回复血量，耐久较高
    /// 编号：1002
    /// </summary>
    public static MedicalItem CreateMedicalKit()
    {
        return new MedicalItem
        {
            itemName         = "医疗急救套组",
            itemID           = 1002,
            description      = "可以消耗耐久快速回复血量，耐久较高",
            healAmount       = 35f,
            useTime          = 1.2f,
            isSingleUse      = false,
            maxDurability    = 100f,
            currentDurability= 100f,
            durabilityPerUse = 12f,
            quantity         = 1,
        };
    }

    /// <summary>
    /// 医疗包：可消耗耐久恢复血量，耐久中等
    /// 编号：1003
    /// </summary>
    public static MedicalItem CreateMedPack()
    {
        return new MedicalItem
        {
            itemName         = "医疗包",
            itemID           = 1003,
            description      = "可以消耗耐久恢复血量，耐久中等",
            healAmount       = 50f,
            useTime          = 2.0f,
            isSingleUse      = false,
            maxDurability    = 60f,
            currentDurability= 60f,
            durabilityPerUse = 15f,
            quantity         = 1,
        };
    }
}
