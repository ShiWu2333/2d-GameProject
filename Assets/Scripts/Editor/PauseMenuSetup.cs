using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 暂停菜单一键搭建
/// 菜单：Tools → 搭建暂停菜单
/// </summary>
public static class PauseMenuSetup
{
    [MenuItem("Tools/搭建暂停菜单")]
    public static void Setup()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PauseMenuSetup] 场景中没有 Canvas");
            return;
        }

        // ── 根节点 ────────────────────────────────────
        var rootGO = FindOrMake(canvas.transform, "PauseMenuRoot");
        var pauseMenu = GetOrAdd<PauseMenu>(rootGO);

        // ── 深色遮罩（全屏半透明黑色）────────────────
        var overlayGO = FindOrMake(rootGO.transform, "Overlay");
        var overlayRT = GetOrAdd<RectTransform>(overlayGO);
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;
        var overlayImg = GetOrAdd<Image>(overlayGO);
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImg.raycastTarget = true; // 阻挡下层点击

        // ── 菜单面板（居中）──────────────────────────
        var panelGO = FindOrMake(rootGO.transform, "MenuPanel");
        var panelRT = GetOrAdd<RectTransform>(panelGO);
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(300f, 260f);
        panelRT.anchoredPosition = Vector2.zero;
        var panelImg = GetOrAdd<Image>(panelGO);
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // ── 标题 ──────────────────────────────────────
        var titleGO = FindOrMake(panelRT.transform, "Title");
        var titleRT = GetOrAdd<RectTransform>(titleGO);
        titleRT.anchorMin        = new Vector2(0f, 0.8f);
        titleRT.anchorMax        = new Vector2(1f, 1f);
        titleRT.offsetMin        = new Vector2(10f, 0f);
        titleRT.offsetMax        = new Vector2(-10f, 0f);
        var titleTMP = GetOrAdd<TextMeshProUGUI>(titleGO);
        titleTMP.text      = "暂停";
        titleTMP.fontSize  = 28;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;

        // ── 继续按钮 ──────────────────────────────────
        var resumeBtn = CreateButton(panelRT.transform, "ResumeBtn", "继续游戏",
            new Vector2(0f, 20f), new Vector2(200f, 45f));

        // ── 设置按钮 ──────────────────────────────────
        var settingsBtn = CreateButton(panelRT.transform, "SettingsBtn", "设置",
            new Vector2(0f, -35f), new Vector2(200f, 45f));

        // ── 退出按钮 ──────────────────────────────────
        var quitBtn = CreateButton(panelRT.transform, "QuitBtn", "退出游戏",
            new Vector2(0f, -90f), new Vector2(200f, 45f));
        // 退出按钮红色
        var quitImg = quitBtn.GetComponent<Image>();
        if (quitImg != null) quitImg.color = new Color(0.7f, 0.15f, 0.15f, 1f);

        // ── 赋值给 PauseMenu ──────────────────────────
        pauseMenu.overlay       = overlayGO;
        pauseMenu.menuPanel     = panelGO;
        pauseMenu.resumeButton  = resumeBtn.GetComponent<Button>();
        pauseMenu.settingsButton= settingsBtn.GetComponent<Button>();
        pauseMenu.quitButton    = quitBtn.GetComponent<Button>();

        // 默认隐藏
        overlayGO.SetActive(false);
        panelGO.SetActive(false);

        EditorUtility.SetDirty(rootGO);
        EditorUtility.SetDirty(pauseMenu);

        Debug.Log("[PauseMenuSetup] 暂停菜单搭建完成！");
        EditorUtility.DisplayDialog("✅ 暂停菜单搭建完成",
            "已在 Canvas 下创建 PauseMenuRoot。\n\n" +
            "• Escape 键打开/关闭\n" +
            "• 深色半透明遮罩\n" +
            "• 继续游戏 / 设置 / 退出游戏\n" +
            "• 暂停时 TimeScale = 0", "OK");
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size)
    {
        var btnGO = FindOrMake(parent, name);
        var btnRT = GetOrAdd<RectTransform>(btnGO);
        btnRT.anchorMin        = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax        = new Vector2(0.5f, 0.5f);
        btnRT.pivot            = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta        = size;
        btnRT.anchoredPosition = pos;

        var btnImg = GetOrAdd<Image>(btnGO);
        btnImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);

        GetOrAdd<Button>(btnGO);

        // 文字
        var textGO = FindOrMake(btnGO.transform, "Text");
        var textRT = GetOrAdd<RectTransform>(textGO);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var textTMP = GetOrAdd<TextMeshProUGUI>(textGO);
        textTMP.text      = label;
        textTMP.fontSize  = 18;
        textTMP.color     = Color.white;
        textTMP.alignment = TextAlignmentOptions.Center;
        textTMP.raycastTarget = false;

        return btnGO;
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
