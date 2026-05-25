using UnityEngine;

/// <summary>
/// 轻机枪 (LMG)
/// 射速中高 | 伤害中 | 弹容极大 | 射程中远 | 后坐力极大 | 持枪移速极慢
/// 弹药：机枪弹
/// 颜色：黄色
/// </summary>
public class LMG : WeaponBase
{
    protected override void Awake()
    {
        weaponName     = "轻机枪";
        ammoType       = AmmoType.LMG;

        damage         = 18f;
        fireRate       = 0.10f;   // 10发/秒
        maxAmmo        = 100;
        reloadTime     = 4.5f;    // 换弹极慢
        range          = 28f;
        recoil         = 10f;     // 后坐力极大
        baseSpread     = 4f;
        moveSpeedMult  = 0.45f;   // 持枪极慢
        isSemiAuto     = false;

        bulletSpeed    = 26f;
        pelletsPerShot = 1;

        base.Awake();
    }
}
