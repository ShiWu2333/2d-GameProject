using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 散落物面板 UI
/// 屏幕中间偏右靠上，3列×5行可见区域，超出可滚轮滚动
/// 左键点击选中，F键拾取选中物品
/// </summary>
public class LootPanelUI : MonoBehaviour
{
    [Header("布局")]
    public int columns     = 3;
    public int visibleRows = 5;
    public float slotSize  = 48f;
    public float spacing   = 4f;

    [Header("颜色")]
    public Color slotNormalColor   = new Color(0.25f, 0.25f, 0.25f, 0.85f);
    public Color slotSelectedColor = new Color(1f, 0.85f, 0f, 0.9f);
    public Color panelBgColor      = new Color(0.05f, 0.05f, 0.08f, 0.9f);
    public Color itemIconColor     = Color.white;

    [Header("引用（由 Editor 工具自动赋值）")]
    public RectTransform contentRoot;
    public ScrollRect    scrollRect;

    // 运行时
    private List<LootSlot> slotPool = new List<LootSlot>();
    private List<GroundItem> currentItems;
    private int selectedIndex = -1;

    public int SelectedIndex => selectedIndex;

    void Awake()
    {
        // 面板显隐由 PickupSystem 控制，不在此处设置
    }

    public void Show(List<GroundItem> items)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        currentItems = items;
        RebuildSlots();
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            selectedIndex = -1;
        }
    }

    public void SelectSlot(int index)
    {
        if (selectedIndex == index)
        {
            DeselectAll();
            return;
        }
        DeselectAll();
        selectedIndex = index;
        if (index >= 0 && index < slotPool.Count)
            slotPool[index].SetSelected(true);
    }

    public void DeselectAll()
    {
        if (selectedIndex >= 0 && selectedIndex < slotPool.Count)
            slotPool[selectedIndex].SetSelected(false);
        selectedIndex = -1;
    }

    private void RebuildSlots()
    {
        if (contentRoot == null || currentItems == null) return;

        int needed = currentItems.Count;

        // 扩充池
        while (slotPool.Count < needed)
        {
            var go = CreateSlot(contentRoot);
            var slot = go.AddComponent<LootSlot>();
            slot.panel = this;
            slot.index = slotPool.Count;
            slotPool.Add(slot);
        }

        // 显示/隐藏
        for (int i = 0; i < slotPool.Count; i++)
        {
            if (i < needed)
            {
                slotPool[i].gameObject.SetActive(true);
                slotPool[i].index = i;
                var gi = currentItems[i];
                Sprite icon = gi.GetDisplayIcon();
                string name = gi.GetDisplayName();
                slotPool[i].SetData(icon, name, slotNormalColor, itemIconColor);
                slotPool[i].SetSelected(i == selectedIndex);
            }
            else
            {
                slotPool[i].gameObject.SetActive(false);
            }
        }

        // 更新 content 高度
        int rows = Mathf.CeilToInt((float)needed / columns);
        float h = rows * (slotSize + spacing) + spacing;
        contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, h);
    }

    private GameObject CreateSlot(Transform parent)
    {
        var go = new GameObject("LootSlot", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        // 背景
        var bg = go.AddComponent<Image>();
        bg.color = slotNormalColor;

        // 图标
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(go.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.enabled = false;

        return go;
    }
}

/// <summary>
/// 散落物面板单个格子
/// </summary>
public class LootSlot : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public LootPanelUI panel;
    [HideInInspector] public int index;

    private Image background;
    private Image iconImage;
    private Color normalColor;

    void Awake()
    {
        background = GetComponent<Image>();
        var iconTf = transform.Find("Icon");
        if (iconTf != null) iconImage = iconTf.GetComponent<Image>();
    }

    public void SetData(Sprite icon, string itemName, Color bgColor, Color iconColor)
    {
        normalColor = bgColor;
        if (background != null) background.color = bgColor;

        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite  = icon;
                iconImage.color   = iconColor;
                iconImage.enabled = true;
            }
            else
            {
                // 无图标时显示白色正方形占位
                iconImage.color   = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                iconImage.enabled = true;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected
                ? new Color(1f, 0.85f, 0f, 0.9f)
                : normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (panel != null) panel.SelectSlot(index);
        }
    }
}
