using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 武器栏 HUD 搭建
/// 负责：Canvas 下的 WeaponSlotPanel 及其子物体创建和配置
/// </summary>
public static class WeaponHUDSetup
{
    public struct Options
    {
        public float slotSize, slotSpacing, padding;
        public Color colSelected, colNormal, colEmpty, colPanel;
    }

    public static void Run(
        Canvas canvas, WeaponSlotSystem slotSys,
        int slot1Index, int slot2Index, int meleeIndex,
        Options opt)
    {
        var canvasTf = canvas.transform;

        // 面板根节点
        var panelGO   = EditorHelper.FindOrMakeChild(canvasTf, "WeaponSlotPanel");
        var panelRect = EditorHelper.GetOrAdd<RectTransform>(panelGO);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot     = Vector2.zero;
        float totalH = opt.slotSize * 3 + opt.slotSpacing * 2 + opt.padding * 2;
        float totalW = opt.slotSize + opt.padding * 2;
        panelRect.sizeDelta        = new Vector2(totalW, totalH);
        panelRect.anchoredPosition = new Vector2(16f, 16f);

        var panelImg = EditorHelper.GetOrAdd<Image>(panelGO);
        panelImg.color = opt.colPanel;

        var hud = EditorHelper.GetOrAdd<WeaponSlotHUD>(panelGO);
        Undo.RecordObject(hud, "Setup HUD");
        hud.selectedColor  = opt.colSelected;
        hud.normalColor    = opt.colNormal;
        hud.emptySlotColor = opt.colEmpty;
        hud.slotSystemRef  = slotSys;
        if (hud.slots == null || hud.slots.Length != 3)
            hud.slots = new WeaponSlotHUD.SlotUI[3];

        string[] slotNames = { "Slot_Primary1", "Slot_Primary2", "Slot_Melee" };
        string[] keyHints  = { "1", "2", "3" };
        string[] labels    =
        {
            PrefabBuilder.WeaponDefs[slot1Index].label,
            PrefabBuilder.WeaponDefs[slot2Index].label,
            PrefabBuilder.WeaponDefs[meleeIndex].label,
        };

        for (int i = 0; i < 3; i++)
        {
            int visualOrder = 2 - i;
            var slotGO   = EditorHelper.FindOrMakeChild(panelRect.transform, slotNames[i]);
            var slotRect = EditorHelper.GetOrAdd<RectTransform>(slotGO);
            slotRect.anchorMin = Vector2.zero;
            slotRect.anchorMax = Vector2.zero;
            slotRect.pivot     = Vector2.zero;
            slotRect.sizeDelta = new Vector2(opt.slotSize, opt.slotSize);
            slotRect.anchoredPosition = new Vector2(opt.padding,
                opt.padding + visualOrder * (opt.slotSize + opt.slotSpacing));

            var bg = EditorHelper.GetOrAdd<Image>(slotGO);
            bg.color = (i == 0) ? opt.colSelected : opt.colNormal;

            // 图标
            var iconGO   = EditorHelper.FindOrMakeChild(slotRect.transform, "WeaponIcon");
            var iconRect = EditorHelper.GetOrAdd<RectTransform>(iconGO);
            iconRect.anchorMin = new Vector2(0.1f, 0.22f);
            iconRect.anchorMax = new Vector2(0.9f, 0.88f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            var iconImg = EditorHelper.GetOrAdd<Image>(iconGO);
            iconImg.enabled = false;

            // 按键提示
            var keyGO   = EditorHelper.FindOrMakeChild(slotRect.transform, "KeyHint");
            var keyRect = EditorHelper.GetOrAdd<RectTransform>(keyGO);
            keyRect.anchorMin = new Vector2(0f, 0.72f);
            keyRect.anchorMax = new Vector2(0.45f, 1f);
            keyRect.offsetMin = new Vector2(4f, -2f);
            keyRect.offsetMax = new Vector2(0f, -2f);
            var keyTMP = EditorHelper.GetOrAdd<TextMeshProUGUI>(keyGO);
            keyTMP.text      = keyHints[i];
            keyTMP.fontSize  = 14;
            keyTMP.fontStyle = FontStyles.Bold;
            keyTMP.color     = Color.white;
            keyTMP.alignment = TextAlignmentOptions.TopLeft;

            // 武器名
            var nameGO   = EditorHelper.FindOrMakeChild(slotRect.transform, "WeaponName");
            var nameRect = EditorHelper.GetOrAdd<RectTransform>(nameGO);
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.28f);
            nameRect.offsetMin = new Vector2(2f, 2f);
            nameRect.offsetMax = new Vector2(-2f, 0f);
            var nameTMP = EditorHelper.GetOrAdd<TextMeshProUGUI>(nameGO);
            nameTMP.text              = labels[i];
            nameTMP.fontSize          = 11;
            nameTMP.color             = Color.white;
            nameTMP.alignment         = TextAlignmentOptions.Center;
            nameTMP.enableWordWrapping = false;
            nameTMP.overflowMode      = TextOverflowModes.Ellipsis;

            // 填入 HUD slots
            if (hud.slots[i] == null) hud.slots[i] = new WeaponSlotHUD.SlotUI();
            hud.slots[i].root           = slotGO;
            hud.slots[i].background     = bg;
            hud.slots[i].weaponIcon     = iconImg;
            hud.slots[i].weaponNameText = nameTMP;
            hud.slots[i].keyHintText    = keyTMP;

            EditorUtility.SetDirty(slotGO);
        }

        EditorUtility.SetDirty(panelGO);
        EditorUtility.SetDirty(hud);
    }
}
