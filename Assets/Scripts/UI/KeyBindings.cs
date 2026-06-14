using UnityEngine;

/// <summary>
/// 全局按键绑定管理器（单例）
/// 所有脚本通过 KeyBindings.Instance.xxx 读取按键
/// 设置面板修改此处的值即可全局生效
/// </summary>
public class KeyBindings : MonoBehaviour
{
    public static KeyBindings Instance { get; private set; }

    [Header("移动")]
    public KeyCode moveUp    = KeyCode.W;
    public KeyCode moveDown  = KeyCode.S;
    public KeyCode moveLeft  = KeyCode.A;
    public KeyCode moveRight = KeyCode.D;
    public KeyCode sprint    = KeyCode.LeftShift;

    [Header("战斗")]
    public KeyCode reload    = KeyCode.R;
    public KeyCode weapon1   = KeyCode.Alpha1;
    public KeyCode weapon2   = KeyCode.Alpha2;
    public KeyCode weapon3   = KeyCode.Alpha3;
    public KeyCode dropWeapon = KeyCode.G;

    [Header("交互")]
    public KeyCode interact  = KeyCode.F;
    public KeyCode inventory = KeyCode.M;

    [Header("系统")]
    public KeyCode pause     = KeyCode.Escape;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadBindings();
    }

    /// <summary>保存按键到 PlayerPrefs</summary>
    public void SaveBindings()
    {
        PlayerPrefs.SetInt("Key_MoveUp",    (int)moveUp);
        PlayerPrefs.SetInt("Key_MoveDown",  (int)moveDown);
        PlayerPrefs.SetInt("Key_MoveLeft",  (int)moveLeft);
        PlayerPrefs.SetInt("Key_MoveRight", (int)moveRight);
        PlayerPrefs.SetInt("Key_Sprint",    (int)sprint);
        PlayerPrefs.SetInt("Key_Reload",    (int)reload);
        PlayerPrefs.SetInt("Key_Weapon1",   (int)weapon1);
        PlayerPrefs.SetInt("Key_Weapon2",   (int)weapon2);
        PlayerPrefs.SetInt("Key_Weapon3",   (int)weapon3);
        PlayerPrefs.SetInt("Key_Drop",      (int)dropWeapon);
        PlayerPrefs.SetInt("Key_Interact",  (int)interact);
        PlayerPrefs.SetInt("Key_Inventory", (int)inventory);
        PlayerPrefs.SetInt("Key_Pause",     (int)pause);
        PlayerPrefs.Save();
        Debug.Log("[KeyBindings] 按键设置已保存");
    }

    /// <summary>从 PlayerPrefs 加载按键</summary>
    public void LoadBindings()
    {
        moveUp    = (KeyCode)PlayerPrefs.GetInt("Key_MoveUp",    (int)KeyCode.W);
        moveDown  = (KeyCode)PlayerPrefs.GetInt("Key_MoveDown",  (int)KeyCode.S);
        moveLeft  = (KeyCode)PlayerPrefs.GetInt("Key_MoveLeft",  (int)KeyCode.A);
        moveRight = (KeyCode)PlayerPrefs.GetInt("Key_MoveRight", (int)KeyCode.D);
        sprint    = (KeyCode)PlayerPrefs.GetInt("Key_Sprint",    (int)KeyCode.LeftShift);
        reload    = (KeyCode)PlayerPrefs.GetInt("Key_Reload",    (int)KeyCode.R);
        weapon1   = (KeyCode)PlayerPrefs.GetInt("Key_Weapon1",   (int)KeyCode.Alpha1);
        weapon2   = (KeyCode)PlayerPrefs.GetInt("Key_Weapon2",   (int)KeyCode.Alpha2);
        weapon3   = (KeyCode)PlayerPrefs.GetInt("Key_Weapon3",   (int)KeyCode.Alpha3);
        dropWeapon= (KeyCode)PlayerPrefs.GetInt("Key_Drop",      (int)KeyCode.G);
        interact  = (KeyCode)PlayerPrefs.GetInt("Key_Interact",  (int)KeyCode.F);
        inventory = (KeyCode)PlayerPrefs.GetInt("Key_Inventory", (int)KeyCode.M);
        pause     = (KeyCode)PlayerPrefs.GetInt("Key_Pause",     (int)KeyCode.Escape);
    }

    /// <summary>恢复默认按键</summary>
    public void ResetToDefault()
    {
        moveUp = KeyCode.W; moveDown = KeyCode.S;
        moveLeft = KeyCode.A; moveRight = KeyCode.D;
        sprint = KeyCode.LeftShift; reload = KeyCode.R;
        weapon1 = KeyCode.Alpha1; weapon2 = KeyCode.Alpha2; weapon3 = KeyCode.Alpha3;
        dropWeapon = KeyCode.G; interact = KeyCode.F;
        inventory = KeyCode.M; pause = KeyCode.Escape;
        SaveBindings();
    }
}
