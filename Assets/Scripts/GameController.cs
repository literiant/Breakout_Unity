using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 全局游戏管理器 —— 挂载到场景中的空物体 "GameController" 上。
/// 职责:持有全局状态(分数、生命、关卡进度),并决定游戏的规则。
///
/// ⚠️ 它【不是】单例,也不提供全局访问入口 —— 谁想改分数就广播事件。
/// </summary>
public class GameController : MonoBehaviour
{
    // ⚠️ 这里【没有】单例。
    //    原来有 Instance,是为了让 BrickController / DeathZone 能直接调进来。
    //    它们改成事件广播之后,唯一还在找 GameController 的只剩 DebugCheats ——
    //    一个编辑器专用的调试工具。而【工具应该去找被工具,不是反过来】。
    //    「需要一个引用」不等于「需要单例」。

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
        // ⚠️ 原来这里有一个"重复实例守卫"(发现第二个就把自己销毁)。
        //    那是【单例】才需要的防御 —— 单例语义要求"全局唯一",所以必须强制执行。
        //    现在没有单例了,这个守卫也就失去了依据,一并删除。
        //    代价是:手滑在场景里放两份 GameController 不再被自动发现(会两份都跑)。
        //    本项目只有一个场景、一个实例,这个代价可以接受。

        // 开局就把"依赖没接好"的问题吼出来,而不是等运行时莫名崩溃。
        // Debug.LogError 的第二个参数 this 会把报错绑定到物体上,Console 里点一下就能定位。
        // ⚠️ 配置故障:吼一声【然后止损】。
        //    只吼不止损的话,后面的 StartLevel → _ball.ResetBall() 会每关抛一次 NRE,
        //    日志被刷屏,真正的原因反而被埋掉。
        //    enabled = false 之后 Start 也不会被调用 —— 这是 Unity 的保证。
        if (_ball == null || _spawner == null ||
            _scoreText == null || _livesText == null || _messageText == null)
        {
            Debug.LogError("GameController: 场景引用没接全(_ball / _spawner / UI 文本),组件已停用", this);
            enabled = false;
            return;
        }

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
            EndGame("Success!", true);
    }

    // ==================== 结束与重开 ====================

    /// <summary>Win / GameOver 的公共部分 —— 两种结局只有文案不同,其余完全一样。</summary>
    private void EndGame(string message, bool won)
    {
        IsGameOver = true;
        _messageText.text = message;
        _messageText.gameObject.SetActive(true);

        _ball.ResetBall();
        _ball.enabled = false;      // 关掉球这一个组件 → Update 和碰撞回调都不再派发

        // 广播结局。音效、成就、结算界面都可以自己来听。
        GameEvents.RaiseGameEnded(won);
    }

    public void Win()
    {
        if (!IsGameOver) EndGame("Success!", true);
    }

    public void GameOver()
    {
        if (!IsGameOver) EndGame("Game Over", false);
    }

    // 「寄存 → 修改 → 恢复」的寄存器。
    // 同一个模式你已经用过两次:_fullColor(砖块原始颜色)、_livesDefaultColor(生命文本原始颜色)。
    private float _timeScaleBeforePause = 1f;

    private void TogglePause()
    {
        // ⚠️ 用 > 0 而不是 == 0 判断。浮点数比相等是不可靠的写法,
        //    而且以后若有人把 timeScale 调成 0.5(慢动作),"是否暂停"的判断也不该崩。
        bool pauseNow = Time.timeScale > 0f;

        if (pauseNow)
        {
            // 暂停:先【寄存】当前时率,再归零
            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            // 恢复:用【寄存下来的那个值】,而不是写死 1f
            // —— 否则"慢动作 → 暂停 → 恢复"会把 0.25 丢掉,直接跳回正常速度
            Time.timeScale = _timeScaleBeforePause;
        }

        _messageText.text = pauseNow ? "Paused" : "";
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
