using UnityEngine;

/// <summary>
/// 弹药数据 ScriptableObject
/// 在 Project 窗口右键 → Create → Ammo Data 创建实例
/// 每种弹药（低级/高级）对应一个资产文件
/// </summary>
[CreateAssetMenu(fileName = "NewAmmoData", menuName = "Game/Ammo Data")]
public class AmmoData : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("弹药显示名称，如「冲锋枪弹药（高级）」")]
    public string ammoName = "弹药";

    [Tooltip("弹药所属武器类型")]
    public AmmoType ammoType = AmmoType.SMG;

    [Tooltip("是否为高级弹药")]
    public bool isHighGrade = false;

    [Header("穿透属性")]
    [Tooltip("穿透等级，决定对护甲的效果")]
    public ArmorPenetrationLevel penetrationLevel = ArmorPenetrationLevel.Low;

    [Header("伤害修正")]
    [Tooltip("基础伤害倍率（最终伤害 = 武器伤害 × 此值）")]
    public float baseDamageMultiplier = 1.0f;

    // ══════════════════════════════════════════════════
    //  核心计算：根据穿透等级 vs 护甲等级，返回最终伤害倍率
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 计算对指定护甲等级目标的实际伤害倍率
    /// </summary>
    /// <param name="targetArmor">目标护甲等级</param>
    /// <returns>最终伤害倍率（乘以武器基础伤害）</returns>
    public float GetDamageMultiplier(ArmorClass targetArmor)
    {
        return baseDamageMultiplier * GetArmorInteractionMult(penetrationLevel, targetArmor);
    }

    /// <summary>
    /// 计算对指定护甲等级目标的护甲耐久消耗倍率
    /// </summary>
    public float GetArmorDurabilityDamageMultiplier(ArmorClass targetArmor)
    {
        return GetArmorDurabilityMult(penetrationLevel, targetArmor);
    }

    // ══════════════════════════════════════════════════
    //  穿透 vs 护甲 交互表
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 穿透等级 vs 护甲等级 → 人体伤害倍率
    ///
    /// 规则：
    ///   穿透 > 护甲：正常伤害（1.0）
    ///   穿透 = 护甲：轻微减伤（0.75）
    ///   穿透 < 护甲：
    ///     低级弹打高级甲：只消耗耐久，人体伤害极低（0.1）
    ///     高级弹打低级甲：正常伤害，但耐久消耗少
    /// </summary>
    private static float GetArmorInteractionMult(
        ArmorPenetrationLevel pen, ArmorClass armor)
    {
        if (armor == ArmorClass.None) return 1.0f;   // 无甲：全额伤害

        int penVal   = (int)pen;
        int armorVal = (int)armor;
        int diff     = penVal - armorVal;

        if (diff >= 2)  return 1.00f;   // 穿透远超护甲：全额
        if (diff == 1)  return 0.90f;   // 穿透略超护甲：轻微减少
        if (diff == 0)  return 0.75f;   // 穿透等于护甲：明显减少
        if (diff == -1) return 0.35f;   // 穿透略低于护甲：大幅减少
        return 0.10f;                   // 穿透远低于护甲：几乎无效，只消耗耐久
    }

    /// <summary>
    /// 穿透等级 vs 护甲等级 → 护甲耐久消耗倍率
    ///
    /// 规则：
    ///   低级弹打高级甲：消耗耐久（1.0，但人体伤害极低）
    ///   高级弹打低级甲：耐久消耗少（0.2，甲很快被穿透）
    ///   穿透等于护甲：正常耐久消耗（0.6）
    /// </summary>
    private static float GetArmorDurabilityMult(
        ArmorPenetrationLevel pen, ArmorClass armor)
    {
        if (armor == ArmorClass.None) return 0f;

        int penVal   = (int)pen;
        int armorVal = (int)armor;
        int diff     = penVal - armorVal;

        if (diff >= 2)  return 0.15f;   // 穿透远超：耐久消耗极少（直接穿过）
        if (diff == 1)  return 0.30f;   // 穿透略超：少量耐久消耗
        if (diff == 0)  return 0.60f;   // 穿透等于：正常耐久消耗
        if (diff == -1) return 0.85f;   // 穿透略低：大量耐久消耗
        return 1.00f;                   // 穿透远低：全额耐久消耗（弹药被甲挡住）
    }
}
