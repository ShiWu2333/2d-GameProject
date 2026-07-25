using UnityEngine;

/// <summary>
/// 简易命中特效
/// 显示一个扩散消散的圆形/十字
/// </summary>
public class HitEffect : MonoBehaviour
{
    public float duration = 0.3f;
    public float expandSpeed = 3f;
    public Color color = new Color(1f, 0.8f, 0.2f, 0.8f);

    private float timer;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        sr.color = color;
        transform.localScale = new Vector3(0.1f, 0.1f, 1f);
        sr.sortingOrder = 50;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        // 扩散
        float scale = Mathf.Lerp(0.1f, 0.4f, t);
        transform.localScale = new Vector3(scale, scale, 1f);

        // 淡出
        if (sr != null)
        {
            Color c = color;
            c.a = Mathf.Lerp(color.a, 0f, t);
            sr.color = c;
        }

        if (timer >= duration)
            Destroy(gameObject);
    }

    /// <summary>在指定位置生成命中特效</summary>
    public static void Spawn(Vector3 position, Color color)
    {
        var go = new GameObject("HitFX");
        go.transform.position = position;
        var fx = go.AddComponent<HitEffect>();
        fx.color = color;
    }
}
