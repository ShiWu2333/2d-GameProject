using UnityEngine;

/// <summary>
/// 刀（近战武器）
/// 持刀移速极快 | 攻击速度中 | 低级穿透 | 伤害中等
/// 无弹药消耗，攻击为扇形范围检测
/// 颜色：灰色
/// </summary>
public class Knife : WeaponBase
{
    [Header("近战属性")]
    [Tooltip("攻击范围半径（世界单位）")]
    public float attackRadius = 1.5f;

    [Tooltip("攻击扇形角度（度）")]
    public float attackAngle  = 90f;

    [Tooltip("可命中的层")]
    public LayerMask hitLayers;

    [Tooltip("穿透目标数上限（低级穿透 = 2）")]
    public int maxPenetration = 2;

    [Header("攻击特效")]
    [Tooltip("挥刀弧形特效预制体（留空则自动生成默认特效）")]
    public GameObject slashEffectPrefab;

    [Tooltip("特效颜色")]
    public Color slashColor = new Color(0.9f, 0.95f, 1f, 0.8f);

    protected override void Awake()
    {
        weaponName     = "刀";
        ammoType       = AmmoType.None;   // 无限使用

        damage         = 35f;
        fireRate       = 0.45f;   // 攻击速度中
        maxAmmo        = 1;       // 占位，不实际消耗
        reloadTime     = 0f;
        range          = attackRadius;
        recoil         = 0f;
        baseSpread     = 0f;
        moveSpreadBonus= 0f;      // 刀无散射概念
        moveSpeedMult  = 1.0f;    // 持刀移速极快（由PlayerController额外加成）
        aimSpreadMult  = 1f;      // 刀不使用瞄准
        aimMoveSpeedMult= 1f;     // 刀不使用瞄准
        isSemiAuto     = true;    // 每次点击攻击一次

        bulletSpeed    = 0f;
        pelletsPerShot = 0;

        base.Awake();
    }

    protected override void Shoot()
    {
        fireTimer = fireRate;
        onShoot?.Invoke();

        SpawnSlashEffect();
        MeleeAttack();
    }

    private void SpawnSlashEffect()
    {
        if (firePoint == null) return;

        if (slashEffectPrefab != null)
        {
            var inst = Instantiate(slashEffectPrefab, firePoint.position, firePoint.rotation, firePoint);
            return;
        }

        // 默认：用 LineRenderer 画弧线特效，挂在 firePoint 下跟随人物
        var go = new GameObject("SlashFX");
        go.transform.SetParent(firePoint, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        go.AddComponent<LineRenderer>();
        var fx = go.AddComponent<SlashEffect>();
        fx.duration  = 0.15f;
        fx.arcRadius = attackRadius * 0.9f;
        fx.arcAngle  = attackAngle;
        fx.lineWidth = 0.25f;
        fx.color     = slashColor;
    }

    /// <summary>运行时生成一个简单的弧形 Sprite（白色半圆）</summary>
    private static Sprite cachedArcSprite;
    private static Sprite CreateArcSprite()
    {
        // 不再使用，保留空实现避免编译错误
        return null;
    }

    private void MeleeAttack()
    {
        if (firePoint == null) return;

        // 以 firePoint 为圆心做圆形检测
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            firePoint.position, attackRadius, hitLayers);

        int penetrated = 0;
        foreach (Collider2D col in hits)
        {
            if (penetrated >= maxPenetration) break;

            // 扇形角度过滤
            Vector2 dir = (col.transform.position - firePoint.position).normalized;
            float   dot = Vector2.Dot(firePoint.up, dir);
            float   threshold = Mathf.Cos(attackAngle * 0.5f * Mathf.Deg2Rad);
            if (dot < threshold) continue;

            IDamageable target = col.GetComponent<IDamageable>();
            if (target != null)
            {
                // 近战走护甲的 ProcessMeleeHit，有护甲则减伤但不消耗耐久
                float finalDamage = damage;
                var armor = col.GetComponent<ArmorComponent>();
                if (armor != null)
                    finalDamage = armor.ProcessMeleeHit(damage);

                target.TakeDamage(finalDamage);
                penetrated++;
            }
        }
    }

    // 刀不消耗弹药，重写换弹为空操作
    public override void TryReload() { }

    // 刀弹药始终"满"，不显示弹药UI
    protected override void FinishReload() { }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;
        UnityEditor.Handles.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        UnityEditor.Handles.DrawSolidArc(
            firePoint.position,
            Vector3.forward,
            Quaternion.Euler(0, 0, -attackAngle * 0.5f) * firePoint.up,
            attackAngle,
            attackRadius);
    }
#endif
}
