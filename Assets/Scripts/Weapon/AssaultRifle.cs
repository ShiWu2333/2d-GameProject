using UnityEngine;

/// <summary>
/// 突击步枪 (AR)
/// 射速中 | 伤害中 | 弹容中 | 射程中远 | 后坐力中 | 持枪移速标准
/// 弹药：步枪弹
/// 颜色：蓝色
/// </summary>
public class AssaultRifle : WeaponBase
{
    protected override void Awake()
    {
        weaponName     = "突击步枪";
        ammoType       = AmmoType.Rifle;

        damage         = 20f;
        fireRate       = 0.13f;   // ~7.7发/秒
        maxAmmo        = 30;
        reloadTime     = 2.2f;
        range          = 25f;
        recoil         = 3f;
        baseSpread     = 2f;
        moveSpeedMult  = 0.80f;
        isSemiAuto     = false;

        bulletSpeed    = 24f;
        pelletsPerShot = 1;

        base.Awake();
    }
}
