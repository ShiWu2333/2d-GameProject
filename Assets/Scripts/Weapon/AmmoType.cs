/// <summary>
/// 弹药类型枚举
/// 每种武器消耗对应类型弹药
/// </summary>
public enum AmmoType
{
    None,           // 无限弹药（刀）
    Pistol,         // 手枪弹：自动手枪
    SMG,            // 冲锋枪弹：冲锋枪、自动手枪（共用）
    Rifle,          // 步枪弹：突击步枪、射手步枪
    Shotgun,        // 霰弹：连发霰弹枪
    LMG,            // 机枪弹：轻机枪
}
