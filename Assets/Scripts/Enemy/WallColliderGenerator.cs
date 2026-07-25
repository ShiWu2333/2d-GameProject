using UnityEngine;

/// <summary>
/// 墙壁碰撞体生成器
/// 给"墙壁组"下所有子物体添加 BoxCollider2D
/// </summary>
public class WallColliderGenerator : MonoBehaviour
{
    [Tooltip("墙壁组父物体。留空则自动查找名为'墙壁组'的物体。")]
    public Transform wallParent;

    [Tooltip("是否递归处理墙壁组下的所有子物体。")]
    public bool includeNestedChildren = true;

    [Tooltip("是否处理未激活的墙壁子物体。")]
    public bool includeInactive = true;

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
            Debug.LogWarning("[WallColliderGenerator] 找不到'墙壁组'");
            return;
        }

        hasGenerated = true;

        int count = 0;

        Transform[] wallTransforms = includeNestedChildren
            ? wallParent.GetComponentsInChildren<Transform>(includeInactive)
            : GetDirectChildren(wallParent);

        int wallLayer = LayerMask.NameToLayer("Wall");
        ConfigureWallObject(wallParent, wallLayer, ref count);

        // 遍历所有子物体（不依赖 SpriteRenderer）
        foreach (Transform child in wallTransforms)
        {
            if (child == null || child == wallParent) continue;
            ConfigureWallObject(child, wallLayer, ref count);
        }

        Debug.Log($"[WallColliderGenerator] 已配置 {count} 个墙壁碰撞体");
    }

    public void Regenerate()
    {
        hasGenerated = false;
        Generate();
    }

    private void ConfigureWallObject(Transform target, int wallLayer, ref int count)
    {
        if (target == null) return;

        var go = target.gameObject;
        if (go == null) return;

        var renderer = go.GetComponent<Renderer>();
        var collider = go.GetComponent<Collider2D>();
        if (renderer == null && collider == null)
            return;

        if (wallLayer >= 0)
            go.layer = wallLayer;

        var box = collider as BoxCollider2D;
        bool createdBox = false;
        if (collider == null)
        {
            box = go.AddComponent<BoxCollider2D>();
            createdBox = true;
        }
        else
        {
            collider.isTrigger = false;
        }

        if (box == null)
        {
            Debug.LogWarning($"[WallColliderGenerator] {go.name} 已有非 BoxCollider2D，已设为实体墙但不改尺寸");
            count++;
            return;
        }

        if (!createdBox)
        {
            box.isTrigger = false;
            count++;
            return;
        }

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            box.size = spriteRenderer.sprite.bounds.size;
            box.offset = Vector2.zero;
        }
        else if (renderer != null)
        {
            box.size = Vector2.one;
            box.offset = Vector2.zero;
        }
        else if (box.size.sqrMagnitude <= 0.0001f)
        {
            box.size = Vector2.one;
            box.offset = Vector2.zero;
        }

        box.isTrigger = false;
        count++;
    }

    private Transform FindWallParent()
    {
        string[] names =
        {
            "墙壁组",
            "Walls",
            "WallGroup",
            "Wall Group",
            "MVP_Walls"
        };

        foreach (string name in names)
        {
            var go = GameObject.Find(name);
            if (go != null)
                return go.transform;
        }

        return null;
    }

    private Transform[] GetDirectChildren(Transform parent)
    {
        Transform[] children = new Transform[parent.childCount];
        for (int i = 0; i < parent.childCount; i++)
            children[i] = parent.GetChild(i);
        return children;
    }
}
