using UnityEngine;

/// <summary>
/// 武器图层排序器
/// 确保武器精灵始终渲染在玩家精灵之上
/// </summary>
public class WeaponLayerSorter : MonoBehaviour
{
    [Header("图层偏移（相对于玩家最高 sortingOrder）")]
    public int frontOffset = 2;
    public int backOffset  = -1;

    [Header("自动查找")]
    public bool autoFindSpriteRenderers = true;
    public string bodyObjectName   = "Body";
    public string barrelObjectName = "Barrel";

    private SpriteRenderer[] playerRenderers;
    private SpriteRenderer   bodyRenderer;
    private SpriteRenderer   barrelRenderer;
    private bool             isEquipped;

    void Awake()
    {
        if (autoFindSpriteRenderers)
            FindWeaponRenderers();
    }

    public void OnWeaponEquipped(PlayerController player)
    {
        isEquipped = true;
        if (player != null)
            playerRenderers = player.GetComponentsInChildren<SpriteRenderer>();

        if (autoFindSpriteRenderers)
            FindWeaponRenderers();
    }

    public void OnWeaponUnequipped()
    {
        isEquipped = false;
        if (bodyRenderer != null)   bodyRenderer.sortingOrder = 0;
        if (barrelRenderer != null) barrelRenderer.sortingOrder = 0;
    }

    public void ApplySorting()
    {
        if (!isEquipped)
        {
            var pc = GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                playerRenderers = pc.GetComponentsInChildren<SpriteRenderer>();
                isEquipped = true;
            }
        }
    }

    void LateUpdate()
    {
        if (!isEquipped) return;
        if (playerRenderers == null || playerRenderers.Length == 0) return;

        // 找到玩家精灵中最低和最高的 sortingOrder
        // 身体应该是最低的，头应该是最高的
        // 武器放在中间（身体之上，头之下）
        int minPlayerOrder = int.MaxValue;
        int maxPlayerOrder = int.MinValue;
        foreach (var sr in playerRenderers)
        {
            if (sr == bodyRenderer || sr == barrelRenderer) continue;
            if (sr.sortingOrder < minPlayerOrder)
                minPlayerOrder = sr.sortingOrder;
            if (sr.sortingOrder > maxPlayerOrder)
                maxPlayerOrder = sr.sortingOrder;
        }

        if (minPlayerOrder == int.MaxValue) minPlayerOrder = 0;
        if (maxPlayerOrder == int.MinValue) maxPlayerOrder = 0;

        // 武器放在最低（身体）和最高（头）之间
        int weaponOrder = minPlayerOrder + 1;

        if (bodyRenderer != null)
            bodyRenderer.sortingOrder = weaponOrder;
        if (barrelRenderer != null)
            barrelRenderer.sortingOrder = weaponOrder;
    }

    private void FindWeaponRenderers()
    {
        Transform bodyTf = transform.Find(bodyObjectName);
        if (bodyTf != null)
            bodyRenderer = bodyTf.GetComponent<SpriteRenderer>();

        Transform barrelTf = transform.Find(barrelObjectName);
        if (barrelTf != null)
            barrelRenderer = barrelTf.GetComponent<SpriteRenderer>();

        if (bodyRenderer == null)
        {
            var children = GetComponentsInChildren<SpriteRenderer>();
            if (children.Length >= 1) bodyRenderer = children[0];
            if (children.Length >= 2) barrelRenderer = children[1];
        }
    }
}
