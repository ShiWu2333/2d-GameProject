using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 物品/武器详细信息面板
/// 锁定物品时在背包右侧展开
/// 武器显示完整属性，弹药/医疗/普通物品分别显示对应信息
/// </summary>
public class ItemDetailPanel : MonoBehaviour
{
    [Header("通用信息")]
    public TextMeshProUGUI itemNameText;
    public Image           itemIconImage;
    public TextMeshProUGUI descriptionText;

    [Header("武器详细栏（仅武器时显示）")]
    public GameObject      weaponStatsGroup;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI fireRateText;
    public TextMeshProUGUI ammoCapacityText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI recoilText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI ammoTypeText;
    public TextMeshProUGUI penetrationText;
    // 新增详细属性
    public TextMeshProUGUI spreadText;
    public TextMeshProUGUI moveSpreadText;
    public TextMeshProUGUI aimSpreadText;
    public TextMeshProUGUI aimSpeedText;
    public TextMeshProUGUI reloadTimeText;
    public TextMeshProUGUI fireModeText;

    [Header("弹药详细栏（仅弹药时显示）")]
    public GameObject      ammoStatsGroup;
    public TextMeshProUGUI ammoCountText;
    public TextMeshProUGUI ammoPenetrationText;

    // 滚动支持
    private ScrollRect scrollRect;
    private RectTransform contentRT;

    void Awake()
    {
        gameObject.SetActive(false);
        EnsureScrollSetup();
    }

    /// <summary>
    /// 确保面板有ScrollRect支持滚轮滚动
    /// </summary>
    private void EnsureScrollSetup()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null) return; // 已有，跳过

        // 把现有子物体移入一个Content容器
        var viewport = transform.Find("Viewport");
        var content  = transform.Find("Viewport/Content");

        if (viewport == null)
        {
            // 创建Viewport（遮罩区域）
            var vpGO = new GameObject("Viewport", typeof(RectTransform));
            vpGO.transform.SetParent(transform, false);
            var vpRT = vpGO.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(4f, 4f);
            vpRT.offsetMax = new Vector2(-4f, -4f);

            // Mask组件裁剪超出内容
            var mask = vpGO.AddComponent<RectMask2D>();
            viewport = vpGO.transform;

            // 创建Content容器
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewport, false);
            contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.offsetMin = new Vector2(0f, 0f);
            contentRT.offsetMax = new Vector2(0f, 0f);
            contentRT.sizeDelta = new Vector2(0f, 500f); // 初始高度，后面动态调整
            content = contentGO.transform;

            // 把现有子物体（除Viewport外）移入Content
            var children = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child != viewport) children.Add(child);
            }
            foreach (var child in children)
                child.SetParent(content, false);
        }
        else
        {
            contentRT = content != null ? content.GetComponent<RectTransform>() : null;
        }

        // 添加ScrollRect
        scrollRect = gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport    = viewport.GetComponent<RectTransform>();
        scrollRect.content     = contentRT;
        scrollRect.horizontal  = false;
        scrollRect.vertical    = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
    }

    /// <summary>显示普通物品信息</summary>
    public void ShowItem(InventoryItem item)
    {
        gameObject.SetActive(true);

        // 如果是弹药物品且图标为空，自动生成图标
        if (item.icon == null && item is AmmoItem ammoForIcon)
            item.icon = AmmoIconManager.GetAmmoIcon(ammoForIcon.ammoType, ammoForIcon.isHighGrade);

        SetName(item.itemName);
        SetIcon(item.icon);

        // 弹药物品
        if (item is AmmoItem ammo)
        {
            ShowGroup(false, true);
            if (ammoCountText != null)
                ammoCountText.text = $"数量：{ammo.ammoAmount} / {AmmoItem.MaxPerStack}";
            if (ammoPenetrationText != null)
            {
                string pen = ammo.ammoData != null ? ammo.ammoData.penetrationLevel.ToString() : "—";
                ammoPenetrationText.text = $"穿透等级：{pen}";
            }
            string grade = ammo.isHighGrade ? "高级" : "低级";
            SetDesc($"弹药类型：{GetAmmoTypeName(ammo.ammoType)}\n等级：{grade}");
        }
        else if (item is MedicalItem med)
        {
            ShowGroup(false, false);
            string durInfo = med.isSingleUse
                ? $"一次性（剩余 {med.quantity}）"
                : $"耐久：{med.currentDurability:F0} / {med.maxDurability:F0}";
            SetDesc($"{med.description}\n\n回复血量：{med.healAmount:F0}\n{durInfo}\n\n[右键使用]");
        }
        else
        {
            ShowGroup(false, false);
            SetDesc($"数量：{item.quantity}");
        }

        // 动态调整Content高度
        if (contentRT != null)
        {
            contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, CalcContentHeight());
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void LateUpdate()
    {
        // 延迟修正高度（TMP需要一帧计算preferredHeight）
        if (contentRT != null && gameObject.activeSelf)
        {
            float h = CalcContentHeight();
            if (Mathf.Abs(contentRT.sizeDelta.y - h) > 1f)
                contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, h);
        }
    }

    /// <summary>显示武器完整详细信息</summary>
    public void ShowWeapon(WeaponBase weapon)
    {
        gameObject.SetActive(true);

        SetName(weapon.weaponName);
        SetIcon(null);

        // 确保武器详细组存在
        if (weaponStatsGroup == null)
        {
            weaponStatsGroup = new GameObject("WeaponStats", typeof(RectTransform));
            weaponStatsGroup.transform.SetParent(transform, false);
            var rt = weaponStatsGroup.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(5f, 30f);
            rt.offsetMax = new Vector2(-5f, -30f);
        }

        ShowGroup(true, false);

        // 如果weaponStatsGroup下没有足够的文本组件，自动创建
        EnsureWeaponTexts();

        // ── 基础属性 ──
        SetText(damageText,       $"伤害：{weapon.damage:F0}");
        SetText(fireRateText,     $"射速：{(1f / weapon.fireRate):F1} 发/秒");
        SetText(ammoCapacityText, $"弹容：{weapon.currentAmmo} / {weapon.maxAmmo}");
        SetText(rangeText,        $"射程：{weapon.range:F0}m");
        SetText(reloadTimeText,   $"换弹：{weapon.reloadTime:F1}s");

        // ── 精准度 ──
        SetText(recoilText,       $"后坐力：{weapon.recoil:F1}");
        SetText(spreadText,       $"基础散射：{weapon.baseSpread:F1}°");
        SetText(moveSpreadText,   $"移动散射：+{weapon.moveSpreadBonus:F1}°");
        SetText(aimSpreadText,    $"瞄准精度：×{weapon.aimSpreadMult:F2}");

        // ── 机动性 ──
        SetText(moveSpeedText,    $"持枪移速：{weapon.moveSpeedMult * 100f:F0}%");
        SetText(aimSpeedText,     $"瞄准移速：{weapon.aimMoveSpeedMult * 100f:F0}%");

        // ── 弹药 ──
        SetText(ammoTypeText,     $"弹药：{GetAmmoTypeName(weapon.ammoType)}");
        if (penetrationText != null)
        {
            string pen = weapon.currentAmmoData != null
                ? weapon.currentAmmoData.penetrationLevel.ToString()
                : "—";
            penetrationText.text = $"穿透等级：{pen}";
        }

        // ── 射击模式 ──
        string mode = weapon.isSemiAuto ? "半自动" : "全自动";
        if (weapon.pelletsPerShot > 1)
            mode += $" ({weapon.pelletsPerShot}发/射)";
        SetText(fireModeText, $"模式：{mode}");

        // ── 特性标签（描述区域，与属性之间留间距）──
        if (descriptionText != null)
        {
            // 把descriptionText移到属性组下方，留出间距
            var descRT = descriptionText.GetComponent<RectTransform>();
            if (descRT != null && weaponStatsGroup != null)
            {
                var wgRT = weaponStatsGroup.GetComponent<RectTransform>();
                float below = wgRT.anchoredPosition.y - wgRT.sizeDelta.y - 16f;
                descRT.anchoredPosition = new Vector2(descRT.anchoredPosition.x, below);
            }

            string summary = BuildWeaponSummary(weapon);
            descriptionText.text = summary;
        }

        // 重置滚动位置到顶部
        if (contentRT != null)
        {
            contentRT.sizeDelta = new Vector2(contentRT.sizeDelta.x, CalcContentHeight());
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// 确保武器详细栏有所有需要的文本组件，缺少的自动创建
    /// </summary>
    private void EnsureWeaponTexts()
    {
        if (weaponStatsGroup == null) return;
        Transform parent = weaponStatsGroup.transform;

        damageText       = EnsureTextChild(parent, "DamageText",       damageText);
        fireRateText     = EnsureTextChild(parent, "FireRateText",     fireRateText);
        ammoCapacityText = EnsureTextChild(parent, "AmmoCapText",      ammoCapacityText);
        rangeText        = EnsureTextChild(parent, "RangeText",        rangeText);
        reloadTimeText   = EnsureTextChild(parent, "ReloadTimeText",   reloadTimeText);
        recoilText       = EnsureTextChild(parent, "RecoilText",       recoilText);
        spreadText       = EnsureTextChild(parent, "SpreadText",       spreadText);
        moveSpreadText   = EnsureTextChild(parent, "MoveSpreadText",   moveSpreadText);
        aimSpreadText    = EnsureTextChild(parent, "AimSpreadText",    aimSpreadText);
        moveSpeedText    = EnsureTextChild(parent, "MoveSpeedText",    moveSpeedText);
        aimSpeedText     = EnsureTextChild(parent, "AimSpeedText",     aimSpeedText);
        ammoTypeText     = EnsureTextChild(parent, "AmmoTypeText",     ammoTypeText);
        penetrationText  = EnsureTextChild(parent, "PenetrationText",  penetrationText);
        fireModeText     = EnsureTextChild(parent, "FireModeText",     fireModeText);
    }

    private TextMeshProUGUI EnsureTextChild(Transform parent, string name, TextMeshProUGUI existing)
    {
        if (existing != null) return existing;

        // 查找已有子物体
        Transform found = parent.Find(name);
        if (found != null)
        {
            var tmp = found.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }

        // 创建新的
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        // 根据已有子物体数量向下排列
        int childIndex = parent.childCount - 1;
        rt.anchoredPosition = new Vector2(5f, -childIndex * 20f);
        rt.sizeDelta = new Vector2(-10f, 20f);

        var tmp2 = go.AddComponent<TextMeshProUGUI>();
        tmp2.fontSize  = 13;
        tmp2.color     = Color.white;
        tmp2.alignment = TextAlignmentOptions.Left;
        tmp2.enableWordWrapping = false;
        tmp2.overflowMode = TextOverflowModes.Ellipsis;

        return tmp2;
    }

    /// <summary>隐藏面板</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════
    //  内部工具
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 根据实际显示的文本行数计算Content总高度
    /// </summary>
    private float CalcContentHeight()
    {
        float height = 20f; // 顶部间距

        // 武器名
        if (itemNameText != null && itemNameText.gameObject.activeInHierarchy)
            height += itemNameText.preferredHeight + 8f;

        // 武器属性组
        if (weaponStatsGroup != null && weaponStatsGroup.activeInHierarchy)
        {
            foreach (Transform child in weaponStatsGroup.transform)
            {
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null && tmp.gameObject.activeInHierarchy && !string.IsNullOrEmpty(tmp.text))
                    height += 22f;
            }
            height += 12f; // 属性组后的间距
        }

        // 描述/特性标签
        if (descriptionText != null && !string.IsNullOrEmpty(descriptionText.text))
            height += descriptionText.preferredHeight + 24f; // 16px间距 + 文本高度

        // 弹药属性组
        if (ammoStatsGroup != null && ammoStatsGroup.activeInHierarchy)
            height += 60f;

        return Mathf.Max(height + 40f, 100f); // 底部留40px余量，最小高度100
    }

    private void SetName(string name)
    {
        if (itemNameText != null) itemNameText.text = name;
    }

    private void SetIcon(Sprite icon)
    {
        if (itemIconImage == null) return;
        if (icon != null)
        {
            itemIconImage.sprite  = icon;
            itemIconImage.color   = Color.white;
            itemIconImage.enabled = true;
        }
        else
        {
            itemIconImage.enabled = false;
        }
    }

    private void SetDesc(string text)
    {
        if (descriptionText != null) descriptionText.text = text;
    }

    private void ShowGroup(bool weapon, bool ammo)
    {
        if (weaponStatsGroup != null) weaponStatsGroup.SetActive(weapon);
        if (ammoStatsGroup != null)   ammoStatsGroup.SetActive(ammo);
    }

    private static void SetText(TextMeshProUGUI tmp, string text)
    {
        if (tmp != null) tmp.text = text;
    }

    private static string GetAmmoTypeName(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:  return "手枪弹";
            case AmmoType.SMG:     return "冲锋枪弹";
            case AmmoType.Rifle:   return "步枪弹";
            case AmmoType.Shotgun: return "霰弹";
            case AmmoType.LMG:     return "机枪弹";
            case AmmoType.None:    return "无（近战）";
            default:               return "未知";
        }
    }

    /// <summary>生成武器简评文本</summary>
    private static string BuildWeaponSummary(WeaponBase weapon)
    {
        // DPS估算
        float dps = weapon.damage * (1f / weapon.fireRate) * weapon.pelletsPerShot;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // 顶部空行作为与属性文本的间距
        sb.AppendLine();
        sb.AppendLine("─────────────────");
        sb.AppendLine();
        sb.AppendLine($"<b>DPS估算：{dps:F0}</b>");
        sb.AppendLine();

        // 特性标签
        if (weapon.moveSpeedMult >= 0.9f) sb.AppendLine("• 高机动");
        else if (weapon.moveSpeedMult <= 0.6f) sb.AppendLine("• 重型武器");

        if (weapon.baseSpread <= 1.5f) sb.AppendLine("• 高精度");
        else if (weapon.baseSpread >= 8f) sb.AppendLine("• 大范围");

        if (weapon.recoil <= 2f) sb.AppendLine("• 低后坐力");
        else if (weapon.recoil >= 5f) sb.AppendLine("• 高后坐力");

        if (weapon.isSemiAuto) sb.AppendLine("• 半自动");
        if (weapon.pelletsPerShot > 1) sb.AppendLine($"• 多弹丸 ×{weapon.pelletsPerShot}");

        return sb.ToString();
    }
}
