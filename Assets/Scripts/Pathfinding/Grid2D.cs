using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 2D网格寻路 - 基于物理碰撞检测生成可行走网格
/// 在场景中放一个空物体挂此脚本，运行时自动扫描障碍生成网格
/// </summary>
public class Grid2D : MonoBehaviour
{
    public static Grid2D Instance { get; private set; }

    [Header("网格设置")]
    [Tooltip("网格中心位置")]
    public Vector2 gridCenter = Vector2.zero;

    [Tooltip("网格世界尺寸")]
    public Vector2 gridSize = new Vector2(80f, 80f);

    [Tooltip("每个节点的大小（越小越精确但性能消耗越大）")]
    public float nodeSize = 0.5f;

    [Tooltip("障碍物检测层")]
    public LayerMask obstacleLayer;

    [Tooltip("障碍物检测扩展半径（角色半径）")]
    public float obstacleCheckRadius = 0.45f;

    [Header("调试")]
    public bool drawGizmos = false;

    [HideInInspector]
    public bool delayGeneration = false; // 为true时Awake不自动生成网格

    // 网格数据
    private Node[,] grid;
    private int gridWidth;
    private int gridHeight;
    private bool gridReady = false;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public bool IsReady => gridReady;

    /// <summary>寻路节点</summary>
    public class Node
    {
        public bool walkable;
        public Vector2 worldPosition;
        public int gridX;
        public int gridY;
        public int penalty; // 靠近墙壁的额外代价

        // A* 数据
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        public Node parent;

        public Node(bool walkable, Vector2 worldPos, int x, int y, int penalty = 0)
        {
            this.walkable = walkable;
            this.worldPosition = worldPos;
            this.gridX = x;
            this.gridY = y;
            this.penalty = penalty;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!delayGeneration)
            GenerateGrid();
    }

    /// <summary>生成/刷新网格</summary>
    public void GenerateGrid()
    {
        gridWidth = Mathf.RoundToInt(gridSize.x / nodeSize);
        gridHeight = Mathf.RoundToInt(gridSize.y / nodeSize);
        grid = new Node[gridWidth, gridHeight];

        Vector2 bottomLeft = gridCenter - gridSize * 0.5f;

        int unwalkableCount = 0;

        // 第一遍：标记可行走性
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 worldPoint = bottomLeft + new Vector2(
                    x * nodeSize + nodeSize * 0.5f,
                    y * nodeSize + nodeSize * 0.5f
                );

                // 检测该点是否有碰撞体覆盖（非trigger的实体碰撞体=障碍）
                bool walkable = IsPointWalkable(worldPoint);
                grid[x, y] = new Node(walkable, worldPoint, x, y);
                if (!walkable) unwalkableCount++;
            }
        }

        // 第二遍：计算靠近墙壁的惩罚值（让路径远离墙壁）
        int blurSize = 2;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (!grid[x, y].walkable) continue;

                int penalty = 0;
                for (int dx = -blurSize; dx <= blurSize; dx++)
                {
                    for (int dy = -blurSize; dy <= blurSize; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight) continue;
                        if (!grid[nx, ny].walkable)
                        {
                            int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                            penalty += (blurSize * 2 + 1 - dist) * 2;
                        }
                    }
                }
                grid[x, y].penalty = penalty;
            }
        }

        gridReady = true;
    }

    /// <summary>检测某点是否可行走</summary>
    private bool IsPointWalkable(Vector2 point)
    {
        // 检测该点覆盖的碰撞体
        Collider2D[] colliders;

        if (obstacleLayer.value != 0)
        {
            colliders = Physics2D.OverlapCircleAll(point, obstacleCheckRadius, obstacleLayer);
        }
        else
        {
            colliders = Physics2D.OverlapCircleAll(point, obstacleCheckRadius);
        }

        foreach (var col in colliders)
        {
            if (col == null || col.isTrigger) continue;

            // 只有Static碰撞体（无Rigidbody或Static类型的Rigidbody）才视为墙壁障碍
            // 排除敌人、玩家、子弹等动态物体
            var rb = col.attachedRigidbody;
            if (rb != null && rb.bodyType != RigidbodyType2D.Static)
                continue;

            // 这是一个静态实体碰撞体 = 墙壁
            return false;
        }
        return true;
    }

    /// <summary>世界坐标转网格节点</summary>
    public Node WorldToNode(Vector2 worldPos)
    {
        if (grid == null) return null;

        Vector2 bottomLeft = gridCenter - gridSize * 0.5f;
        float percentX = (worldPos.x - bottomLeft.x) / gridSize.x;
        float percentY = (worldPos.y - bottomLeft.y) / gridSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.Clamp(Mathf.FloorToInt(percentX * gridWidth), 0, gridWidth - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(percentY * gridHeight), 0, gridHeight - 1);

        return grid[x, y];
    }

    /// <summary>检查世界坐标是否在网格范围内</summary>
    public bool IsInBounds(Vector2 worldPos)
    {
        Vector2 bottomLeft = gridCenter - gridSize * 0.5f;
        Vector2 topRight = gridCenter + gridSize * 0.5f;
        return worldPos.x >= bottomLeft.x && worldPos.x <= topRight.x
            && worldPos.y >= bottomLeft.y && worldPos.y <= topRight.y;
    }

    /// <summary>获取相邻节点（8方向）</summary>
    public List<Node> GetNeighbours(Node node)
    {
        var neighbours = new List<Node>(8);
        if (grid == null) return neighbours;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int checkX = node.gridX + dx;
                int checkY = node.gridY + dy;

                if (checkX >= 0 && checkX < gridWidth && checkY >= 0 && checkY < gridHeight)
                {
                    // 对角线移动时检查相邻两格是否都可走（防止穿墙角）
                    if (dx != 0 && dy != 0)
                    {
                        if (!grid[node.gridX + dx, node.gridY].walkable ||
                            !grid[node.gridX, node.gridY + dy].walkable)
                            continue;
                    }

                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    /// <summary>检查某世界坐标是否可行走</summary>
    public bool IsWalkable(Vector2 worldPos)
    {
        var node = WorldToNode(worldPos);
        return node != null && node.walkable;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos || grid == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                var node = grid[x, y];
                Gizmos.color = node.walkable
                    ? new Color(0f, 1f, 0f, 0.1f)
                    : new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawCube(node.worldPosition, Vector3.one * nodeSize * 0.9f);
            }
        }

        // 网格边界
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(gridCenter, gridSize);
    }
#endif
}
