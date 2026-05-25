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

    /// <summary>鼠标世界坐标（供其他脚本读取）</summary>
    public Vector2 MouseWorldPos { get; private set; }

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

        triggerHeld = Input.GetMouseButton(0);

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

        // 基础朝向
        Quaternion baseRot = Quaternion.Euler(0f, 0f, angle);

        // 叠加后坐力偏移（向上偏移，模拟枪口上扬）
        float recoilOffset = currentWeapon != null ? currentWeapon.CurrentRecoilAngle : 0f;
        aimPivot.rotation = baseRot * Quaternion.Euler(0f, 0f, recoilOffset);
    }

    // ── 武器输入 ──────────────────────────────────────
    private void HandleWeaponInput()
    {
        if (currentWeapon == null) return;

        // 传入当前帧扳机状态（支持全自动/半自动）
        currentWeapon.TryShoot(triggerHeld);

        if (Input.GetKeyDown(KeyCode.R))
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
