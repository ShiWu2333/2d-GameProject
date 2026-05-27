using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 物品/武器详细信息面板
/// 锁定物品时在背包右侧展开
/// 武器显示武器详细属性，普通物品显示基础信息
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

    [Header("弹药详细栏（仅弹药时显示）")]
    public GameObject      ammoStatsGroup;
    public TextMeshProUGUI ammoCountText;
    public TextMeshProUGUI ammoPenetrationText;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>显示普通物品信息</summary>
    public void ShowItem(InventoryItem item)
    {
        gameObject.SetActive(true);

        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemIconImage != null)
        {
            if (item.icon != null)
            {
                itemIconImage.sprite  = item.icon;
                itemIconImage.color   = Color.white;
                itemIconImage.enabled = true;
            }
            else
            {
                itemIconImage.enabled = false;
            }
        }

        // 弹药物品
        if (item is AmmoItem ammo)
        {
            if (weaponStatsGroup != null) weaponStatsGroup.SetActive(false);
            if (ammoStatsGroup != null) ammoStatsGroup.SetActive(true);
            if (ammoCountText != null) ammoCountText.text = $"数量：{ammo.ammoAmount} / {AmmoItem.MaxPerStack}";
            if (ammoPenetrationText != null) ammoPenetrationText.text = $"弹药类型：{ammo.ammoType}";
            if (descriptionText != null) descriptionText.text = $"{ammo.itemName}";
        }
        else
        {
            // 普通物品
            if (weaponStatsGroup != null) weaponStatsGroup.SetActive(false);
            if (ammoStatsGroup != null) ammoStatsGroup.SetActive(false);
            if (descriptionText != null) descriptionText.text = $"数量：{item.quantity}";
        }
    }

    /// <summary>显示武器详细信息</summary>
    public void ShowWeapon(WeaponBase weapon)
    {
        gameObject.SetActive(true);

        if (itemNameText != null) itemNameText.text = weapon.weaponName;
        if (itemIconImage != null) itemIconImage.enabled = false;
        if (descriptionText != null) descriptionText.text = "";

        if (ammoStatsGroup != null) ammoStatsGroup.SetActive(false);
        if (weaponStatsGroup != null) weaponStatsGroup.SetActive(true);

        if (damageText != null)       damageText.text       = $"伤害：{weapon.damage:F0}";
        if (fireRateText != null)     fireRateText.text     = $"射速：{(1f / weapon.fireRate):F1} 发/秒";
        if (ammoCapacityText != null) ammoCapacityText.text = $"弹容：{weapon.currentAmmo} / {weapon.maxAmmo}";
        if (rangeText != null)        rangeText.text        = $"射程：{weapon.range:F0}";
        if (recoilText != null)       recoilText.text       = $"后坐力：{weapon.recoil:F1}";
        if (moveSpeedText != null)    moveSpeedText.text    = $"持枪移速：{weapon.moveSpeedMult * 100f:F0}%";
        if (ammoTypeText != null)     ammoTypeText.text     = $"弹药类型：{weapon.ammoType}";
        if (penetrationText != null)
        {
            string pen = weapon.currentAmmoData != null
                ? weapon.currentAmmoData.penetrationLevel.ToString()
                : "无";
            penetrationText.text = $"穿透等级：{pen}";
        }
    }

    /// <summary>隐藏面板</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
