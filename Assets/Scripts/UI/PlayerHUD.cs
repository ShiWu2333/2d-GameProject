using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 玩家 HUD：血量条、体力条、弹药显示
/// 挂在 Canvas 下的 HUD GameObject 上
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("血量")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;

    [Header("体力")]
    public Slider staminaBar;
    public TextMeshProUGUI staminaText;

    [Header("弹药")]
    public TextMeshProUGUI ammoText;             // 格式：当前/最大

    [Header("换弹进度条")]
    [Tooltip("换弹进度条根物体（平时隐藏）")]
    public GameObject reloadBarRoot;
    [Tooltip("换弹进度条填充Image（fillAmount控制）")]
    public Image reloadBarFill;

    [Header("背包UI根节点")]
    public GameObject inventoryPanel;            // M键控制显示/隐藏

    [Header("交互提示")]
    public TextMeshProUGUI interactionPromptText; // 如"[F] 搜索"

    private PlayerStats stats;
    private InventorySystem inventory;
    private WeaponSlotSystem weaponSlotSystem;

    void Start()
    {
        // 找到玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("PlayerHUD: 找不到 Tag 为 Player 的对象");
            return;
        }

        stats = player.GetComponent<PlayerStats>();
        inventory = player.GetComponent<InventorySystem>();
        weaponSlotSystem = player.GetComponent<WeaponSlotSystem>();

        // 注册事件
        if (stats != null)
        {
            stats.onHealthChanged.AddListener(UpdateHealth);
            stats.onStaminaChanged.AddListener(UpdateStamina);
        }

        // 初始化显示
        UpdateHealth(stats != null ? stats.currentHealth : 100f,
                     stats != null ? stats.maxHealth : 100f);
        UpdateStamina(stats != null ? stats.currentStamina : 100f,
                      stats != null ? stats.maxStamina : 100f);

        // 背包默认关闭
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // 自动创建换弹进度条（如果未手动赋值）
        if (reloadBarRoot == null)
            CreateReloadBar();
    }

    void Update()
    {
        // 同步背包面板显示状态
        if (inventory != null && inventoryPanel != null)
            inventoryPanel.SetActive(inventory.IsOpen);

        // 同步弹药显示
        UpdateAmmo();

        // 同步换弹进度条
        UpdateReloadBar();
    }

    // ── 血量更新 ──────────────────────────────────────
    private void UpdateHealth(float current, float max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
        }
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    // ── 体力更新 ──────────────────────────────────────
    private void UpdateStamina(float current, float max)
    {
        if (staminaBar != null)
        {
            staminaBar.maxValue = max;
            staminaBar.value = current;
        }
        if (staminaText != null)
            staminaText.text = $"{Mathf.CeilToInt(current)}";
    }

    // ── 弹药更新 ──────────────────────────────────────
    private void UpdateAmmo()
    {
        if (ammoText == null) return;

        // 优先从武器槽系统读取当前手持武器
        WeaponBase weapon = weaponSlotSystem != null
            ? weaponSlotSystem.CurrentWeapon
            : (inventory != null ? inventory.equippedWeapon : null);

        if (weapon != null)
            ammoText.text = $"{weapon.currentAmmo} / {weapon.maxAmmo}";
        else
            ammoText.text = "-- / --";
    }

    // ── 交互提示 ──────────────────────────────────────
    public void ShowInteractionPrompt(string message)
    {
        if (interactionPromptText == null) return;
        interactionPromptText.gameObject.SetActive(true);
        interactionPromptText.text = message;
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptText == null) return;
        interactionPromptText.gameObject.SetActive(false);
    }

    // ── 换弹进度条 ────────────────────────────────────
    private RectTransform reloadFillRT;

    private void UpdateReloadBar()
    {
        WeaponBase weapon = weaponSlotSystem != null
            ? weaponSlotSystem.CurrentWeapon
            : (inventory != null ? inventory.equippedWeapon : null);

        bool showReload = weapon != null && weapon.isReloading;

        if (reloadBarRoot != null)
            reloadBarRoot.SetActive(showReload);

        if (showReload && reloadFillRT != null)
        {
            // 通过anchorMax.x控制宽度比例（0~1）
            float progress = weapon.ReloadProgress;
            reloadFillRT.anchorMax = new Vector2(progress, 1f);
        }
    }

    /// <summary>
    /// 运行时自动创建换弹进度条（放在弹药文本下方）
    /// </summary>
    private void CreateReloadBar()
    {
        Transform parent = ammoText != null ? ammoText.transform.parent : transform;

        // 创建进度条根节点
        reloadBarRoot = new GameObject("ReloadBar");
        reloadBarRoot.transform.SetParent(parent, false);

        var rootRT = reloadBarRoot.AddComponent<RectTransform>();
        if (ammoText != null)
        {
            var ammoRT = ammoText.GetComponent<RectTransform>();
            rootRT.anchorMin = ammoRT.anchorMin;
            rootRT.anchorMax = ammoRT.anchorMax;
            rootRT.pivot     = ammoRT.pivot;
            rootRT.anchoredPosition = ammoRT.anchoredPosition + new Vector2(0f, -22f);
            rootRT.sizeDelta = new Vector2(ammoRT.sizeDelta.x > 0 ? ammoRT.sizeDelta.x : 120f, 6f);
        }
        else
        {
            rootRT.anchorMin = new Vector2(1f, 0f);
            rootRT.anchorMax = new Vector2(1f, 0f);
            rootRT.pivot     = new Vector2(1f, 0f);
            rootRT.anchoredPosition = new Vector2(-16f, 50f);
            rootRT.sizeDelta = new Vector2(120f, 6f);
        }

        // 背景（深灰）
        var bgImg = reloadBarRoot.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);

        // 填充条（白色，通过anchorMax控制宽度）
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(reloadBarRoot.transform, false);

        reloadFillRT = fillGO.AddComponent<RectTransform>();
        reloadFillRT.anchorMin = Vector2.zero;
        reloadFillRT.anchorMax = new Vector2(0f, 1f); // 初始宽度为0
        reloadFillRT.offsetMin = Vector2.zero;
        reloadFillRT.offsetMax = Vector2.zero;

        reloadBarFill = fillGO.AddComponent<Image>();
        reloadBarFill.color = Color.white;

        // 初始隐藏
        reloadBarRoot.SetActive(false);
    }
}
