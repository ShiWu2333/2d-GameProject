using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 游戏管理器
/// 追踪敌人数量、玩家存活状态
/// 全部击杀 = 胜利 | 玩家死亡 = 失败
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 游戏状态
    public bool isGameOver { get; private set; }
    public bool isVictory { get; private set; }

    [Header("UI（自动生成，留空即可）")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI statsText;

    // 统计
    private int totalEnemies;
    private int killedEnemies;
    private float gameStartTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        isGameOver = false;
        isVictory = false;
        gameStartTime = Time.time;

        // 统计场景中的敌人数量
        totalEnemies = FindObjectsOfType<EnemyStats>().Length;
        killedEnemies = 0;

        Debug.Log($"[GameManager] 关卡开始！敌人数量：{totalEnemies}");

        // 监听玩家死亡
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null)
                stats.onDeath.AddListener(OnPlayerDeath);
        }
    }

    void Update()
    {
        if (!isGameOver) return;

        // 游戏结束后按R重新开始
        if (Input.GetKeyDown(KeyCode.R))
            RestartLevel();

        // 按Esc退出（返回主菜单或退出游戏）
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    /// <summary>敌人被击杀时调用</summary>
    public void OnEnemyKilled(EnemyStats enemy)
    {
        if (isGameOver) return;

        killedEnemies++;
        Debug.Log($"[GameManager] 击杀敌人 {killedEnemies}/{totalEnemies}");

        // 全部击杀 → 胜利
        if (killedEnemies >= totalEnemies)
            Victory();
    }

    /// <summary>玩家死亡</summary>
    private void OnPlayerDeath()
    {
        if (isGameOver) return;
        GameOver(false);
    }

    /// <summary>胜利</summary>
    private void Victory()
    {
        isGameOver = true;
        isVictory = true;
        Debug.Log("[GameManager] 胜利！所有敌人已被消灭！");
        ShowEndScreen(true);
    }

    /// <summary>游戏结束</summary>
    private void GameOver(bool won)
    {
        isGameOver = true;
        isVictory = won;
        Debug.Log(won ? "[GameManager] 胜利！" : "[GameManager] 失败！玩家死亡");
        ShowEndScreen(won);
    }

    private void ShowEndScreen(bool won)
    {
        // 如果没有预设UI，自动生成
        if (gameOverPanel == null)
            CreateEndScreenUI();

        gameOverPanel.SetActive(true);

        float elapsed = Time.time - gameStartTime;
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);

        if (resultText != null)
            resultText.text = won ? "任务完成" : "任务失败";

        if (statsText != null)
        {
            string timeStr = $"{minutes:00}:{seconds:00}";
            statsText.text = $"击杀：{killedEnemies}/{totalEnemies}\n用时：{timeStr}\n\n按 R 重新开始";
        }

        // 不暂停时间（保留子弹飞行等效果）
        // Time.timeScale = 0f; // 不使用，保持游戏运行
    }

    private void CreateEndScreenUI()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // 半透明黑色背景
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvas.transform, false);

        var rt = gameOverPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = gameOverPanel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        // 结果标题
        var titleGO = new GameObject("ResultTitle");
        titleGO.transform.SetParent(gameOverPanel.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.6f);
        titleRT.anchorMax = new Vector2(0.5f, 0.6f);
        titleRT.pivot = new Vector2(0.5f, 0.5f);
        titleRT.sizeDelta = new Vector2(400f, 80f);
        resultText = titleGO.AddComponent<TextMeshProUGUI>();
        resultText.fontSize = 48;
        resultText.fontStyle = FontStyles.Bold;
        resultText.color = Color.white;
        resultText.alignment = TextAlignmentOptions.Center;

        // 统计信息
        var statsGO = new GameObject("StatsText");
        statsGO.transform.SetParent(gameOverPanel.transform, false);
        var statsRT = statsGO.AddComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0.5f, 0.4f);
        statsRT.anchorMax = new Vector2(0.5f, 0.4f);
        statsRT.pivot = new Vector2(0.5f, 0.5f);
        statsRT.sizeDelta = new Vector2(400f, 150f);
        statsText = statsGO.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 24;
        statsText.color = new Color(0.8f, 0.8f, 0.8f);
        statsText.alignment = TextAlignmentOptions.Center;

        gameOverPanel.SetActive(false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
