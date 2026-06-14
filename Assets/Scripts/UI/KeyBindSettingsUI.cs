using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 按键设置面板 UI
/// 显示所有可绑定按键，点击后等待玩家按下新键
/// </summary>
public class KeyBindSettingsUI : MonoBehaviour
{
    [Header("引用")]
    public Transform contentRoot;     // 按键列表容器
    public Button   resetButton;      // 恢复默认按钮
    public Button   backButton;       // 返回按钮

    [Header("样式")]
    public float rowHeight   = 52f;
    public Color normalColor = new Color(0.25f, 0.25f, 0.3f, 1f);
    public Color waitColor   = new Color(0.9f, 0.6f, 0.1f, 1f);

    // 按键绑定条目
    private struct BindEntry
    {
        public string label;
        public System.Func<KeyCode> getter;
        public System.Action<KeyCode> setter;
    }

    private List<BindEntry> entries = new List<BindEntry>();
    private List<TextMeshProUGUI> valueTexts = new List<TextMeshProUGUI>();
    private List<Image> rowBGs = new List<Image>();

    private int waitingIndex = -1; // 正在等待按键输入的条目索引

    void Start()
    {
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
        if (backButton != null) backButton.onClick.AddListener(OnBack);

        BuildEntries();
        BuildUI();
    }

    void Update()
    {
        if (waitingIndex < 0) return;

        // 等待玩家按下任意键
        if (Input.anyKeyDown)
        {
            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (kc == KeyCode.None) continue;
                if (kc == KeyCode.Mouse0 || kc == KeyCode.Mouse1) continue; // 排除鼠标
                if (Input.GetKeyDown(kc))
                {
                    // Escape 取消绑定
                    if (kc == KeyCode.Escape)
                    {
                        CancelWaiting();
                        return;
                    }

                    // 设置新按键
                    entries[waitingIndex].setter(kc);
                    KeyBindings.Instance.SaveBindings();
                    RefreshDisplay();
                    CancelWaiting();
                    return;
                }
            }
        }
    }

    private void BuildEntries()
    {
        var kb = KeyBindings.Instance;
        if (kb == null) return;

        entries.Add(new BindEntry { label="上移",     getter=()=>kb.moveUp,    setter=v=>kb.moveUp=v });
        entries.Add(new BindEntry { label="下移",     getter=()=>kb.moveDown,  setter=v=>kb.moveDown=v });
        entries.Add(new BindEntry { label="左移",     getter=()=>kb.moveLeft,  setter=v=>kb.moveLeft=v });
        entries.Add(new BindEntry { label="右移",     getter=()=>kb.moveRight, setter=v=>kb.moveRight=v });
        entries.Add(new BindEntry { label="奔跑",     getter=()=>kb.sprint,    setter=v=>kb.sprint=v });
        entries.Add(new BindEntry { label="换弹",     getter=()=>kb.reload,    setter=v=>kb.reload=v });
        entries.Add(new BindEntry { label="武器槽1",  getter=()=>kb.weapon1,   setter=v=>kb.weapon1=v });
        entries.Add(new BindEntry { label="武器槽2",  getter=()=>kb.weapon2,   setter=v=>kb.weapon2=v });
        entries.Add(new BindEntry { label="武器槽3",  getter=()=>kb.weapon3,   setter=v=>kb.weapon3=v });
        entries.Add(new BindEntry { label="丢弃/卸下",getter=()=>kb.dropWeapon,setter=v=>kb.dropWeapon=v });
        entries.Add(new BindEntry { label="交互/拾取",getter=()=>kb.interact,  setter=v=>kb.interact=v });
        entries.Add(new BindEntry { label="背包",     getter=()=>kb.inventory, setter=v=>kb.inventory=v });
        entries.Add(new BindEntry { label="暂停",     getter=()=>kb.pause,     setter=v=>kb.pause=v });
    }

    private void BuildUI()
    {
        if (contentRoot == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            int idx = i; // 闭包捕获

            var rowGO = new GameObject($"Row_{i}", typeof(RectTransform));
            rowGO.transform.SetParent(contentRoot, false);
            var rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, rowHeight);

            var rowImg = rowGO.AddComponent<Image>();
            rowImg.color = normalColor;
            rowBGs.Add(rowImg);

            // 标签（左侧）
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(0.5f, 1f);
            labelRT.offsetMin = new Vector2(10f, 0f);
            labelRT.offsetMax = Vector2.zero;
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text      = entries[i].label;
            labelTMP.fontSize  = 16;
            labelTMP.color     = Color.white;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.raycastTarget = false;

            // 按键值按钮（右侧）
            var btnGO = new GameObject("KeyBtn", typeof(RectTransform));
            btnGO.transform.SetParent(rowGO.transform, false);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.55f, 0.1f);
            btnRT.anchorMax = new Vector2(0.95f, 0.9f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(() => StartWaiting(idx));

            var valGO = new GameObject("Value", typeof(RectTransform));
            valGO.transform.SetParent(btnGO.transform, false);
            var valRT = valGO.GetComponent<RectTransform>();
            valRT.anchorMin = Vector2.zero;
            valRT.anchorMax = Vector2.one;
            valRT.offsetMin = valRT.offsetMax = Vector2.zero;
            var valTMP = valGO.AddComponent<TextMeshProUGUI>();
            valTMP.text      = entries[i].getter().ToString();
            valTMP.fontSize  = 15;
            valTMP.color     = Color.white;
            valTMP.alignment = TextAlignmentOptions.Center;
            valTMP.raycastTarget = false;
            valueTexts.Add(valTMP);
        }
    }

    private void StartWaiting(int index)
    {
        CancelWaiting();
        waitingIndex = index;
        if (index < valueTexts.Count)
            valueTexts[index].text = "按下新键...";
        if (index < rowBGs.Count)
            rowBGs[index].color = waitColor;
    }

    private void CancelWaiting()
    {
        if (waitingIndex >= 0 && waitingIndex < rowBGs.Count)
            rowBGs[waitingIndex].color = normalColor;
        waitingIndex = -1;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        for (int i = 0; i < entries.Count && i < valueTexts.Count; i++)
            valueTexts[i].text = entries[i].getter().ToString();
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
}
