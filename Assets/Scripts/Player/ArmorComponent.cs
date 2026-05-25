using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 护甲组件
/// 挂在任何可穿戴护甲的角色（玩家/敌人）身上
/// 护甲会先吸收伤害，耐久耗尽后护甲失效，之后全额承受人体伤害
/// </summary>
public class ArmorComponent : MonoBehaviour
{
    [Header("护甲属性")]
    [Tooltip("护甲等级，决定对不同穿透弹药的抵抗能力")]
    public ArmorClass armorClass = ArmorClass.Light;

    [Tooltip("护甲最大耐久")]
    public float maxDurability = 100f;

    [Tooltip("护甲当前耐久")]
    public float currentDurability { get; private set; }

    [Header("护甲吸收")]
    [Tooltip("护甲完好时，吸收人体伤害的比例（0~1）。耐久越低吸收越少")]
    [Range(0f, 1f)]
    public float maxAbsorption = 0.8f;

    // 事件
    public UnityEvent<float, float> onDurabilityChanged;  // (current, max)
    public UnityEvent               onArmorBroken;

    public bool IsBroken => currentDurability <= 0f;

    /// <summary>护甲有效吸收率（随耐久线性衰减）</summary>
    public float CurrentAbsorption =>
        IsBroken ? 0f : maxAbsorption * (currentDurability / maxDurability);

    void Awake()
    {
        currentDurability = maxDurability;
    }

    // ══════════════════════════════════════════════════
    //  核心接口：处理一次命中
    //  返回最终传递给 IDamageable 的人体伤害值
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 处理弹药命中，返回穿透到人体的实际伤害
    /// </summary>
    /// <param name="rawDamage">武器基础伤害</param>
    /// <param name="ammo">弹药数据（含穿透等级）</param>
    public float ProcessHit(float rawDamage, AmmoData ammo)
    {
        if (ammo == null)
        {
            // 无弹药数据（近战等）：直接全额伤害，不消耗护甲
            return rawDamage;
        }

        // 1. 计算人体伤害倍率
        float bodyDamageMult = ammo.GetDamageMultiplier(armorClass);

        // 2. 护甲完好时，额外用吸收率再减一次人体伤害
        float bodyDamage = rawDamage * bodyDamageMult;
        if (!IsBroken)
            bodyDamage *= (1f - CurrentAbsorption);

        // 3. 计算护甲耐久消耗
        float durabilityDamageMult = ammo.GetArmorDurabilityDamageMultiplier(armorClass);
        float durabilityDamage     = rawDamage * durabilityDamageMult;
        ApplyDurabilityDamage(durabilityDamage);

        return Mathf.Max(0f, bodyDamage);
    }

    /// <summary>
    /// 近战命中：不消耗护甲耐久，但护甲吸收部分伤害
    /// </summary>
    public float ProcessMeleeHit(float rawDamage)
    {
        if (IsBroken) return rawDamage;
        // 近战对护甲穿透能力弱，吸收率减半
        return rawDamage * (1f - CurrentAbsorption * 0.5f);
    }

    // ══════════════════════════════════════════════════
    //  耐久管理
    // ══════════════════════════════════════════════════

    private void ApplyDurabilityDamage(float amount)
    {
        if (IsBroken || amount <= 0f) return;

        float prev = currentDurability;
        currentDurability = Mathf.Max(0f, currentDurability - amount);
        onDurabilityChanged?.Invoke(currentDurability, maxDurability);

        if (prev > 0f && currentDurability <= 0f)
        {
            onArmorBroken?.Invoke();
            Debug.Log($"[ArmorComponent] {gameObject.name} 的护甲已损毁！");
        }
    }

    public void RepairArmor(float amount)
    {
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
        onDurabilityChanged?.Invoke(currentDurability, maxDurability);
    }

    public void RepairFull()
    {
        currentDurability = maxDurability;
        onDurabilityChanged?.Invoke(currentDurability, maxDurability);
    }
}
