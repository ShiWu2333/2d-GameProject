using UnityEngine;

/// <summary>
/// 敌人警报系统
/// 当玩家开枪时通知附近敌人
/// 挂在玩家身上
/// </summary>
public class EnemyAlertSystem : MonoBehaviour
{
    [Header("枪声传播范围")]
    [Tooltip("玩家开枪时的声响传播半径")]
    public float gunshotAlertRadius = 15f;

    private WeaponSlotSystem weaponSlotSystem;

    void Start()
    {
        weaponSlotSystem = GetComponent<WeaponSlotSystem>();
    }

    /// <summary>
    /// 由武器射击事件调用，通知附近所有敌人
    /// 需要在武器的onShoot事件中绑定
    /// </summary>
    public void OnPlayerShoot()
    {
        AlertNearbyEnemies(transform.position, gunshotAlertRadius);
    }

    /// <summary>向指定范围内的敌人发出声响警报</summary>
    public static void AlertNearbyEnemies(Vector3 position, float radius)
    {
        var enemies = Object.FindObjectsOfType<EnemyAI>();
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.enabled) continue;
            enemy.AlertSound(position);
        }
    }

    void Update()
    {
        // 检测当前武器是否开火（通过监听）
        // 更好的方式是在 WeaponBase.Shoot 中调用，但为了不修改现有代码，
        // 我们在 WeaponSlotSystem 的当前武器上注册事件
        RegisterWeaponEvents();
    }

    private WeaponBase lastRegisteredWeapon;

    private void RegisterWeaponEvents()
    {
        if (weaponSlotSystem == null) return;

        var current = weaponSlotSystem.CurrentWeapon;
        if (current == lastRegisteredWeapon) return;

        // 取消旧注册
        if (lastRegisteredWeapon != null)
            lastRegisteredWeapon.onShoot.RemoveListener(OnPlayerShoot);

        // 注册新武器
        if (current != null)
            current.onShoot.AddListener(OnPlayerShoot);

        lastRegisteredWeapon = current;
    }

    void OnDestroy()
    {
        if (lastRegisteredWeapon != null)
            lastRegisteredWeapon.onShoot.RemoveListener(OnPlayerShoot);
    }
}
