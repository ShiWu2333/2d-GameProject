using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 暂停菜单：Escape 键打开/关闭
/// 显示深色滤镜 + 退出按钮 + 设置按钮
/// 暂停时 Time.timeScale = 0
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI 引用（由 Editor 工具自动赋值）")]
    public GameObject overlay;        // 深色半透明遮罩
    public GameObject menuPanel;      // 菜单面板
    public Button    resumeButton;    // 继续按钮
    public Button    settingsButton;  // 设置按钮
    public Button    quitButton;      // 退出按钮

    [Header("设置面板（可选，留空则设置按钮无效果）")]
    public GameObject settingsPanel;

    private bool isPaused;

    /// <summary>全局暂停状态（供其他脚本检查）</summary>
    public static bool IsGamePaused { get; private set; }

    void Start()
    {
        if (overlay != null) overlay.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        KeyCode pauseKey = KeyBindings.Instance != null ? KeyBindings.Instance.pause : KeyCode.Escape;
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        IsGamePaused = true;
        Time.timeScale = 0f;

        if (overlay != null) overlay.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void Resume()
    {
        isPaused = false;
        IsGamePaused = false;
        Time.timeScale = 1f;

        if (overlay != null) overlay.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public bool IsPaused => isPaused;
}
