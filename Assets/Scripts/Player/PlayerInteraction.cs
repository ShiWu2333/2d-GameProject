using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 玩家交互：F键搜索物资点、开门、拾取
/// 使用射线检测前方可交互物体
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("检测参数")]
    public float interactionRange = 2f;          // 交互距离
    public LayerMask interactableLayer;          // 可交互物体层
    public Transform rayOrigin;                  // 射线起点（默认用玩家位置）

    [Header("UI提示")]
    public GameObject interactionPromptUI;       // 可交互时显示的UI（可选）

    // 当前可交互物体
    private IInteractable currentInteractable;
    private bool canInteract;

    /// <summary>是否有可交互目标（供其他系统查询优先级）</summary>
    public bool CanInteract => canInteract;

    // 事件
    public UnityEvent<IInteractable> onInteractableFound;
    public UnityEvent onInteractableLost;

    void Awake()
    {
        if (rayOrigin == null) rayOrigin = transform;
    }

    void Update()
    {
        ScanForInteractables();
        HandleInteractionInput();
    }

    // ── 扫描前方可交互物体 ─────────────────────────────
    private void ScanForInteractables()
    {
        // 从玩家位置向面朝方向发射射线
        Vector2 direction = transform.up; // 玩家面朝方向（PlayerController 旋转后 up 朝向鼠标）
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin.position, direction, interactionRange, interactableLayer);

        IInteractable newInteractable = null;
        if (hit.collider != null)
        {
            newInteractable = hit.collider.GetComponent<IInteractable>();
        }

        // 如果检测到的物体发生变化
        if (newInteractable != currentInteractable)
        {
            if (currentInteractable != null)
            {
                currentInteractable.OnHoverEnd();
                onInteractableLost?.Invoke();
            }

            currentInteractable = newInteractable;
            canInteract = currentInteractable != null;

            if (currentInteractable != null)
            {
                currentInteractable.OnHoverStart();
                onInteractableFound?.Invoke(currentInteractable);
            }

            // 更新UI提示
            if (interactionPromptUI != null)
                interactionPromptUI.SetActive(canInteract);
        }
    }

    // ── 处理F键交互 ───────────────────────────────────
    private void HandleInteractionInput()
    {
        if (!canInteract) return;

        KeyCode interactKey = KeyBindings.Instance != null ? KeyBindings.Instance.interact : KeyCode.F;
        if (!Input.GetKeyDown(interactKey)) return;

        // 背包打开时不处理场景交互
        var inv = GetComponent<InventorySystem>();
        if (inv != null && inv.IsOpen) return;

        currentInteractable?.Interact(this);
    }

    // ── 调试绘制 ──────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (rayOrigin == null) return;
        Gizmos.color = Color.yellow;
        Vector2 dir = transform.up * interactionRange;
        Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + (Vector3)dir);
    }
}

/// <summary>
/// 可交互物体接口
/// </summary>
public interface IInteractable
{
    /// <summary>交互时调用</summary>
    void Interact(PlayerInteraction player);

    /// <summary>玩家进入交互范围时调用（显示提示）</summary>
    void OnHoverStart();

    /// <summary>玩家离开交互范围时调用（隐藏提示）</summary>
    void OnHoverEnd();
}
