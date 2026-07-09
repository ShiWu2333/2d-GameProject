using UnityEngine;

/// <summary>
/// 连发霰弹枪
/// 射速中 | 伤害极高（多弹丸） | 弹容小 | 射程极近 | 后坐力中 | 持枪移速快
/// 全自动，每发6颗弹丸
/// 弹药：霰弹
/// 颜色：橙色
/// </summary>
public class AutoShotgun : WeaponBase
{
    protected override void Awake()
    {
        weaponName     = "连发霰弹枪";
        ammoType       = AmmoType.Shotgun;

        damage         = 12f;     // 单颗弹丸伤害，6颗合计72
        fireRate       = 0.25f;   // ~4发/秒
        maxAmmo        = 8;
        reloadTime     = 2.5f;
        range          = 8f;      // 射程极近
        recoil         = 5f;
        baseSpread     = 10f;     // 大散射
        moveSpreadBonus= 2f;      // 移动散射小（霰弹本身散射大）
        moveSpeedMult  = 0.88f;   // 持枪移速快
        aimSpreadMult  = 0.5f;    // 瞄准收缩中等（霰弹不适合精确）
        aimMoveSpeedMult= 0.55f;  // 瞄准移速较快
        isSemiAuto     = false;

        bulletSpeed    = 16f;
        pelletsPerShot = 6;       // 每发6颗

        base.Awake();
    }
}
