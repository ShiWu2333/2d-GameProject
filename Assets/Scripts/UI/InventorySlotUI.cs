using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 单个背包格子 UI
/// 左键点击锁定选中
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public int slotIndex;

    private Image           background;
    private Image           iconImage;
    private TextMeshProUGUI countText;
    private InventoryUI     parentUI;

    // 选中边框
    private GameObject selectionBorder;

    void Awake()
    {
        background = GetComponent<Image>();

        var iconTf = transform.Find("Icon");
        if (iconTf != null) iconImage = iconTf.GetComponent<Image>();

        var countTf = transform.Find("Count");
        if (countTf != null) countText = countTf.GetComponent<TextMeshProUGUI>();

        parentUI = GetComponentInParent<InventoryUI>();

        // 创建选中边框
        CreateSelectionBorder();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (parentUI != null)
                parentUI.SelectSlot(slotIndex);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
            selectionBorder.SetActive(selected);
    }

    public void SetItem(InventoryItem item, Color bgColor)
    {
        if (background != null) background.color = bgColor;

        // 如果是弹药物品且图标为空，自动生成图标
        if (item.icon == null && item is AmmoItem ammo)
        {
            item.icon = AmmoIconManager.GetAmmoIcon(ammo.ammoType, ammo.isHighGrade);
        }

        if (iconImage != null)
        {
            if (item.icon != null)
            {
                iconImage.sprite  = item.icon;
                iconImage.color   = Color.white;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        if (countText != null)
            countText.text = item.quantity > 1 ? item.quantity.ToString() : "";
    }

    public void SetEmpty(Color bgColor)
    {
        if (background != null) background.color = bgColor;
        if (iconImage != null)  iconImage.enabled = false;
        if (countText != null)  countText.text = "";
    }

    private void CreateSelectionBorder()
    {
        selectionBorder = new GameObject("SelectionBorder", typeof(RectTransform));
        selectionBorder.transform.SetParent(transform, false);

        var rt = selectionBorder.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-2f, -2f);
        rt.offsetMax = new Vector2(2f, 2f);

        var outline = selectionBorder.AddComponent<Image>();
        outline.color = new Color(1f, 0.85f, 0f, 0.9f); // 金黄色边框
        outline.raycastTarget = false;

        // 内部挖空（用子物体遮挡中间）
        var inner = new GameObject("Inner", typeof(RectTransform));
        inner.transform.SetParent(selectionBorder.transform, false);
        var innerRT = inner.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(3f, 3f);
        innerRT.offsetMax = new Vector2(-3f, -3f);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = background != null ? background.color : new Color(0.2f, 0.2f, 0.2f, 0.8f);
        innerImg.raycastTarget = false;

        selectionBorder.SetActive(false);
    }
}
