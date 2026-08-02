using UnityEngine;

/// <summary>
/// 寻路系统配置（仅作为标记，实际初始化由EnemyAI.EnsureGrid完成）
/// </summary>
public class PathfindingSetup : MonoBehaviour
{
    // 不再自动创建，由EnemyAI负责
}

/// <summary>
/// 保留兼容性
/// </summary>
public class DelayedGridGeneration : MonoBehaviour
{
    void Start()
    {
        Destroy(this);
    }
}
