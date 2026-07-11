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
        weaponSlotCount= 8;

        damage         = 20f;
        fireRate       = 0.13f;   // ~7.7发/秒
        maxAmmo        = 30;
        reloadTime     = 2.2f;
        range          = 25f;
        recoil         = 3f;
        baseSpread     = 2f;
        moveSpreadBonus= 3f;      // 移动散射中等
        moveSpeedMult  = 0.80f;
        aimSpreadMult  = 0.25f;   // 瞄准精准度较高
        aimMoveSpeedMult= 0.4f;   // 瞄准时移速较慢
        isSemiAuto     = false;

        bulletSpeed    = 24f;
        pelletsPerShot = 1;

        base.Awake();
    }
}
