using UnityEngine;

/// <summary>
/// 玩家移动控制器
///
/// 移动方式：直接修改 transform.position，无加速无滑行。
/// 墙壁阻挡：移动前用 Rigidbody2D.Cast 检测碰撞，碰到就停。
/// 朝向：身体和 AimPivot 面向鼠标。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("速度")]
    public float walkSpeed    = 3.5f;
    public float sprintSpeed  = 6f;

    [Tooltip("持刀附加移速倍率")]
    public float knifeSpeedBonus = 1.15f;

    [Header("朝向")]
    [Tooltip("武器挂载节点，跟随鼠标旋转")]
    public Transform aimPivot;

    [Tooltip("身体旋转速度（度/秒），0 = 瞬间")]
    public float bodyRotateSpeed = 720f;

    [Header("碰撞")]
    [Tooltip("碰撞检测时的额外间距")]
    public float skinWidth = 0.01f;

    // ── 缓存 ─────────────────────────────────────────
    private Rigidbody2D       rb;
    private CapsuleCollider2D col;
    private PlayerStats       stats;
    private Camera            cam;
    private ArmorComponent    armor;
    private WeaponBase        currentWeapon;
    private ContactFilter2D   moveFilter;
    private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];

    // ── 状态 ─────────────────────────────────────────
    private Vector2 moveDir;
    private bool    isSprinting;
    private bool    triggerHeld;
    private bool    isAiming;

    // ── 公开属性 ─────────────────────────────────────
    public Vector2 MouseWorldPos { get; private set; }
    public bool    IsAiming    => isAiming;
    public bool    IsSprinting => isSprinting;
    public Vector2 MoveInput   => moveDir;

    // ═════════════════════════════════════════════════
    void Awake()
    {
        rb    = GetComponent<Rigidbody2D>();
        col   = GetComponent<CapsuleCollider2D>();
        stats = GetComponent<PlayerStats>();
        armor = GetComponent<ArmorComponent>();
        cam   = Camera.main;

        // 碰撞体大小：略大于贴图
        col.size   = new Vector2(0.75f, 1.0f);
        col.offset = Vector2.zero;

        // Rigidbody 设为 Kinematic，纯粹用来做 Cast 碰撞检测
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.simulated    = true;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.useFullKinematicContacts = false;

        // 碰撞过滤：排除 Trigger
        moveFilter = new ContactFilter2D();
        moveFilter.useTriggers = false;
        moveFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        moveFilter.useLayerMask = true;

        // 清理遗留
        var wallGuard = GetComponent<WallCollisionGuard>();
        if (wallGuard != null) Destroy(wallGuard);
    }

    // ═════════════════════════════════════════════════
    void Update()
    {
        if (!stats.IsAlive) return;
        if (PauseMenu.IsGamePaused) return;
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (IsUIBlocking())
        {
            moveDir = Vector2.zero;
            return;
        }

        ReadInput();
        UpdateMousePos();
        FaceTowardsMouse();
        Move();
    }

    // ═════════════════════════════════════════════════
    //  移动（带碰撞滑墙）
    // ═════════════════════════════════════════════════

    private void Move()
    {
        if (moveDir.sqrMagnitude < 0.001f) return;

        float speed = CalcSpeed();
        float dist  = speed * Time.deltaTime;

        // 第一步：尝试完整方向
        float moved = DoMove(moveDir, dist);

        // 第二步：如果被挡住且是对角线输入，尝试分轴滑动
        if (moved < dist - 0.001f && Mathf.Abs(moveDir.x) > 0.1f && Mathf.Abs(moveDir.y) > 0.1f)
        {
            float remaining = dist - moved;

            // 先试水平
            Vector2 hDir = new Vector2(Mathf.Sign(moveDir.x), 0f);
            float hMoved = DoMove(hDir, remaining);

            // 水平也走不了就试垂直
            if (hMoved < 0.001f)
            {
                Vector2 vDir = new Vector2(0f, Mathf.Sign(moveDir.y));
                DoMove(vDir, remaining);
            }
        }
    }

    /// <summary>
    /// 向 direction 移动 distance，碰到墙停住。返回实际移动距离。
    /// </summary>
    private float DoMove(Vector2 direction, float distance)
    {
        // 先把碰撞体稍微缩一点做 Cast，避免已接触的表面干扰
        int hitCount = rb.Cast(direction, moveFilter, hitBuffer, distance + skinWidth);

        float safeDistance = distance;
        for (int i = 0; i < hitCount; i++)
        {
            // 忽略法线与移动方向夹角大于90°的（不是挡在前面的墙）
            float dot = Vector2.Dot(hitBuffer[i].normal, direction);
            if (dot >= -0.01f) continue; // 法线和移动方向不对立，说明不是阻挡

            float d = hitBuffer[i].distance - skinWidth;
            if (d < safeDistance)
                safeDistance = d;
        }

        if (safeDistance <= 0f) return 0f;

        transform.position += (Vector3)(direction * safeDistance);
        return safeDistance;
    }

    // ═════════════════════════════════════════════════
    //  输入
    // ═════════════════════════════════════════════════

    // 后键优先记录
    private int lastHDir = 0; // -1=左, 1=右, 0=无
    private int lastVDir = 0; // -1=下, 1=上, 0=无

    private void ReadInput()
    {
        var kb = KeyBindings.Instance;

        // 水平输入（后键优先）
        bool left  = kb != null ? Input.GetKey(kb.moveLeft)  : Input.GetKey(KeyCode.A);
        bool right = kb != null ? Input.GetKey(kb.moveRight) : Input.GetKey(KeyCode.D);

        if (left && right)
        {
            // 两个都按着，用后按的方向
            // 如果之前记录的是右，说明左是后按的，反之亦然
            // 检测是否有新按下
            bool leftDown  = kb != null ? Input.GetKeyDown(kb.moveLeft)  : Input.GetKeyDown(KeyCode.A);
            bool rightDown = kb != null ? Input.GetKeyDown(kb.moveRight) : Input.GetKeyDown(KeyCode.D);
            if (leftDown)       lastHDir = -1;
            else if (rightDown) lastHDir = 1;
            // 否则保持上一帧的 lastHDir
        }
        else if (left)  lastHDir = -1;
        else if (right) lastHDir = 1;
        else            lastHDir = 0;

        // 垂直输入（后键优先）
        bool down = kb != null ? Input.GetKey(kb.moveDown) : Input.GetKey(KeyCode.S);
        bool up   = kb != null ? Input.GetKey(kb.moveUp)   : Input.GetKey(KeyCode.W);

        if (down && up)
        {
            bool downDown = kb != null ? Input.GetKeyDown(kb.moveDown) : Input.GetKeyDown(KeyCode.S);
            bool upDown   = kb != null ? Input.GetKeyDown(kb.moveUp)   : Input.GetKeyDown(KeyCode.W);
            if (downDown)    lastVDir = -1;
            else if (upDown) lastVDir = 1;
        }
        else if (down) lastVDir = -1;
        else if (up)   lastVDir = 1;
        else           lastVDir = 0;

        moveDir = new Vector2(lastHDir, lastVDir).normalized;

        triggerHeld = Input.GetMouseButton(0);
        isAiming    = Input.GetMouseButton(1)
                      && currentWeapon != null
                      && !(currentWeapon is Knife);

        if (currentWeapon != null)
            currentWeapon.IsAiming = isAiming;

        KeyCode sprintKey = kb != null ? kb.sprint : KeyCode.LeftShift;
        isSprinting = Input.GetKey(sprintKey)
                      && moveDir.sqrMagnitude > 0f
                      && stats.HasStamina
                      && !triggerHeld
                      && !isAiming;

        stats.TickStamina(isSprinting);

        // 武器射击 / 换弹
        if (currentWeapon != null)
        {
            currentWeapon.TryShoot(triggerHeld);
            KeyCode reloadKey = kb != null ? kb.reload : KeyCode.R;
            if (Input.GetKeyDown(reloadKey))
                currentWeapon.TryReload();
        }
    }

    private float CalcSpeed()
    {
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        if (armor != null)
            speed *= isSprinting ? armor.SprintSpeedPenalty : armor.WalkSpeedPenalty;

        if (currentWeapon != null)
        {
            speed *= currentWeapon.moveSpeedMult;
            if (currentWeapon is Knife)  speed *= knifeSpeedBonus;
            if (isAiming)                speed *= currentWeapon.aimMoveSpeedMult;
        }
        return speed;
    }

    // ═════════════════════════════════════════════════
    //  朝向
    // ═════════════════════════════════════════════════

    private void UpdateMousePos()
    {
        if (cam == null) return;
        Vector3 s = Input.mousePosition;
        s.z = Mathf.Abs(cam.transform.position.z);
        MouseWorldPos = cam.ScreenToWorldPoint(s);
    }

    private void FaceTowardsMouse()
    {
        Vector2 dir = MouseWorldPos - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 身体朝向鼠标（贴图正面朝上，偏移-90°）
        Quaternion bodyTarget = Quaternion.Euler(0f, 0f, angle - 90f);
        transform.rotation = bodyRotateSpeed <= 0f
            ? bodyTarget
            : Quaternion.RotateTowards(transform.rotation, bodyTarget,
                                       bodyRotateSpeed * Time.deltaTime);

        if (aimPivot != null)
            aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ═════════════════════════════════════════════════
    //  公开接口
    // ═════════════════════════════════════════════════

    public void EquipWeapon(WeaponBase weapon)
    {
        currentWeapon = weapon;
    }

    // ═════════════════════════════════════════════════
    //  工具
    // ═════════════════════════════════════════════════

    private bool IsUIBlocking()
    {
        var inv = GetComponent<InventorySystem>();
        if (inv != null && inv.IsOpen) return true;
        if (ContainerUI.Instance != null && ContainerUI.Instance.IsOpen) return true;
        return false;
    }

    void FixedUpdate() { }
}
