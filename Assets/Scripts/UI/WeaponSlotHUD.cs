using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 武器栏 HUD：左下角竖排3个格子
/// 显示主武器1、主武器2、刀，高亮当前选中槽位
///
/// UI 层级结构（在 Canvas 下手动搭建）：
///   WeaponSlotPanel
///     ├── Slot_0  (主武器1)
///     │     ├── Background (Image)
///     │     ├── WeaponIcon (Image)
///     │     ├── SlotLabel  (TMP: "1")
///     │     └── WeaponName (TMP: 武器名)
///     ├── Slot_1  (主武器2)
///     └── Slot_2  (刀)
/// </summary>
public class WeaponSlotHUD : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        [Tooltip("整个格子的根 GameObject")]
        public GameObject root;

        [Tooltip("格子背景 Image，用于高亮")]
        public Image background;

        [Tooltip("武器图标 Image")]
        public Image weaponIcon;

        [Tooltip("武器名称文本")]
        public TextMeshProUGUI weaponNameText;

        [Tooltip("槽位按键提示文本（1/2/3）")]
        public TextMeshProUGUI keyHintText;
    }

    [Header("三个武器槽 UI（从上到下：主武器1、主武器2、刀）")]
    public SlotUI[] slots = new SlotUI[3];

    [Header("高亮颜色")]
    public Color selectedColor   = new Color(1f, 0.85f, 0f, 0.9f);
    public Color normalColor     = new Color(0.15f, 0.15f, 0.15f, 0.75f);
    public Color emptySlotColor  = new Color(0.1f, 0.1f, 0.1f, 0.5f);

    [Header("武器图标（无武器时显示）")]
    public Sprite emptySlotSprite;

    [Header("武器图标（按槽位顺序：主武器1、主武器2、刀）")]
    public Sprite[] weaponIcons = new Sprite[3];

    [Header("直接引用（由配置向导自动赋值，无需手动填）")]
    [Tooltip("直接拖入玩家身上的 WeaponSlotSystem，优先于 Tag 查找")]
    public WeaponSlotSystem slotSystemRef;

    [Header("锁定选中")]
    public Color lockedColor = new Color(0.2f, 0.8f, 1f, 0.9f); // 锁定时蓝色边框

    private WeaponSlotSystem slotSystem;
    private int currentSlotIndex = 0;
    private int lockedSlotIndex  = -1;  // -1 = 无锁定

    /// <summary>当前锁定的武器槽索引（-1=无）</summary>
    public int LockedSlotIndex => lockedSlotIndex;

    void Start()
    {
        // 优先使用直接引用，其次通过 Tag 查找
        if (slotSystemRef != null)
        {
            slotSystem = slotSystemRef;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                slotSystem = player.GetComponent<WeaponSlotSystem>();
        }

        if (slotSystem != null)
            slotSystem.onSlotChanged.AddListener(OnSlotChanged);
        else
            Debug.LogWarning("[WeaponSlotHUD] 找不到 WeaponSlotSystem，请检查玩家 Tag 或直接赋值 slotSystemRef");

        // 初始化按键提示
        string[] keyHints = { "1", "2", "3" };
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (slots[i].keyHintText != null)
                slots[i].keyHintText.text = keyHints[i];
        }

        RefreshAllSlots();
        HighlightSlot(0);
    }

    void Update()
    {
        RefreshAllSlots();
        HandleWeaponSlotClick();
        HandleUnequipInput();
    }

    // ── 背包打开时左键点击武器栏锁定 ──────────────────
    private void HandleWeaponSlotClick()
    {
        if (!IsInventoryOpen()) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // 检查鼠标是否点击了某个武器槽格子
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null) return;

        var pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };
        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);

        int clickedSlot = -1;
        foreach (var result in results)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || slots[i].root == null) continue;
                if (result.gameObject == slots[i].root ||
                    result.gameObject.transform.IsChildOf(slots[i].root.transform))
                {
                    clickedSlot = i;
                    break;
                }
            }
            if (clickedSlot >= 0) break;
        }

        if (clickedSlot >= 0)
        {
            // 再次点击同一格取消锁定
            if (lockedSlotIndex == clickedSlot)
                UnlockSlot();
            else
                LockSlot(clickedSlot);
        }
    }

    // ── G键卸下锁定的武器到背包 ──────────────────────
    private void HandleUnequipInput()
    {
        if (!IsInventoryOpen()) return;
        if (lockedSlotIndex < 0) return;

        KeyCode dropKey = KeyBindings.Instance != null ? KeyBindings.Instance.dropWeapon : KeyCode.G;
        if (!Input.GetKeyDown(dropKey)) return;

        UnequipLockedWeapon();
    }

    /// <summary>锁定指定武器槽</summary>
    public void LockSlot(int index)
    {
        UnlockSlot(); // 先取消旧锁定
        lockedSlotIndex = index;
        // 显示锁定高亮
        if (index >= 0 && index < slots.Length && slots[index].background != null)
            slots[index].background.color = lockedColor;
    }

    /// <summary>取消锁定</summary>
    public void UnlockSlot()
    {
        if (lockedSlotIndex >= 0)
        {
            // 恢复正常颜色（下一帧 RefreshAllSlots 会覆盖，这里先手动恢复）
            lockedSlotIndex = -1;
        }
    }

    /// <summary>卸下锁定的武器到背包</summary>
    private void UnequipLockedWeapon()
    {
        if (slotSystem == null) return;
        var slot = (WeaponSlotSystem.WeaponSlot)lockedSlotIndex;
        var weapon = slotSystem.GetWeaponInSlot(slot);

        if (weapon == null)
        {
            Debug.Log("[WeaponSlotHUD] 该槽位没有武器");
            UnlockSlot();
            return;
        }

        // 放入背包
        var player = slotSystem.gameObject;
        var inventory = player.GetComponent<InventorySystem>();
        if (inventory == null)
        {
            Debug.Log("[WeaponSlotHUD] 找不到背包系统");
            return;
        }

        var weaponItem = new InventoryItem
        {
            itemName  = weapon.weaponName,
            icon      = null,
            quantity  = 1,
            weaponRef = weapon,  // 保存武器引用，丢弃时恢复
        };
        if (!inventory.AddItem(weaponItem))
        {
            Debug.Log("背包已满，无法卸下武器");
            return;
        }

        // 从槽位移除
        weapon.gameObject.SetActive(false);
        slotSystem.SetWeaponInSlot(slot, null);

        // 如果卸下的是当前手持，通知 PlayerController
        if (slot == slotSystem.CurrentSlot)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.EquipWeapon(null);
        }

        Debug.Log($"卸下武器到背包：{weapon.weaponName}");
        UnlockSlot();
    }

    private bool IsInventoryOpen()
    {
        if (slotSystem == null) return false;
        var inv = slotSystem.GetComponent<InventorySystem>();
        return inv != null && inv.IsOpen;
    }

    // ── 槽位切换回调 ──────────────────────────────────
    private void OnSlotChanged(int newSlotIndex)
    {
        currentSlotIndex = newSlotIndex;
        HighlightSlot(newSlotIndex);
    }

    // ── 高亮指定槽位 ──────────────────────────────────
    private void HighlightSlot(int selectedIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].background == null) continue;

            // 锁定的格子保持锁定颜色
            if (i == lockedSlotIndex)
            {
                slots[i].background.color = lockedColor;
                continue;
            }

            bool isSelected = (i == selectedIndex);
            bool hasWeapon  = slotSystem != null &&
                              slotSystem.GetWeaponInSlot((WeaponSlotSystem.WeaponSlot)i) != null;

            if (isSelected)
                slots[i].background.color = selectedColor;
            else if (!hasWeapon)
                slots[i].background.color = emptySlotColor;
            else
                slots[i].background.color = normalColor;
        }
    }

    // ── 刷新所有槽位显示 ──────────────────────────────
    private void RefreshAllSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            WeaponBase weapon = slotSystem != null
                ? slotSystem.GetWeaponInSlot((WeaponSlotSystem.WeaponSlot)i)
                : null;

            // 武器名
            if (slots[i].weaponNameText != null)
            {
                slots[i].weaponNameText.text = weapon != null ? weapon.weaponName : "空";
            }

            // 武器图标
            if (slots[i].weaponIcon != null)
            {
                Sprite icon = (i < weaponIcons.Length) ? weaponIcons[i] : null;
                if (weapon != null && icon != null)
                {
                    slots[i].weaponIcon.sprite  = icon;
                    slots[i].weaponIcon.color   = Color.white;
                    slots[i].weaponIcon.enabled = true;
                }
                else if (emptySlotSprite != null)
                {
                    slots[i].weaponIcon.sprite  = emptySlotSprite;
                    slots[i].weaponIcon.color   = new Color(1f, 1f, 1f, 0.3f);
                    slots[i].weaponIcon.enabled = true;
                }
                else
                {
                    slots[i].weaponIcon.enabled = false;
                }
            }
        }

        // 保持高亮同步
        HighlightSlot(currentSlotIndex);
    }
}
