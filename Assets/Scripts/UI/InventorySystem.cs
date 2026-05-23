using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包系统：管理物品、武器、弹药
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("背包容量")]
    public int maxSlots = 20;                    // 最大格子数

    [Header("当前装备")]
    public WeaponBase equippedWeapon;            // 当前手持武器

    // 物品列表
    private List<InventoryItem> items = new List<InventoryItem>();

    // 状态
    private bool isOpen;

    void Update()
    {
        // M键开关背包
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleInventory();
        }
    }

    // ── 背包开关 ──────────────────────────────────────
    private void ToggleInventory()
    {
        isOpen = !isOpen;
        Debug.Log($"背包 {(isOpen ? "打开" : "关闭")}");

        // 这里可以触发UI显示/隐藏
        // InventoryUI.Instance?.SetVisible(isOpen);
    }

    // ── 物品管理 ──────────────────────────────────────
    public bool AddItem(InventoryItem item)
    {
        if (items.Count >= maxSlots)
        {
            Debug.Log("背包已满");
            return false;
        }

        items.Add(item);
        Debug.Log($"获得物品：{item.itemName}");
        return true;
    }

    public bool RemoveItem(InventoryItem item)
    {
        return items.Remove(item);
    }

    public bool HasItem(string itemName)
    {
        return items.Exists(i => i.itemName == itemName);
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
    public string ammoType;  // 对应武器类型
    public int ammoAmount = 30;
}
