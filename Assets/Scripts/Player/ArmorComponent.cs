using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 护甲组件 v2
/// 挂在玩家/敌人身上，管理护甲穿戴、耐久、负面效果、修复
/// </summary>
public class ArmorComponent : MonoBehaviour
{
    [Header("当前装备的护甲（运行时可动态更换）")]
    [SerializeField] private ArmorData equippedArmor;

    // ── 运行时状态 ────────────────────────────────────
    public float currentDurability  { get; private set; }
    public int   remainingRepairs   { get; private set; }
    public bool  IsBroken           => currentDurability <= 0f;
    public bool  HasArmor           => equippedArmor != null;

    // 护甲等级（无甲时返回 None）
    public ArmorClass ArmorClass    => HasArmor ? equippedArmor.armorClass : ArmorClass.None;

    // 当前有效吸收率（随耐久线性衰减）
    public float CurrentAbsorption  =>
        (HasArmor && !IsBroken)
            ? equippedArmor.maxAbsorption * (currentDurability / equippedArmor.maxDurability)
            : 0f;

    // 负面效果：移速倍率（无甲或护甲损毁时无惩罚）
    public float WalkSpeedPenalty   => (HasArmor && !IsBroken) ? equippedArmor.walkSpeedPenalty   : 1f;
    public float SprintSpeedPenalty => (HasArmor && !IsBroken) ? equippedArmor.sprintSpeedPenalty : 1f;

    // ── 事件 ──────────────────────────────────────────
    public UnityEvent<float, float> onDurabilityChanged;  // (current, max)
    public UnityEvent<ArmorData>    onArmorEquipped;
    public UnityEvent               onArmorBroken;
    public UnityEvent               onArmorRemoved;

    // ══════════════════════════════════════════════════
    void Awake()
    {
        if (equippedArmor != null)
            InitArmor();
    }

    private void InitArmor()
    {
        currentDurability = equippedArmor.maxDurability;
        remainingRepairs  = equippedArmor.maxRepairCount; // -1 = 无限
    }

    // ══════════════════════════════════════════════════
    //  装备 / 卸下
    // ══════════════════════════════════════════════════

    /// <summary>装备新护甲（替换当前护甲）</summary>
    public void EquipArmor(ArmorData armor)
    {
        equippedArmor    = armor;
        InitArmor();
        onArmorEquipped?.Invoke(armor);
        onDurabilityChanged?.Invoke(currentDurability, equippedArmor.maxDurability);
        Debug.Log($"[ArmorComponent] 装备护甲：{armor.armorName}");
    }

    /// <summary>卸下护甲</summary>
    public void RemoveArmor()
    {
        equippedArmor = null;
        currentDurability = 0f;
        onArmorRemoved?.Invoke();
    }

    /// <summary>获取当前护甲数据（只读）</summary>
    public ArmorData GetArmorData() => equippedArmor;

    // ══════════════════════════════════════════════════
    //  伤害处理
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 处理弹药命中，返回穿透到人体的实际伤害
    /// </summary>
    public float ProcessHit(float rawDamage, AmmoData ammo)
    {
        if (!HasArmor || ammo == null)
            return rawDamage;

        // 1. 穿透 vs 护甲等级 → 人体伤害倍率
        float bodyDamageMult = ammo.GetDamageMultiplier(ArmorClass);
        float bodyDamage     = rawDamage * bodyDamageMult;

        // 2. 护甲吸收（随耐久衰减）
        if (!IsBroken)
            bodyDamage *= (1f - CurrentAbsorption);

        // 3. 护甲耐久消耗
        float durMult    = ammo.GetArmorDurabilityDamageMultiplier(ArmorClass);
        float durDamage  = rawDamage * durMult;
        ApplyDurabilityDamage(durDamage);

        return Mathf.Max(0f, bodyDamage);
    }

    /// <summary>近战命中：护甲减伤但不消耗耐久</summary>
    public float ProcessMeleeHit(float rawDamage)
    {
        if (!HasArmor || IsBroken) return rawDamage;
        return rawDamage * (1f - CurrentAbsorption * 0.5f);
    }

    // ══════════════════════════════════════════════════
    //  耐久管理
    // ══════════════════════════════════════════════════

    private void ApplyDurabilityDamage(float amount)
    {
        if (!HasArmor || IsBroken || amount <= 0f) return;

        float prev        = currentDurability;
        currentDurability = Mathf.Max(0f, currentDurability - amount);
        onDurabilityChanged?.Invoke(currentDurability, equippedArmor.maxDurability);

        if (prev > 0f && currentDurability <= 0f)
        {
            onArmorBroken?.Invoke();
            Debug.Log($"[ArmorComponent] {gameObject.name} 的护甲已损毁！");
        }
    }

    // ══════════════════════════════════════════════════
    //  修复
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 尝试使用修维包修复护甲
    /// 返回 true 表示修复成功
    /// </summary>
    public bool TryRepair(RepairKit kit)
    {
        if (!HasArmor)
        {
            Debug.Log("[ArmorComponent] 未装备护甲，无法修复");
            return false;
        }
        if (!IsBroken && currentDurability >= equippedArmor.maxDurability)
        {
            Debug.Log("[ArmorComponent] 护甲耐久已满，无需修复");
            return false;
        }
        if (!kit.CanRepair(equippedArmor))
        {
            Debug.Log($"[ArmorComponent] {kit.kitName} 无法修复 {equippedArmor.armorName}（等级不匹配）");
            return false;
        }
        // 检查可修次数（-1 = 无限）
        if (equippedArmor.maxRepairCount >= 0 && remainingRepairs <= 0)
        {
            Debug.Log($"[ArmorComponent] {equippedArmor.armorName} 已达最大修复次数");
            return false;
        }

        // 执行修复
        float restoreAmount = equippedArmor.maxDurability * equippedArmor.repairRestoreRatio;
        currentDurability   = Mathf.Min(equippedArmor.maxDurability,
                                        currentDurability + restoreAmount);

        if (equippedArmor.maxRepairCount >= 0)
            remainingRepairs--;

        onDurabilityChanged?.Invoke(currentDurability, equippedArmor.maxDurability);
        Debug.Log($"[ArmorComponent] 修复成功：{equippedArmor.armorName}" +
                  $"  耐久 {currentDurability:F0}/{equippedArmor.maxDurability:F0}" +
                  $"  剩余修复次数：{(equippedArmor.maxRepairCount < 0 ? "∞" : remainingRepairs.ToString())}");
        return true;
    }

    /// <summary>强制满耐久（调试用）</summary>
    public void RepairFull()
    {
        if (!HasArmor) return;
        currentDurability = equippedArmor.maxDurability;
        onDurabilityChanged?.Invoke(currentDurability, equippedArmor.maxDurability);
    }
}
