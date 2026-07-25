using UnityEngine;

/// <summary>
/// 敌人血条（世界空间）
/// 受击后显示，过一段时间隐藏
/// </summary>
[RequireComponent(typeof(EnemyStats))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("血条显示时间（秒），被击后开始计时")]
    public float showDuration = 3f;

    [Tooltip("血条Y轴偏移（相对敌人中心）")]
    public float yOffset = 0.8f;

    [Tooltip("血条宽度（世界单位）")]
    public float barWidth = 0.8f;

    [Tooltip("血条高度（世界单位）")]
    public float barHeight = 0.08f;

    private EnemyStats stats;
    private GameObject barRoot;
    private Transform fillTransform;
    private SpriteRenderer fillRenderer;
    private SpriteRenderer bgRenderer;
    private float showTimer;

    void Start()
    {
        stats = GetComponent<EnemyStats>();
        if (stats == null) return;

        if (stats.onHit != null)
            stats.onHit.AddListener(OnHit);
        if (stats.onHealthChanged != null)
            stats.onHealthChanged.AddListener(OnHealthChanged);

        CreateHealthBar();
        barRoot.SetActive(false);
    }

    private void CreateHealthBar()
    {
        // 用1x1白色sprite做背景和填充
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);

        // 根节点
        barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = new Vector3(-barWidth * 0.5f, yOffset, 0f);
        barRoot.transform.localScale = Vector3.one;

        // 背景（深灰）
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(barRoot.transform, false);
        bgGO.transform.localPosition = Vector3.zero;
        bgGO.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        bgRenderer = bgGO.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = sprite;
        bgRenderer.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        bgRenderer.sortingOrder = 100;

        // 填充（红色）
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barRoot.transform, false);
        fillGO.transform.localPosition = Vector3.zero;
        fillGO.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        fillRenderer = fillGO.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = sprite;
        fillRenderer.color = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        fillRenderer.sortingOrder = 101;
        fillTransform = fillGO.transform;
    }

    void Update()
    {
        if (barRoot == null) return;

        // 血条始终面向相机（世界空间不旋转）
        barRoot.transform.rotation = Quaternion.identity;

        // 计时隐藏
        if (showTimer > 0f)
        {
            showTimer -= Time.deltaTime;
            if (showTimer <= 0f)
                barRoot.SetActive(false);
        }
    }

    private void OnHit()
    {
        if (stats.IsDead) return;
        barRoot.SetActive(true);
        showTimer = showDuration;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (fillTransform == null) return;
        float ratio = Mathf.Clamp01(current / max);
        fillTransform.localScale = new Vector3(barWidth * ratio, barHeight, 1f);
    }

    void OnDestroy()
    {
        if (barRoot != null)
            Destroy(barRoot);
    }
}
