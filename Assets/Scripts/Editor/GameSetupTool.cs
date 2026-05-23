#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 菜单路径：Tools / Game Setup / Setup Player
/// 一键在场景中创建并配置好玩家 GameObject 及 HUD Canvas
/// </summary>
public class GameSetupTool
{
    // ══════════════════════════════════════════════════
    //  主入口
    // ══════════════════════════════════════════════════
    [MenuItem("Tools/Game Setup/Setup Player &p")]
    public static void SetupPlayer()
    {
        // 防止重复创建
        GameObject existing = GameObject.FindGameObjectWithTag("Player");
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "已存在玩家",
                "场景中已有 Tag=Player 的对象，是否删除并重新创建？",
                "重新创建", "取消");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        // ── 1. 创建玩家 GameObject ─────────────────────
        GameObject player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");

        // ── 2. 添加 Rigidbody2D ───────────────────────
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // ── 3. 添加 Collider2D ────────────────────────
        CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 0.8f);
        col.direction = CapsuleDirection2D.Vertical;

        // ── 4. 添加玩家脚本 ───────────────────────────
        player.AddComponent<PlayerStats>();
        PlayerController controller = player.AddComponent<PlayerController>();
        PlayerInteraction interaction = player.AddComponent<PlayerInteraction>();
        player.AddComponent<InventorySystem>();

        // ── 5. 添加 SpriteRenderer（占位用蓝色方块）──
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSprite();
        sr.color = new Color(0.3f, 0.7f, 1f);
        controller.bodySprite = sr;

        // ── 6. 创建 AimPivot（武器/朝向节点，跟随鼠标旋转）──
        GameObject aimPivot = new GameObject("AimPivot");
        Undo.RegisterCreatedObjectUndo(aimPivot, "Create AimPivot");
        aimPivot.transform.SetParent(player.transform);
        aimPivot.transform.localPosition = Vector3.zero;
        controller.aimPivot = aimPivot.transform;

        // ── 7. 在 AimPivot 下创建 FirePoint（枪口位置）──
        GameObject firePoint = new GameObject("FirePoint");
        Undo.RegisterCreatedObjectUndo(firePoint, "Create FirePoint");
        firePoint.transform.SetParent(aimPivot.transform);
        firePoint.transform.localPosition = new Vector3(0.5f, 0f, 0f); // 枪口在右侧

        // ── 8. 创建射线起点（交互检测）────────────────
        GameObject rayOrigin = new GameObject("RayOrigin");
        Undo.RegisterCreatedObjectUndo(rayOrigin, "Create RayOrigin");
        rayOrigin.transform.SetParent(player.transform);
        rayOrigin.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        interaction.rayOrigin = rayOrigin.transform;

        // 设置交互层（确保 Interactable 层存在）
        int interactableLayer = EnsureLayer("Interactable");
        interaction.interactableLayer = 1 << interactableLayer;
        interaction.interactionRange = 2f;

        // ── 9. 创建 HUD Canvas ────────────────────────
        SetupHUD(player);

        // ── 10. 创建摄像机跟随（如果没有）────────────
        SetupCamera(player);

        // 选中玩家
        Selection.activeGameObject = player;
        EditorUtility.DisplayDialog("完成", "玩家 GameObject 创建完毕！\n\n请查看 Inspector 确认各组件配置。", "OK");
        Debug.Log("[GameSetupTool] 玩家创建完毕");
    }

    // ══════════════════════════════════════════════════
    //  HUD Canvas
    // ══════════════════════════════════════════════════
    private static void SetupHUD(GameObject player)
    {
        // Canvas
        GameObject canvasGO = new GameObject("HUD_Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create HUD Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        PlayerHUD hud = canvasGO.AddComponent<PlayerHUD>();

        // ── 血量条 ────────────────────────────────────
        GameObject healthBarGO = CreateSlider(canvasGO.transform, "HealthBar",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(160f, -30f), new Vector2(300f, 20f));
        Slider healthSlider = healthBarGO.GetComponent<Slider>();
        healthSlider.fillRect.GetComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f);
        hud.healthBar = healthSlider;

        // 血量文字
        GameObject healthTextGO = CreateText(canvasGO.transform, "HealthText",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(160f, -55f), new Vector2(200f, 25f), "100 / 100");
        hud.healthText = healthTextGO.GetComponent<TextMeshProUGUI>();

        // ── 体力条 ────────────────────────────────────
        GameObject staminaBarGO = CreateSlider(canvasGO.transform, "StaminaBar",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(160f, -60f), new Vector2(300f, 15f));
        Slider staminaSlider = staminaBarGO.GetComponent<Slider>();
        staminaSlider.fillRect.GetComponent<Image>().color = new Color(0.1f, 0.8f, 0.2f);
        hud.staminaBar = staminaSlider;

        // ── 弹药文字 ──────────────────────────────────
        GameObject ammoTextGO = CreateText(canvasGO.transform, "AmmoText",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-100f, 50f), new Vector2(180f, 40f), "30 / 30");
        TextMeshProUGUI ammoTMP = ammoTextGO.GetComponent<TextMeshProUGUI>();
        ammoTMP.fontSize = 24;
        ammoTMP.alignment = TextAlignmentOptions.Right;
        hud.ammoText = ammoTMP;

        // ── 交互提示 ──────────────────────────────────
        GameObject promptGO = CreateText(canvasGO.transform, "InteractionPrompt",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 80f), new Vector2(300f, 40f), "[F] 交互");
        TextMeshProUGUI promptTMP = promptGO.GetComponent<TextMeshProUGUI>();
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.color = Color.yellow;
        promptGO.SetActive(false);
        hud.interactionPromptText = promptTMP;

        // ── 背包面板（占位）──────────────────────────
        GameObject inventoryPanel = new GameObject("InventoryPanel");
        Undo.RegisterCreatedObjectUndo(inventoryPanel, "Create InventoryPanel");
        inventoryPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform invRect = inventoryPanel.AddComponent<RectTransform>();
        invRect.anchorMin = new Vector2(0.5f, 0.5f);
        invRect.anchorMax = new Vector2(0.5f, 0.5f);
        invRect.sizeDelta = new Vector2(600f, 400f);
        invRect.anchoredPosition = Vector2.zero;
        Image invBg = inventoryPanel.AddComponent<Image>();
        invBg.color = new Color(0f, 0f, 0f, 0.85f);
        inventoryPanel.SetActive(false);
        hud.inventoryPanel = inventoryPanel;

        // 背包标题
        CreateText(inventoryPanel.transform, "InventoryTitle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(400f, 40f), "背 包 [M]");
    }

    // ══════════════════════════════════════════════════
    //  摄像机跟随
    // ══════════════════════════════════════════════════
    private static void SetupCamera(GameObject player)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGO, "Create Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        // 添加简单跟随脚本
        if (cam.GetComponent<CameraFollow>() == null)
        {
            CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
        }
    }

    // ══════════════════════════════════════════════════
    //  辅助方法
    // ══════════════════════════════════════════════════
    private static GameObject CreateSlider(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Slider slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.red;

        slider.fillRect = fillRect;

        return go;
    }

    private static GameObject CreateText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, string text)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.color = Color.white;

        return go;
    }

    private static Sprite CreateDefaultSprite()
    {
        // 创建一个 32x32 白色方块 Sprite 作为占位
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    private static int EnsureLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
        {
            Debug.LogWarning($"[GameSetupTool] Layer '{layerName}' 不存在，请在 Project Settings > Tags and Layers 中手动添加，然后重新运行工具。");
            return 0;
        }
        return layer;
    }
}
#endif
