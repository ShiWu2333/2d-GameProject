using UnityEngine;

/// <summary>
/// 武器图层排序器
/// 确保武器所有部件（枪身+枪口）始终渲染在角色之上
/// 挂在武器根对象上，Awake时自动设置
/// </summary>
public class WeaponLayerSorter : MonoBehaviour
{
    [Header("图层偏移（相对于角色 sortingOrder）")]
    [Tooltip("枪身图层偏移")]
    public int bodyLayerOffset = 1;

    [Tooltip("枪管/枪口图层偏移")]
    public int barrelLayerOffset = 2;

    [Header("自动查找")]
    public bool autoFindSpriteRenderers = true;
    public string bodyObjectName   = "Body";
    public string barrelObjectName = "Barrel";

    // 运行时引用
    private SpriteRenderer playerRenderer;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer barrelRenderer;

    private void Awake()
    {
        if (autoFindSpriteRenderers)
            FindRenderers();
    }

    private void Start()
    {
        ApplySorting();
    }

    /// <summary>
    /// 武器被装备时由WeaponBase调用
    /// </summary>
    public void OnWeaponEquipped(PlayerController player)
    {
        if (player != null)
            playerRenderer = player.GetComponent<SpriteRenderer>();

        if (autoFindSpriteRenderers)
            FindRenderers();

        ApplySorting();
    }

    /// <summary>
    /// 武器被卸下时调用
    /// </summary>
    public void OnWeaponUnequipped()
    {
        if (bodyRenderer != null)   bodyRenderer.sortingOrder = 0;
        if (barrelRenderer != null) barrelRenderer.sortingOrder = 0;
    }

    /// <summary>
    /// 应用图层排序
    /// </summary>
    public void ApplySorting()
    {
        // 如果还没找到玩家renderer，尝试从父级获取
        if (playerRenderer == null)
        {
            var pc = GetComponentInParent<PlayerController>();
            if (pc != null)
                playerRenderer = pc.GetComponent<SpriteRenderer>();
        }

        int baseOrder = playerRenderer != null ? playerRenderer.sortingOrder : 0;

        if (bodyRenderer != null)
            bodyRenderer.sortingOrder = baseOrder + bodyLayerOffset;

        if (barrelRenderer != null)
            barrelRenderer.sortingOrder = baseOrder + barrelLayerOffset;

        // 兜底：即使没找到具体部件，也把所有子SpriteRenderer提到角色之上
        if (bodyRenderer == null && barrelRenderer == null)
        {
            var allRenderers = GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < allRenderers.Length; i++)
            {
                allRenderers[i].sortingOrder = baseOrder + 1 + i;
            }
        }
    }

    private void FindRenderers()
    {
        // 查找Body
        Transform bodyTf = transform.Find(bodyObjectName);
        if (bodyTf != null)
            bodyRenderer = bodyTf.GetComponent<SpriteRenderer>();

        // 查找Barrel
        Transform barrelTf = transform.Find(barrelObjectName);
        if (barrelTf != null)
            barrelRenderer = barrelTf.GetComponent<SpriteRenderer>();

        // 兜底：按子对象顺序赋值
        if (bodyRenderer == null || barrelRenderer == null)
        {
            var children = GetComponentsInChildren<SpriteRenderer>();
            if (children.Length >= 1 && bodyRenderer == null)
                bodyRenderer = children[0];
            if (children.Length >= 2 && barrelRenderer == null)
                barrelRenderer = children[1];
        }

        // 查找玩家renderer
        if (playerRenderer == null)
        {
            var pc = GetComponentInParent<PlayerController>();
            if (pc != null)
                playerRenderer = pc.GetComponent<SpriteRenderer>();
        }
    }
}
