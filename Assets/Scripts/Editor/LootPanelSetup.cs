using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 散落物面板 UI 一键搭建
/// 菜单：Tools → 搭建散落物面板
/// </summary>
public static class LootPanelSetup
{
    [MenuItem("Tools/搭建散落物面板")]
    public static void Setup()
    {
#if UNITY_2023_1_OR_NEWER
        var canvas = Object.FindFirstObjectByType<Canvas>();
#else
        var canvas = Object.FindObjectOfType<Canvas>();
#endif
        if (canvas == null)
        {
            Debug.LogError("[LootPanelSetup] 场景中没有 Canvas");
            return;
        }

        int columns     = 3;
        int visibleRows = 5;
        float slotSize  = 48f;
        float spacing   = 4f;
        float padding   = 8f;

        float panelW = columns * (slotSize + spacing) + spacing + padding * 2;
        float panelH = visibleRows * (slotSize + spacing) + spacing + padding * 2 + 28f; // +标题

        // ── 面板根节点 ────────────────────────────────
        var panelGO = FindOrMake(canvas.transform, "LootPanel");
        var panelRT = GetOrAdd<RectTransform>(panelGO);
        // 中间偏右靠上
        panelRT.anchorMin        = new Vector2(0.6f, 0.6f);
        panelRT.anchorMax        = new Vector2(0.6f, 0.6f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(panelW, panelH);
        panelRT.anchoredPosition = Vector2.zero;

        var panelImg = GetOrAdd<Image>(panelGO);
        panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

        var lootUI = GetOrAdd<LootPanelUI>(panelGO);

        // ── 标题 ──────────────────────────────────────
        var titleGO = FindOrMake(panelRT.transform, "Title");
        var titleRT = GetOrAdd<RectTransform>(titleGO);
        titleRT.anchorMin = new Vector2(0f, 0.9f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = new Vector2(8f, 0f);
        titleRT.offsetMax = new Vector2(-8f, 0f);
        var titleTMP = GetOrAdd<TextMeshProUGUI>(titleGO);
        titleTMP.text      = "附近物品";
        titleTMP.fontSize  = 14;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = Color.white;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── ScrollView ────────────────────────────────
        var scrollGO = FindOrMake(panelRT.transform, "ScrollView");
        var scrollRT = GetOrAdd<RectTransform>(scrollGO);
        scrollRT.anchorMin = new Vector2(0f, 0f);
        scrollRT.anchorMax = new Vector2(1f, 0.88f);
        scrollRT.offsetMin = new Vector2(padding, padding);
        scrollRT.offsetMax = new Vector2(-padding, -4f);

        var scrollRect = GetOrAdd<ScrollRect>(scrollGO);
        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.movementType      = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25f;

        var scrollImg = GetOrAdd<Image>(scrollGO);
        scrollImg.color = new Color(0f, 0f, 0f, 0.01f);
        var mask = GetOrAdd<Mask>(scrollGO);
        mask.showMaskGraphic = false;

        // ── Viewport ──────────────────────────────────
        var viewportGO = FindOrMake(scrollRT.transform, "Viewport");
        var viewportRT = GetOrAdd<RectTransform>(viewportGO);
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = viewportRT.offsetMax = Vector2.zero;
        scrollRect.viewport = viewportRT;

        // ── Content ───────────────────────────────────
        var contentGO = FindOrMake(viewportRT.transform, "Content");
        var contentRT = GetOrAdd<RectTransform>(contentGO);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 100f);
        contentRT.anchoredPosition = Vector2.zero;

        var grid = GetOrAdd<GridLayoutGroup>(contentGO);
        grid.cellSize        = new Vector2(slotSize, slotSize);
        grid.spacing         = new Vector2(spacing, spacing);
        grid.padding         = new RectOffset(2, 2, 2, 2);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment  = TextAnchor.UpperLeft;

        var fitter = GetOrAdd<ContentSizeFitter>(contentGO);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // ── 赋值给 LootPanelUI ────────────────────────
        lootUI.contentRoot  = contentRT;
        lootUI.scrollRect   = scrollRect;
        lootUI.columns      = columns;
        lootUI.visibleRows  = visibleRows;
        lootUI.slotSize     = slotSize;
        lootUI.spacing      = spacing;

        // ── 关联玩家 PickupSystem ─────────────────────
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pickup = player.GetComponent<PickupSystem>();
            if (pickup == null)
                pickup = Undo.AddComponent<PickupSystem>(player);
            Undo.RecordObject(pickup, "Link LootPanel");
            pickup.lootPanel = lootUI;
            EditorUtility.SetDirty(pickup);
        }

        // 默认隐藏
        panelGO.SetActive(false);

        EditorUtility.SetDirty(panelGO);
        EditorUtility.SetDirty(lootUI);

        Debug.Log("[LootPanelSetup] 散落物面板搭建完成！");
        EditorUtility.DisplayDialog("✅ 散落物面板搭建完成",
            "已在 Canvas 下创建 LootPanel。\n\n" +
            "• 3列×5行可见区域\n" +
            "• 超出可滚轮滚动\n" +
            "• 范围内有物品时自动显示\n" +
            "• 左键选中，F键拾取\n" +
            "• 物品图标默认正方形，可在 GroundItem 上修改", "OK");
    }

    private static GameObject FindOrMake(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }
}
