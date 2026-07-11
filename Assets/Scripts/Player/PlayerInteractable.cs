using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 可打开的物资容器（LootBox）
/// 挂在场景中的箱子上，按F打开，显示内容物
/// 支持编号标识（LootBox 1, LootBox 2...）
/// </summary>
public class LootContainer : MonoBehaviour, IInteractable
{
    [Header("容器配置")]
    [Tooltip("容器编号（场景中唯一标识）")]
    public int containerID = 1;

    [Tooltip("容器显示名称（留空则自动生成）")]
    public string containerName;

    [Tooltip("容器大小（决定行数和可生成物品类型）")]
    public ContainerSize containerSize = ContainerSize.Small;

    [Header("物资配置")]
    [Tooltip("容器内的物品列表")]
    public List<InventoryItem> lootItems = new List<InventoryItem>();

    [Tooltip("容器内的弹药（类型+数量）")]
    public List<AmmoLoot> ammoLoot = new List<AmmoLoot>();

    [Header("状态")]
    public bool isOpen = false;          // 只要打开过就为true，初始false
    public bool hasGenerated = false;    // 是否已生成过物品

    [Header("容器大小")]
    [Tooltip("容器格子行数（由containerSize决定）")]
    public int rows = 2;

    [Header("外观")]
    [Tooltip("未打开时的纯色")]
    public Color closedColor = new Color(0.6f, 0.4f, 0.2f);
    [Tooltip("已打开时内描边颜色（灰色半透明）")]
    public Color openFilterColor = new Color(0.3f, 0.3f, 0.3f, 0.65f);
    [Tooltip("内描边厚度占容器短边的比例")]
    [Range(0.05f, 0.25f)]
    public float borderRatio = 0.12f;

    [Header("提示UI")]
    public GameObject hoverPrompt;

    private SpriteRenderer spriteRenderer;

    /// <summary>获取容器显示名称</summary>
    public string DisplayName => string.IsNullOrEmpty(containerName)
        ? $"物资箱 {containerID}"
        : containerName;

    /// <summary>获取容器标签列表</summary>
    public List<string> GetTags()
    {
        var tags = new List<string>();
        switch (containerSize)
        {
            case ContainerSize.Small:
                tags.Add(LootTags.Ammo);
                break;
            case ContainerSize.Medium:
            case ContainerSize.Large:
                tags.Add(LootTags.Weapon);
                break;
        }
        return tags;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 行数由containerSize决定
        rows = (int)containerSize;
        UpdateVisual();
    }

    public void Interact(PlayerInteraction player)
    {
        // 容器可重复打开
        Open();
    }

    /// <summary>打开容器（展示容器UI）</summary>
    public void Open()
    {
        isOpen = true;
        UpdateVisual();

        // 首次打开时生成物品
        if (!hasGenerated)
            ContainerLootGenerator.GenerateLoot(this);

        // 打开容器UI
        var containerUI = ContainerUI.Instance;
        if (containerUI == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var go = new GameObject("ContainerUI");
                go.transform.SetParent(canvas.transform, false);
                containerUI = go.AddComponent<ContainerUI>();
            }
        }

        if (containerUI != null)
        {
            var player = FindObjectOfType<PlayerController>();
            var inventory = player != null ? player.GetComponent<InventorySystem>() : null;
            containerUI.Open(this, inventory);
        }
    }

    /// <summary>取出全部物品放入玩家背包</summary>
    private void TakeAll(PlayerInteraction player)
    {
        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory == null) return;

        // 添加普通物品
        foreach (var item in lootItems)
        {
            if (item != null)
                inventory.AddItem(item);
        }

        // 添加弹药
        foreach (var ammo in ammoLoot)
        {
            var ammoItem = AmmoItemFactory.CreateAmmoItem(ammo.ammoType, ammo.amount, ammo.isHighGrade);
            inventory.AddItem(ammoItem);
        }

        isOpen = true;
        lootItems.Clear();
        ammoLoot.Clear();
        UpdateVisual();

        // 清除散落的地面物品
        ClearGroundItems();

        Debug.Log($"[{DisplayName}] 已取出全部物品");
    }

    /// <summary>在容器附近生成地面物品供预览</summary>
    private void SpawnGroundItems()
    {
        float offset = 0.8f;
        int index = 0;

        foreach (var ammo in ammoLoot)
        {
            float angle = (360f / Mathf.Max(ammoLoot.Count + lootItems.Count, 1)) * index * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle) * offset, Mathf.Sin(angle) * offset, 0f);

            var go = new GameObject($"LootPreview_{index}");
            go.transform.position = pos;
            go.transform.SetParent(transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = AmmoIconManager.GetAmmoBaseColor(ammo.ammoType);
            go.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            // 加白色sprite
            var white = Resources.Load<Sprite>("Sprites/Ammo/ammo_" + ammo.ammoType.ToString().ToLower());
            if (white != null) sr.sprite = white;

            index++;
        }
    }

    /// <summary>清除预览散落物</summary>
    private void ClearGroundItems()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("LootPreview_"))
                Destroy(child.gameObject);
        }
    }

    private GameObject borderGO;    // 已打开状态的内描边容器

    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = closedColor;

        if (!isOpen)
        {
            if (borderGO != null) borderGO.SetActive(false);
        }
        else
        {
            EnsureOpenVisuals();
            borderGO.SetActive(true);
        }
    }

    /// <summary>创建已打开状态的内描边（4条灰色半透明边，在容器内不超出）</summary>
    private void EnsureOpenVisuals()
    {
        if (borderGO != null) return;

        Vector3 parentScale = transform.localScale;
        float worldW, worldH;
        if (spriteRenderer.sprite != null)
        {
            worldW = spriteRenderer.bounds.size.x;
            worldH = spriteRenderer.bounds.size.y;
        }
        else
        {
            worldW = Mathf.Abs(parentScale.x);
            worldH = Mathf.Abs(parentScale.y);
        }

        // 计算本地空间尺寸
        float localW = worldW / Mathf.Abs(parentScale.x);
        float localH = worldH / Mathf.Abs(parentScale.y);
        // 描边厚度随容器大小（短边的比例）
        float border = Mathf.Min(localW, localH) * borderRatio;

        int baseOrder = spriteRenderer.sortingOrder + 1;

        // 创建白色sprite
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        var whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        // 父容器（不可见，只做分组）
        borderGO = new GameObject("InnerBorder");
        borderGO.transform.SetParent(transform, false);
        borderGO.transform.localPosition = Vector3.zero;
        borderGO.transform.localScale = Vector3.one;

        // 上边
        CreateBorderBar(borderGO.transform, "Top", whiteSprite, baseOrder,
            new Vector3(0f, (localH - border) * 0.5f, 0f),
            new Vector3(localW, border, 1f));

        // 下边
        CreateBorderBar(borderGO.transform, "Bottom", whiteSprite, baseOrder,
            new Vector3(0f, -(localH - border) * 0.5f, 0f),
            new Vector3(localW, border, 1f));

        // 左边
        CreateBorderBar(borderGO.transform, "Left", whiteSprite, baseOrder,
            new Vector3(-(localW - border) * 0.5f, 0f, 0f),
            new Vector3(border, localH, 1f));

        // 右边
        CreateBorderBar(borderGO.transform, "Right", whiteSprite, baseOrder,
            new Vector3((localW - border) * 0.5f, 0f, 0f),
            new Vector3(border, localH, 1f));

        borderGO.SetActive(false);
    }

    private void CreateBorderBar(Transform parent, string name, Sprite sprite, int order, Vector3 localPos, Vector3 localScale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = openFilterColor;
        sr.sortingOrder = order;
    }

    // ── 悬停叠层 ──────────────────────────────────────
    private GameObject overlayGO;
    private SpriteRenderer overlayRenderer;
    private GameObject textGO;
    private TextMesh textMesh;

    public void OnHoverStart()
    {
        if (hoverPrompt != null) hoverPrompt.SetActive(true);
        ShowOverlay(true);
    }

    public void OnHoverEnd()
    {
        if (hoverPrompt != null) hoverPrompt.SetActive(false);
        ShowOverlay(false);
    }

    private void ShowOverlay(bool show)
    {
        if (show)
        {
            EnsureOverlay();
            overlayGO.SetActive(true);
            textGO.SetActive(true);

            // 更新文本
            if (isOpen)
                textMesh.text = "打开";
            else
                textMesh.text = "打开";
        }
        else
        {
            if (overlayGO != null) overlayGO.SetActive(false);
            if (textGO != null)    textGO.SetActive(false);
        }
    }

    private void EnsureOverlay()
    {
        if (overlayGO != null) return;

        int baseOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

        // 计算本地空间下的尺寸（滤镜作为子物体，需要抵消父物体缩放）
        Vector3 parentScale = transform.localScale;
        // 容器的实际世界尺寸
        float worldW, worldH;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            worldW = spriteRenderer.bounds.size.x;
            worldH = spriteRenderer.bounds.size.y;
        }
        else
        {
            worldW = parentScale.x;
            worldH = parentScale.y;
        }

        // 滤镜的localScale需要除以父缩放，这样世界大小才等于容器大小
        float overlayScaleX = worldW / Mathf.Abs(parentScale.x);
        float overlayScaleY = worldH / Mathf.Abs(parentScale.y);

        // 滤镜层
        overlayGO = new GameObject("HoverOverlay");
        overlayGO.transform.SetParent(transform, false);
        overlayGO.transform.localPosition = Vector3.zero;
        overlayGO.transform.localScale = new Vector3(overlayScaleX, overlayScaleY, 1f);

        overlayRenderer = overlayGO.AddComponent<SpriteRenderer>();
        overlayRenderer.color = new Color(0.75f, 0.75f, 0.75f, 0.4f);
        overlayRenderer.sortingOrder = baseOrder + 1;

        // 1x1白色sprite，PPU=1，所以1单位localScale = 1世界单位
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        overlayRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        // 文本层
        textGO = new GameObject("HoverText");
        textGO.transform.SetParent(transform, false);
        textGO.transform.localPosition = Vector3.zero;
        // 文本也要抵消父缩放，保持正常比例
        float textScaleFactor = 1f / Mathf.Max(Mathf.Abs(parentScale.x), Mathf.Abs(parentScale.y));
        textGO.transform.localScale = new Vector3(textScaleFactor, textScaleFactor, 1f);

        textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = "打开";
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.fontSize = 48;
        // 字符大小基于容器世界尺寸，确保不超出
        textMesh.characterSize = Mathf.Min(worldW, worldH) * 0.08f;

        var textRenderer = textGO.GetComponent<MeshRenderer>();
        if (textRenderer != null)
            textRenderer.sortingOrder = baseOrder + 2;

        overlayGO.SetActive(false);
        textGO.SetActive(false);
    }

    /// <summary>获取交互提示文本</summary>
    public string GetPromptText()
    {
        if (isOpen)
            return $"[F] 打开 {DisplayName}";
        return $"[F] 打开 {DisplayName}";
    }
}

/// <summary>
/// 弹药掉落配置
/// </summary>
[System.Serializable]
public class AmmoLoot
{
    public AmmoType ammoType = AmmoType.Rifle;
    public int amount = 30;
    public bool isHighGrade = false;
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
        if (weapon == null) return;

        // 使用 WeaponSlotSystem 装备（新系统）
        var slotSystem = player.GetComponent<WeaponSlotSystem>();
        if (slotSystem != null)
        {
            WeaponSlotSystem.WeaponSlot assignSlot;
            if (weapon is Knife)
                assignSlot = WeaponSlotSystem.WeaponSlot.Melee;
            else if (slotSystem.GetWeaponInSlot(WeaponSlotSystem.WeaponSlot.Primary1) == null)
                assignSlot = WeaponSlotSystem.WeaponSlot.Primary1;
            else if (slotSystem.GetWeaponInSlot(WeaponSlotSystem.WeaponSlot.Primary2) == null)
                assignSlot = WeaponSlotSystem.WeaponSlot.Primary2;
            else
            {
                // 槽位满，放入背包
                var inv = player.GetComponent<InventorySystem>();
                if (inv != null)
                {
                    var item = new InventoryItem
                    {
                        itemName = weapon.weaponName,
                        quantity = 1,
                        slotCount = weapon.weaponSlotCount,
                        tags = new System.Collections.Generic.List<string> { LootTags.Weapon },
                        weaponRef = weapon
                    };
                    inv.AddItem(item);
                    weapon.gameObject.SetActive(false);
                }
                gameObject.SetActive(false);
                return;
            }

            slotSystem.SetWeaponInSlot(assignSlot, weapon);
            var pc = player.GetComponent<PlayerController>();
            if (pc != null && pc.aimPivot != null)
            {
                weapon.transform.SetParent(pc.aimPivot);
                weapon.transform.localPosition = Vector3.zero;
                weapon.transform.localRotation = Quaternion.identity;
            }
            if (assignSlot != slotSystem.CurrentSlot)
                weapon.gameObject.SetActive(false);
        }

        Debug.Log($"拾取武器：{weapon.weaponName}");
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
