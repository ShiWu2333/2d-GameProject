/// <summary>
/// 物品/容器标签系统
/// 用于控制容器能生成什么类型的物品
/// </summary>
public static class LootTags
{
    public const string Ammo   = "弹药";
    public const string Weapon = "武器";
}

/// <summary>
/// 容器大小枚举
/// </summary>
public enum ContainerSize
{
    Small  = 2,  // 小容器：2行，标签=弹药
    Medium = 3,  // 中容器：3行，标签=武器
    Large  = 4,  // 大容器：4行，标签=武器
}
