using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 武器槽系统：管理主武器1、主武器2、刀三个槽位
/// 按 1/2/3 切换当前手持武器
/// 挂在玩家 GameObject 上
/// </summary>
public class WeaponSlotSystem : MonoBehaviour
{
    // ── 槽位枚举 ──────────────────────────────────────
    public enum WeaponSlot
    {
        Primary1 = 0,   // 主武器1（键1）
        Primary2 = 1,   // 主武器2（键2）
        Melee    = 2,   // 刀      （键3）
    }

    [Header("武器槽（在 Inspector 中拖入武器 GameObject）")]
    [Tooltip("主武器1，对应按键 1")]
    public WeaponBase primary1;

    [Tooltip("主武器2，对应按键 2")]
    public WeaponBase primary2;

    [Tooltip("近战武器（刀），对应按键 3")]
    public WeaponBase melee;

    // 事件：切换武器时触发，参数为新槽位索引
    public UnityEvent<int> onSlotChanged;

    // 当前激活槽位
    public WeaponSlot CurrentSlot { get; private set; } = WeaponSlot.Primary1;

    // 当前手持武器（可能为 null，如该槽位没有武器）
    public WeaponBase CurrentWeapon { get; private set; }

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        // 初始化：隐藏所有武器，激活默认槽位
        SetAllWeaponsInactive();
        SwitchToSlot(WeaponSlot.Primary1);
    }

    void Update()
    {
        HandleSlotInput();
    }

    // ── 按键检测 ──────────────────────────────────────
    private void HandleSlotInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchToSlot(WeaponSlot.Primary1);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SwitchToSlot(WeaponSlot.Primary2);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SwitchToSlot(WeaponSlot.Melee);
    }

    // ── 切换槽位 ──────────────────────────────────────
    public void SwitchToSlot(WeaponSlot slot)
    {
        // 隐藏当前武器
        SetWeaponActive(CurrentWeapon, false);

        CurrentSlot = slot;
        CurrentWeapon = GetWeaponInSlot(slot);

        // 显示新武器
        SetWeaponActive(CurrentWeapon, true);

        // 通知 PlayerController 更新当前武器
        if (playerController != null)
            playerController.EquipWeapon(CurrentWeapon);

        onSlotChanged?.Invoke((int)slot);

        Debug.Log($"切换到槽位：{slot}，武器：{(CurrentWeapon != null ? CurrentWeapon.weaponName : "空")}");
    }

    // ── 按槽位索引切换（供外部调用）─────────────────
    public void SwitchToSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex <= 2)
            SwitchToSlot((WeaponSlot)slotIndex);
    }

    // ── 装备武器到指定槽位 ────────────────────────────
    public void SetWeaponInSlot(WeaponSlot slot, WeaponBase weapon)
    {
        // 先隐藏旧武器
        WeaponBase old = GetWeaponInSlot(slot);
        if (old != null) SetWeaponActive(old, false);

        switch (slot)
        {
            case WeaponSlot.Primary1: primary1 = weapon; break;
            case WeaponSlot.Primary2: primary2 = weapon; break;
            case WeaponSlot.Melee:    melee    = weapon; break;
        }

        // 如果装备的是当前槽位，立即激活
        if (slot == CurrentSlot)
        {
            CurrentWeapon = weapon;
            SetWeaponActive(weapon, true);
            if (playerController != null)
                playerController.EquipWeapon(weapon);
        }
    }

    // ── 获取指定槽位武器 ──────────────────────────────
    public WeaponBase GetWeaponInSlot(WeaponSlot slot)
    {
        return slot switch
        {
            WeaponSlot.Primary1 => primary1,
            WeaponSlot.Primary2 => primary2,
            WeaponSlot.Melee    => melee,
            _                   => null,
        };
    }

    // ── 工具方法 ──────────────────────────────────────
    private void SetAllWeaponsInactive()
    {
        SetWeaponActive(primary1, false);
        SetWeaponActive(primary2, false);
        SetWeaponActive(melee,    false);
    }

    private void SetWeaponActive(WeaponBase weapon, bool active)
    {
        if (weapon != null)
            weapon.gameObject.SetActive(active);
    }
}
