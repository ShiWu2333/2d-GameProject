using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A*寻路算法
/// 基于Grid2D网格，提供路径查找服务
/// </summary>
public static class Pathfinder2D
{
    /// <summary>
    /// A*寻路：从起点到终点
    /// 返回世界坐标路径点列表（从起点到终点），找不到路返回null
    /// </summary>
    public static List<Vector2> FindPath(Vector2 startPos, Vector2 endPos)
    {
        var grid = Grid2D.Instance;
        if (grid == null || !grid.IsReady) return null;

        // 检查是否在网格范围内
        if (!grid.IsInBounds(startPos) || !grid.IsInBounds(endPos))
        {
            return null;
        }

        var startNode = grid.WorldToNode(startPos);
        var endNode = grid.WorldToNode(endPos);

        if (startNode == null || endNode == null) return null;

        // 终点不可走时，找终点附近最近的可走节点
        if (!endNode.walkable)
        {
            endNode = FindNearestWalkable(endNode, grid);
            if (endNode == null) return null;
        }

        // 起点不可走（卡在墙里）直接返回终点方向
        if (!startNode.walkable)
        {
            return new List<Vector2> { endNode.worldPosition };
        }

        var openSet = new List<Grid2D.Node> { startNode };
        var closedSet = new HashSet<Grid2D.Node>();

        // 重置节点数据
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, endNode);
        startNode.parent = null;

        int maxIterations = grid.GridWidth * grid.GridHeight;
        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // 找fCost最小的节点
            var current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < current.fCost ||
                    (openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost))
                {
                    current = openSet[i];
                }
            }

            openSet.Remove(current);
            closedSet.Add(current);

            // 到达终点
            if (current == endNode)
            {
                return RetracePath(startNode, endNode);
            }

            // 处理邻居
            foreach (var neighbour in grid.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;

                // 加上邻居的墙壁惩罚值，让路径远离墙壁
                float newCost = current.gCost + GetDistance(current, neighbour) + neighbour.penalty * 0.1f;
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, endNode);
                    neighbour.parent = current;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }

        // 找不到路径
        return null;
    }

    /// <summary>路径简化：移除不必要的中间点（用圆形扫描检测直通性，考虑角色碰撞体宽度）</summary>
    public static List<Vector2> SmoothPath(List<Vector2> path, LayerMask wallLayer)
    {
        if (path == null || path.Count <= 2) return path;

        float agentRadius = Grid2D.Instance != null ? Grid2D.Instance.obstacleCheckRadius : 0.35f;

        var smoothed = new List<Vector2> { path[0] };
        int current = 0;

        while (current < path.Count - 1)
        {
            int farthestVisible = current + 1;

            for (int i = current + 2; i < path.Count; i++)
            {
                Vector2 dir = path[i] - path[current];
                float dist = dir.magnitude;

                // 用CircleCast代替Raycast，考虑角色宽度
                RaycastHit2D hit = Physics2D.CircleCast(
                    path[current], agentRadius, dir.normalized, dist, wallLayer);
                if (hit.collider == null)
                {
                    farthestVisible = i;
                }
                else
                {
                    break;
                }
            }

            current = farthestVisible;
            smoothed.Add(path[current]);
        }

        return smoothed;
    }

    // ── 内部方法 ──────────────────────────────────

    private static List<Vector2> RetracePath(Grid2D.Node start, Grid2D.Node end)
    {
        var path = new List<Vector2>();
        var current = end;

        while (current != start)
        {
            path.Add(current.worldPosition);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    private static float GetDistance(Grid2D.Node a, Grid2D.Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);

        // 对角线代价1.414，直线代价1
        if (dx > dy)
            return 1.414f * dy + (dx - dy);
        return 1.414f * dx + (dy - dx);
    }

    private static Grid2D.Node FindNearestWalkable(Grid2D.Node target, Grid2D grid)
    {
        // BFS找最近可走节点
        var queue = new Queue<Grid2D.Node>();
        var visited = new HashSet<Grid2D.Node>();
        queue.Enqueue(target);
        visited.Add(target);

        int maxSearch = 100;
        int searched = 0;

        while (queue.Count > 0 && searched < maxSearch)
        {
            searched++;
            var current = queue.Dequeue();

            if (current.walkable)
                return current;

            foreach (var n in grid.GetNeighbours(current))
            {
                if (!visited.Contains(n))
                {
                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }
        }

        return null;
    }
}
