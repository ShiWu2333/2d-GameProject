using UnityEngine;

/// <summary>
/// 巡逻路线
/// 场景中放置此脚本作为巡逻点集合，敌人AI引用它来获取巡逻路径
/// 子物体作为巡逻点，按顺序排列
/// </summary>
public class PatrolRoute : MonoBehaviour
{
    [Header("巡逻模式")]
    public PatrolMode mode = PatrolMode.Loop;

    [Tooltip("每个巡逻点的停留时间")]
    public float waitTimePerPoint = 2f;

    [Tooltip("是否显示路径Gizmo")]
    public bool showPath = true;

    public enum PatrolMode
    {
        Loop,       // 循环：A→B→C→A→B→C...
        PingPong,   // 折返：A→B→C→B→A→B...
    }

    /// <summary>获取巡逻点数量</summary>
    public int PointCount => transform.childCount;

    /// <summary>获取指定索引的巡逻点世界坐标</summary>
    public Vector2 GetPoint(int index)
    {
        if (index < 0 || index >= transform.childCount)
            return transform.position;

        return transform.GetChild(index).position;
    }

    /// <summary>获取下一个巡逻点索引</summary>
    public int GetNextIndex(int currentIndex, ref bool isReversing)
    {
        if (transform.childCount <= 1) return 0;

        switch (mode)
        {
            case PatrolMode.Loop:
                return (currentIndex + 1) % transform.childCount;

            case PatrolMode.PingPong:
                if (isReversing)
                {
                    if (currentIndex <= 0)
                    {
                        isReversing = false;
                        return 1;
                    }
                    return currentIndex - 1;
                }
                else
                {
                    if (currentIndex >= transform.childCount - 1)
                    {
                        isReversing = true;
                        return currentIndex - 1;
                    }
                    return currentIndex + 1;
                }
        }

        return 0;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showPath || transform.childCount < 2) return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < transform.childCount; i++)
        {
            var current = transform.GetChild(i);
            if (current == null) continue;

            // 画点
            Gizmos.DrawSphere(current.position, 0.15f);

            // 画线
            int nextIndex = (i + 1) % transform.childCount;
            if (mode == PatrolMode.PingPong && i == transform.childCount - 1)
                continue;

            var next = transform.GetChild(nextIndex);
            if (next != null)
                Gizmos.DrawLine(current.position, next.position);
        }

        // 循环模式画回起点的线
        if (mode == PatrolMode.Loop && transform.childCount > 2)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawLine(
                transform.GetChild(transform.childCount - 1).position,
                transform.GetChild(0).position);
        }
    }
#endif
}
