using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 容器交互UI
/// 打开容器时显示：左边=背包面板，右边=容器面板，被大框包裹
/// 两个面板结构相同，仅每行格数和数据源不同
/// </summary>
public class ContainerUI : MonoBehaviour
{
    [Header("背包配置")]
    public int invColumns = 9;

    [Header("容器配置")]
    public int containerColumns = 5;
    public int containerRows = 1;

    [Header("通用")]
    public float slotSize = 64f;
    public float slotSpacing = 4f;
    public float padding = 8f;

    [Header("颜色")]
    public Color bgColor      = new Color(0.06f, 0.06f, 0.1f, 0.93f);
    public Color slotColor    = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color filledColor  = new Color(0.3f, 0.3f, 0.3f, 0.9f);
    public Color wrapperColor = new Color(0.03f, 0.03f, 0.05f, 0.95f);

    // 运行时
    private GameObject wrapperGO;
    private GameObject invPanel;
    private GameObject containerPanel;
    private TextMeshProUGUI invTitle;
    private TextMeshProUGUI containerTitle;
    private List<SlotData> invSlots = new List<SlotData>();
    private List<SlotData> containerSlots = new List<SlotData>();

    private LootContainer currentContainer;
    private InventorySystem playerInventory;

    private static ContainerUI _instance;
    public static ContainerUI Instance => _instance;
    public bool IsOpen => wrapperGO != null && wrapperGO.activeSelf;

    private struct SlotData
    {
        public GameObject go;
        public Image bg;
        public Image icon;
        public TextMeshProUGUI count;
    }

    void Awake() { _instance = this; }

    // ══════════════════════════════════════════════════
    //  公开接口
    // ══════════════════════════════════════════════════

    public void Open(LootContainer container, InventorySystem inventory)
    {
        currentContainer = container;
        playerInventory = inventory;
        containerRows = Mathf.Max(1, container.rows);

        // 同步背包格数
        var invUI = inventory != null ? inventory.inventoryUI : null;
        if (invUI != null) invColumns = invUI.columnsPerRow;

        // 每次打开都重建UI（不同容器大小不同）
        if (wrapperGO != null)
            DestroyImmediate(wrapperGO);
        BuildAll();

        containerTitle.text = container.DisplayName;
        wrapperGO.SetActive(true);
        RefreshAll();
    }

    public void Close()
    {
        if (wrapperGO != null)
            wrapperGO.SetActive(false);
        selectedSide = -1;
        selectedIndex = -1;
        currentContainer = null;
    }

    // 选中状态：-1=无，0=背包侧，1=容器侧
    private int selectedSide = -1;
    private int selectedIndex = -1;

    void Update()
    {
        if (!IsOpen) return;

        // Esc关闭
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // F键转移物品
        KeyCode interactKey = KeyBindings.Instance != null ? KeyBindings.Instance.interact : KeyCode.F;
        if (Input.GetKeyDown(interactKey) && selectedSide >= 0 && selectedIndex >= 0)
        {
            TransferSelected();
            return;
        }

        // G键丢弃选中物品到地上
        KeyCode dropKey = KeyBindings.Instance != null ? KeyBindings.Instance.dropWeapon : KeyCode.G;
        if (Input.GetKeyDown(dropKey) && selectedSide >= 0 && selectedIndex >= 0)
        {
            DropSelected();
        }
    }

    /// <summary>G键丢弃选中物品到地面</summary>
    private void DropSelected()
    {
        if (currentContainer == null || playerInventory == null) return;

        InventoryItem itemToDrop = null;

        if (selectedSide == 0)
        {
            // 背包侧
            var items = playerInventory.GetItems();
            var slotStates = SlotLockManager.CalculateSlotStates(items, invSlots.Count);
            if (selectedIndex >= 0 && selectedIndex < slotStates.Length)
            {
                var state = slotStates[selectedIndex];
                if (state.item == null || state.isSubSlot) { DeselectAll(); return; }
                itemToDrop = state.item;
                int itemIdx = items.IndexOf(itemToDrop);
                if (itemIdx >= 0) playerInventory.RemoveItemAt(itemIdx);
            }
        }
        else if (selectedSide == 1)
        {
            // 容器侧
            var containerItems = BuildContainerItemList();
            var slotStates = SlotLockManager.CalculateSlotStates(containerItems, containerSlots.Count);
            if (selectedIndex >= 0 && selectedIndex < slotStates.Length)
            {
                var state = slotStates[selectedIndex];
                if (state.item == null || state.isSubSlot) { DeselectAll(); return; }
                itemToDrop = state.item;
                int containerItemIdx = containerItems.IndexOf(itemToDrop);
                int ammoCount = currentContainer.ammoLoot.Count;
                if (containerItemIdx < ammoCount)
                    currentContainer.ammoLoot.RemoveAt(containerItemIdx);
                else
                {
                    int lootIdx = containerItemIdx - ammoCount;
                    if (lootIdx >= 0 && lootIdx < currentContainer.lootItems.Count)
                        currentContainer.lootItems.RemoveAt(lootIdx);
                }
            }
        }

        if (itemToDrop != null)
            SpawnDroppedItem(itemToDrop);

        DeselectAll();
        RefreshAll();
    }

    private void SpawnDroppedItem(InventoryItem item)
    {
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (player == null) return;

        Vector2 dropDir = UnityEngine.Random.insideUnitCircle.normalized;
        Vector3 dropPos = player.transform.position + (Vector3)(dropDir * 1.5f);

        var go = new GameObject($"Dropped_{item.itemName}");
        go.transform.position = dropPos;

        var sr = go.AddComponent<SpriteRenderer>();
        if (item.icon != null)
        {
            sr.sprite = item.icon;
            sr.color = Color.white;
        }
        else
        {
            sr.color = new Color(0.7f, 0.7f, 0.7f);
        }
        go.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        var gi = go.AddComponent<GroundItem>();
        if (item is AmmoItem ammo)
        {
            gi.itemType = GroundItem.GroundItemType.Ammo;
            gi.ammoItem = ammo;
        }
        else
        {
            gi.itemType = GroundItem.GroundItemType.Item;
            gi.item = item;
        }
        gi.displayName = item.itemName;
    }

    /// <summary>选中背包侧格子</summary>
    public void SelectInvSlot(int index)
    {
        DeselectAll();
        if (index >= 0 && index < invSlots.Count)
        {
            selectedSide = 0;
            selectedIndex = index;
            HighlightSlot(invSlots[index], true);
        }
    }

    /// <summary>选中容器侧格子</summary>
    public void SelectContainerSlot(int index)
    {
        DeselectAll();
        if (index >= 0 && index < containerSlots.Count)
        {
            selectedSide = 1;
            selectedIndex = index;
            HighlightSlot(containerSlots[index], true);
        }
    }

    private void DeselectAll()
    {
        if (selectedSide == 0 && selectedIndex >= 0 && selectedIndex < invSlots.Count)
            HighlightSlot(invSlots[selectedIndex], false);
        if (selectedSide == 1 && selectedIndex >= 0 && selectedIndex < containerSlots.Count)
            HighlightSlot(containerSlots[selectedIndex], false);
        selectedSide = -1;
        selectedIndex = -1;
    }

    private void HighlightSlot(SlotData slot, bool selected)
    {
        slot.bg.color = selected ? new Color(1f, 0.85f, 0f, 0.9f) : slotColor;
    }

    /// <summary>F键转移选中物品</summary>
    private void TransferSelected()
    {
        if (currentContainer == null || playerInventory == null) return;

        if (selectedSide == 0)
        {
            // 背包 → 容器：slot索引转物品索引
            var items = playerInventory.GetItems();
            var slotStates = SlotLockManager.CalculateSlotStates(items, invSlots.Count);

            if (selectedIndex >= 0 && selectedIndex < slotStates.Length)
            {
                var state = slotStates[selectedIndex];
                if (state.item == null || state.isSubSlot) { DeselectAll(); return; }

                var item = state.item;
                int itemIdx = items.IndexOf(item);
                if (itemIdx < 0) { DeselectAll(); return; }

                // 检查容器是否有空间
                var containerItems = BuildContainerItemList();
                int used = SlotLockManager.GetTotalSlotsUsed(containerItems);
                int totalContainerSlots = containerColumns * containerRows;
                if (used + item.GetSlotCount() > totalContainerSlots)
                {
                    DeselectAll();
                    return;
                }

                // 转移
                if (item is AmmoItem ammo)
                {
                    currentContainer.ammoLoot.Add(new AmmoLoot
                    {
                        ammoType = ammo.ammoType,
                        amount = ammo.ammoAmount,
                        isHighGrade = ammo.isHighGrade
                    });
                }
                else
                {
                    currentContainer.lootItems.Add(item);
                }
                playerInventory.RemoveItemAt(itemIdx);
            }
        }
        else if (selectedSide == 1)
        {
            // 容器 → 背包：slot索引转物品索引
            var containerItems = BuildContainerItemList();
            var slotStates = SlotLockManager.CalculateSlotStates(containerItems, containerSlots.Count);

            if (selectedIndex >= 0 && selectedIndex < slotStates.Length)
            {
                var state = slotStates[selectedIndex];
                if (state.item == null || state.isSubSlot) { DeselectAll(); return; }

                var item = state.item;

                // 检查背包是否有空间
                var invItems = playerInventory.GetItems();
                int usedInv = SlotLockManager.GetTotalSlotsUsed(invItems);
                int totalInvSlots = invColumns * 1; // 1行
                if (usedInv + item.GetSlotCount() > totalInvSlots)
                {
                    DeselectAll();
                    return;
                }

                // 找到物品在容器中的真实索引并移除
                int ammoCount = currentContainer.ammoLoot.Count;
                int containerItemIdx = containerItems.IndexOf(item);

                if (containerItemIdx < ammoCount)
                {
                    // 弹药
                    var ammoLoot = currentContainer.ammoLoot[containerItemIdx];
                    var ammoItem = AmmoItemFactory.CreateAmmoItem(ammoLoot.ammoType, ammoLoot.amount, ammoLoot.isHighGrade);
                    playerInventory.AddItem(ammoItem);
                    currentContainer.ammoLoot.RemoveAt(containerItemIdx);
                }
                else
                {
                    // 普通/武器物品
                    int lootIdx = containerItemIdx - ammoCount;
                    if (lootIdx >= 0 && lootIdx < currentContainer.lootItems.Count)
                    {
                        playerInventory.AddItem(currentContainer.lootItems[lootIdx]);
                        currentContainer.lootItems.RemoveAt(lootIdx);
                    }
                }
            }
        }

        DeselectAll();
        RefreshAll();
    }

    // ══════════════════════════════════════════════════
    //  构建UI
    // ══════════════════════════════════════════════════

    private void BuildAll()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Wrapper（大框居中）
        wrapperGO = new GameObject("ContainerWrapper", typeof(RectTransform));
        wrapperGO.transform.SetParent(canvas.transform, false);
        var wRT = wrapperGO.GetComponent<RectTransform>();
        wRT.anchorMin = new Vector2(0.5f, 0.5f);
        wRT.anchorMax = new Vector2(0.5f, 0.5f);
        wRT.pivot = new Vector2(0.5f, 0.5f);
        wRT.anchoredPosition = Vector2.zero;
        wrapperGO.AddComponent<Image>().color = wrapperColor;

        // 计算尺寸
        float invW = CalcPanelWidth(invColumns);
        float conW = CalcPanelWidth(containerColumns);
        float panelH = CalcPanelHeight(containerRows);
        float gap = 6f;
        float totalW = padding + invW + gap + conW + padding;
        float totalH = panelH + padding * 2;
        wRT.sizeDelta = new Vector2(totalW, totalH);

        // 左面板（背包）
        invPanel = BuildPanel(wrapperGO.transform, "InvPanel",
            new Vector2(padding, padding),
            new Vector2(invW, panelH));
        invTitle = BuildTitle(invPanel.transform, "背包");
        invSlots = BuildSlots(invPanel.transform, invColumns, 1);

        // 右面板（容器）
        containerPanel = BuildPanel(wrapperGO.transform, "ContainerPanel",
            new Vector2(padding + invW + gap, padding),
            new Vector2(conW, panelH));
        containerTitle = BuildTitle(containerPanel.transform, "容器");
        containerSlots = BuildSlots(containerPanel.transform, containerColumns, containerRows);

        wrapperGO.SetActive(false);
    }

    private GameObject BuildPanel(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.AddComponent<Image>().color = bgColor;
        return go;
    }

    private TextMeshProUGUI BuildTitle(Transform parent, string text)
    {
        var go = new GameObject("Title", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -4f);
        rt.sizeDelta = new Vector2(0f, 28f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private List<SlotData> BuildSlots(Transform parent, int cols, int rowCount)
    {
        var list = new List<SlotData>();
        float startY = -36f;
        int total = cols * rowCount;

        for (int i = 0; i < total; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = padding + col * (slotSize + slotSpacing);
            float y = startY - row * (slotSize + slotSpacing);

            var slotGO = new GameObject($"Slot_{i}", typeof(RectTransform));
            slotGO.transform.SetParent(parent, false);
            var srt = slotGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(0f, 1f);
            srt.pivot = new Vector2(0f, 1f);
            srt.anchoredPosition = new Vector2(x, y);
            srt.sizeDelta = new Vector2(slotSize, slotSize);

            var bg = slotGO.AddComponent<Image>();
            bg.color = slotColor;
            bg.raycastTarget = true; // 接收点击

            // 预添加Button
            slotGO.AddComponent<Button>();

            // Icon
            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(slotGO.transform, false);
            var irt = iconGO.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.1f, 0.1f);
            irt.anchorMax = new Vector2(0.9f, 0.9f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.enabled = false;
            iconImg.raycastTarget = false; // 不拦截点击

            // Count
            var countGO = new GameObject("Count", typeof(RectTransform));
            countGO.transform.SetParent(slotGO.transform, false);
            var crt = countGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0f);
            crt.anchorMax = new Vector2(1f, 0.35f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var countTMP = countGO.AddComponent<TextMeshProUGUI>();
            countTMP.fontSize = 11;
            countTMP.color = Color.white;
            countTMP.alignment = TextAlignmentOptions.BottomRight;
            countTMP.raycastTarget = false; // 不拦截点击

            list.Add(new SlotData { go = slotGO, bg = bg, icon = iconImg, count = countTMP });
        }
        return list;
    }

    // ══════════════════════════════════════════════════
    //  刷新显示
    // ══════════════════════════════════════════════════

    private void RefreshAll()
    {
        RefreshInvSlots();
        RefreshContainerSlots();
    }

    private void RefreshInvSlots()
    {
        if (playerInventory == null) return;
        var items = playerInventory.GetItems();
        var slotStates = SlotLockManager.CalculateSlotStates(items, invSlots.Count);

        for (int i = 0; i < invSlots.Count; i++)
        {
            var state = slotStates[i];

            if (state.isSubSlot)
            {
                // 子格：显示主格物品 + 灰色滤镜
                SetSlotAsSubSlot(invSlots[i], state.mainItem);
            }
            else if (state.item != null)
            {
                SetSlot(invSlots[i], state.item);
            }
            else
            {
                ClearSlot(invSlots[i]);
            }

            // 绑定点击选中（子格不可选中）
            var btn = invSlots[i].go.GetComponent<Button>();
            if (btn == null) btn = invSlots[i].go.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            if (!state.isLocked)
            {
                int idx = i;
                btn.onClick.AddListener(() => SelectInvSlot(idx));
            }
        }
    }

    private void RefreshContainerSlots()
    {
        if (currentContainer == null) return;

        // 合并弹药和物品为统一列表用于SlotLockManager
        var allItems = BuildContainerItemList();
        var slotStates = SlotLockManager.CalculateSlotStates(allItems, containerSlots.Count);

        for (int i = 0; i < containerSlots.Count; i++)
        {
            var state = slotStates[i];

            if (state.isSubSlot)
            {
                SetSlotAsSubSlot(containerSlots[i], state.mainItem);
            }
            else if (state.item != null)
            {
                SetSlot(containerSlots[i], state.item);
            }
            else
            {
                ClearSlot(containerSlots[i]);
            }

            // 绑定点击选中（子格不可选中）
            var btn = containerSlots[i].go.GetComponent<Button>();
            if (btn == null) btn = containerSlots[i].go.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            if (!state.isLocked)
            {
                int idx = i;
                btn.onClick.AddListener(() => SelectContainerSlot(idx));
            }
        }
    }

    /// <summary>合并容器中的弹药和物品为统一的InventoryItem列表</summary>
    private List<InventoryItem> BuildContainerItemList()
    {
        var list = new List<InventoryItem>();
        if (currentContainer == null) return list;

        foreach (var ammo in currentContainer.ammoLoot)
        {
            list.Add(new AmmoItem
            {
                itemName = $"{AmmoTypeName(ammo.ammoType)}弹药",
                ammoType = ammo.ammoType,
                ammoAmount = ammo.amount,
                quantity = ammo.amount,
                isHighGrade = ammo.isHighGrade,
                icon = AmmoIconManager.GetAmmoIcon(ammo.ammoType, ammo.isHighGrade),
                slotCount = 1,
            });
        }

        foreach (var item in currentContainer.lootItems)
            list.Add(item);

        return list;
    }

    /// <summary>设置格子为子格样式（主格图标 + 30%灰色滤镜）</summary>
    private void SetSlotAsSubSlot(SlotData slot, InventoryItem mainItem)
    {
        // 显示主格物品的图标
        if (mainItem != null && mainItem.icon != null)
        {
            slot.icon.sprite = mainItem.icon;
            slot.icon.color = new Color(1f, 1f, 1f, 0.5f); // 半透明
            slot.icon.enabled = true;
        }
        else
        {
            slot.icon.enabled = false;
        }

        // 背景加灰色滤镜（30%透明度的灰色叠加）
        slot.bg.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        slot.count.text = "";
    }

    private void SetSlot(SlotData slot, InventoryItem item)
    {
        slot.bg.color = filledColor;

        if (item.icon == null && item is AmmoItem ammo)
            item.icon = AmmoIconManager.GetAmmoIcon(ammo.ammoType, ammo.isHighGrade);

        if (item.icon != null)
        {
            slot.icon.sprite = item.icon;
            slot.icon.color = Color.white;
            slot.icon.enabled = true;
        }
        else
        {
            slot.icon.enabled = false;
        }

        slot.count.text = item.quantity > 1 ? item.quantity.ToString() : "";
    }

    private void ClearSlot(SlotData slot)
    {
        slot.bg.color = slotColor;
        slot.icon.enabled = false;
        slot.count.text = "";
    }

    // ══════════════════════════════════════════════════
    //  容器格子点击 → 取出到背包
    // ══════════════════════════════════════════════════

    private void AddClickHandler(GameObject slotGO, int index)
    {
        var btn = slotGO.GetComponent<Button>();
        if (btn == null) btn = slotGO.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        int idx = index;
        btn.onClick.AddListener(() => SelectContainerSlot(idx));
    }

    private void TakeItem(int slotIndex)
    {
        if (currentContainer == null || playerInventory == null) return;

        if (slotIndex < currentContainer.ammoLoot.Count)
        {
            var ammo = currentContainer.ammoLoot[slotIndex];
            var ammoItem = AmmoItemFactory.CreateAmmoItem(ammo.ammoType, ammo.amount, ammo.isHighGrade);
            playerInventory.AddItem(ammoItem);
            currentContainer.ammoLoot.RemoveAt(slotIndex);
        }
        else
        {
            int itemIndex = slotIndex - currentContainer.ammoLoot.Count;
            if (itemIndex < currentContainer.lootItems.Count)
            {
                playerInventory.AddItem(currentContainer.lootItems[itemIndex]);
                currentContainer.lootItems.RemoveAt(itemIndex);
            }
        }

        RefreshAll();
    }

    // ══════════════════════════════════════════════════
    //  工具
    // ══════════════════════════════════════════════════

    private float CalcPanelWidth(int cols)
    {
        return padding + cols * (slotSize + slotSpacing) + padding;
    }

    private float CalcPanelHeight(int rowCount)
    {
        return 36f + rowCount * (slotSize + slotSpacing) + padding;
    }

    private static string AmmoTypeName(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:  return "手枪";
            case AmmoType.SMG:     return "冲锋枪";
            case AmmoType.Rifle:   return "步枪";
            case AmmoType.Shotgun: return "霰弹枪";
            case AmmoType.LMG:     return "轻机枪";
            default:               return "";
        }
    }
}
