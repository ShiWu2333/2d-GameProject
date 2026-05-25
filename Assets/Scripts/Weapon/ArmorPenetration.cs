/// <summary>
/// 穿透等级枚举
/// 数值越大穿透能力越强
/// </summary>
public enum ArmorPenetrationLevel
{
    None   = 0,   // 无穿透（刀等近战）
    Low    = 1,   // 低级穿透
    Medium = 2,   // 中级穿透
    High   = 3,   // 高级穿透
    Elite  = 4,   // 特级穿透
}

/// <summary>
/// 护甲等级枚举
/// 数值越大防护越强
/// </summary>
public enum ArmorClass
{
    None   = 0,   // 无甲
    Light  = 1,   // 轻甲（低级）
    Medium = 2,   // 中甲（中级）
    Heavy  = 3,   // 重甲（高级）
}
