using UnityEngine;

/// <summary>
/// 冲锋枪 (SMG)
/// 射速快 | 伤害中 | 弹容中 | 射程近 | 后坐力小 | 持枪移速快
/// 弹药：SMG弹
/// 颜色：青色
/// </summary>
public class SMG : WeaponBase
{
    protected override void Awake()
    {
        weaponName     = "冲锋枪";
        ammoType       = AmmoType.SMG;

        damage         = 14f;
        fireRate       = 0.08f;   // ~12.5发/秒
        maxAmmo        = 32;
        reloadTime     = 1.8f;
        range          = 12f;
        recoil         = 1.8f;
        baseSpread     = 3f;
        moveSpeedMult  = 0.90f;   // 持枪移速快
        isSemiAuto     = false;

        bulletSpeed    = 18f;
        pelletsPerShot = 1;

        base.Awake();
    }
}
