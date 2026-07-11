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
        weaponSlotCount= 10;

        damage         = 18f;
        fireRate       = 0.10f;   // 10发/秒
        maxAmmo        = 100;
        reloadTime     = 4.5f;
        range          = 28f;
        recoil         = 4f;      // 单发后坐力降低（连射累积仍然很大）
        baseSpread     = 4f;
        moveSpreadBonus= 5f;      // 移动散射极大（重武器惩罚高）
        moveSpeedMult  = 0.45f;
        aimSpreadMult  = 0.3f;    // 瞄准精准度中等
        aimMoveSpeedMult= 0.3f;   // 瞄准几乎不能动
        isSemiAuto     = false;

        bulletSpeed    = 26f;
        pelletsPerShot = 1;

        recoilRecoverySpeed = 50f; // 快速恢复，停火后迅速回正

        base.Awake();
    }
}
