using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;


/// <summary>
/// 全局游戏管理器 —— 挂载到场景中的空物体 "GameController" 上。
/// 职责:持有跨物体的全局状态(分数、生命),并提供唯一访问入口。
/// </summary>
public class GameController : MonoBehaviour
{
    /// <summary>
    /// 单例入口。任何脚本都可以用 GameController.Instance 拿到它。
    /// { get; private set; } = 外部只能读,只有本类能改。
    /// </summary>
    public static GameController Instance { get; private set; }

    [Header("分数")]
    [Tooltip("当前分数。字段 —— 外部只能通过 ModifyScore 修改")]
    [SerializeField]
    private int _score;


    public int Score { get => _score; private set { _score = value; RefreshUI(); } }

    [Header("生命")]
    [SerializeField]
    private int _lives = 3;
    public int Lives { get => _lives; private set { _lives = value; RefreshUI(); } }

    [Header("砖块")]
    [SerializeField] private int _bricksRemaining;
    public int BricksRemaining { get => _bricksRemaining; private set { _bricksRemaining = value; RefreshUI(); } }

    public bool IsGameOver { get; private set; }      // 只读,不需要 Inspector


    [Header("场景引用")]
    [SerializeField]
    private BallController _ball;

    [Header("UI")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _livesText;
    [SerializeField] private TMP_Text _messageText;

    private Color initialColor;
    void Awake()
    {
        if (_ball == null)
            Debug.LogError("GameController: 没有指定球!", this);
        initialColor = _livesText.color;
        _messageText.gameObject.SetActive(false);

        // 单例守卫:如果已经有实例了,就把自己销毁掉。
        // 为什么可能出现两个?比如切场景时旧的不销毁、或者手滑在场景里放了两份。
        // 注意这里用 != null 而不是 is null —— 因为这是 UnityEngine.Object。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // 自杀,让先来的那个继续当老大
            return;                // 关键:return 掉,别往下执行
        }

        Instance = this;
        RefreshUI();
    }

    private void Update()
    {
        if (IsGameOver && Input.GetKeyDown(KeyCode.R))
            Restart();
        if (!IsGameOver && Input.GetKeyDown(KeyCode.P))
            if (Time.timeScale == 1f)
                Time.timeScale = 0f;
            else if (Time.timeScale == 0f)
                Time.timeScale = 1f;
    }

    /// <summary>加分。分数只能通过这里增长,不允许外部直接赋值。</summary>
    public void ModifyScore(int amount)
    {
        Score += amount;
        Debug.Log($"当前分数:{Score}");
    }
    public void ModifyLife(int amount)
    {
        Lives += amount;
        Debug.Log($"当前生命{Lives}");
    }
    public void OnBallLost()
    {
        ModifyLife(-1);
        if (Lives <= 0) GameOver();
        else
        {
            _ball.ResetBall();
            _ball.ModifySpeed(_ball.CurrentSpeed * 0.1f);
        }


    }
    private void RefreshUI()
    {
        _scoreText.text = $"Score:{Score}";
        _livesText.text = $"Lives:{Lives}";
        if (Lives <= 1) _livesText.color = Color.red;
        else _livesText.color = initialColor;
    }

    public void RegisterBricks(int count)
    {
        BricksRemaining += count;
    }
    public void OnBrickDestroyed(int score)
    {
        if (IsGameOver) return;
        ModifyScore(score);
        BricksRemaining -= 1;
        if (BricksRemaining <= 0) Win();
    }
    void EndGame(string message)
    {
        IsGameOver = true;
        _messageText.gameObject.SetActive(true);
        _messageText.text = message;
        _ball.ResetBall();
        _ball.enabled = false;
    }
    public void Win()
    {
        if (!IsGameOver) EndGame("Success!");
    }
    public void GameOver()
    {
        if (!IsGameOver) EndGame("GameOver!");
    }
    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1f;
    }
}
