/// <summary>
/// 穿透等级枚举
/// </summary>
public enum ArmorPenetrationLevel
{
    None   = 0,
    Low    = 1,
    Medium = 2,
    High   = 3,
    Elite  = 4,
}

/// <summary>
/// 护甲防护等级枚举（对应弹药穿透计算）
/// </summary>
public enum ArmorClass
{
    None    = 0,   // 无甲
    Light   = 1,   // 低级防护
    Medium  = 2,   // 中级防护
    Heavy   = 3,   // 高级防护
    Elite   = 4,   // 特级防护
}

/// <summary>
/// 护甲类型枚举（8种具体护甲）
/// </summary>
public enum ArmorType
{
    LightArmor_Light,    // 低级护甲（轻型）
    LightArmor_Heavy,    // 低级护甲（重型）
    MediumArmor_Light,   // 中级护甲（轻型）
    MediumArmor_Heavy,   // 中级护甲（重型）
    HeavyArmor_Light,    // 高级护甲（轻型）
    HeavyArmor_Heavy,    // 高级护甲（重型）
    CompoundHeavy,       // 复合重装
    CustomLight,         // 定制轻装
}

/// <summary>
/// 修维包等级枚举
/// </summary>
public enum RepairKitTier
{
    Basic,    // 基础修维包：可修低级护甲
    Advanced, // 高级修维包：可修中级及以下护甲
    Elite,    // 精英修维包：可修高级及以下护甲
    Special,  // 特种修维套件：不可用（特级护甲专属，无法被修）
}
