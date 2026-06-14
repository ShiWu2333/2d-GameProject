using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// TMP 字体批量替换工具
/// 菜单：Tools → TMP字体批量替换
/// 将场景内 / Prefab 中所有 TextMeshProUGUI 和 TextMeshPro 的字体
/// 替换为 Assets/Assets/Fonts/KeAiZhongWenZiti
/// </summary>
public class TMPFontReplacer : EditorWindow
{
    // ══════════════════════════════════════════════════
    //  常量
    // ══════════════════════════════════════════════════
    private const string FONT_ASSET_PATH = "Assets/Assets/Fonts/KeAiZhongWenZiti.asset";

    // ══════════════════════════════════════════════════
    //  窗口状态
    // ══════════════════════════════════════════════════
    private TMP_FontAsset targetFont;

    private bool replaceScene   = true;
    private bool replacePrefabs = true;

    // 预览列表
    private List<string> previewLines = new List<string>();
    private Vector2      scroll;
    private bool         previewed = false;

    // ══════════════════════════════════════════════════
    //  菜单入口
    // ══════════════════════════════════════════════════
    [MenuItem("Tools/TMP字体批量替换")]
    public static void Open()
    {
        var w = GetWindow<TMPFontReplacer>("TMP字体批量替换");
        w.minSize = new Vector2(420, 460);
        w.Show();
        w.LoadFont();
    }

    private void LoadFont()
    {
        targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (targetFont == null)
            Debug.LogWarning($"[TMPFontReplacer] 未能加载字体：{FONT_ASSET_PATH}");
    }

    // ══════════════════════════════════════════════════
    //  GUI
    // ══════════════════════════════════════════════════
    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        // 标题
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 15, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("🔤  TMP 字体批量替换", titleStyle);
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "将所有 TextMeshProUGUI / TextMeshPro 的字体替换为指定中文字体。\n" +
            "支持当前场景和 Assets/Prefabs 目录下所有 Prefab。",
            MessageType.Info);
        EditorGUILayout.Space(6);
        DrawLine();

        // ── 字体选择 ──────────────────────────────────
        EditorGUILayout.LabelField("目标字体", EditorStyles.boldLabel);
        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "字体资源", targetFont, typeof(TMP_FontAsset), false);

        if (targetFont == null)
        {
            EditorGUILayout.HelpBox($"字体未加载，请确认路径：\n{FONT_ASSET_PATH}", MessageType.Error);
        }

        EditorGUILayout.Space(4);
        DrawLine();

        // ── 范围选择 ──────────────────────────────────
        EditorGUILayout.LabelField("替换范围", EditorStyles.boldLabel);
        replaceScene   = EditorGUILayout.ToggleLeft("✅ 当前场景（已打开的场景）", replaceScene);
        replacePrefabs = EditorGUILayout.ToggleLeft("✅ Prefab（Assets/Prefabs 目录）", replacePrefabs);
        EditorGUILayout.Space(4);
        DrawLine();

        // ── 预览 ──────────────────────────────────────
        EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);
        GUI.enabled = targetFont != null && (replaceScene || replacePrefabs);
        if (GUILayout.Button("🔍  预览会被替换的组件", GUILayout.Height(28)))
        {
            RunPreview();
        }
        GUI.enabled = true;

        if (previewed)
        {
            EditorGUILayout.HelpBox(
                $"共找到 {previewLines.Count} 个 TMP 组件将被替换。",
                previewLines.Count > 0 ? MessageType.Info : MessageType.Warning);

            // 列表（可滚动）
            if (previewLines.Count > 0)
            {
                var boxStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 11 };
                using (var scrollView = new EditorGUILayout.ScrollViewScope(
                    Vector2.zero, GUILayout.MaxHeight(160)))
                {
                    foreach (var line in previewLines)
                        EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
                }
            }
        }

        EditorGUILayout.Space(4);
        DrawLine();

        // ── 执行按钮 ──────────────────────────────────
        GUI.enabled = targetFont != null && (replaceScene || replacePrefabs);
        var btnStyle = new GUIStyle(GUI.skin.button)
            { fontSize = 13, fontStyle = FontStyle.Bold };
        if (GUILayout.Button("▶  执行替换", btnStyle, GUILayout.Height(40)))
        {
            bool confirm = EditorUtility.DisplayDialog(
                "确认替换",
                $"即将把所有 TMP 组件的字体替换为：\n{targetFont.name}\n\n此操作可通过 Ctrl+Z 撤销（场景内），但 Prefab 修改将直接写入磁盘。\n\n确认继续？",
                "确认替换", "取消");
            if (confirm) RunReplace();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(8);
        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════════════════
    //  预览
    // ══════════════════════════════════════════════════
    private void RunPreview()
    {
        previewLines.Clear();
        previewed = true;

        if (replaceScene)
            CollectFromScene(previewLines);

        if (replacePrefabs)
            CollectFromPrefabs(previewLines);
    }

    private void CollectFromScene(List<string> lines)
    {
        // TextMeshProUGUI
        foreach (var t in FindAllInScene<TextMeshProUGUI>())
        {
            if (t.font != targetFont)
                lines.Add($"[Scene] {GetPath(t.gameObject)}  ({t.GetType().Name})");
        }
        // TextMeshPro (3D)
        foreach (var t in FindAllInScene<TextMeshPro>())
        {
            if (t.font != targetFont)
                lines.Add($"[Scene] {GetPath(t.gameObject)}  ({t.GetType().Name})");
        }
    }

    private void CollectFromPrefabs(List<string> lines)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go      = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            foreach (var t in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.font != targetFont)
                    lines.Add($"[Prefab] {path} / {GetPath(t.gameObject)}");
            }
            foreach (var t in go.GetComponentsInChildren<TextMeshPro>(true))
            {
                if (t.font != targetFont)
                    lines.Add($"[Prefab] {path} / {GetPath(t.gameObject)}");
            }
        }
    }

    // ══════════════════════════════════════════════════
    //  执行替换
    // ══════════════════════════════════════════════════
    private void RunReplace()
    {
        int count = 0;

        try
        {
            // ── 场景 ──────────────────────────────────
            if (replaceScene)
            {
                EditorUtility.DisplayProgressBar("替换中...", "处理场景", 0.1f);
                Undo.SetCurrentGroupName("TMP字体批量替换");
                int group = Undo.GetCurrentGroup();

                foreach (var t in FindAllInScene<TextMeshProUGUI>())
                {
                    if (t.font == targetFont) continue;
                    Undo.RecordObject(t, "Replace TMP Font");
                    t.font = targetFont;
                    EditorUtility.SetDirty(t);
                    count++;
                }
                foreach (var t in FindAllInScene<TextMeshPro>())
                {
                    if (t.font == targetFont) continue;
                    Undo.RecordObject(t, "Replace TMP Font");
                    t.font = targetFont;
                    EditorUtility.SetDirty(t);
                    count++;
                }

                Undo.CollapseUndoOperations(group);
            }

            // ── Prefab ────────────────────────────────
            if (replacePrefabs)
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("替换中...",
                        $"处理 Prefab ({i + 1}/{guids.Length})：{path}",
                        0.2f + 0.8f * i / guids.Length);

                    // 用 EditPrefabContentsScope 修改 Prefab
                    using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        var root = scope.prefabContentsRoot;
                        bool modified = false;

                        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                        {
                            if (t.font == targetFont) continue;
                            t.font = targetFont;
                            EditorUtility.SetDirty(t);
                            modified = true;
                            count++;
                        }
                        foreach (var t in root.GetComponentsInChildren<TextMeshPro>(true))
                        {
                            if (t.font == targetFont) continue;
                            t.font = targetFont;
                            EditorUtility.SetDirty(t);
                            modified = true;
                            count++;
                        }

                        // modified == true 时 scope.Dispose() 会自动保存
                        _ = modified;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 刷新预览
            previewed = false;
            previewLines.Clear();

            Debug.Log($"[TMPFontReplacer] 替换完成，共修改 {count} 个组件。字体：{targetFont.name}");
            EditorUtility.DisplayDialog("✅ 替换完成",
                $"共替换了 {count} 个 TMP 组件。\n\n" +
                "• 场景内修改可通过 Ctrl+Z 撤销\n" +
                "• Prefab 修改已直接写入磁盘",
                "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ══════════════════════════════════════════════════
    //  工具方法
    // ══════════════════════════════════════════════════

    /// <summary>在当前已加载的场景中查找所有指定类型的组件（含非激活对象）</summary>
    private static List<T> FindAllInScene<T>() where T : Component
    {
        var result = new List<T>();
        // 遍历所有根节点（兼容多场景）
        foreach (var root in UnityEngine.SceneManagement.SceneManager
                     .GetActiveScene().GetRootGameObjects())
        {
            result.AddRange(root.GetComponentsInChildren<T>(true));
        }
        return result;
    }

    /// <summary>获取 GameObject 在层级中的路径</summary>
    private static string GetPath(GameObject go)
    {
        var parts = new System.Text.StringBuilder(go.name);
        var t = go.transform.parent;
        while (t != null)
        {
            parts.Insert(0, t.name + "/");
            t = t.parent;
        }
        return parts.ToString();
    }

    private static void DrawLine()
    {
        var r = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        EditorGUILayout.Space(3);
    }
}
