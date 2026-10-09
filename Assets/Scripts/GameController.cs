using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 全局游戏管理器 —— 挂载到场景中的空物体 "GameController" 上。
/// 职责:持有跨物体的全局状态(分数、生命、关卡进度),并提供唯一访问入口。
/// </summary>
public class GameController : MonoBehaviour
{
    /// <summary>单例入口。任何脚本都可以用 GameController.Instance 拿到它。</summary>
    public static GameController Instance { get; private set; }

    [Header("分数")]
    [Tooltip("当前分数。字段 —— 外部只能通过 ModifyScore 修改")]
    [SerializeField] private int _score;
    public int Score { get => _score; private set { _score = value; RefreshUI(); } }

    [Header("生命")]
    [SerializeField] private int _lives = 3;
    public int Lives { get => _lives; private set { _lives = value; RefreshUI(); } }

    [Header("砖块")]
    [Tooltip("本关还剩多少块砖。由 StartLevel 写入,不需要手工填")]
    [SerializeField] private int _bricksRemaining;
    // 注意:这里【不】调 RefreshUI —— 屏幕上没有显示砖块数,没必要为它重建文本网格。
    // 这条和"数据变了才刷 UI"是一回事:没有观察者,就不需要通知。
    public int BricksRemaining { get => _bricksRemaining; private set => _bricksRemaining = value; }

    /// <summary>游戏是否已经结束(通关或失败)。结束后一律不再计分、不再响应操作。</summary>
    public bool IsGameOver { get; private set; }

    [Header("关卡")]
    [Tooltip("按顺序排列的关卡。打完最后一个才算通关")]
    [SerializeField] private LevelData[] _levels;

    [Tooltip("进下一关前的停顿(秒),让玩家看清最后一块砖碎掉")]
    [SerializeField] private float _nextLevelDelay = 0.8f;

    [Header("场景引用")]
    [SerializeField] private BallController _ball;
    [SerializeField] private BrickSpawner _spawner;

    [Header("UI")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _livesText;
    [SerializeField] private TMP_Text _messageText;

    private Color _livesDefaultColor;   // 生命文本的原始颜色 —— 开局只读一次
    private int _currentLevelIndex = -1;

    void Awake()
    {
        // 单例守卫:如果已经有实例了,就把自己销毁掉。
        // 为什么可能出现两个?比如切场景时旧的不销毁、或者手滑在场景里放了两份。
        // 注意这里用 != null 而不是 is null —— 因为这是 UnityEngine.Object。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;                      // 关键:return 掉,别往下执行
        }

        Instance = this;

        // 开局就把"依赖没接好"的问题吼出来,而不是等运行时莫名崩溃。
        // Debug.LogError 的第二个参数 this 会把报错绑定到物体上,Console 里点一下就能定位。
        if (_ball == null) Debug.LogError("GameController: 没有指定球 _ball", this);
        if (_spawner == null) Debug.LogError("GameController: 没有指定生成器 _spawner", this);
        if (_scoreText == null || _livesText == null || _messageText == null)
            Debug.LogError("GameController: UI 文本没有拖全", this);

        // ⚠️ 原始颜色必须在这里【读一次并存下来】。
        //    如果每次刷新时现读 _livesText.color,读到的是"当前颜色" ——
        //    一旦变过红,就再也恢复不了了。
        _livesDefaultColor = _livesText.color;

        _messageText.gameObject.SetActive(false);
        RefreshUI();
    }

    void OnEnable()
    {
        // 订阅全局事件。用 OnEnable / OnDisable 而不是 Awake / OnDestroy ——
        // 它们在每次"启用 / 禁用"时成对调用,物体被临时关掉再打开也不会漏。
        //
        // ⚠️ 必须能退订,所以这里用具名方法,不能用 Lambda
        //    (两次 Lambda 表达式是两个不同的对象,-= 匹配不上)。
        GameEvents.BrickDestroyed += HandleBrickDestroyed;
        GameEvents.BallLost += HandleBallLost;
    }

    void OnDisable()
    {
        // ⚠️ 静态事件跨场景存活,不退订就会留下指向"已销毁的 GameController"的悬挂引用。
        //    下次事件触发时调用它的方法 → MissingReferenceException。
        GameEvents.BrickDestroyed -= HandleBrickDestroyed;
        GameEvents.BallLost -= HandleBallLost;
    }

    void Start()
    {
        // 由 GameController 主动发起第一关,而不是让 BrickSpawner 在自己的 Start 里生成。
        // 这样"谁先跑"这件事从【Unity 的执行顺序】变成了【代码里的调用顺序】,不再靠运气。
        StartLevel(0);
    }

    private void Update()
    {
        if (IsGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            return;                      // 结束后不再处理暂停
        }

        if (Input.GetKeyDown(KeyCode.P)) TogglePause();
    }

    // ==================== 关卡流程 ====================

    /// <summary>开始第 index 关:清空旧砖、重置球、生成新砖墙。</summary>
    private void StartLevel(int index)
    {
        if (_levels == null || _levels.Length == 0)
        {
            // 同样是"大声失败":没有任何关卡是配置错误,不是正常状态
            Debug.LogError("GameController: Levels 数组是空的,没有关卡可玩", this);
            return;
        }

        if (index < 0 || index >= _levels.Length) return;

        _currentLevelIndex = index;

        _spawner.ClearBricks();
        _ball.ResetBall();                                     // 球先回起点,避免和新砖块重叠
        BricksRemaining = _spawner.SpawnLevel(_levels[index]); // 生成器返回真实数量(有留空的格子)

        Debug.Log($"第 {index + 1} 关开始,砖块 {BricksRemaining} 块");
    }

    /// <summary>本关清空后被调用:还有下一关就切过去,没有就通关。</summary>
    private IEnumerator GoToNextLevel()
    {
        Debug.Log("本关通过!");
        yield return new WaitForSeconds(_nextLevelDelay);   // 停一下,让玩家看完最后一块砖碎掉
        StartLevel(_currentLevelIndex + 1);
    }

    // ==================== 对外接口 ====================

    /// <summary>加分。分数只能通过这里增长,不允许外部直接赋值。</summary>
    public void ModifyScore(int amount)
    {
        Score += amount;
    }

    public void ModifyLife(int amount)
    {
        Lives += amount;
    }

    /// <summary>事件回调:球掉出场地。由 DeathZone 广播,这里决定规则。</summary>
    private void HandleBallLost()
    {
        if (IsGameOver) return;

        ModifyLife(-1);

        if (Lives <= 0)
        {
            GameOver();
            return;
        }

        _ball.ResetBall();
        _ball.ModifySpeed(_ball.CurrentSpeed * 0.1f);   // 每掉一条命,球更快一点
    }

    /// <summary>事件回调:一块砖被打碎。由 BrickController 广播,这里决定规则。</summary>
    private void HandleBrickDestroyed(BrickDestroyedEvent data)
    {
        if (IsGameOver) return;

        ModifyScore(data.score);
        BricksRemaining -= 1;

        if (BricksRemaining > 0) return;

        // 本关清空:还有下一关就切,否则真·通关
        if (_currentLevelIndex + 1 < _levels.Length)
            StartCoroutine(GoToNextLevel());
        else
            EndGame("通关!");
    }

    // ==================== 结束与重开 ====================

    /// <summary>Win / GameOver 的公共部分 —— 两种结局只有文案不同,其余完全一样。</summary>
    private void EndGame(string message)
    {
        IsGameOver = true;
        _messageText.text = message;
        _messageText.gameObject.SetActive(true);

        _ball.ResetBall();
        _ball.enabled = false;      // 关掉球这一个组件 → Update 和碰撞回调都不再派发
    }

    public void Win()
    {
        if (!IsGameOver) EndGame("通关!");
    }

    public void GameOver()
    {
        if (!IsGameOver) EndGame("Game Over");
    }

    private void TogglePause()
    {
        // ⚠️ 用 > 0 而不是 == 0 判断。浮点数比相等是不可靠的写法,
        //    而且以后若有人把 timeScale 调成 0.5(慢动作),"是否暂停"的判断也不该崩。
        bool pauseNow = Time.timeScale > 0f;

        Time.timeScale = pauseNow ? 0f : 1f;

        _messageText.text = pauseNow ? "已暂停" : "";
        _messageText.gameObject.SetActive(pauseNow);
    }

    private void Restart()
    {
        // ⚠️ 必须在重载【之前】恢复 timeScale。
        //    Time.timeScale 是引擎级的全局状态,不属于任何场景 ——
        //    场景重载【不会】把它重置。带着 0 进新场景,整个世界都是冻住的。
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ==================== UI ====================

    private void RefreshUI()
    {
        _scoreText.text = $"Score: {Score}";
        _livesText.text = $"Lives: {Lives}";

        // 两条分支都要处理 —— 只写"变红"不写"恢复"的话,一旦有加命机制,
        // 颜色就永远卡在红色下不来。
        _livesText.color = (Lives <= 1) ? Color.red : _livesDefaultColor;
    }
}
