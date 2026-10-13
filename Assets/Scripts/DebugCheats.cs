using UnityEngine;

/// <summary>
/// 调试作弊台 —— 挂在场景里任意一个空物体上(建议就叫 "DebugCheats")。
///
/// 为什么需要它:
///   游戏代码的状态空间很大,触发条件又依赖物理和玩家操作 ——
///   想测"最后一关通关"就得真打完三关,想测"掉命时 UI 变红"就得连掉两条命。
///   手动玩测的成本高到会让人放弃测试。
///
///   ⚠️ 让东西【容易被测】是设计的一部分,不是"测试工作"。
///      它需要知道"这个项目里哪些状态最难到达" —— 那是对项目的理解,不是通用知识。
///
/// ⚠️ 整个类被 #if UNITY_EDITOR 包住 —— 打包时这些代码会被【完全删除】,
///    正式版里不存在任何作弊入口。
/// </summary>
#if UNITY_EDITOR
public class DebugCheats : MonoBehaviour
{
    [Header("按键")]
    [Tooltip("炸掉场上所有砖块 —— 快速测通关流程")]
    public KeyCode bombAllKey = KeyCode.K;

    [Tooltip("加一条命 —— 测 UI 刷新和'生命变红后能否恢复'")]
    public KeyCode addLifeKey = KeyCode.L;

    [Tooltip("在指定位置手动广播一次碎砖事件 —— 测飘分 / 粒子 / 音效")]
    public KeyCode fakeBrickKey = KeyCode.B;

    [Tooltip("子弹时间")]
    public KeyCode toggleSlowTimeScaleKey = KeyCode.T;

    [Header("假事件参数")]
    [Tooltip("从哪个世界坐标发出")]
    public Vector2 fakeBrickPosition = new Vector2(0f, 1.5f);

    [Tooltip("假事件带多少分")]
    public int fakeBrickScore = 1;

    // GameController 的引用。开一次就缓存下来,不在按键时反复找。
    private GameController _game;

    private void Awake()
    {
        // ⚠️ 这里【故意】用运行时查找,既不是单例,也不是 Inspector 拖引用。
        //
        //   ① 这是编辑器专用代码,打包后整段消失 —— 性能根本不是问题
        //   ② 只在开局查一次,缓存在 _game 里
        //   ③ 最重要的一条:GameController 【不需要知道调试工具的存在】。
        //      工具应该去找被工具,而不是反过来 —— 原来的 Instance 正是反的。
        //   ④ 丢进任何场景、任何位置都能用。调试工具本来就该这样。
        //
        //   这就是"运行时查找"唯一真正合适的场景:一次性 + 工具代码 + 零耦合。
        _game = FindObjectOfType<GameController>();

        if (_game == null)
        {
            // 吼,但【不】enabled = false —— 另外三个键(K / B / T)并不依赖它,
            // 停用整个组件会误伤。
            // 止损的方式不止一种:"只在初始化时吼一次,而不是每次按键都吼"也是一种。
            Debug.LogWarning("DebugCheats: 场景里找不到 GameController,L 键(加命)不可用", this);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(bombAllKey)) BombAllBricks();
        if (Input.GetKeyDown(addLifeKey)) AddLife();
        if (Input.GetKeyDown(fakeBrickKey)) FakeBrickDestroyed();
        if (Input.GetKeyDown(toggleSlowTimeScaleKey)) toggleSlowTimeScale();
    }

    private void BombAllBricks()
    {
        BrickController[] bricks = FindObjectsOfType<BrickController>();
        foreach (BrickController brick in bricks)
            brick.ForceBreak();

        Debug.Log($"[调试] 炸掉了 {bricks.Length} 块砖");
    }

    private void AddLife()
    {
        // 这里可以安静地 return:Awake 已经吼过一次了,原因不用重复播报。
        if (_game == null) return;

        _game.ModifyLife(1);
        Debug.Log("[调试] 生命 +1");
    }

    /// <summary>
    /// 手动广播一次碎砖事件 —— 不必真打球,就能测所有"听众"。
    ///
    /// ⚠️ 注意副作用:事件是【广播】的,所以所有订阅者都会响应,
    ///    包括 GameController —— 它会以为真碎了一块砖,从而扣减剩余砖块数。
    ///    按多了会莫名其妙过关。
    ///
    ///    这是事件系统的双刃:解耦的代价是"你无法只通知其中一个人"。
    ///    真要送假事件也有办法(事件带一个 isDebug 标记),但那会污染生产代码。
    /// </summary>
    private void FakeBrickDestroyed()
    {
        GameEvents.RaiseBrickDestroyed(new BrickDestroyedEvent
        {
            position = fakeBrickPosition,
            score = fakeBrickScore,
            color = Color.cyan,
        });
        Debug.Log("[调试] 手动广播了一次碎砖事件");
    }
    private void toggleSlowTimeScale()
    {
        if (Time.timeScale == 0f)
        {
            Debug.Log("暂停中,不能变时率");
            return;
        }
        bool isNotSlow = Time.timeScale == 1.0f;
        Time.timeScale = isNotSlow ? 0.25f : 1.0f;
    }
}
#endif
