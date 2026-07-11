using System.Collections.Generic;

/// <summary>
/// 格子锁定管理器
/// 处理武器占多格时的子格锁定逻辑
/// 
/// 规则：
/// - 武器放入时，从武器所在格后面开始锁定(slotCount-1)个格子
/// - 优先锁定最下行最右的格子（即索引大的格子）
/// - 实际实现：武器在index位置，锁定index+1到index+slotCount-1
/// - 被锁定的格子不可放入物品
/// - 武器移走时解锁子格
/// </summary>
public static class SlotLockManager
{
    /// <summary>
    /// 计算物品列表中的锁定状态
    /// 返回每个格子的锁定信息
    /// </summary>
    public static SlotInfo[] CalculateSlotStates(List<InventoryItem> items, int totalSlots)
    {
        var slots = new SlotInfo[totalSlots];
        for (int i = 0; i < totalSlots; i++)
        {
            slots[i] = new SlotInfo();
        }

        // 填入物品
        int slotIndex = 0;
        for (int itemIdx = 0; itemIdx < items.Count && slotIndex < totalSlots; itemIdx++)
        {
            var item = items[itemIdx];
            if (item == null) continue;

            // 主格
            slots[slotIndex].item = item;
            slots[slotIndex].isMainSlot = true;
            slots[slotIndex].weaponName = item.IsWeapon() ? item.itemName : null;

            // 子格（武器占格数-1）
            int extraSlots = item.GetSlotCount() - 1;
            for (int s = 1; s <= extraSlots && slotIndex + s < totalSlots; s++)
            {
                slots[slotIndex + s].isLocked = true;
                slots[slotIndex + s].isSubSlot = true;
                slots[slotIndex + s].weaponName = item.itemName;
                slots[slotIndex + s].mainItem = item;
            }

            // 跳过武器占用的所有格子
            slotIndex += 1 + extraSlots;
        }

        return slots;
    }

    /// <summary>
    /// 检查是否可以在指定位置放入物品
    /// </summary>
    public static bool CanPlaceAt(SlotInfo[] slots, int index, InventoryItem item)
    {
        if (index < 0 || index >= slots.Length) return false;
        if (slots[index].isLocked) return false;

        int needed = item.GetSlotCount();
        // 检查从index开始连续needed个格子是否都可用
        for (int i = 0; i < needed; i++)
        {
            int checkIdx = index + i;
            if (checkIdx >= slots.Length) return false;
            if (i > 0 && (slots[checkIdx].isLocked || slots[checkIdx].item != null))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 获取物品列表占用的总格子数（含子格）
    /// </summary>
    public static int GetTotalSlotsUsed(List<InventoryItem> items)
    {
        int total = 0;
        foreach (var item in items)
        {
            if (item == null) continue;
            total += item.GetSlotCount();
        }
        return total;
    }
}

/// <summary>
/// 单个格子的状态信息
/// </summary>
public class SlotInfo
{
    public InventoryItem item;      // 该格子的物品（主格才有）
    public bool isMainSlot;         // 是否是武器主格
    public bool isSubSlot;          // 是否是武器子格
    public bool isLocked;           // 是否被锁定（不可放入）
    public string weaponName;       // 锁定该格的武器名
    public InventoryItem mainItem;  // 子格对应的主格物品
}
