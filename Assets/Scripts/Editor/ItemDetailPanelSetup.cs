using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 物品详细面板一键搭建
/// 菜单：Tools → 搭建物品详细面板
/// </summary>
public static class ItemDetailPanelSetup
{
    [MenuItem("Tools/搭建物品详细面板")]
    public static void Setup()
    {
        // 找背包面板（支持未激活状态）
        GameObject invPanel = GameObject.Find("InventoryPanel");
        if (invPanel == null)
        {
            // Find 找不到未激活对象，遍历 Canvas 子物体查找
#if UNITY_2023_1_OR_NEWER
            var canvasSearch = Object.FindFirstObjectByType<Canvas>();
#else
            var canvasSearch = Object.FindObjectOfType<Canvas>();
#endif
            if (canvasSearch != null)
            {
                var tf = canvasSearch.transform.Find("InventoryPanel");
                if (tf != null) invPanel = tf.gameObject;
            }
        }
        if (invPanel == null)
        {
            Debug.LogError("[ItemDetailPanelSetup] 找不到 InventoryPanel，请先运行 Tools → 搭建背包UI");
            return;
        }

        var canvas = invPanel.GetComponentInParent<Canvas>();
        float panelW = 220f;
        float panelH = 350f;

        // ── 详细面板根节点（在背包右侧）────────────────
        var detailGO = FindOrMake(canvas.transform, "ItemDetailPanel");
        var detailRT = GetOrAdd<RectTransform>(detailGO);

        // 定位到背包面板右侧
        var invRT = invPanel.GetComponent<RectTransform>();
        detailRT.anchorMin        = new Vector2(0.5f, 0.5f);
        detailRT.anchorMax        = new Vector2(0.5f, 0.5f);
        detailRT.pivot            = new Vector2(0f, 0.5f);
        detailRT.sizeDelta        = new Vector2(panelW, panelH);
        // 放在背包右边
        float invRight = invRT.anchoredPosition.x + invRT.sizeDelta.x * 0.5f + 8f;
        detailRT.anchoredPosition = new Vector2(invRight, invRT.anchoredPosition.y);

        var bgImg = GetOrAdd<Image>(detailGO);
        bgImg.color = new Color(0.06f, 0.06f, 0.1f, 0.93f);

        var detail = GetOrAdd<ItemDetailPanel>(detailGO);

        float y = -12f;
        float lineH = 24f;

        // ── 物品名称 ──────────────────────────────────
        detail.itemNameText = MakeLabel(detailRT, "ItemName", ref y, 20, FontStyles.Bold);

        // ── 图标 ──────────────────────────────────────
        var iconGO = FindOrMake(detailRT.transform, "ItemIcon");
        var iconRT = GetOrAdd<RectTransform>(iconGO);
        iconRT.anchorMin        = new Vector2(0.5f, 1f);
        iconRT.anchorMax        = new Vector2(0.5f, 1f);
        iconRT.pivot            = new Vector2(0.5f, 1f);
        iconRT.sizeDelta        = new Vector2(64f, 64f);
        iconRT.anchoredPosition = new Vector2(0f, y - 8f);
        y -= 72f;
        detail.itemIconImage = GetOrAdd<Image>(iconGO);
        detail.itemIconImage.enabled = false;

        // ── 描述 ──────────────────────────────────────
        detail.descriptionText = MakeLabel(detailRT, "Description", ref y, 13, FontStyles.Normal);

        y -= 8f;

        // ── 武器属性组 ────────────────────────────────
        var weaponGroup = FindOrMake(detailRT.transform, "WeaponStats");
        var wgRT = GetOrAdd<RectTransform>(weaponGroup);
        wgRT.anchorMin        = new Vector2(0f, 1f);
        wgRT.anchorMax        = new Vector2(1f, 1f);
        wgRT.pivot            = new Vector2(0.5f, 1f);
        wgRT.anchoredPosition = new Vector2(0f, y);
        wgRT.sizeDelta        = new Vector2(0f, lineH * 14);
        detail.weaponStatsGroup = weaponGroup;

        float wy = 0f;
        detail.damageText       = MakeLabel(wgRT, "Damage",      ref wy, 13, FontStyles.Normal);
        detail.fireRateText     = MakeLabel(wgRT, "FireRate",    ref wy, 13, FontStyles.Normal);
        detail.ammoCapacityText = MakeLabel(wgRT, "AmmoCap",     ref wy, 13, FontStyles.Normal);
        detail.rangeText        = MakeLabel(wgRT, "Range",       ref wy, 13, FontStyles.Normal);
        detail.reloadTimeText   = MakeLabel(wgRT, "ReloadTime",  ref wy, 13, FontStyles.Normal);
        detail.recoilText       = MakeLabel(wgRT, "Recoil",      ref wy, 13, FontStyles.Normal);
        detail.spreadText       = MakeLabel(wgRT, "Spread",      ref wy, 13, FontStyles.Normal);
        detail.moveSpreadText   = MakeLabel(wgRT, "MoveSpread",  ref wy, 13, FontStyles.Normal);
        detail.aimSpreadText    = MakeLabel(wgRT, "AimSpread",   ref wy, 13, FontStyles.Normal);
        detail.moveSpeedText    = MakeLabel(wgRT, "MoveSpeed",   ref wy, 13, FontStyles.Normal);
        detail.aimSpeedText     = MakeLabel(wgRT, "AimSpeed",    ref wy, 13, FontStyles.Normal);
        detail.ammoTypeText     = MakeLabel(wgRT, "AmmoType",    ref wy, 13, FontStyles.Normal);
        detail.penetrationText  = MakeLabel(wgRT, "Penetration", ref wy, 13, FontStyles.Normal);
        detail.fireModeText     = MakeLabel(wgRT, "FireMode",    ref wy, 13, FontStyles.Normal);

        // ── 弹药属性组 ────────────────────────────────
        var ammoGroup = FindOrMake(detailRT.transform, "AmmoStats");
        var agRT = GetOrAdd<RectTransform>(ammoGroup);
        agRT.anchorMin        = new Vector2(0f, 1f);
        agRT.anchorMax        = new Vector2(1f, 1f);
        agRT.pivot            = new Vector2(0.5f, 1f);
        agRT.anchoredPosition = new Vector2(0f, y);
        agRT.sizeDelta        = new Vector2(0f, lineH * 2);
        detail.ammoStatsGroup = ammoGroup;

        float ay = 0f;
        detail.ammoCountText       = MakeLabel(agRT, "AmmoCount", ref ay, 13, FontStyles.Normal);
        detail.ammoPenetrationText = MakeLabel(agRT, "AmmoPen",   ref ay, 13, FontStyles.Normal);

        // ── 关联到 InventoryUI ────────────────────────
        var invUI = invPanel.GetComponent<InventoryUI>();
        if (invUI != null)
        {
            Undo.RecordObject(invUI, "Link DetailPanel");
            invUI.detailPanel = detail;
            EditorUtility.SetDirty(invUI);
        }

        // 默认隐藏
        detailGO.SetActive(false);

        EditorUtility.SetDirty(detailGO);
        EditorUtility.SetDirty(detail);

        Debug.Log("[ItemDetailPanelSetup] 物品详细面板搭建完成！");
        EditorUtility.DisplayDialog("✅ 详细面板搭建完成",
            "已在 Canvas 下创建 ItemDetailPanel。\n\n" +
            "• 锁定物品时自动展开\n" +
            "• 武器显示完整属性\n" +
            "• 弹药显示数量和类型\n" +
            "• 取消选中时自动隐藏", "OK");
    }

    private static TextMeshProUGUI MakeLabel(RectTransform parent, string name,
        ref float y, int fontSize, FontStyles style)
    {
        var go = FindOrMake(parent.transform, name);
        var rt = GetOrAdd<RectTransform>(go);
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(-16f, 22f);
        rt.anchoredPosition = new Vector2(0f, y);
        y -= 24f;

        var tmp = GetOrAdd<TextMeshProUGUI>(go);
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.text      = name;
        tmp.raycastTarget = false;
        return tmp;
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
