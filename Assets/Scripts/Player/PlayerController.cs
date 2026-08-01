using UnityEngine;

/// <summary>
/// 玩家控制器
/// 移动方式：Rigidbody2D.MovePosition（物理碰撞自动阻挡墙壁）
/// 朝向：身体和AimPivot均朝向鼠标
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("移动速度")]
    public float walkSpeed   = 4f;
    public float sprintSpeed = 7f;

    [Tooltip("持刀时的额外移速加成倍率")]
    public float knifeSpeedBonus = 1.15f;

    [Header("朝向节点")]
    [Tooltip("跟随鼠标旋转的节点，武器挂在此节点下")]
    public Transform aimPivot;

    [Tooltip("玩家身体 SpriteRenderer")]
    public SpriteRenderer bodySprite;

    [Tooltip("身体朝向旋转速度（度/秒，0 = 瞬间）")]
    public float bodyRotateSpeed = 720f;

    // ── 组件缓存 ─────────────────────────────────────
    private Rigidbody2D    rb;
    private PlayerStats    stats;
    private Camera         mainCam;
    private ArmorComponent armor;

    // ── 当前武器 ─────────────────────────────────────
    private WeaponBase currentWeapon;

    // ── 运行时状态 ───────────────────────────────────
    private Vector2 moveInput;
    private bool    isSprinting;
    private bool    triggerHeld;
    private bool    isAiming;

    /// <summary>鼠标世界坐标（供其他脚本读取）</summary>
    public Vector2 MouseWorldPos { get; private set; }

    /// <summary>是否正在瞄准</summary>
    public bool IsAiming => isAiming;

    public bool    IsSprinting => isSprinting;
    public Vector2 MoveInput   => moveInput;

    // ══════════════════════════════════════════════════
    //  初始化
    // ══════════════════════════════════════════════════

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        stats   = GetComponent<PlayerStats>();
        armor   = GetComponent<ArmorComponent>();
        mainCam = Camera.main;

        // Rigidbody2D 配置：俯视角标准设定
        rb.bodyType               = RigidbodyType2D.Dynamic;
        rb.gravityScale           = 0f;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        rb.drag                   = 0f;
        rb.angularDrag            = 0f;

        if (bodySprite == null)
            bodySprite = GetComponent<SpriteRenderer>();

        // 移除遗留的 WallCollisionGuard（不再需要）
        var wallGuard = GetComponent<WallCollisionGuard>();
        if (wallGuard != null)
            Destroy(wallGuard);
    }

    // ══════════════════════════════════════════════════
    //  帧更新：输入 + 朝向 + 武器
    // ══════════════════════════════════════════════════

    void Update()
    {
        if (!stats.IsAlive) return;
        if (PauseMenu.IsGamePaused) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (IsInventoryOpen() || IsContainerOpen())
        {
            moveInput = Vector2.zero;
            return;
        }

        GatherInput();
        UpdateMouseWorldPos();
        RotateTowardsMouse();
        HandleWeaponInput();
    }

    // ══════════════════════════════════════════════════
    //  物理更新：移动
    // ══════════════════════════════════════════════════

    void FixedUpdate()
    {
        if (!stats.IsAlive) return;
        if (IsInventoryOpen() || IsContainerOpen()) return;

        float speed = CalculateSpeed();
        Vector2 displacement = moveInput * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + displacement);
    }

    // ══════════════════════════════════════════════════
    //  输入收集
    // ══════════════════════════════════════════════════

    private void GatherInput()
    {
        var kb = KeyBindings.Instance;

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
                      && !triggerHeld
                      && !isAiming;

        triggerHeld = Input.GetMouseButton(0);

        isAiming = Input.GetMouseButton(1)
                   && currentWeapon != null
                   && !(currentWeapon is Knife);

        if (currentWeapon != null)
            currentWeapon.IsAiming = isAiming;

        stats.TickStamina(isSprinting);
    }

    // ══════════════════════════════════════════════════
    //  速度计算
    // ══════════════════════════════════════════════════

    private float CalculateSpeed()
    {
        float baseSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // 护甲惩罚
        float armorMult = 1f;
        if (armor != null)
            armorMult = isSprinting ? armor.SprintSpeedPenalty : armor.WalkSpeedPenalty;

        // 武器移速修正
        float weaponMult = 1f;
        if (currentWeapon != null)
        {
            weaponMult = currentWeapon.moveSpeedMult;
            if (currentWeapon is Knife)
                weaponMult *= knifeSpeedBonus;
            if (isAiming)
                weaponMult *= currentWeapon.aimMoveSpeedMult;
        }

        return baseSpeed * armorMult * weaponMult;
    }

    // ══════════════════════════════════════════════════
    //  朝向：身体 + AimPivot 都面向鼠标
    // ══════════════════════════════════════════════════

    private void UpdateMouseWorldPos()
    {
        if (mainCam == null) return;
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(mainCam.transform.position.z);
        MouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreen);
    }

    private void RotateTowardsMouse()
    {
        if (mainCam == null) return;

        Vector2 dir = MouseWorldPos - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 身体朝向鼠标（-90 因为 Sprite 默认正面朝上）
        float bodyAngle = angle - 90f;
        if (bodyRotateSpeed <= 0f)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, bodyAngle);
        }
        else
        {
            Quaternion target = Quaternion.Euler(0f, 0f, bodyAngle);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, bodyRotateSpeed * Time.deltaTime);
        }

        // AimPivot 精确指向鼠标（无平滑）
        if (aimPivot != null)
            aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ══════════════════════════════════════════════════
    //  武器输入
    // ══════════════════════════════════════════════════

    private void HandleWeaponInput()
    {
        if (currentWeapon == null) return;

        currentWeapon.TryShoot(triggerHeld);

        KeyCode reloadKey = KeyBindings.Instance != null ? KeyBindings.Instance.reload : KeyCode.R;
        if (Input.GetKeyDown(reloadKey))
            currentWeapon.TryReload();
    }

    // ══════════════════════════════════════════════════
    //  公开接口
    // ══════════════════════════════════════════════════

    public void EquipWeapon(WeaponBase weapon)
    {
        currentWeapon = weapon;
    }

    // ══════════════════════════════════════════════════
    //  工具
    // ══════════════════════════════════════════════════

    private bool IsInventoryOpen()
    {
        var inv = GetComponent<InventorySystem>();
        return inv != null && inv.IsOpen;
    }

    private bool IsContainerOpen()
    {
        return ContainerUI.Instance != null && ContainerUI.Instance.IsOpen;
    }
}
