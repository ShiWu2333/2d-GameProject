using UnityEngine;

/// <summary>
/// 墙壁碰撞体生成器
/// 确保"墙壁组"下所有子物体：
/// 1. 有 BoxCollider2D（非 Trigger）
/// 2. 有 Rigidbody2D（Static，不参与物理模拟但确保碰撞响应）
/// 3. 在 Wall 层
/// </summary>
public class WallColliderGenerator : MonoBehaviour
{
    [Tooltip("墙壁组父物体。留空则自动查找。")]
    public Transform wallParent;

    private bool hasGenerated = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapWallColliders()
    {
        var generator = FindObjectOfType<WallColliderGenerator>();
        if (generator == null)
        {
            var go = new GameObject("WallColliderGenerator");
            generator = go.AddComponent<WallColliderGenerator>();
        }
        generator.Generate();
    }

    void Start()
    {
        if (!hasGenerated)
            Generate();
    }

    public void Generate()
    {
        if (hasGenerated) return;

        if (wallParent == null)
            wallParent = FindWallParent();

        if (wallParent == null)
        {
            Debug.LogWarning("[WallColliderGenerator] 找不到墙壁组");
            return;
        }

        hasGenerated = true;
        int wallLayer = LayerMask.NameToLayer("Wall");
        int count = 0;

        // 处理墙壁组自身和所有子物体
        var allTransforms = wallParent.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t == null) continue;
            var go = t.gameObject;

            // 只处理有 Renderer 或已有 Collider 的物体
            if (go.GetComponent<Renderer>() == null && go.GetComponent<Collider2D>() == null)
                continue;

            // 设置 Layer
            if (wallLayer >= 0)
                go.layer = wallLayer;

            // 确保有 BoxCollider2D，且非 Trigger
            var col = go.GetComponent<Collider2D>();
            if (col == null)
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = false;

                // 根据 SpriteRenderer 自动设置尺寸
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    box.size = sr.sprite.bounds.size;
                    box.offset = Vector2.zero;
                }
            }
            else
            {
                col.isTrigger = false;
            }

            // 确保有 Static Rigidbody2D（让物理引擎正确处理碰撞）
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody2D>();
            }
            rb.bodyType    = RigidbodyType2D.Static;
            rb.simulated   = true;

            count++;
        }

        Debug.Log($"[WallColliderGenerator] 已配置 {count} 个墙壁（Static Rigidbody2D + BoxCollider2D）");
    }

    public void Regenerate()
    {
        hasGenerated = false;
        Generate();
    }

    private Transform FindWallParent()
    {
        string[] names = { "墙壁组", "Walls", "WallGroup", "Wall Group", "MVP_Walls" };
        foreach (string name in names)
        {
            var go = GameObject.Find(name);
            if (go != null) return go.transform;
        }
        return null;
    }
}
