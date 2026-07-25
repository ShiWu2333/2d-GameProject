using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 敌人属性：血量、护甲、死亡处理
/// 实现 IDamageable 接口，可被子弹/近战击中
/// </summary>
public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("血量")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("死亡掉落")]
    [Tooltip("死亡后掉落的弹药类型（None=不掉落）")]
    public AmmoType dropAmmoType = AmmoType.Rifle;
    [Tooltip("掉落弹药数量")]
    public int dropAmmoAmount = 20;
    [Tooltip("是否掉落医疗物品")]
    public bool dropMedical = false;

    [Header("视觉反馈")]
    [Tooltip("受击闪烁持续时间")]
    public float hitFlashDuration = 0.1f;

    // 事件
    public UnityEvent<float, float> onHealthChanged = new UnityEvent<float, float>();
    public UnityEvent onDeath = new UnityEvent();
    public UnityEvent onHit = new UnityEvent();

    // 状态
    public bool IsAlive => currentHealth > 0f;
    public bool IsDead => currentHealth <= 0f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float flashTimer;

    void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    /// <summary>重新初始化血量（设置maxHealth后调用）</summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        // 受击闪烁恢复
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }
    }

    /// <summary>IDamageable 接口实现</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        onHit?.Invoke();

        // 受击闪白
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            flashTimer = hitFlashDuration;
        }

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>治疗（如果需要）</summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        onDeath?.Invoke();
        DropLoot();

        // 通知GameManager
        GameManager.Instance?.OnEnemyKilled(this);

        // 禁用AI和碰撞
        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        // 变暗表示死亡
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        // 2秒后销毁
        Destroy(gameObject, 2f);
    }

    private void DropLoot()
    {
        // 掉落弹药
        if (dropAmmoType != AmmoType.None && dropAmmoAmount > 0)
        {
            SpawnAmmoPickup(dropAmmoType, dropAmmoAmount);
        }

        // 掉落医疗物品（30%概率）
        if (dropMedical && Random.value < 0.3f)
        {
            SpawnMedicalPickup();
        }
    }

    private void SpawnAmmoPickup(AmmoType type, int amount)
    {
        Vector2 offset = Random.insideUnitCircle * 0.5f;
        Vector3 pos = transform.position + (Vector3)offset;

        var go = new GameObject($"Drop_Ammo_{type}");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = AmmoIconManager.GetAmmoBaseColor(type);
        go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        // 创建1x1白色sprite
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        var gi = go.AddComponent<GroundItem>();
        gi.itemType = GroundItem.GroundItemType.Ammo;
        gi.ammoItem = AmmoItemFactory.CreateAmmoItem(type, amount, false);
        gi.displayName = $"{type}弹药 ×{amount}";
    }

    private void SpawnMedicalPickup()
    {
        Vector2 offset = Random.insideUnitCircle * 0.5f;
        Vector3 pos = transform.position + (Vector3)offset;

        var go = new GameObject("Drop_Medical");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.2f, 0.9f, 0.3f);
        go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        var gi = go.AddComponent<GroundItem>();
        gi.itemType = GroundItem.GroundItemType.Item;
        gi.item = MedicalItemFactory.CreateMedicalNeedle();
        gi.displayName = "医疗针";
    }
}
