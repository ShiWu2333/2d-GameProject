using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 背包 UI 控制器
/// 每行9格，默认1行，可扩展行数，超出可滚轮滚动
/// 挂在背包面板根节点上
///
/// UI 层级（由 Editor 工具自动生成）：
///   InventoryPanel (this)
///     ├── Title (TMP)
///     ├── ScrollView
///     │     └── Viewport
///     │           └── Content (GridLayoutGroup)
///     │                 ├── Slot_0
///     │                 ├── Slot_1 ...
///     └── CloseBtn
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("每行格子数")]
    public int columnsPerRow = 9;

    [Tooltip("当前行数（可运行时动态增加）")]
    public int currentRows = 1;

    [Tooltip("单个格子尺寸")]
    public float slotSize = 64f;

    [Tooltip("格子间距")]
    public float slotSpacing = 4f;

    [Header("引用（由配置工具自动赋值）")]
    public RectTransform contentRoot;   // GridLayout 所在的 Content
    public ScrollRect    scrollRect;
    public GameObject    slotPrefab;    // 格子预制体

    [Header("颜色")]
    public Color emptySlotColor  = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color filledSlotColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

    [Header("详细信息面板")]
    [Tooltip("物品/武器详细面板（在背包右侧）")]
    public ItemDetailPanel detailPanel;

    // 运行时
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private InventorySystem inventorySystem;
    private int selectedSlotIndex = -1;  // -1 = 无选中

    public int TotalSlots => columnsPerRow * currentRows;
    public int SelectedSlotIndex => selectedSlotIndex;

    void Start()
    {
        // 找到玩家背包
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            inventorySystem = player.GetComponent<InventorySystem>();

        RebuildGrid();
    }

    void OnEnable()
    {
        // 自动查找详细面板
        if (detailPanel == null)
        {
            var panelGO = GameObject.Find("ItemDetailPanel");
            if (panelGO != null)
                detailPanel = panelGO.GetComponent<ItemDetailPanel>();
        }

        RefreshDisplay();
        // 背包打开时默认显示当前武器信息
        ShowCurrentWeaponDetail();
    }

    void OnDisable()
    {
        // 背包关闭时取消选中并隐藏详细面板
        DeselectAll();
    }

    void Update()
    {
        var kb = KeyBindings.Instance;
        KeyCode dropKey    = kb != null ? kb.dropWeapon : KeyCode.G;
        KeyCode interactKey = kb != null ? kb.interact  : KeyCode.F;

        // G键丢弃选中物品（背包打开时，且武器栏没有锁定）
        if (Input.GetKeyDown(dropKey) && selectedSlotIndex >= 0)
        {
            var weaponHUD = FindObjectOfType<WeaponSlotHUD>();
            if (weaponHUD != null && weaponHUD.LockedSlotIndex >= 0)
                return;

            DropSelectedItem();
        }

        // F键装备选中的武器（背包打开时）
        if (Input.GetKeyDown(interactKey) && selectedSlotIndex >= 0)
        {
            EquipSelectedWeapon();
        }

        // 鼠标右键使用选中物品（医疗品等）
        if (Input.GetMouseButtonDown(1) && selectedSlotIndex >= 0)
        {
            UseSelectedItem();
        }
    }

    /// <summary>选中指定槽位（左键点击触发）</summary>
    public void SelectSlot(int index)
    {
        // 再次点击同一格取消选中
        if (selectedSlotIndex == index)
        {
            DeselectAll();
            return;
        }

        DeselectAll();
        selectedSlotIndex = index;

        if (index >= 0 && index < slotUIs.Count)
            slotUIs[index].SetSelected(true);

        // 展开详细面板
        ShowDetailForSlot(index);
    }

    /// <summary>取消所有选中</summary>
    public void DeselectAll()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIs.Count)
            slotUIs[selectedSlotIndex].SetSelected(false);
        selectedSlotIndex = -1;

        if (detailPanel != null) detailPanel.Hide();
    }

    /// <summary>根据选中槽位展开详细信息</summary>
    private void ShowDetailForSlot(int index)
    {
        if (detailPanel == null || inventorySystem == null) return;
        var items = inventorySystem.GetItems();
        var slotStates = SlotLockManager.CalculateSlotStates(items, TotalSlots);

        // 空格或子格：隐藏详细面板
        if (index < 0 || index >= slotStates.Length || slotStates[index].item == null || slotStates[index].isSubSlot)
        {
            detailPanel.Hide();
            return;
        }

        var item = slotStates[index].item;

        // 检查是否是武器物品（通过名称匹配场景中隐藏的武器）
        var slotSystem = inventorySystem.GetComponent<WeaponSlotSystem>();
        if (slotSystem != null)
        {
            WeaponBase matchedWeapon = FindWeaponByName(slotSystem, item.itemName);
            if (matchedWeapon != null)
            {
                detailPanel.ShowWeapon(matchedWeapon);
                return;
            }
        }

        // 也检查weaponRef引用
        if (item.weaponRef != null)
        {
            detailPanel.ShowWeapon(item.weaponRef);
            return;
        }

        // 普通物品/弹药
        detailPanel.ShowItem(item);
    }

    /// <summary>显示当前手持武器的详细信息</summary>
    private void ShowCurrentWeaponDetail()
    {
        if (detailPanel == null) return;

        var slotSystem = inventorySystem != null
            ? inventorySystem.GetComponent<WeaponSlotSystem>()
            : null;

        if (slotSystem != null && slotSystem.CurrentWeapon != null)
        {
            detailPanel.ShowWeapon(slotSystem.CurrentWeapon);
        }
        else
        {
            detailPanel.Hide();
        }
    }

    private WeaponBase FindWeaponByName(WeaponSlotSystem slotSystem, string weaponName)
    {
        for (int i = 0; i <= 2; i++)
        {
            var w = slotSystem.GetWeaponInSlot((WeaponSlotSystem.WeaponSlot)i);
            if (w != null && w.weaponName == weaponName) return w;
        }
        return null;
    }

    /// <summary>丢弃当前选中的物品</summary>
    private void DropSelectedItem()
    {
        if (inventorySystem == null) return;
        var items = inventorySystem.GetItems();

        if (selectedSlotIndex < 0 || selectedSlotIndex >= items.Count)
        {
            Debug.Log("[InventoryUI] 选中的格子没有物品");
            DeselectAll();
            return;
        }

        var item = items[selectedSlotIndex];
        inventorySystem.RemoveItemAt(selectedSlotIndex);

        // 在玩家前方生成地面物品
        SpawnDroppedItem(item);

        Debug.Log($"丢弃物品：{item.itemName}");
        DeselectAll();
        RefreshDisplay();
    }

    /// <summary>装备选中的武器到武器槽</summary>
    private void EquipSelectedWeapon()
    {
        if (inventorySystem == null) return;
        var items = inventorySystem.GetItems();

        if (selectedSlotIndex < 0 || selectedSlotIndex >= items.Count)
            return;

        var item = items[selectedSlotIndex];

        // 检查是否有武器引用
        if (item.weaponRef == null)
        {
            Debug.Log("[InventoryUI] 该物品不是武器，无法装备");
            return;
        }

        var weapon = item.weaponRef;
        var slotSystem = inventorySystem.GetComponent<WeaponSlotSystem>();
        if (slotSystem == null) return;

        // 智能分配槽位：刀 → Melee，枪 → Primary1 → Primary2
        WeaponSlotSystem.WeaponSlot assignSlot;
        if (weapon is Knife)
        {
            assignSlot = WeaponSlotSystem.WeaponSlot.Melee;
            // 如果刀具槽已有武器，先卸下旧的到背包
            var existing = slotSystem.GetWeaponInSlot(assignSlot);
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                slotSystem.SetWeaponInSlot(assignSlot, null);
                var oldItem = new InventoryItem
                {
                    itemName  = existing.weaponName,
                    quantity  = 1,
                    weaponRef = existing,
                };
                inventorySystem.AddItem(oldItem);
            }
        }
        else if (slotSystem.GetWeaponInSlot(WeaponSlotSystem.WeaponSlot.Primary1) == null)
            assignSlot = WeaponSlotSystem.WeaponSlot.Primary1;
        else if (slotSystem.GetWeaponInSlot(WeaponSlotSystem.WeaponSlot.Primary2) == null)
            assignSlot = WeaponSlotSystem.WeaponSlot.Primary2;
        else
        {
            // 两个主武器槽都满，替换当前手持
            assignSlot = slotSystem.CurrentSlot;
            if (assignSlot == WeaponSlotSystem.WeaponSlot.Melee)
                assignSlot = WeaponSlotSystem.WeaponSlot.Primary1;

            var existing = slotSystem.GetWeaponInSlot(assignSlot);
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                slotSystem.SetWeaponInSlot(assignSlot, null);
                var oldItem = new InventoryItem
                {
                    itemName  = existing.weaponName,
                    quantity  = 1,
                    weaponRef = existing,
                };
                inventorySystem.AddItem(oldItem);
            }
        }

        // 从背包移除
        inventorySystem.RemoveItemAt(selectedSlotIndex);

        // 装备武器
        weapon.gameObject.SetActive(true);
        slotSystem.SetWeaponInSlot(assignSlot, weapon);

        // 挂到 AimPivot 下
        var pc = inventorySystem.GetComponent<PlayerController>();
        if (pc != null && pc.aimPivot != null)
        {
            weapon.transform.SetParent(pc.aimPivot);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        // 如果不是当前槽位，隐藏
        if (assignSlot != slotSystem.CurrentSlot)
            weapon.gameObject.SetActive(false);

        Debug.Log($"从背包装备武器：{weapon.weaponName} → {assignSlot}");
        DeselectAll();
        RefreshDisplay();
    }

    /// <summary>使用选中的物品（医疗品等）</summary>
    private void UseSelectedItem()
    {
        if (inventorySystem == null) return;
        var items = inventorySystem.GetItems();

        if (selectedSlotIndex < 0 || selectedSlotIndex >= items.Count)
            return;

        var item = items[selectedSlotIndex];
        var player = inventorySystem.GetComponent<PlayerController>();
        if (player == null) return;

        // 调用物品的 Use 方法
        item.Use(player);

        // 检查是否需要移除（一次性物品用完 / 耐久耗尽）
        if (item is MedicalItem med)
        {
            if (med.isSingleUse && med.quantity <= 0)
            {
                inventorySystem.RemoveItemAt(selectedSlotIndex);
                DeselectAll();
            }
            else if (!med.isSingleUse && !med.CanUse())
            {
                inventorySystem.RemoveItemAt(selectedSlotIndex);
                DeselectAll();
            }
        }
        else if (item.quantity <= 0)
        {
            inventorySystem.RemoveItemAt(selectedSlotIndex);
            DeselectAll();
        }

        RefreshDisplay();
    }

    /// <summary>在玩家前方生成丢弃的地面物品</summary>
    private void SpawnDroppedItem(InventoryItem item)
    {
        var player = inventorySystem != null ? inventorySystem.gameObject : null;
        if (player == null) return;

        // 丢弃方向
        Vector2 dropDir = Vector2.right;
        var pc = player.GetComponent<PlayerController>();
        if (pc != null && pc.aimPivot != null)
            dropDir = pc.aimPivot.right;

        Vector2 dropPos = (Vector2)player.transform.position + dropDir * 1.5f;

        // 如果是武器物品（有武器引用），恢复武器 GameObject
        if (item.weaponRef != null)
        {
            var weapon = item.weaponRef;
            weapon.transform.SetParent(null);
            weapon.transform.position = dropPos;
            weapon.gameObject.SetActive(true);

            // 添加 GroundItem 使其可再次拾取
            var gi = weapon.GetComponent<GroundItem>();
            if (gi == null) gi = weapon.gameObject.AddComponent<GroundItem>();
            gi.itemType    = GroundItem.GroundItemType.Weapon;
            gi.weapon      = weapon;
            gi.displayName = weapon.weaponName;

            var col = weapon.GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            Debug.Log($"丢弃武器到地面：{weapon.weaponName}");
            return;
        }

        // 普通物品：生成新 GameObject
        var go = new GameObject($"DroppedItem_{item.itemName}");
        go.transform.position = dropPos;

        // 确保弹药物品有图标
        if (item.icon == null && item is AmmoItem droppedAmmo)
        {
            item.icon = AmmoIconManager.GetAmmoIcon(droppedAmmo.ammoType, droppedAmmo.isHighGrade);
        }

        var sr = go.AddComponent<SpriteRenderer>();
        if (item.icon != null)
        {
            sr.sprite = item.icon;
            sr.color  = Color.white;
        }
        else
        {
            sr.sprite = CreateWhiteSprite();
            sr.color  = new Color(0.7f, 0.7f, 0.7f, 0.9f);
        }
        go.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        var newCol = go.AddComponent<CircleCollider2D>();
        newCol.radius    = 0.3f;
        newCol.isTrigger = true;

        var newGi = go.AddComponent<GroundItem>();
        // 弹药物品使用Ammo类型
        if (item is AmmoItem ammoDropped)
        {
            newGi.itemType    = GroundItem.GroundItemType.Ammo;
            newGi.ammoItem    = ammoDropped;
        }
        else
        {
            newGi.itemType    = GroundItem.GroundItemType.Item;
            newGi.item        = item;
        }
        newGi.displayIcon = item.icon;
        newGi.displayName = item.itemName;
    }

    private static Sprite _cachedWhiteSprite;
    private static Sprite CreateWhiteSprite()
    {
        if (_cachedWhiteSprite != null) return _cachedWhiteSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        _cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        return _cachedWhiteSprite;
    }

    /// <summary>增加一行背包格子</summary>
    public void AddRow()
    {
        currentRows++;
        if (inventorySystem != null)
            inventorySystem.maxSlots = TotalSlots;
        RebuildGrid();
    }

    /// <summary>重建所有格子</summary>
    public void RebuildGrid()
    {
        if (contentRoot == null) return;

        int targetCount = TotalSlots;

        // 补充不足的格子
        while (slotUIs.Count < targetCount)
        {
            GameObject go;
            if (slotPrefab != null)
                go = Instantiate(slotPrefab, contentRoot);
            else
                go = CreateDefaultSlot(contentRoot);

            var slotUI = go.GetComponent<InventorySlotUI>();
            if (slotUI == null) slotUI = go.AddComponent<InventorySlotUI>();
            slotUI.slotIndex = slotUIs.Count;
            slotUIs.Add(slotUI);
        }

        // 隐藏多余的格子
        for (int i = 0; i < slotUIs.Count; i++)
            slotUIs[i].gameObject.SetActive(i < targetCount);

        // 更新 Content 高度（让 ScrollRect 知道内容大小）
        float rowHeight = slotSize + slotSpacing;
        float totalHeight = rowHeight * currentRows + slotSpacing;
        contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, totalHeight);

        RefreshDisplay();
    }

    /// <summary>刷新所有格子显示</summary>
    public void RefreshDisplay()
    {
        if (inventorySystem == null) return;
        var items = inventorySystem.GetItems();
        var slotStates = SlotLockManager.CalculateSlotStates(items, slotUIs.Count);

        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (!slotUIs[i].gameObject.activeSelf) continue;

            var state = slotStates[i];

            if (state.isSubSlot)
            {
                // 子格：主格图标 + 灰色滤镜
                slotUIs[i].SetSubSlot(state.mainItem, filledSlotColor);
            }
            else if (state.item != null)
            {
                slotUIs[i].SetItem(state.item, filledSlotColor);
            }
            else
            {
                slotUIs[i].SetEmpty(emptySlotColor);
            }
        }
    }

    /// <summary>创建默认格子（无预制体时）</summary>
    private GameObject CreateDefaultSlot(Transform parent)
    {
        var go = new GameObject("Slot", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        // 背景
        var bg = go.AddComponent<Image>();
        bg.color = emptySlotColor;

        // 图标子物体
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(go.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.enabled = false;

        // 数量文本
        var countGO = new GameObject("Count", typeof(RectTransform));
        countGO.transform.SetParent(go.transform, false);
        var countRT = countGO.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0.55f, 0f);
        countRT.anchorMax = new Vector2(1f, 0.35f);
        countRT.offsetMin = countRT.offsetMax = Vector2.zero;
        var countTMP = countGO.AddComponent<TextMeshProUGUI>();
        countTMP.fontSize  = 12;
        countTMP.alignment = TextAlignmentOptions.BottomRight;
        countTMP.color     = Color.white;
        countTMP.text      = "";

        return go;
    }
}
