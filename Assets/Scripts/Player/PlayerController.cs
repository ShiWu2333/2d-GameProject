using UnityEngine;

/// <summary>
/// 玩家控制器：WASD移动、鼠标瞄准、Shift奔跑、左键开火、R换弹
///
/// 朝向方案（俯视角2D）：
///   - 玩家身体（SpriteRenderer）朝向移动方向
///   - AimPivot 节点始终朝向鼠标（武器/枪口挂在此节点下）
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("移动速度")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;

    [Header("朝向节点")]
    [Tooltip("跟随鼠标旋转的节点，武器/枪口挂在此节点下")]
    public Transform aimPivot;

    [Tooltip("玩家身体 SpriteRenderer，朝向移动方向")]
    public SpriteRenderer bodySprite;

    [Tooltip("身体朝向旋转速度（0 = 瞬间）")]
    public float bodyRotateSpeed = 0f;

    // 组件引用
    private Rigidbody2D rb;
    private PlayerStats stats;
    private Camera mainCam;

    // 当前武器
    private WeaponBase currentWeapon;

    // 状态
    private Vector2 moveInput;
    private bool isSprinting;

    /// <summary>鼠标世界坐标（供其他脚本读取）</summary>
    public Vector2 MouseWorldPos { get; private set; }

    void Awake()
    {
        rb    = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        mainCam = Camera.main;

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;   // 物理不旋转，由脚本控制

        if (bodySprite == null)
            bodySprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!stats.IsAlive) return;

        GatherInput();
        AimTowardsMouse();
        RotateBodyToMovement();
        HandleWeaponInput();
    }

    void FixedUpdate()
    {
        if (!stats.IsAlive) return;
        Move();
    }

    // ── 输入收集 ──────────────────────────────────────
    private void GatherInput()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        isSprinting = Input.GetKey(KeyCode.LeftShift)
                      && moveInput.sqrMagnitude > 0f
                      && stats.HasStamina;

        stats.TickStamina(isSprinting);
    }

    // ── 移动 ──────────────────────────────────────────
    private void Move()
    {
        float speed = isSprinting ? sprintSpeed : walkSpeed;
        rb.velocity = moveInput * speed;
    }

    // ── 身体朝向移动方向 ──────────────────────────────
    private void RotateBodyToMovement()
    {
        if (moveInput.sqrMagnitude < 0.01f) return; // 静止时保持上一帧朝向

        float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
        // -90° 修正：让 Sprite 的"上方"对应移动方向

        if (bodyRotateSpeed <= 0f)
        {
            // 瞬间旋转
            transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
        }
        else
        {
            // 平滑旋转
            Quaternion target = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, bodyRotateSpeed * Time.deltaTime);
        }
    }

    // ── AimPivot 朝向鼠标 ─────────────────────────────
    private void AimTowardsMouse()
    {
        // 屏幕坐标 → 世界坐标（正交摄像机）
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(mainCam.transform.position.z);
        MouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreen);

        if (aimPivot == null) return;

        Vector2 dir = MouseWorldPos - (Vector2)transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // AimPivot 在世界空间旋转，不受身体旋转影响
        aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ── 武器输入 ──────────────────────────────────────
    private void HandleWeaponInput()
    {
        if (currentWeapon == null) return;

        if (Input.GetMouseButton(0))        // 左键持续：全自动开火
            currentWeapon.TryShoot();

        if (Input.GetKeyDown(KeyCode.R))    // R：换弹
            currentWeapon.TryReload();
    }

    // ── 公开接口 ──────────────────────────────────────
    public void EquipWeapon(WeaponBase weapon)
    {
        currentWeapon = weapon;
    }

    public bool IsSprinting  => isSprinting;
    public Vector2 MoveInput => moveInput;
}
