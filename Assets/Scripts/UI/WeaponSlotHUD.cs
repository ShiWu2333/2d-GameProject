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
    public Color selectedColor   = new Color(1f, 0.85f, 0f, 0.9f);   // 金黄色
    public Color normalColor     = new Color(0.15f, 0.15f, 0.15f, 0.75f); // 深灰半透明
    public Color emptySlotColor  = new Color(0.1f, 0.1f, 0.1f, 0.5f);    // 空槽更暗

    [Header("武器图标（无武器时显示）")]
    public Sprite emptySlotSprite;   // 空槽占位图（可留 null）

    // 武器图标配置（在 Inspector 中手动指定，或由武器自身携带）
    [Header("武器图标（按槽位顺序：主武器1、主武器2、刀）")]
    public Sprite[] weaponIcons = new Sprite[3];

    private WeaponSlotSystem slotSystem;
    private int currentSlotIndex = 0;

    void Start()
    {
        // 找到玩家上的 WeaponSlotSystem
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            slotSystem = player.GetComponent<WeaponSlotSystem>();
            if (slotSystem != null)
                slotSystem.onSlotChanged.AddListener(OnSlotChanged);
        }

        // 初始化按键提示
        string[] keyHints = { "1", "2", "3" };
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (slots[i].keyHintText != null)
                slots[i].keyHintText.text = keyHints[i];
        }

        // 初始刷新
        RefreshAllSlots();
        HighlightSlot(0);
    }

    void Update()
    {
        // 每帧刷新武器名（武器可能在运行时被装备/卸下）
        RefreshAllSlots();
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
