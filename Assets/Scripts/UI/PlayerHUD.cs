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
    }

    void Update()
    {
        // 同步背包面板显示状态
        if (inventory != null && inventoryPanel != null)
            inventoryPanel.SetActive(inventory.IsOpen);

        // 同步弹药显示
        UpdateAmmo();
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
}
