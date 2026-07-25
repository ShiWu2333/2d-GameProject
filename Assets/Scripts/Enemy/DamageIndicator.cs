using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家受击屏幕闪红效果
/// 挂在有 PlayerStats 的玩家身上
/// 自动在Canvas上创建红色闪烁覆盖层
/// </summary>
public class DamageIndicator : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("受击时红色覆盖层的最大透明度")]
    public float maxAlpha = 0.3f;

    [Tooltip("闪烁消退速度")]
    public float fadeSpeed = 2f;

    [Tooltip("低血量持续红色阈值（比例）")]
    public float lowHealthThreshold = 0.3f;

    [Tooltip("低血量时的持续红色透明度")]
    public float lowHealthAlpha = 0.1f;

    private Image overlayImage;
    private PlayerStats stats;
    private float currentAlpha;
    private float targetAlpha;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.onHealthChanged.AddListener(OnHealthChanged);
        CreateOverlay();
    }

    private void CreateOverlay()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("DamageOverlay");
        go.transform.SetParent(canvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlayImage = go.AddComponent<Image>();
        overlayImage.color = new Color(0.8f, 0f, 0f, 0f);
        overlayImage.raycastTarget = false;

        // 确保在最顶层
        go.transform.SetAsLastSibling();
    }

    private float lastHealth;

    private void OnHealthChanged(float current, float max)
    {
        // 只在血量下降时闪烁
        if (current < lastHealth)
        {
            float damageRatio = (lastHealth - current) / max;
            targetAlpha = Mathf.Clamp(damageRatio * 2f, 0.1f, maxAlpha);
            currentAlpha = targetAlpha;
        }

        lastHealth = current;
    }

    void Update()
    {
        if (overlayImage == null || stats == null) return;

        // 消退
        if (currentAlpha > 0f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, fadeSpeed * Time.deltaTime);
        }

        // 低血量持续红色
        float healthRatio = stats.currentHealth / stats.maxHealth;
        float baseAlpha = healthRatio < lowHealthThreshold ? lowHealthAlpha : 0f;

        float finalAlpha = Mathf.Max(currentAlpha, baseAlpha);
        overlayImage.color = new Color(0.8f, 0f, 0f, finalAlpha);
    }
}
