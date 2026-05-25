using UnityEngine;

/// <summary>
/// 修维包数据 ScriptableObject
/// 右键 → Create → Game/Repair Kit 创建实例
/// </summary>
[CreateAssetMenu(fileName = "NewRepairKit", menuName = "Game/Repair Kit")]
public class RepairKit : ScriptableObject
{
    [Header("基础信息")]
    public string        kitName  = "修维包";
    public RepairKitTier tier     = RepairKitTier.Basic;
    public Sprite        icon;

    [Header("修复属性")]
    [Tooltip("此修维包能修复的最高护甲等级")]
    public ArmorClass maxArmorClass = ArmorClass.Light;

    [Tooltip("使用时间（秒）")]
    public float useTime = 3f;

    /// <summary>
    /// 判断此修维包是否可用于指定护甲
    /// 规则：修维包等级 >= 护甲所需最低修维包等级，且护甲等级 <= 修维包最高可修等级
    /// 特种修维套件（Special）不可用于任何护甲
    /// </summary>
    public bool CanRepair(ArmorData armor)
    {
        if (armor == null) return false;
        if (tier == RepairKitTier.Special) return false;          // 特种套件不可用
        if ((int)tier < (int)armor.minRepairKitTier) return false; // 等级不够
        if ((int)armor.armorClass > (int)maxArmorClass) return false; // 护甲等级超出范围
        return true;
    }
}
