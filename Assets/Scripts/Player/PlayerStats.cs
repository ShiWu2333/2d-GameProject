using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 玩家属性：血量、体力
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("血量")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("体力")]
    public float maxStamina = 100f;
    public float currentStamina { get; private set; }
    public float staminaDrainRate = 20f;      // 奔跑时每秒消耗
    public float staminaRegenRate = 10f;       // 静止/走路时每秒恢复
    public float staminaRegenDelay = 1.5f;     // 停止奔跑后多久开始恢复

    // 事件
    public UnityEvent<float, float> onHealthChanged;   // (current, max)
    public UnityEvent<float, float> onStaminaChanged;  // (current, max)
    public UnityEvent onDeath;

    private float staminaRegenTimer;
    private bool isDead;

    void Awake()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    // ── 血量 ──────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        onDeath?.Invoke();
        Debug.Log("玩家死亡");
    }

    public bool IsAlive => !isDead;

    // ── 体力 ──────────────────────────────────────────
    /// <summary>每帧由 PlayerController 调用，传入是否正在奔跑</summary>
    public void TickStamina(bool isSprinting)
    {
        if (isSprinting && currentStamina > 0f)
        {
            currentStamina = Mathf.Clamp(currentStamina - staminaDrainRate * Time.deltaTime, 0f, maxStamina);
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            if (staminaRegenTimer > 0f)
                staminaRegenTimer -= Time.deltaTime;
            else
                currentStamina = Mathf.Clamp(currentStamina + staminaRegenRate * Time.deltaTime, 0f, maxStamina);
        }
        onStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    /// <summary>体力是否足够奔跑</summary>
    public bool HasStamina => currentStamina > 0f;
}
