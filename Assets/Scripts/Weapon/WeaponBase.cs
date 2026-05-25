using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 武器基类 v2
/// 新增：射程、后坐力、弹药类型、持枪移速修正
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    // ══════════════════════════════════════════════════
    //  Inspector 属性
    // ══════════════════════════════════════════════════

    [Header("基础信息")]
    public string   weaponName = "Weapon";
    public AmmoType ammoType   = AmmoType.Rifle;

    [Tooltip("当前装填的弹药数据（决定穿透等级和伤害倍率）。留空则使用武器基础伤害")]
    public AmmoData currentAmmoData;

    [Header("战斗属性")]
    [Tooltip("每发伤害")]
    public float damage       = 15f;

    [Tooltip("射击间隔（秒），越小射速越快")]
    public float fireRate     = 0.15f;

    [Tooltip("弹匣容量")]
    public int   maxAmmo      = 30;

    [Tooltip("换弹时间（秒）")]
    public float reloadTime   = 2f;

    [Tooltip("子弹最大射程（世界单位）。超出后子弹立即消失")]
    public float range        = 20f;

    [Tooltip("后坐力强度。每次射击给 AimPivot 施加的角度偏移（度）")]
    public float recoil       = 3f;

    [Tooltip("基础散射角（度），后坐力会在此基础上叠加")]
    public float baseSpread   = 2f;

    [Tooltip("持枪时的移速倍率（1 = 不影响，0.6 = 减速40%）")]
    [Range(0.1f, 1.5f)]
    public float moveSpeedMult = 1f;

    [Tooltip("是否半自动（true = 每次按下只射一发）")]
    public bool  isSemiAuto   = false;

    [Header("子弹生成")]
    [Tooltip("枪口位置")]
    public Transform  firePoint;

    [Tooltip("子弹预制体")]
    public GameObject bulletPrefab;

    [Tooltip("子弹飞行速度")]
    public float bulletSpeed  = 20f;

    [Tooltip("每次射击发射的子弹数（霰弹 > 1）")]
    public int   pelletsPerShot = 1;

    // ══════════════════════════════════════════════════
    //  运行时状态
    // ══════════════════════════════════════════════════

    public int   currentAmmo   { get; protected set; }
    public bool  isReloading   { get; protected set; }
    public bool  canShoot      { get; protected set; }

    /// <summary>当前累积后坐力偏移角（度），随时间恢复</summary>
    public float CurrentRecoilAngle { get; private set; }

    // ══════════════════════════════════════════════════
    //  事件
    // ══════════════════════════════════════════════════

    public UnityEvent              onShoot;
    public UnityEvent              onReloadStart;
    public UnityEvent              onReloadComplete;
    public UnityEvent<int, int>    onAmmoChanged;   // (current, max)

    // ══════════════════════════════════════════════════
    //  私有
    // ══════════════════════════════════════════════════

    protected float fireTimer;

    [Tooltip("后坐力恢复速度（度/秒）")]
    [SerializeField] protected float recoilRecoverySpeed = 15f;

    // 半自动：记录上一帧鼠标是否按下
    private bool prevTriggerHeld;

    // ══════════════════════════════════════════════════
    //  生命周期
    // ══════════════════════════════════════════════════

    protected virtual void Awake()
    {
        currentAmmo = maxAmmo;
        canShoot    = true;
        isReloading = false;
    }

    protected virtual void Update()
    {
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;

        // 后坐力自然恢复
        if (CurrentRecoilAngle > 0f)
        {
            CurrentRecoilAngle = Mathf.Max(0f,
                CurrentRecoilAngle - recoilRecoverySpeed * Time.deltaTime);
        }
    }

    // ══════════════════════════════════════════════════
    //  射击
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 由 PlayerController 每帧调用（传入当前帧是否按住扳机）
    /// </summary>
    public virtual void TryShoot(bool triggerHeld)
    {
        if (!canShoot || isReloading || fireTimer > 0f || currentAmmo <= 0)
        {
            prevTriggerHeld = triggerHeld;
            return;
        }

        bool fire = isSemiAuto
            ? (triggerHeld && !prevTriggerHeld)   // 半自动：仅按下瞬间
            : triggerHeld;                         // 全自动：持续按住

        prevTriggerHeld = triggerHeld;

        if (fire) Shoot();
    }

    /// <summary>保留无参版本兼容旧调用</summary>
    public virtual void TryShoot() => TryShoot(true);

    protected virtual void Shoot()
    {
        currentAmmo--;
        fireTimer = fireRate;

        // 累积后坐力
        CurrentRecoilAngle += recoil;

        onShoot?.Invoke();
        onAmmoChanged?.Invoke(currentAmmo, maxAmmo);

        SpawnBullets();
    }

    protected virtual void SpawnBullets()
    {
        if (bulletPrefab == null || firePoint == null) return;

        float totalSpread = baseSpread + CurrentRecoilAngle;

        if (pelletsPerShot <= 1)
        {
            // 单发
            float angle = Random.Range(-totalSpread, totalSpread);
            FireOneBullet(firePoint.rotation * Quaternion.Euler(0f, 0f, angle));
        }
        else
        {
            // 多发（霰弹）：均匀分布 + 微抖动
            float half = totalSpread;
            float step = pelletsPerShot > 1 ? (half * 2f) / (pelletsPerShot - 1) : 0f;
            for (int i = 0; i < pelletsPerShot; i++)
            {
                float angle = -half + step * i + Random.Range(-1.5f, 1.5f);
                FireOneBullet(firePoint.rotation * Quaternion.Euler(0f, 0f, angle));
            }
        }
    }

    private void FireOneBullet(Quaternion rotation)
    {
        GameObject go = Instantiate(bulletPrefab, firePoint.position, rotation);

        Bullet b = go.GetComponent<Bullet>();
        if (b != null)
        {
            b.damage    = damage;
            b.maxRange  = range;
            b.origin    = firePoint.position;
            b.ammoData  = currentAmmoData;   // 注入弹药数据
        }

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = go.transform.right * bulletSpeed;
    }

    // ══════════════════════════════════════════════════
    //  换弹
    // ══════════════════════════════════════════════════

    public virtual void TryReload()
    {
        if (isReloading || currentAmmo == maxAmmo) return;
        StartReload();
    }

    protected virtual void StartReload()
    {
        isReloading = true;
        canShoot    = false;
        onReloadStart?.Invoke();
        Invoke(nameof(FinishReload), reloadTime);
    }

    protected virtual void FinishReload()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
        canShoot    = true;
        onReloadComplete?.Invoke();
        onAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    // ══════════════════════════════════════════════════
    //  拾取 / 丢弃
    // ══════════════════════════════════════════════════

    public virtual void OnPickup(PlayerController player)
    {
        if (player == null) return;
        transform.SetParent(player.aimPivot != null ? player.aimPivot : player.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        player.EquipWeapon(this);
    }

    public virtual void OnDrop()
    {
        transform.SetParent(null);
    }
}
