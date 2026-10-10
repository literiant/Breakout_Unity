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

    [Header("假事件参数")]
    [Tooltip("从哪个世界坐标发出")]
    public Vector2 fakeBrickPosition = new Vector2(0f, 1.5f);

    [Tooltip("假事件带多少分")]
    public int fakeBrickScore = 1;

    private void Update()
    {
        if (Input.GetKeyDown(bombAllKey)) BombAllBricks();
        if (Input.GetKeyDown(addLifeKey)) AddLife();
        if (Input.GetKeyDown(fakeBrickKey)) FakeBrickDestroyed();
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
        if (GameController.Instance == null) return;
        GameController.Instance.ModifyLife(1);
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
}
#endif
