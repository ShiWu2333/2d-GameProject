using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包系统：管理物品、武器、弹药
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("背包容量")]
    public int maxSlots = 9;                     // 默认1行9格

    [Header("当前装备")]
    public WeaponBase equippedWeapon;

    [Header("UI 引用")]
    [Tooltip("背包 UI 控制器（自动查找或手动赋值）")]
    public InventoryUI inventoryUI;

    // 物品列表
    private List<InventoryItem> items = new List<InventoryItem>();

    // 状态
    private bool isOpen;

    void Update()
    {
        KeyCode invKey = KeyBindings.Instance != null ? KeyBindings.Instance.inventory : KeyCode.M;
        if (Input.GetKeyDown(invKey))
        {
            ToggleInventory();
        }
    }

    // ── 背包开关 ──────────────────────────────────────
    private void ToggleInventory()
    {
        isOpen = !isOpen;
        Debug.Log($"背包 {(isOpen ? "打开" : "关闭")}");

        // 显示/隐藏 UI
        if (inventoryUI != null)
        {
            inventoryUI.gameObject.SetActive(isOpen);
            if (isOpen) inventoryUI.RefreshDisplay();
        }
    }

    // ── 物品管理 ──────────────────────────────────────
    public bool AddItem(InventoryItem item)
    {
        // 弹药类型：尝试堆叠到已有格子
        if (item is AmmoItem newAmmo)
            return AddAmmoItem(newAmmo);

        if (items.Count >= maxSlots)
        {
            Debug.Log("背包已满");
            return false;
        }

        items.Add(item);
        Debug.Log($"获得物品：{item.itemName}");
        if (inventoryUI != null) inventoryUI.RefreshDisplay();
        return true;
    }

    private bool AddAmmoItem(AmmoItem newAmmo)
    {
        int remaining = newAmmo.ammoAmount;

        // 先尝试堆叠到已有同类型弹药格子
        foreach (var item in items)
        {
            if (remaining <= 0) break;
            if (item is AmmoItem existing && existing.ammoType == newAmmo.ammoType)
            {
                int canAdd = AmmoItem.MaxPerStack - existing.ammoAmount;
                if (canAdd > 0)
                {
                    int add = Mathf.Min(canAdd, remaining);
                    existing.ammoAmount += add;
                    existing.quantity    = existing.ammoAmount; // 同步显示数量
                    remaining -= add;
                }
            }
        }

        // 剩余的开新格
        while (remaining > 0)
        {
            if (items.Count >= maxSlots)
            {
                Debug.Log("背包已满，部分弹药无法放入");
                break;
            }
            int stackAmount = Mathf.Min(remaining, AmmoItem.MaxPerStack);
            var stack = new AmmoItem
            {
                itemName   = newAmmo.itemName,
                icon       = newAmmo.icon,
                ammoType   = newAmmo.ammoType,
                ammoAmount = stackAmount,
                quantity   = stackAmount,
            };
            items.Add(stack);
            remaining -= stackAmount;
        }

        if (inventoryUI != null) inventoryUI.RefreshDisplay();
        Debug.Log($"获得弹药：{newAmmo.itemName} ×{newAmmo.ammoAmount - remaining}");
        return true;
    }

    public bool RemoveItem(InventoryItem item)
    {
        bool removed = items.Remove(item);
        if (removed && inventoryUI != null) inventoryUI.RefreshDisplay();
        return removed;
    }

    /// <summary>按索引移除物品（丢弃用）</summary>
    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= items.Count) return false;
        items.RemoveAt(index);
        if (inventoryUI != null) inventoryUI.RefreshDisplay();
        return true;
    }

    public bool HasItem(string itemName)
    {
        return items.Exists(i => i.itemName == itemName);
    }

    // ── 弹药管理 ──────────────────────────────────────

    /// <summary>查询背包中指定类型弹药的总数量</summary>
    public int GetAmmoCount(AmmoType ammoType)
    {
        int total = 0;
        foreach (var item in items)
        {
            if (item is AmmoItem ammo && ammo.ammoType == ammoType.ToString())
                total += ammo.ammoAmount;
        }
        return total;
    }

    /// <summary>消耗指定类型弹药，返回实际消耗数量</summary>
    public int ConsumeAmmo(AmmoType ammoType, int amount)
    {
        int remaining = amount;
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (items[i] is AmmoItem ammo && ammo.ammoType == ammoType.ToString())
            {
                int take = Mathf.Min(ammo.ammoAmount, remaining);
                ammo.ammoAmount -= take;
                remaining -= take;

                if (ammo.ammoAmount <= 0)
                    items.RemoveAt(i);
            }
        }
        if (inventoryUI != null) inventoryUI.RefreshDisplay();
        return amount - remaining;
    }

    /// <summary>检查是否有足够弹药换弹</summary>
    public bool HasAmmo(AmmoType ammoType)
    {
        return GetAmmoCount(ammoType) > 0;
    }

    // ── 武器装备 ──────────────────────────────────────
    public void EquipWeapon(WeaponBase weapon)
    {
        if (equippedWeapon != null)
        {
            // 卸下当前武器
            equippedWeapon.OnDrop();
        }

        equippedWeapon = weapon;
        if (weapon != null)
        {
            weapon.OnPickup(GetComponent<PlayerController>());
        }
    }

    // ── 公开接口 ──────────────────────────────────────
    public List<InventoryItem> GetItems() => items;
    public bool IsOpen => isOpen;
}

/// <summary>
/// 背包物品基类
/// </summary>
[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public Sprite icon;
    public int quantity = 1;

    /// <summary>如果此物品是武器，存储武器 GameObject 引用（用于丢弃时恢复）</summary>
    [System.NonSerialized]
    public WeaponBase weaponRef;

    public virtual void Use(PlayerController player)
    {
        Debug.Log($"使用物品：{itemName}");
    }
}

/// <summary>
/// 弹药物品
/// </summary>
[System.Serializable]
public class AmmoItem : InventoryItem
{
    public string ammoType;       // 对应武器类型（如 "SMG"、"Rifle"）
    public int ammoAmount = 60;   // 当前弹药数量（每格上限60）

    public const int MaxPerStack = 60;  // 每格最大堆叠数
}
