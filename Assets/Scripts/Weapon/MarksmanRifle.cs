using UnityEngine;

/// <summary>
/// 射手步枪 (DMR)
/// 射速慢 | 伤害高 | 弹容小 | 射程远 | 后坐力中高 | 持枪移速略慢
/// 半自动：每次按下只射一发
/// 弹药：步枪弹
/// 颜色：深绿色
/// </summary>
public class MarksmanRifle : WeaponBase
{
    protected override void Awake()
    {
        weaponName     = "射手步枪";
        ammoType       = AmmoType.Rifle;

        damage         = 45f;
        fireRate       = 0.2f;    // ~5发/秒
        maxAmmo        = 15;
        reloadTime     = 2.8f;
        range          = 40f;
        recoil         = 6f;
        baseSpread     = 0.8f;    // 精准度高，基础散射小
        moveSpeedMult  = 0.70f;   // 持枪略慢
        isSemiAuto     = true;    // 半自动

        bulletSpeed    = 32f;
        pelletsPerShot = 1;

        base.Awake();
    }
}
