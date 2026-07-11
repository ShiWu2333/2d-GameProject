using UnityEngine;
using UnityEditor;

/// <summary>
/// LootBox容器配置工具
/// 菜单：Tools → 配置LootBox容器
/// 自动为场景中所有"LootBox"开头的对象添加LootContainer组件和必要配置
/// </summary>
public static class LootBoxSetup
{
    [MenuItem("Tools/配置LootBox容器")]
    public static void SetupAllLootBoxes()
    {
        // 找到场景中所有名称包含LootBox的对象
        var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var go in allGOs)
        {
            if (!go.name.Contains("LootBox") && !go.name.Contains("lootbox") && !go.name.Contains("Loot Box"))
                continue;

            Undo.RecordObject(go, "Setup LootBox");

            // 添加LootContainer组件
            var container = go.GetComponent<LootContainer>();
            if (container == null)
                container = Undo.AddComponent<LootContainer>(go);

            // 解析编号
            int id = ExtractNumber(go.name);
            container.containerID = id > 0 ? id : count + 1;

            // 按编号分配容器大小：1=大容器，2=中容器，3=小容器
            switch (container.containerID)
            {
                case 1: container.containerSize = ContainerSize.Large;  break;
                case 2: container.containerSize = ContainerSize.Medium; break;
                default: container.containerSize = ContainerSize.Small; break;
            }
            container.rows = (int)container.containerSize;

            // 初始无物品（打开时生成）
            container.ammoLoot.Clear();
            container.lootItems.Clear();
            container.hasGenerated = false;

            // 确保有SpriteRenderer
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = Undo.AddComponent<SpriteRenderer>(go);
                var white = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefabs/WhiteSquare.png");
                if (white != null) sr.sprite = white;
            }

            // 按大小设置不同颜色和尺寸
            switch (container.containerSize)
            {
                case ContainerSize.Large:
                    sr.color = new Color(0.7f, 0.35f, 0.1f); // 深橙
                    go.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
                    break;
                case ContainerSize.Medium:
                    sr.color = new Color(0.5f, 0.4f, 0.2f); // 棕色
                    go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
                    break;
                case ContainerSize.Small:
                    sr.color = new Color(0.4f, 0.5f, 0.3f); // 绿棕
                    go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                    break;
            }
            container.closedColor = sr.color;

            // 确保有Collider2D
            var col = go.GetComponent<Collider2D>();
            if (col == null)
            {
                var box = Undo.AddComponent<BoxCollider2D>(go);
                box.isTrigger = true;
                box.size = new Vector2(1f, 1f);
            }

            EditorUtility.SetDirty(go);
            count++;
            string sizeLabel = container.containerSize == ContainerSize.Large ? "大容器" :
                               container.containerSize == ContainerSize.Medium ? "中容器" : "小容器";
            Debug.Log($"已配置: {go.name} → {sizeLabel} ({container.rows}行)");
        }

        if (count == 0)
        {
            EditorUtility.DisplayDialog("提示",
                "场景中没有找到名称包含\"LootBox\"的对象。\n\n" +
                "请确保场景中有LootBox 1、LootBox 2等对象。", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("完成",
                $"已配置 {count} 个LootBox容器。\n\n" +
                "每个容器已添加：\n" +
                "• LootContainer组件\n" +
                "• Collider2D（交互检测）\n" +
                "• 默认弹药掉落\n\n" +
                "可在Inspector中修改容器内容物。", "OK");
        }
    }

    private static int ExtractNumber(string name)
    {
        // 从名称中提取数字，如"LootBox 2" → 2
        string digits = "";
        foreach (char c in name)
        {
            if (char.IsDigit(c))
                digits += c;
        }
        return digits.Length > 0 ? int.Parse(digits) : 0;
    }

    private static AmmoType GetAmmoTypeForIndex(int index)
    {
        // 不同编号给不同弹药（初始为空，打开时才生成）
        switch ((index - 1) % 5)
        {
            case 0: return AmmoType.Rifle;
            case 1: return AmmoType.SMG;
            case 2: return AmmoType.Shotgun;
            case 3: return AmmoType.LMG;
            case 4: return AmmoType.Pistol;
            default: return AmmoType.Rifle;
        }
    }
}
