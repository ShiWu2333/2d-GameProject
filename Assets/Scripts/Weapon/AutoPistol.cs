using UnityEngine;

/// <summary>
/// 自动手枪
/// 射速极快 | 伤害低 | 弹容大 | 射程极近 | 后坐力小 | 持枪移速极快
/// 弹药：SMG弹（与冲锋枪共用）
/// 颜色：紫色
/// </summary>
public class AutoPistol : WeaponBase
{
    protected override void Awake()
    {
        weaponName     = "自动手枪";
        ammoType       = AmmoType.SMG;    // 共用冲锋枪弹

        damage         = 9f;
        fireRate       = 0.05f;   // 20发/秒
        maxAmmo        = 33;
        reloadTime     = 1.5f;
        range          = 10f;     // 射程极近
        recoil         = 1.5f;
        baseSpread     = 4f;
        moveSpeedMult  = 0.95f;   // 持枪移速极快
        isSemiAuto     = false;

        bulletSpeed    = 16f;
        pelletsPerShot = 1;

        base.Awake();
    }
}
