using UnityEngine;

/// <summary>
/// 相机跟随玩家
/// 挂在 Main Camera 上，平滑跟随目标位置。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("跟随目标（留空则自动查找 Player Tag）")]
    public Transform target;

    [Tooltip("跟随平滑速度（越大越快，0 = 无延迟）")]
    public float smoothSpeed = 8f;

    [Tooltip("相机与目标的固定Z轴偏移")]
    public float zOffset = -10f;

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(target.position.x, target.position.y, zOffset);

        if (smoothSpeed <= 0f)
        {
            transform.position = targetPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        }
    }
}
