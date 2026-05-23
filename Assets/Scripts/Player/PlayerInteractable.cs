using UnityEngine;

/// <summary>
/// 可搜索的物资点示例（实现 IInteractable）
/// 挂在场景中的箱子、柜子等物体上
/// </summary>
public class LootContainer : MonoBehaviour, IInteractable
{
    [Header("物资配置")]
    public InventoryItem[] lootItems;            // 可掉落的物品列表
    public bool isLooted = false;                // 是否已被搜索

    [Header("提示UI")]
    public GameObject hoverPrompt;              // 悬停提示（如"按F搜索"）

    public void Interact(PlayerInteraction player)
    {
        if (isLooted)
        {
            Debug.Log("已经搜索过了");
            return;
        }

        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory == null) return;

        foreach (var item in lootItems)
        {
            if (item != null)
                inventory.AddItem(item);
        }

        isLooted = true;
        Debug.Log($"搜索完毕：{gameObject.name}");
    }

    public void OnHoverStart()
    {
        if (hoverPrompt != null) hoverPrompt.SetActive(true);
    }

    public void OnHoverEnd()
    {
        if (hoverPrompt != null) hoverPrompt.SetActive(false);
    }
}

/// <summary>
/// 可开关的门（实现 IInteractable）
/// </summary>
public class Door : MonoBehaviour, IInteractable
{
    [Header("门状态")]
    public bool isOpen = false;

    [Header("开关位移（本地坐标）")]
    public Vector3 openOffset = new Vector3(0f, 1.5f, 0f);

    [Header("提示UI")]
    public GameObject hoverPrompt;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Awake()
    {
        closedPosition = transform.localPosition;
        openPosition = closedPosition + openOffset;
    }

    public void Interact(PlayerInteraction player)
    {
        isOpen = !isOpen;
        transform.localPosition = isOpen ? openPosition : closedPosition;
        Debug.Log($"门 {(isOpen ? "打开" : "关闭")}");
    }

    public void OnHoverStart()
    {
        if (hoverPrompt != null) hoverPrompt.SetActive(true);
    }

    public void OnHoverEnd()
    {
        if (hoverPrompt != null) hoverPrompt.SetActive(false);
    }
}

/// <summary>
/// 可拾取的武器（实现 IInteractable）
/// 挂在场景中的武器物体上
/// </summary>
public class PickupWeapon : MonoBehaviour, IInteractable
{
    [Header("武器引用")]
    public WeaponBase weapon;

    [Header("提示UI")]
    public GameObject hoverPrompt;

    public void Interact(PlayerInteraction player)
    {
        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory == null) return;

        inventory.EquipWeapon(weapon);
        Debug.Log($"拾取武器：{weapon.weaponName}");

        // 隐藏地面上的武器图标
        gameObject.SetActive(false);
    }

    public void OnHoverStart()
    {
        if (hoverPrompt != null) hoverPrompt.SetActive(true);
    }

    public void OnHoverEnd()
    {
        if (hoverPrompt != null) hoverPrompt.SetActive(false);
    }
}
