using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 设置面板 UI
/// 运行时自动构建完整的按键绑定界面（含滚动）
/// 无需依赖编辑器搭建工具
/// </summary>
public class KeyBindSettingsUI : MonoBehaviour
{
    [Header("引用（可选，留空则自动创建）")]
    public Transform contentRoot;
    public Button   resetButton;
    public Button   backButton;

    [Header("样式")]
    public float rowHeight    = 60f;
    public float headerHeight = 50f;
    public float spacing      = 30f;
    public float topPadding   = 40f;
    public Color normalColor  = new Color(0.2f, 0.2f, 0.25f, 0.9f);
    public Color altColor     = new Color(0.16f, 0.16f, 0.2f, 0.9f);
    public Color waitColor    = new Color(0.9f, 0.6f, 0.1f, 1f);
    public Color headerColor  = new Color(0.3f, 0.3f, 0.4f, 1f);

    // 内部
    private struct BindEntry
    {
        public string label;
        public System.Func<KeyCode> getter;
        public System.Action<KeyCode> setter;
    }

    private List<BindEntry> entries = new List<BindEntry>();
    private List<TextMeshProUGUI> valueTexts = new List<TextMeshProUGUI>();
    private List<Image> rowBGs = new List<Image>();
    private int waitingIndex = -1;

    void Start()
    {
        BuildEntries();
        
        // 如果没有contentRoot，自己从零构建整个UI
        if (contentRoot == null)
            BuildFullUI();
        else
            BuildRows();

        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
        if (backButton != null)  backButton.onClick.AddListener(OnBack);
    }

    void Update()
    {
        if (waitingIndex < 0) return;

        if (Input.anyKeyDown)
        {
            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (kc == KeyCode.None || kc == KeyCode.Mouse0 || kc == KeyCode.Mouse1) continue;
                if (Input.GetKeyDown(kc))
                {
                    if (kc == KeyCode.Escape) { CancelWaiting(); return; }
                    entries[waitingIndex].setter(kc);
                    KeyBindings.Instance.SaveBindings();
                    RefreshDisplay();
                    CancelWaiting();
                    return;
                }
            }
        }
    }

    // ══════════════════════════════════════════════════
    //  自动构建完整UI
    // ══════════════════════════════════════════════════

    private void BuildFullUI()
    {
        var myRT = GetComponent<RectTransform>();

        // ScrollView区域（上下留间距）
        var viewportGO = new GameObject("Viewport", typeof(RectTransform));
        viewportGO.transform.SetParent(transform, false);
        var vpRT = viewportGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(0f, 55f);   // 底部留给按钮
        vpRT.offsetMax = new Vector2(0f, -10f);  // 顶部间距
        viewportGO.AddComponent<RectMask2D>();

        // Content（所有行放在这里）
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, 100f); // 先占位，后面算

        contentRoot = contentRT;

        // ScrollRect
        var scroll = gameObject.AddComponent<ScrollRect>();
        scroll.viewport = vpRT;
        scroll.content = contentRT;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 35f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // 构建行
        BuildRows();

        // 设置content高度
        contentRT.sizeDelta = new Vector2(0f, CalcTotalHeight());

        // 底部按钮
        CreateBottomButtons();
    }

    // ══════════════════════════════════════════════════
    //  构建行
    // ══════════════════════════════════════════════════

    private void BuildRows()
    {
        if (contentRoot == null) return;

        float y = -topPadding;
        int bindIndex = 0;
        bool alt = false;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            bool isHeader = entry.getter == null;

            if (isHeader)
            {
                CreateHeader(entry.label, y);
                y -= headerHeight + spacing;
                alt = false;
            }
            else
            {
                CreateBindRow(entry, bindIndex, y, alt);
                y -= rowHeight + spacing;
                bindIndex++;
                alt = !alt;
            }
        }
    }

    private void CreateHeader(string title, float yPos)
    {
        var go = new GameObject($"Header_{title}", typeof(RectTransform));
        go.transform.SetParent(contentRoot, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(0f, headerHeight);

        var img = go.AddComponent<Image>();
        img.color = headerColor;

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12f, 0f);
        textRT.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = $"【{title}】";
        tmp.fontSize = 15;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.9f, 0.85f, 0.5f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
    }

    private void CreateBindRow(BindEntry entry, int bindIndex, float yPos, bool alt)
    {
        int idx = bindIndex;

        var rowGO = new GameObject($"Row_{entry.label}", typeof(RectTransform));
        rowGO.transform.SetParent(contentRoot, false);
        var rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, yPos);
        rowRT.sizeDelta = new Vector2(0f, rowHeight);

        var rowImg = rowGO.AddComponent<Image>();
        rowImg.color = alt ? altColor : normalColor;
        rowBGs.Add(rowImg);

        // 标签
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(rowGO.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(0.5f, 1f);
        labelRT.offsetMin = new Vector2(16f, 0f);
        labelRT.offsetMax = Vector2.zero;
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = entry.label;
        labelTMP.fontSize = 14;
        labelTMP.color = Color.white;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        labelTMP.raycastTarget = false;

        // 按键按钮
        var btnGO = new GameObject("KeyBtn", typeof(RectTransform));
        btnGO.transform.SetParent(rowGO.transform, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.55f, 0.12f);
        btnRT.anchorMax = new Vector2(0.92f, 0.88f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.12f, 0.12f, 0.18f);
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => StartWaiting(idx));

        var valGO = new GameObject("Value", typeof(RectTransform));
        valGO.transform.SetParent(btnGO.transform, false);
        var valRT = valGO.GetComponent<RectTransform>();
        valRT.anchorMin = Vector2.zero;
        valRT.anchorMax = Vector2.one;
        valRT.offsetMin = valRT.offsetMax = Vector2.zero;
        var valTMP = valGO.AddComponent<TextMeshProUGUI>();
        valTMP.text = FormatKeyName(entry.getter());
        valTMP.fontSize = 14;
        valTMP.color = Color.white;
        valTMP.alignment = TextAlignmentOptions.Center;
        valTMP.raycastTarget = false;
        valueTexts.Add(valTMP);
    }

    private void CreateBottomButtons()
    {
        // 恢复默认
        var resetGO = CreateBtn("恢复默认", new Vector2(-70f, 28f));
        resetButton = resetGO.GetComponent<Button>();
        resetButton.onClick.AddListener(OnReset);

        // 返回
        var backGO = CreateBtn("返回", new Vector2(70f, 28f));
        backButton = backGO.GetComponent<Button>();
        backButton.onClick.AddListener(OnBack);
    }

    private GameObject CreateBtn(string label, Vector2 pos)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(110f, 36f);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.35f);
        go.AddComponent<Button>();

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return go;
    }

    // ══════════════════════════════════════════════════
    //  按键条目
    // ══════════════════════════════════════════════════

    private void BuildEntries()
    {
        var kb = KeyBindings.Instance;
        if (kb == null) return;

        entries.Add(new BindEntry { label = "移动",      getter = null, setter = null });
        entries.Add(new BindEntry { label = "上移",      getter = () => kb.moveUp,    setter = v => kb.moveUp = v });
        entries.Add(new BindEntry { label = "下移",      getter = () => kb.moveDown,  setter = v => kb.moveDown = v });
        entries.Add(new BindEntry { label = "左移",      getter = () => kb.moveLeft,  setter = v => kb.moveLeft = v });
        entries.Add(new BindEntry { label = "右移",      getter = () => kb.moveRight, setter = v => kb.moveRight = v });
        entries.Add(new BindEntry { label = "冲刺",      getter = () => kb.sprint,    setter = v => kb.sprint = v });

        entries.Add(new BindEntry { label = "战斗",      getter = null, setter = null });
        entries.Add(new BindEntry { label = "换弹",      getter = () => kb.reload,     setter = v => kb.reload = v });
        entries.Add(new BindEntry { label = "主武器1",   getter = () => kb.weapon1,    setter = v => kb.weapon1 = v });
        entries.Add(new BindEntry { label = "主武器2",   getter = () => kb.weapon2,    setter = v => kb.weapon2 = v });
        entries.Add(new BindEntry { label = "近战武器",  getter = () => kb.weapon3,    setter = v => kb.weapon3 = v });
        entries.Add(new BindEntry { label = "丢弃武器",  getter = () => kb.dropWeapon, setter = v => kb.dropWeapon = v });

        entries.Add(new BindEntry { label = "交互",      getter = null, setter = null });
        entries.Add(new BindEntry { label = "拾取/交互", getter = () => kb.interact,   setter = v => kb.interact = v });
        entries.Add(new BindEntry { label = "背包",      getter = () => kb.inventory,  setter = v => kb.inventory = v });

        entries.Add(new BindEntry { label = "系统",      getter = null, setter = null });
        entries.Add(new BindEntry { label = "暂停",      getter = () => kb.pause,      setter = v => kb.pause = v });
    }

    // ══════════════════════════════════════════════════
    //  交互
    // ══════════════════════════════════════════════════

    private void StartWaiting(int index)
    {
        CancelWaiting();
        waitingIndex = index;
        if (index < valueTexts.Count)
            valueTexts[index].text = "< 按下新键 >";
        if (index < rowBGs.Count)
            rowBGs[index].color = waitColor;
    }

    private void CancelWaiting()
    {
        if (waitingIndex >= 0 && waitingIndex < rowBGs.Count)
            rowBGs[waitingIndex].color = (waitingIndex % 2 == 0) ? normalColor : altColor;
        waitingIndex = -1;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        int bindIndex = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].getter == null) continue;
            if (bindIndex < valueTexts.Count)
                valueTexts[bindIndex].text = FormatKeyName(entries[i].getter());
            bindIndex++;
        }
    }

    private void OnReset()
    {
        KeyBindings.Instance?.ResetToDefault();
        RefreshDisplay();
    }

    private void OnBack()
    {
        gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════
    //  工具
    // ══════════════════════════════════════════════════

    private float CalcTotalHeight()
    {
        float h = topPadding;
        for (int i = 0; i < entries.Count; i++)
        {
            h += (entries[i].getter == null ? headerHeight : rowHeight) + spacing;
        }
        h += 20f; // 底部余量
        return h;
    }

    private static string FormatKeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.LeftShift:   return "L-Shift";
            case KeyCode.RightShift:  return "R-Shift";
            case KeyCode.LeftControl: return "L-Ctrl";
            case KeyCode.RightControl:return "R-Ctrl";
            case KeyCode.LeftAlt:     return "L-Alt";
            case KeyCode.RightAlt:    return "R-Alt";
            case KeyCode.Alpha1:      return "1";
            case KeyCode.Alpha2:      return "2";
            case KeyCode.Alpha3:      return "3";
            case KeyCode.Alpha4:      return "4";
            case KeyCode.Alpha5:      return "5";
            case KeyCode.Space:       return "空格";
            case KeyCode.Tab:         return "Tab";
            case KeyCode.Escape:      return "Esc";
            case KeyCode.Return:      return "Enter";
            default:                  return key.ToString();
        }
    }
}
