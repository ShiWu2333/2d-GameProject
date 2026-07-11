using UnityEngine;

/// <summary>
/// 玩家控制器 v2
/// 新增：持枪移速修正、半自动触发传参、刀持刀移速加成
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("移动速度")]
    public float walkSpeed   = 4f;
    public float sprintSpeed = 7f;

    [Tooltip("持刀时的额外移速加成倍率（叠加在武器 moveSpeedMult 之上）")]
    public float knifeSpeedBonus = 1.15f;

    [Header("朝向节点")]
    [Tooltip("跟随鼠标旋转的节点，武器/枪口挂在此节点下")]
    public Transform aimPivot;

    [Tooltip("玩家身体 SpriteRenderer，朝向移动方向")]
    public SpriteRenderer bodySprite;

    [Tooltip("身体朝向旋转速度（0 = 瞬间）")]
    public float bodyRotateSpeed = 0f;

    // ── 组件引用 ──────────────────────────────────────
    private Rigidbody2D    rb;
    private PlayerStats    stats;
    private Camera         mainCam;
    private ArmorComponent armor;

    // ── 当前武器 ──────────────────────────────────────
    private WeaponBase currentWeapon;

    // ── 状态 ──────────────────────────────────────────
    private Vector2 moveInput;
    private bool    isSprinting;
    private bool    triggerHeld;
    private bool    isAiming;

    /// <summary>鼠标世界坐标（供其他脚本读取）</summary>
    public Vector2 MouseWorldPos { get; private set; }

    /// <summary>是否正在瞄准</summary>
    public bool IsAiming => isAiming;

    // ══════════════════════════════════════════════════
    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        stats   = GetComponent<PlayerStats>();
        armor   = GetComponent<ArmorComponent>();
        mainCam = Camera.main;

        if (mainCam == null)
            Debug.LogWarning("[PlayerController] 找不到 MainCamera，请确保场景中有 Tag=MainCamera 的摄像机");

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        if (bodySprite == null)
            bodySprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!stats.IsAlive) return;
        if (PauseMenu.IsGamePaused) return;

        // 背包打开时禁止移动和射击，只允许背包操作
        if (IsInventoryOpen() || IsContainerOpen())
        {
            rb.velocity = Vector2.zero;
            return;
        }

        GatherInput();
        AimTowardsMouse();
        RotateBodyToMovement();
        HandleWeaponInput();
    }

    void FixedUpdate()
    {
        if (!stats.IsAlive) return;
        if (IsInventoryOpen() || IsContainerOpen()) return;
        Move();
    }

    private bool IsInventoryOpen()
    {
        var inv = GetComponent<InventorySystem>();
        return inv != null && inv.IsOpen;
    }

    private bool IsContainerOpen()
    {
        return ContainerUI.Instance != null && ContainerUI.Instance.IsOpen;
    }

    // ── 输入收集 ──────────────────────────────────────
    private void GatherInput()
    {
        var kb = KeyBindings.Instance;

        // 移动输入（支持自定义按键）
        float h = 0f, v = 0f;
        if (kb != null)
        {
            if (Input.GetKey(kb.moveRight)) h += 1f;
            if (Input.GetKey(kb.moveLeft))  h -= 1f;
            if (Input.GetKey(kb.moveUp))    v += 1f;
            if (Input.GetKey(kb.moveDown))  v -= 1f;
        }
        else
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }
        moveInput = new Vector2(h, v).normalized;

        KeyCode sprintKey = kb != null ? kb.sprint : KeyCode.LeftShift;
        isSprinting = Input.GetKey(sprintKey)
                      && moveInput.sqrMagnitude > 0f
                      && stats.HasStamina
                      && !triggerHeld   // 射击时无法冲刺
                      && !isAiming;     // 瞄准时无法冲刺

        triggerHeld = Input.GetMouseButton(0);

        // 右键瞄准（仅持枪时有效，刀无瞄准）
        isAiming = Input.GetMouseButton(1)
                   && currentWeapon != null
                   && !(currentWeapon is Knife);

        // 同步瞄准状态到武器
        if (currentWeapon != null)
            currentWeapon.IsAiming = isAiming;

        stats.TickStamina(isSprinting);
    }

    // ── 移动（含持枪移速修正 + 护甲负面效果）────────
    private void Move()
    {
        // 基础速度
        float baseSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // 护甲负面效果（移速/奔跑速度惩罚）
        float armorPenalty = 1f;
        if (armor != null)
            armorPenalty = isSprinting ? armor.SprintSpeedPenalty : armor.WalkSpeedPenalty;

        // 武器持枪移速修正
        float weaponMult = 1f;
        if (currentWeapon != null)
        {
            weaponMult = currentWeapon.moveSpeedMult;
            if (currentWeapon is Knife)
                weaponMult *= knifeSpeedBonus;
            // 瞄准时额外减速
            if (isAiming)
                weaponMult *= currentWeapon.aimMoveSpeedMult;
        }

        rb.velocity = moveInput * baseSpeed * armorPenalty * weaponMult;
    }

    // ── 身体朝向移动方向 ──────────────────────────────
    private void RotateBodyToMovement()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;

        if (bodyRotateSpeed <= 0f)
            transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
        else
        {
            Quaternion target = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, bodyRotateSpeed * Time.deltaTime);
        }
    }

    // ── AimPivot 朝向鼠标 ─────────────────────────────
    private void AimTowardsMouse()
    {
        if (mainCam == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(mainCam.transform.position.z);
        MouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreen);

        if (aimPivot == null) return;

        Vector2 dir   = MouseWorldPos - (Vector2)transform.position;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // AimPivot 始终精确指向鼠标，后坐力只通过散射影响子弹方向
        aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ── 武器输入 ──────────────────────────────────────
    private void HandleWeaponInput()
    {
        if (currentWeapon == null) return;

        currentWeapon.TryShoot(triggerHeld);

        KeyCode reloadKey = KeyBindings.Instance != null ? KeyBindings.Instance.reload : KeyCode.R;
        if (Input.GetKeyDown(reloadKey))
            currentWeapon.TryReload();
    }

    // ── 公开接口 ──────────────────────────────────────
    public void EquipWeapon(WeaponBase weapon)
    {
        currentWeapon = weapon;
    }

    public bool    IsSprinting  => isSprinting;
    public Vector2 MoveInput    => moveInput;
}
