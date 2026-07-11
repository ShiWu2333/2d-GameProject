using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 拾取系统：检测范围内地面物品，F键拾取，通知UI显示
/// 挂在玩家身上
/// </summary>
public class PickupSystem : MonoBehaviour
{
    [Header("拾取参数")]
    [Tooltip("拾取检测半径")]
    public float pickupRadius = 2.5f;

    [Tooltip("地面物品所在层")]
    public LayerMask groundItemLayer;

    [Header("UI引用")]
    [Tooltip("散落物面板（自动查找或手动赋值）")]
    public LootPanelUI lootPanel;

    // 当前范围内的地面物品
    private List<GroundItem> nearbyItems = new List<GroundItem>();
    private InventorySystem inventory;

    void Start()
    {
        inventory = GetComponent<InventorySystem>();

        // 自动查找散落物面板
        if (lootPanel == null)
        {
            var panelGO = GameObject.Find("LootPanel");
            if (panelGO != null)
                lootPanel = panelGO.GetComponent<LootPanelUI>();
        }

        // 确保面板初始隐藏
        if (lootPanel != null)
            lootPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        ScanNearbyItems();
        HandlePickupInput();
        UpdateLootPanel();
    }

    private void ScanNearbyItems()
    {
        nearbyItems.Clear();

        // 如果设置了 groundItemLayer 则按层检测，否则检测所有碰撞体
        Collider2D[] hits;
        if (groundItemLayer.value != 0)
            hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, groundItemLayer);
        else
            hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);

        foreach (var col in hits)
        {
            var gi = col.GetComponent<GroundItem>();
            if (gi != null)
                nearbyItems.Add(gi);
        }
    }

    private void HandlePickupInput()
    {
        KeyCode interactKey = KeyBindings.Instance != null ? KeyBindings.Instance.interact : KeyCode.F;
        if (!Input.GetKeyDown(interactKey)) return;
        if (nearbyItems.Count == 0) return;

        // 背包打开时F键由InventoryUI处理（装备武器）
        var inv = GetComponent<InventorySystem>();
        if (inv != null && inv.IsOpen) return;

        // 如果 PlayerInteraction 有可交互目标，让它优先
        var interaction = GetComponent<PlayerInteraction>();
        if (interaction != null && interaction.CanInteract) return;

        // 拾取选中的物品（如果面板有选中），否则拾取最近的
        int pickIndex = (lootPanel != null) ? lootPanel.SelectedIndex : -1;

        GroundItem target = null;
        if (pickIndex >= 0 && pickIndex < nearbyItems.Count)
            target = nearbyItems[pickIndex];
        else if (nearbyItems.Count > 0)
            target = GetClosestItem();

        if (target == null) return;

        // 根据类型执行不同拾取逻辑
        if (target.itemType == GroundItem.GroundItemType.Weapon)
        {
            PickupWeapon(target);
        }
        else if (target.itemType == GroundItem.GroundItemType.Ammo)
        {
            PickupAmmo(target);
        }
        else
        {
            PickupItem(target);
        }

        nearbyItems.Remove(target);
        if (lootPanel != null) lootPanel.DeselectAll();
    }

    private void PickupItem(GroundItem target)
    {
        if (inventory != null && target.item != null)
        {
            // 确保物品有图标
            if (target.item.icon == null && target.item is AmmoItem ammoItem)
            {
                ammoItem.icon = AmmoIconManager.GetAmmoIcon(ammoItem.ammoType, ammoItem.isHighGrade);
            }

            if (!inventory.AddItem(target.item))
            {
                Debug.Log("背包已满，无法拾取");
                return;
            }
        }
        target.OnPickedUp();
    }

    private void PickupAmmo(GroundItem target)
    {
        if (inventory == null) return;

        // 优先使用GroundItem上的ammoItem
        AmmoItem ammo = target.ammoItem;

        // 如果ammoItem为空，尝试从AmmoItemData组件获取
        if (ammo == null)
        {
            var ammoData = target.GetComponent<AmmoItemData>();
            if (ammoData != null)
                ammo = ammoData.ammoItem;
        }

        // 如果仍为空，尝试从普通item字段获取
        if (ammo == null && target.item is AmmoItem existingAmmo)
        {
            ammo = existingAmmo;
        }

        if (ammo == null)
        {
            Debug.LogWarning("[PickupSystem] 弹药物品数据为空");
            return;
        }

        // 确保图标存在
        if (ammo.icon == null)
            ammo.icon = AmmoIconManager.GetAmmoIcon(ammo.ammoType, ammo.isHighGrade);

        if (!inventory.AddItem(ammo))
        {
            Debug.Log("背包已满，无法拾取弹药");
            return;
        }

        target.OnPickedUp();
    }

    private void PickupWeapon(GroundItem target)
    {
        if (target.weapon == null)
        {
            Debug.Log("[PickupSystem] 武器引用为空");
            return;
        }

        var slotSystem = GetComponent<WeaponSlotSystem>();
        if (slotSystem == null)
        {
            Debug.Log("[PickupSystem] 玩家没有 WeaponSlotSystem");
            return;
        }

        // 智能分配槽位：刀类 → Melee槽，枪类 → 优先空的主武器1 → 空的主武器2 → 放入背包
        WeaponSlotSystem.WeaponSlot assignSlot;

        // 刀类武器自动装备到刀具槽
        if (target.weapon is Knife)
        {
            if (slotSystem.GetWeaponInSlot(WeaponSlotSystem.WeaponSlot.Melee) == null)
                assignSlot = WeaponSlotSystem.WeaponSlot.Melee;
            else
            {
                // 刀具槽已满，放入背包
                PutWeaponInBag(target);
                return;
            }
        }
        else if (slotSystem.GetWeaponInSlot(WeaponSlotSystem.WeaponSlot.Primary1) == null)
            assignSlot = WeaponSlotSystem.WeaponSlot.Primary1;
        else if (slotSystem.GetWeaponInSlot(WeaponSlotSystem.WeaponSlot.Primary2) == null)
            assignSlot = WeaponSlotSystem.WeaponSlot.Primary2;
        else
        {
            PutWeaponInBag(target);
            return;
        }

        // 装备新武器
        var weapon = target.weapon;
        weapon.transform.SetParent(null);
        slotSystem.SetWeaponInSlot(assignSlot, weapon);

        // 挂到 AimPivot 下
        var pc = GetComponent<PlayerController>();
        if (pc != null && pc.aimPivot != null)
        {
            weapon.transform.SetParent(pc.aimPivot);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        // 重新应用图层排序
        var sorter = weapon.GetComponent<WeaponLayerSorter>();
        if (sorter != null)
            sorter.OnWeaponEquipped(pc);

        // 如果不是当前槽位，隐藏
        if (assignSlot != slotSystem.CurrentSlot)
            weapon.gameObject.SetActive(false);

        Debug.Log($"拾取武器：{weapon.weaponName} → 槽位 {assignSlot}");

        // 移除 GroundItem 组件（武器本体保留，不能 Destroy）
        var groundComp2 = weapon.GetComponent<GroundItem>();
        if (groundComp2 != null) Destroy(groundComp2);
    }

    private void PutWeaponInBag(GroundItem target)
    {
        if (inventory != null)
        {
            var weaponItem = new InventoryItem
            {
                itemName  = target.weapon.weaponName,
                icon      = target.GetDisplayIcon(),
                quantity  = 1,
                weaponRef = target.weapon,  // 保存武器引用
            };
            if (!inventory.AddItem(weaponItem))
            {
                Debug.Log("背包已满，无法拾取武器");
                return;
            }
        }
        target.weapon.gameObject.SetActive(false);
        target.weapon.transform.SetParent(null);
        var groundComp = target.weapon.GetComponent<GroundItem>();
        if (groundComp != null) Destroy(groundComp);
        Debug.Log($"武器槽已满，{target.weapon.weaponName} 放入背包");
    }

    private GroundItem GetClosestItem()
    {
        GroundItem closest = null;
        float minDist = float.MaxValue;
        foreach (var gi in nearbyItems)
        {
            float d = Vector2.Distance(transform.position, gi.transform.position);
            if (d < minDist) { minDist = d; closest = gi; }
        }
        return closest;
    }

    private void UpdateLootPanel()
    {
        if (lootPanel == null) return;

        // 背包打开时隐藏散落物面板
        if (inventory != null && inventory.IsOpen)
        {
            lootPanel.Hide();
            return;
        }

        if (nearbyItems.Count > 0)
        {
            lootPanel.Show(nearbyItems);
        }
        else
        {
            lootPanel.Hide();
        }
    }

    public List<GroundItem> GetNearbyItems() => nearbyItems;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
