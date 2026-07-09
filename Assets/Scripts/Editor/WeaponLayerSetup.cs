using UnityEditor;
using UnityEngine;

/// <summary>
/// 武器图层批量设置工具
/// 菜单：Tools → 武器图层 → ...
/// </summary>
public static class WeaponLayerSetup
{
    private const string WEAPON_DIR = "Assets/Prefabs/Weapons";

    [MenuItem("Tools/武器图层/批量添加图层排序器")]
    public static void AddLayerSorters()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { WEAPON_DIR });

        int processed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            WeaponBase weapon = prefab.GetComponent<WeaponBase>()
                             ?? prefab.GetComponentInChildren<WeaponBase>();
            if (weapon == null) continue;

            if (weapon.GetComponent<WeaponLayerSorter>() != null) continue;

            var sorter = weapon.gameObject.AddComponent<WeaponLayerSorter>();
            sorter.autoFindSpriteRenderers = true;
            sorter.bodyObjectName   = "Body";
            sorter.barrelObjectName = "Barrel";
            sorter.barrelLayerOffset = 1;
            sorter.bodyLayerOffset   = 0;

            EditorUtility.SetDirty(prefab);
            processed++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("武器图层", $"完成：为 {processed} 个武器添加了图层排序器。", "OK");
    }

    [MenuItem("Tools/武器图层/检查图层设置")]
    public static void CheckLayers()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { WEAPON_DIR });

        int total = 0, missing = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            WeaponBase weapon = prefab.GetComponent<WeaponBase>()
                             ?? prefab.GetComponentInChildren<WeaponBase>();
            if (weapon == null) continue;

            total++;
            if (weapon.GetComponent<WeaponLayerSorter>() == null)
            {
                Debug.LogWarning($"缺少图层排序器: {prefab.name}");
                missing++;
            }
        }

        EditorUtility.DisplayDialog("武器图层检查",
            $"总武器数: {total}\n缺少排序器: {missing}", "OK");
    }
}
