using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 武器基类，定义通用行为
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("基础属性")]
    public string weaponName = "Weapon";
    public int maxAmmo = 30;                     // 弹匣容量
    public float reloadTime = 2f;                // 换弹时间
    public float fireRate = 0.1f;                // 射击间隔（秒）

    [Header("子弹生成")]
    public Transform firePoint;                  // 子弹生成点
    public GameObject bulletPrefab;              // 子弹预制体

    // 运行时状态（只读属性，内部用字段）
    public int currentAmmo { get; protected set; }
    public bool isReloading { get; protected set; }
    public bool canShoot { get; protected set; }

    // 事件
    public UnityEvent onShoot;
    public UnityEvent onReloadStart;
    public UnityEvent onReloadComplete;
    public UnityEvent<int, int> onAmmoChanged;   // (current, max)

    // 计时器
    protected float fireTimer;

    protected virtual void Awake()
    {
        currentAmmo = maxAmmo;
        canShoot = true;
        isReloading = false;
    }

    protected virtual void Update()
    {
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    // ── 射击 ──────────────────────────────────────────
    public virtual void TryShoot()
    {
        if (!canShoot || isReloading || fireTimer > 0f || currentAmmo <= 0)
            return;
        Shoot();
    }

    protected virtual void Shoot()
    {
        currentAmmo--;
        fireTimer = fireRate;
        onShoot?.Invoke();
        onAmmoChanged?.Invoke(currentAmmo, maxAmmo);

        if (bulletPrefab != null && firePoint != null)
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }

    // ── 换弹 ──────────────────────────────────────────
    public virtual void TryReload()
    {
        if (isReloading || currentAmmo == maxAmmo)
            return;
        StartReload();
    }

    protected virtual void StartReload()
    {
        isReloading = true;
        canShoot = false;
        onReloadStart?.Invoke();
        Invoke(nameof(FinishReload), reloadTime);
    }

    protected virtual void FinishReload()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
        canShoot = true;
        onReloadComplete?.Invoke();
        onAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    // ── 拾取/丢弃 ─────────────────────────────────────
    public virtual void OnPickup(PlayerController player)
    {
        if (player == null) return;
        transform.SetParent(player.transform);
        transform.localPosition = Vector3.zero;
        player.EquipWeapon(this);
    }

    public virtual void OnDrop()
    {
        transform.SetParent(null);
    }
}
