using UnityEngine;

/// <summary>
/// MVP wall collision fallback for top-down characters.
/// Keeps dynamic characters out of Wall colliders even if scene physics setup is incomplete.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WallCollisionGuard : MonoBehaviour
{
    public LayerMask wallLayer;
    public float depenetrationPadding = 0.02f;
    public int maxResolveIterations = 4;

    private Collider2D ownCollider;
    private Rigidbody2D rb;
    private readonly Collider2D[] overlaps = new Collider2D[12];

    private void Awake()
    {
        ownCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        if (wallLayer.value == 0)
        {
            int wallLayerIndex = LayerMask.NameToLayer("Wall");
            if (wallLayerIndex >= 0)
                wallLayer = 1 << wallLayerIndex;
        }
    }

    private void FixedUpdate()
    {
        ResolveWallOverlap();
    }

    private void LateUpdate()
    {
        ResolveWallOverlap();
    }

    private void ResolveWallOverlap()
    {
        if (ownCollider == null || wallLayer.value == 0)
            return;

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = wallLayer,
            useTriggers = false
        };

        for (int iteration = 0; iteration < maxResolveIterations; iteration++)
        {
            int count = ownCollider.OverlapCollider(filter, overlaps);
            if (count <= 0)
                return;

            bool moved = false;
            for (int i = 0; i < count; i++)
            {
                Collider2D wall = overlaps[i];
                if (wall == null || wall.isTrigger)
                    continue;

                ColliderDistance2D distance = ownCollider.Distance(wall);
                if (!distance.isOverlapped)
                    continue;

                Vector2 correction = -distance.normal * (Mathf.Abs(distance.distance) + depenetrationPadding);
                if (correction.sqrMagnitude <= 0.000001f)
                    continue;

                Vector2 target = (Vector2)transform.position + correction;
                if (rb != null)
                {
                    rb.position = target;
                    rb.velocity = Vector2.zero;
                }
                else
                {
                    transform.position = target;
                }

                moved = true;
            }

            if (!moved)
                return;
        }
    }
}
