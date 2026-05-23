using UnityEngine;

/// <summary>
/// 摄像机平滑跟随玩家（俯视角 2D）
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("平滑速度")]
    public float smoothSpeed = 8f;

    [Header("偏移（朝鼠标方向偏移，增加视野）")]
    public float mouseLeadAmount = 1.5f;        // 摄像机向鼠标方向偏移的强度
    public float maxLeadDistance = 3f;          // 最大偏移距离

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 基础跟随位置
        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, transform.position.z);

        // 向鼠标方向偏移，扩大视野
        if (cam != null)
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 toMouse = (mouseWorld - target.position);
            toMouse = Vector2.ClampMagnitude(toMouse, maxLeadDistance);
            desiredPos += (Vector3)(toMouse * mouseLeadAmount * 0.3f);
        }

        // 平滑插值
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
