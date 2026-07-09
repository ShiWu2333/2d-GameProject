using UnityEngine;

/// <summary>
/// 弹药物品工厂
/// 用于创建各种类型的弹药物品
/// </summary>
public static class AmmoItemFactory
{
    /// <summary>
    /// 创建弹药物品（无弹药数据版本）
    /// </summary>
    /// <param name="type">弹药类型</param>
    /// <param name="amount">弹药数量</param>
    /// <param name="isHighGrade">是否为高级弹药</param>
    /// <returns>弹药物品</returns>
    public static AmmoItem CreateAmmoItem(AmmoType type, int amount = 30, bool isHighGrade = false)
    {
        string gradeName = isHighGrade ? "高级" : "低级";
        string typeName = GetAmmoTypeDisplayName(type);
        
        AmmoItem item = new AmmoItem
        {
            itemName = $"{typeName}弹药（{gradeName}）",
            ammoType = type,
            ammoAmount = Mathf.Min(amount, AmmoItem.MaxPerStack),
            quantity = Mathf.Min(amount, AmmoItem.MaxPerStack),
            isHighGrade = isHighGrade
        };
        
        // 设置图标
        item.icon = AmmoIconManager.GetAmmoIcon(type, isHighGrade);
        
        // 尝试查找对应的弹药数据资产
        string assetName = GetAmmoAssetName(type, isHighGrade);
        item.ammoData = Resources.Load<AmmoData>($"Data/AmmoData/{assetName}");
        
        return item;
    }
    
    /// <summary>
    /// 创建特定武器类型的弹药物品
    /// </summary>
    public static AmmoItem CreateAmmoItemForWeapon(string weaponName, int amount = 30, bool isHighGrade = false)
    {
        AmmoType type = GetAmmoTypeForWeapon(weaponName);
        return CreateAmmoItem(type, amount, isHighGrade);
    }
    
    /// <summary>
    /// 根据武器预制体名称获取弹药类型
    /// </summary>
    private static AmmoType GetAmmoTypeForWeapon(string weaponName)
    {
        switch (weaponName.ToLower())
        {
            case "smg":
            case "冲锋枪":
                return AmmoType.SMG;
                
            case "autopistol":
            case "自动手枪":
                return AmmoType.Pistol;
                
            case "assaultrifle":
            case "突击步枪":
                return AmmoType.Rifle;
                
            case "marksmanrifle":
            case "射手步枪":
                return AmmoType.Rifle;
                
            case "autoshotgun":
            case "连发霰弹枪":
                return AmmoType.Shotgun;
                
            case "lmg":
            case "轻机枪":
                return AmmoType.LMG;
                
            case "knife":
            case "刀":
                return AmmoType.None;
                
            default:
                Debug.LogWarning($"未知武器类型: {weaponName}, 默认使用SMG弹药");
                return AmmoType.SMG;
        }
    }
    
    /// <summary>
    /// 获取弹药类型的显示名称
    /// </summary>
    private static string GetAmmoTypeDisplayName(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.Pistol:   return "手枪";
            case AmmoType.SMG:      return "冲锋枪";
            case AmmoType.Rifle:    return "步枪";
            case AmmoType.Shotgun:  return "霰弹枪";
            case AmmoType.LMG:      return "轻机枪";
            case AmmoType.None:     return "无限";
            default:                return "未知";
        }
    }
    
    /// <summary>
    /// 获取弹药资产文件名
    /// </summary>
    private static string GetAmmoAssetName(AmmoType type, bool isHighGrade)
    {
        string typeName = GetAmmoTypeDisplayName(type);
        string gradeName = isHighGrade ? "高级" : "低级";
        
        // 特殊处理：步枪有两种类型
        if (type == AmmoType.Rifle)
        {
            string rifleType = isHighGrade ? "突击步枪弹药_高级" : "突击步枪弹药_低级";
            return rifleType;
        }
        
        return $"{typeName}弹药_{gradeName}";
    }
    
    /// <summary>
    /// 创建所有类型的弹药物品（用于测试）
    /// </summary>
    public static AmmoItem[] CreateAllAmmoTypes(int amountPerType = 30)
    {
        AmmoType[] allTypes = {
            AmmoType.Pistol,
            AmmoType.SMG,
            AmmoType.Rifle,
            AmmoType.Shotgun,
            AmmoType.LMG
        };
        
        AmmoItem[] items = new AmmoItem[allTypes.Length * 2]; // 每种类型低级和高级
        
        int index = 0;
        foreach (var type in allTypes)
        {
            if (type == AmmoType.None) continue;
            
            // 低级弹药
            items[index++] = CreateAmmoItem(type, amountPerType, false);
            
            // 高级弹药
            items[index++] = CreateAmmoItem(type, amountPerType, true);
        }
        
        return items;
    }
    
    /// <summary>
    /// 从弹药数据资产创建弹药物品
    /// </summary>
    public static AmmoItem CreateAmmoItemFromData(AmmoData ammoData, int amount = 30)
    {
        if (ammoData == null)
        {
            Debug.LogError("无法从空的弹药数据创建弹药物品");
            return null;
        }
        
        AmmoItem item = new AmmoItem
        {
            itemName = ammoData.ammoName,
            ammoType = ammoData.ammoType,
            ammoData = ammoData,
            ammoAmount = Mathf.Min(amount, AmmoItem.MaxPerStack),
            quantity = Mathf.Min(amount, AmmoItem.MaxPerStack),
            isHighGrade = ammoData.isHighGrade
        };
        
        // 设置图标
        item.icon = AmmoIconManager.GetAmmoIcon(ammoData.ammoType, ammoData.isHighGrade);
        
        return item;
    }
    
    /// <summary>
    /// 创建预定义的"Item 1"和"Item 2"弹药
    /// </summary>
    public static AmmoItem CreateItem1()
    {
        // Item 1: 步枪弹药（低级）
        return CreateAmmoItem(AmmoType.Rifle, 30, false);
    }
    
    public static AmmoItem CreateItem2()
    {
        // Item 2: 冲锋枪弹药（低级）
        return CreateAmmoItem(AmmoType.SMG, 30, false);
    }
    
    /// <summary>
    /// 创建高级版本的Item 1和Item 2
    /// </summary>
    public static AmmoItem CreateItem1HighGrade()
    {
        return CreateAmmoItem(AmmoType.Rifle, 30, true);
    }
    
    public static AmmoItem CreateItem2HighGrade()
    {
        return CreateAmmoItem(AmmoType.SMG, 30, true);
    }
}