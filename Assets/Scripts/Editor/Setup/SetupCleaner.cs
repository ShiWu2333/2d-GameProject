using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 配置清理器
/// 在重新配置前删除旧数据，确保全新生成
/// </summary>
public static class SetupCleaner
{
    /// <summary>
    /// 清除所有旧配置数据
    /// </summary>
    public static void CleanAll(GameObject playerGO, Canvas canvas)
    {
        CleanPrefabs();
        CleanPlayer(playerGO);
        CleanHUD(canvas);
        AmmoSpawner.CleanOldAmmo();
    }

    /// <summary>
    /// 清除子弹和武器 Prefab 文件夹
    /// </summary>
    public static void CleanPrefabs()
    {
        DeleteFolderContents(PrefabBuilder.BULLET_DIR, "*.prefab");
        DeleteFolderContents(PrefabBuilder.WEAPON_DIR, "*.prefab");
        // WhiteSquare.png 保留不删（体积极小，其他工具可能也用）
        AssetDatabase.Refresh();
        Debug.Log("[SetupCleaner] 旧Prefab已清除");
    }

    /// <summary>
    /// 清除玩家身上的旧武器实例和武器槽引用
    /// </summary>
    public static void CleanPlayer(GameObject playerGO)
    {
        if (playerGO == null) return;

        // 清除 AimPivot 下的所有武器子物体
        Transform aimPivot = playerGO.transform.Find("AimPivot");
        if (aimPivot != null)
        {
            for (int i = aimPivot.childCount - 1; i >= 0; i--)
            {
                var child = aimPivot.GetChild(i);
                // 只删武器相关子物体（Weapon_开头的）
                if (child.name.StartsWith("Weapon_"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        // 重置 WeaponSlotSystem 引用
        var slotSys = playerGO.GetComponent<WeaponSlotSystem>();
        if (slotSys != null)
        {
            Undo.RecordObject(slotSys, "Clean WeaponSlotSystem");
            slotSys.primary1 = null;
            slotSys.primary2 = null;
            slotSys.melee    = null;
            EditorUtility.SetDirty(slotSys);
        }

        Debug.Log("[SetupCleaner] 玩家旧武器已清除");
    }

    /// <summary>
    /// 清除 Canvas 下的旧 HUD 面板
    /// </summary>
    public static void CleanHUD(Canvas canvas)
    {
        if (canvas == null) return;

        var panel = canvas.transform.Find("WeaponSlotPanel");
        if (panel != null)
        {
            Undo.DestroyObjectImmediate(panel.gameObject);
        }

        Debug.Log("[SetupCleaner] 旧HUD面板已清除");
    }

    // ── 工具方法 ──────────────────────────────────────

    private static void DeleteFolderContents(string folderPath, string pattern)
    {
        if (!AssetDatabase.IsValidFolder(folderPath)) return;

        // 获取文件夹的完整路径
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folderPath));
        if (!Directory.Exists(fullPath)) return;

        string[] files = Directory.GetFiles(fullPath, pattern);
        foreach (string file in files)
        {
            // 转换为相对路径给AssetDatabase用
            string relativePath = folderPath + "/" + Path.GetFileName(file);
            AssetDatabase.DeleteAsset(relativePath);
        }

        // 也删除对应的.meta文件残留
        string[] metas = Directory.GetFiles(fullPath, "*.meta");
        foreach (string meta in metas)
        {
            string baseName = Path.GetFileNameWithoutExtension(meta); // e.g. "SMG.prefab"
            if (baseName.EndsWith(".prefab"))
            {
                // 对应的prefab已经被删了，meta也可以删
                File.Delete(meta);
            }
        }
    }
}
