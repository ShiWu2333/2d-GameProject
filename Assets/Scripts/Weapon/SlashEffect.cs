using UnityEngine;

/// <summary>
/// 刀挥砍弧线特效
/// 用 LineRenderer 画一条弧线，快速淡出后自毁
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SlashEffect : MonoBehaviour
{
    [Header("参数")]
    public float duration    = 0.15f;
    public float arcRadius   = 1.2f;
    public float arcAngle    = 90f;
    public int   segments    = 12;
    public float lineWidth   = 0.08f;
    public Color color       = Color.white;

    private LineRenderer lr;
    private float timer;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace   = false;
        lr.positionCount   = segments + 1;
        lr.startWidth      = lineWidth;
        lr.endWidth        = lineWidth * 0.4f;
        lr.startColor      = color;
        lr.endColor        = color;
        lr.sortingOrder    = 100;
        lr.material        = new Material(Shader.Find("Sprites/Default"));

        // 画弧线点
        float halfArc = arcAngle * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float t     = (float)i / segments;
            float angle = Mathf.Lerp(-halfArc, halfArc, t) * Mathf.Deg2Rad;
            float x     = Mathf.Cos(angle) * arcRadius;
            float y     = Mathf.Sin(angle) * arcRadius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }

        Destroy(gameObject, duration + 0.02f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = 1f - Mathf.Clamp01(timer / duration);

        Color c = color;
        c.a = alpha;
        lr.startColor = c;
        lr.endColor   = new Color(c.r, c.g, c.b, alpha * 0.5f);
    }
}
