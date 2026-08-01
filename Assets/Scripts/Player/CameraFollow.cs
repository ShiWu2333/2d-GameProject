using UnityEngine;

/// <summary>
/// 俯视角 2D 摄像机跟随系统（重做版）
/// 
/// 核心设计：
/// 1. 相机直接锁定玩家位置（零延迟），消除任何物理帧/渲染帧不同步导致的抖动
/// 2. 可选：鼠标前瞻偏移（让视野偏向鼠标方向），使用 SmoothDamp 平滑
/// 3. 可选：屏幕震动接口
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // ══════════════════════════════════════════════════
    //  配置
    // ══════════════════════════════════════════════════

    [Header("跟随目标")]
    public Transform target;

    [Header("跟随模式")]
    [Tooltip("true = 完全锁定玩家，无任何平滑延迟；false = 使用 SmoothDamp 轻微平滑")]
    public bool hardLock = true;

    [Tooltip("仅 hardLock=false 时生效，平滑时间（推荐 0.02~0.08）")]
    public float smoothTime = 0.05f;

    [Header("鼠标前瞻（可选）")]
    [Tooltip("是否启用鼠标前瞻偏移")]
    public bool enableMouseLead = false;

    [Tooltip("前瞻强度（0~1），1 = 完全偏移到鼠标方向")]
    [Range(0f, 1f)]
    public float leadStrength = 0.3f;

    [Tooltip("最大前瞻距离（世界单位）")]
    public float maxLeadDistance = 2.5f;

    [Tooltip("前瞻平滑时间")]
    public float leadSmoothTime = 0.15f;

    // ══════════════════════════════════════════════════
    //  震动
    // ══════════════════════════════════════════════════

    [Header("屏幕震动")]
    [Tooltip("震动衰减速度")]
    public float shakeDecay = 5f;

    // ══════════════════════════════════════════════════
    //  私有状态
    // ══════════════════════════════════════════════════

    private Camera cam;
    private Vector3 followVelocity = Vector3.zero;
    private Vector2 leadVelocity = Vector2.zero;
    private Vector2 currentLead = Vector2.zero;

    // 震动
    private float shakeIntensity;
    private float shakeDuration;
    private float shakeTimer;

    // ══════════════════════════════════════════════════
    //  生命周期
    // ══════════════════════════════════════════════════

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 基础位置：玩家当前位置
        Vector3 basePos = new Vector3(target.position.x, target.position.y, transform.position.z);

        // 2. 鼠标前瞻偏移
        Vector3 leadOffset = Vector3.zero;
        if (enableMouseLead && cam != null)
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(cam.transform.position.z)));
            Vector2 toMouse = (Vector2)(mouseWorld - target.position);
            Vector2 targetLead = Vector2.ClampMagnitude(toMouse * leadStrength, maxLeadDistance);

            currentLead = Vector2.SmoothDamp(currentLead, targetLead, ref leadVelocity, leadSmoothTime);
            leadOffset = (Vector3)currentLead;
        }

        // 3. 最终目标位置
        Vector3 desiredPos = basePos + leadOffset;

        // 4. 应用跟随
        if (hardLock)
        {
            transform.position = desiredPos;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref followVelocity, smoothTime);
        }

        // 5. 屏幕震动叠加
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float currentShake = shakeIntensity * (shakeTimer / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * currentShake;
            transform.position += (Vector3)offset;
        }
    }

    // ══════════════════════════════════════════════════
    //  公开接口
    // ══════════════════════════════════════════════════

    /// <summary>
    /// 触发屏幕震动
    /// </summary>
    /// <param name="intensity">震动强度（世界单位）</param>
    /// <param name="duration">持续时间（秒）</param>
    public void Shake(float intensity, float duration)
    {
        // 取较大的震动（避免弱震动覆盖强震动）
        if (intensity > shakeIntensity * (shakeTimer / Mathf.Max(shakeDuration, 0.001f)))
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }
    }

    /// <summary>
    /// 立即将相机传送到目标位置（用于场景切换/重生）
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        followVelocity = Vector3.zero;
        currentLead = Vector2.zero;
        leadVelocity = Vector2.zero;
        shakeTimer = 0f;
    }
}
