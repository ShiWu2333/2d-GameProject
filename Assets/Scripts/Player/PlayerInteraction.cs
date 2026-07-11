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

    // ── 扫描可交互物体（鼠标指向 + 范围内）──────────
    private void ScanForInteractables()
    {
        IInteractable newInteractable = null;

        // 用OverlapCircle找范围内所有碰撞体，再筛选鼠标最接近的可交互物体
        var cols = Physics2D.OverlapCircleAll(transform.position, interactionRange);

        float bestDist = float.MaxValue;
        Vector2 mouseWorld = GetMouseWorldPos();

        foreach (var col in cols)
        {
            if (col.gameObject == gameObject) continue; // 排除自己

            var interactable = col.GetComponent<IInteractable>();
            if (interactable == null) continue;

            // 检查鼠标是否指向该物体（鼠标到物体的距离）
            float distToMouse = Vector2.Distance(mouseWorld, col.transform.position);
            float distToPlayer = Vector2.Distance(transform.position, col.transform.position);

            // 物体必须在交互范围内，且鼠标离物体够近（物体半径+容差）
            if (distToPlayer <= interactionRange && distToMouse < distToPlayer + 1.5f)
            {
                if (distToMouse < bestDist)
                {
                    bestDist = distToMouse;
                    newInteractable = interactable;
                }
            }
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

            // 更新HUD交互提示文字
            UpdatePromptText();
        }
    }

    private void UpdatePromptText()
    {
        var hud = FindObjectOfType<PlayerHUD>();
        if (hud == null) return;

        if (!canInteract)
        {
            hud.HideInteractionPrompt();
            return;
        }

        // LootContainer有自定义提示
        if (currentInteractable is LootContainer loot)
        {
            hud.ShowInteractionPrompt(loot.GetPromptText());
        }
        else
        {
            hud.ShowInteractionPrompt("[F] 交互");
        }
    }

    // ── 处理交互输入（F键或鼠标左键）─────────────────
    private void HandleInteractionInput()
    {
        if (!canInteract) return;

        KeyCode interactKey = KeyBindings.Instance != null ? KeyBindings.Instance.interact : KeyCode.F;
        bool pressed = Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0);
        if (!pressed) return;

        // 背包或容器打开时不处理场景交互
        var inv = GetComponent<InventorySystem>();
        if (inv != null && inv.IsOpen) return;
        if (ContainerUI.Instance != null && ContainerUI.Instance.IsOpen) return;

        currentInteractable?.Interact(this);
    }

    // ── 获取鼠标世界坐标 ─────────────────────────────
    private Vector2 GetMouseWorldPos()
    {
        var pc = GetComponent<PlayerController>();
        if (pc != null)
            return pc.MouseWorldPos;

        // fallback
        var cam = Camera.main;
        if (cam == null) return transform.position;
        Vector3 screen = Input.mousePosition;
        screen.z = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(screen);
    }

    // ── 调试绘制 ──────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
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
