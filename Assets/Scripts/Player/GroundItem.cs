using UnityEngine;

/// <summary>
/// 地面物品/武器组件
/// 挂在场景中可拾取的物品 GameObject 上
/// 支持普通物品、武器和弹药三种类型
/// </summary>
public class GroundItem : MonoBehaviour
{
    public enum GroundItemType
    {
        Item,    // 普通物品（放入背包）
        Weapon,  // 武器（装备到武器槽）
        Ammo,    // 弹药（放入背包的弹药栏）
    }

    [Header("类型")]
    public GroundItemType itemType = GroundItemType.Item;

    [Header("弹药数据（itemType = Ammo 时使用）")]
    [Tooltip("弹药物品数据")]
    public AmmoItem ammoItem;

    [Header("普通物品数据（itemType = Item 时使用）")]
    public InventoryItem item;

    [Header("武器数据（itemType = Weapon 时使用）")]
    [Tooltip("武器组件引用（通常就是自身或子物体上的 WeaponBase）")]
    public WeaponBase weapon;

    [Tooltip("拾取后装备到哪个槽位")]
    public WeaponSlotSystem.WeaponSlot targetSlot = WeaponSlotSystem.WeaponSlot.Primary1;

    [Header("显示")]
    [Tooltip("散落物面板中显示的图标（正方形，可修改）")]
    public Sprite displayIcon;

    [Tooltip("散落物面板中显示的名称")]
    public string displayName;

    /// <summary>获取显示名称</summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        
        switch (itemType)
        {
            case GroundItemType.Weapon:
                if (weapon != null) return weapon.weaponName;
                break;
            case GroundItemType.Item:
                if (item != null) return item.itemName;
                break;
            case GroundItemType.Ammo:
                if (ammoItem != null) return ammoItem.GetDisplayName();
                break;
        }
        
        return "???";
    }

    /// <summary>获取显示图标</summary>
    public Sprite GetDisplayIcon()
    {
        if (displayIcon != null) return displayIcon;
        
        switch (itemType)
        {
            case GroundItemType.Item:
                if (item != null) return item.icon;
                break;
            case GroundItemType.Ammo:
                // 可以为弹药设置默认图标
                break;
        }
        
        return null;
    }

    /// <summary>被拾取时调用</summary>
    public void OnPickedUp()
    {
        Destroy(gameObject);
    }
}
