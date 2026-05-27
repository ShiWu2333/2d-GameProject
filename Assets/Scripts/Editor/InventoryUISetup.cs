using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 背包 UI 一键搭建工具
/// 菜单：Tools → 搭建背包UI
/// </summary>
public static class InventoryUISetup
{
    [MenuItem("Tools/搭建背包UI")]
    public static void Setup()
    {
        // 找 Canvas
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[InventoryUISetup] 场景中没有 Canvas");
            return;
        }

        // 确保 Canvas 有 GraphicRaycaster（UI点击必须）
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);

        // 确保场景有 EventSystem（UI点击必须）
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            Undo.AddComponent<UnityEngine.EventSystems.EventSystem>(esGO);
            Undo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>(esGO);
        }

        float slotSize    = 64f;
        float spacing     = 4f;
        int   columns     = 9;
        float panelWidth  = columns * (slotSize + spacing) + spacing + 20f;
        float panelHeight = 400f;

        // ── 背包面板根节点 ────────────────────────────
        var panelGO = FindOrMake(canvas.transform, "InventoryPanel");
        var panelRT = GetOrAdd<RectTransform>(panelGO);
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(panelWidth, panelHeight);
        panelRT.anchoredPosition = Vector2.zero;

        var panelImg = GetOrAdd<Image>(panelGO);
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        // InventoryUI 组件
        var invUI = GetOrAdd<InventoryUI>(panelGO);

        // ── 标题 ──────────────────────────────────────
        var titleGO = FindOrMake(panelRT.transform, "Title");
        var titleRT = GetOrAdd<RectTransform>(titleGO);
        titleRT.anchorMin = new Vector2(0f, 0.9f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = new Vector2(10f, 0f);
        titleRT.offsetMax = new Vector2(-10f, 0f);
        var titleTMP = GetOrAdd<TextMeshProUGUI>(titleGO);
        titleTMP.text      = "背包";
        titleTMP.fontSize  = 20;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = Color.white;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // ── ScrollView ────────────────────────────────
        var scrollGO = FindOrMake(panelRT.transform, "ScrollView");
        var scrollRT = GetOrAdd<RectTransform>(scrollGO);
        scrollRT.anchorMin = new Vector2(0f, 0f);
        scrollRT.anchorMax = new Vector2(1f, 0.88f);
        scrollRT.offsetMin = new Vector2(8f, 8f);
        scrollRT.offsetMax = new Vector2(-8f, -4f);

        var scrollRect = GetOrAdd<ScrollRect>(scrollGO);
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.movementType    = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // Mask
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

        // ── Content（GridLayoutGroup）─────────────────
        var contentGO = FindOrMake(viewportRT.transform, "Content");
        var contentRT = GetOrAdd<RectTransform>(contentGO);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 200f);
        contentRT.anchoredPosition = Vector2.zero;

        var grid = GetOrAdd<GridLayoutGroup>(contentGO);
        grid.cellSize        = new Vector2(slotSize, slotSize);
        grid.spacing         = new Vector2(spacing, spacing);
        grid.padding         = new RectOffset(4, 4, 4, 4);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment  = TextAnchor.UpperLeft;

        // ContentSizeFitter 让 Content 高度自适应
        var fitter = GetOrAdd<ContentSizeFitter>(contentGO);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // ── 赋值给 InventoryUI ────────────────────────
        invUI.contentRoot = contentRT;
        invUI.scrollRect  = scrollRect;
        invUI.columnsPerRow = columns;
        invUI.slotSize      = slotSize;
        invUI.slotSpacing   = spacing;

        // ── 关联玩家 InventorySystem ──────────────────
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var invSys = player.GetComponent<InventorySystem>();
            if (invSys != null)
            {
                Undo.RecordObject(invSys, "Link InventoryUI");
                invSys.inventoryUI = invUI;
                invSys.maxSlots    = columns * invUI.currentRows;
                EditorUtility.SetDirty(invSys);
            }
        }

        // 默认隐藏（M键打开）
        panelGO.SetActive(false);

        EditorUtility.SetDirty(panelGO);
        EditorUtility.SetDirty(invUI);

        Debug.Log("[InventoryUISetup] 背包 UI 搭建完成！");
        EditorUtility.DisplayDialog("✅ 背包UI搭建完成",
            "已在 Canvas 下创建 InventoryPanel。\n\n" +
            "• 每行9格，默认1行\n" +
            "• M键开关背包\n" +
            "• 超出区域可滚轮滚动\n" +
            "• 运行时调用 InventoryUI.AddRow() 增加行数", "OK");
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
