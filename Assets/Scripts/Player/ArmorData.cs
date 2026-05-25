using UnityEngine;

/// <summary>
/// 护甲数据 ScriptableObject
/// 右键 → Create → Game/Armor Data 创建实例
/// </summary>
[CreateAssetMenu(fileName = "NewArmorData", menuName = "Game/Armor Data")]
public class ArmorData : ScriptableObject
{
    [Header("基础信息")]
    public string    armorName  = "护甲";
    public ArmorType armorType  = ArmorType.LightArmor_Light;
    public Sprite    icon;

    [Header("防护属性")]
    [Tooltip("防护等级，决定对弹药穿透的抵抗能力")]
    public ArmorClass armorClass = ArmorClass.Light;

    [Tooltip("最大耐久值")]
    public float maxDurability = 60f;

    [Tooltip("护甲完好时最大伤害吸收率（0~1）")]
    [Range(0f, 1f)]
    public float maxAbsorption = 0.55f;

    [Header("负面效果（降低移速/奔跑速度）")]
    [Tooltip("步行移速倍率惩罚（1 = 无惩罚，0.8 = 减速20%）")]
    [Range(0.1f, 1f)]
    public float walkSpeedPenalty   = 0.95f;

    [Tooltip("奔跑移速倍率惩罚（1 = 无惩罚，0.7 = 减速30%）")]
    [Range(0.1f, 1f)]
    public float sprintSpeedPenalty = 0.95f;

    [Header("可修次数")]
    [Tooltip("最大可修次数。-1 表示无限次")]
    public int maxRepairCount = 3;

    [Tooltip("每次修复恢复的耐久量（占最大耐久的比例）")]
    [Range(0.1f, 1f)]
    public float repairRestoreRatio = 0.5f;

    [Tooltip("可使用的最低修维包等级")]
    public RepairKitTier minRepairKitTier = RepairKitTier.Basic;
}
