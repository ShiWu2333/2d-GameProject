using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设置面板一键搭建
/// 菜单：Tools → 搭建设置面板
/// </summary>
public static class SettingsPanelSetup
{
    [MenuItem("Tools/搭建设置面板")]
    public static void Setup()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("没有Canvas"); return; }

        // ── KeyBindings 单例 ──────────────────────────
        var kbGO = GameObject.Find("KeyBindings");
        if (kbGO == null)
        {
            kbGO = new GameObject("KeyBindings");
            Undo.RegisterCreatedObjectUndo(kbGO, "Create KeyBindings");
        }
        GetOrAdd<KeyBindings>(kbGO);

        // ── 设置面板 ──────────────────────────────────
        var panelGO = FindOrMake(canvas.transform, "SettingsPanel");
        var panelRT = GetOrAdd<RectTransform>(panelGO);
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(520f, 680f);
        panelRT.anchoredPosition = Vector2.zero;

        var panelImg = GetOrAdd<Image>(panelGO);
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

        // ── 标题 ──────────────────────────────────────
        var titleGO = FindOrMake(panelRT.transform, "Title");
        var titleRT = GetOrAdd<RectTransform>(titleGO);
        titleRT.anchorMin = new Vector2(0f, 0.92f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = new Vector2(10f, 0f);
        titleRT.offsetMax = new Vector2(-10f, 0f);
        var titleTMP = GetOrAdd<TextMeshProUGUI>(titleGO);
        titleTMP.text = "按键设置";
        titleTMP.fontSize = 22;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;

        // ── ScrollView（按键列表）─────────────────────
        var scrollGO = FindOrMake(panelRT.transform, "ScrollView");
        var scrollRT = GetOrAdd<RectTransform>(scrollGO);
        scrollRT.anchorMin = new Vector2(0f, 0.1f);
        scrollRT.anchorMax = new Vector2(1f, 0.9f);
        scrollRT.offsetMin = new Vector2(8f, 0f);
        scrollRT.offsetMax = new Vector2(-8f, 0f);

        var scrollRect = GetOrAdd<ScrollRect>(scrollGO);
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25f;

        var scrollImg = GetOrAdd<Image>(scrollGO);
        scrollImg.color = new Color(0f, 0f, 0f, 0.01f);
        var mask = GetOrAdd<Mask>(scrollGO);
        mask.showMaskGraphic = false;

        var viewportGO = FindOrMake(scrollRT.transform, "Viewport");
        var viewportRT = GetOrAdd<RectTransform>(viewportGO);
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = viewportRT.offsetMax = Vector2.zero;
        scrollRect.viewport = viewportRT;

        var contentGO = FindOrMake(viewportRT.transform, "Content");
        var contentRT = GetOrAdd<RectTransform>(contentGO);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 500f);
        contentRT.anchoredPosition = Vector2.zero;

        var vlg = GetOrAdd<VerticalLayoutGroup>(contentGO);
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;

        var fitter = GetOrAdd<ContentSizeFitter>(contentGO);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // ── 底部按钮 ──────────────────────────────────
        var resetBtnGO = CreateButton(panelRT.transform, "ResetBtn", "恢复默认",
            new Vector2(-60f, -panelRT.sizeDelta.y * 0.45f + 10f), new Vector2(120f, 35f));
        var backBtnGO = CreateButton(panelRT.transform, "BackBtn", "返回",
            new Vector2(60f, -panelRT.sizeDelta.y * 0.45f + 10f), new Vector2(120f, 35f));

        // ── KeyBindSettingsUI 组件 ────────────────────
        var settingsUI = GetOrAdd<KeyBindSettingsUI>(panelGO);
        settingsUI.contentRoot = contentRT;
        settingsUI.resetButton = resetBtnGO.GetComponent<Button>();
        settingsUI.backButton  = backBtnGO.GetComponent<Button>();

        // ── 关联到 PauseMenu ──────────────────────────
        var pauseRoot = GameObject.Find("PauseMenuRoot");
        if (pauseRoot != null)
        {
            var pauseMenu = pauseRoot.GetComponent<PauseMenu>();
            if (pauseMenu != null)
            {
                Undo.RecordObject(pauseMenu, "Link Settings");
                pauseMenu.settingsPanel = panelGO;
                EditorUtility.SetDirty(pauseMenu);
            }
        }

        // 默认隐藏
        panelGO.SetActive(false);

        EditorUtility.SetDirty(panelGO);
        EditorUtility.SetDirty(settingsUI);

        Debug.Log("[SettingsPanelSetup] 设置面板搭建完成！");
        EditorUtility.DisplayDialog("✅ 设置面板搭建完成",
            "已创建 SettingsPanel + KeyBindings 单例。\n\n" +
            "• 暂停菜单点击「设置」打开\n" +
            "• 点击按键值可重新绑定\n" +
            "• 按 Escape 取消绑定\n" +
            "• 设置自动保存到 PlayerPrefs", "OK");
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size)
    {
        var go = FindOrMake(parent, name);
        var rt = GetOrAdd<RectTransform>(go);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = GetOrAdd<Image>(go);
        img.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        GetOrAdd<Button>(go);

        var textGO = FindOrMake(go.transform, "Text");
        var textRT = GetOrAdd<RectTransform>(textGO);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var tmp = GetOrAdd<TextMeshProUGUI>(textGO);
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return go;
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
