using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 飘分文字【对象池】—— ⚠️ 必须挂在 Canvas 的子物体上。
///
/// 它同时是【事件系统价值】的证明:
///   它是凭空出现的,BrickController 一行代码都没为它改过。
///
/// 池化思路:飘分是"生命周期短、创建频繁、外形固定"的典型 —— 每次打碎砖块都要一个。
///   与其反复 Instantiate / Destroy(每次 Destroy 都产生 GC 垃圾),
///   不如开局造好一批,之后只做"借出 / 归还"。
/// </summary>
public class ScorePopupSpawner : MonoBehaviour
{
    [Header("预制体")]
    [Tooltip("带 ScorePopup 脚本的 TMP 文本预制体")]
    public ScorePopup popupPrefab;

    [Header("池子")]
    [Tooltip("开局预先创建多少个。游戏中不够用时会自动补,但会有一次分配开销")]
    public int prewarmCount = 8;

    // readonly = 这个字段只能在声明处/构造函数里赋值,之后不能再指向别的对象。
    // ⚠️ 它锁的是【引用本身】,不保护内容 —— 你依然可以 Push / Pop。
    private readonly Stack<ScorePopup> _available = new Stack<ScorePopup>();

    void Awake()
    {
        // 预热:开局就把对象造好。这样游戏中最坏的情况也已经付过账了。
        // ⚠️ 配置故障:吼 + 止损。停用之后 OnEnable 也不会订阅,一次日志就够。
        if (popupPrefab == null)
        {
            Debug.LogError("ScorePopupSpawner: 还没有指定 popupPrefab,组件已停用", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < prewarmCount; i++)
        {
            _available.Push(CreateNewOne());
        }
    }

    void OnEnable()
    {
        GameEvents.BrickDestroyed += HandleBrickDestroyed;
    }

    void OnDisable()
    {
        // ⚠️ 静态事件跨场景存活,不退订就会留下指向"已销毁对象"的悬挂引用。
        //    (这个坑你已经亲手撞过一次了 —— 见实验 2 的 MissingReferenceException)
        GameEvents.BrickDestroyed -= HandleBrickDestroyed;
    }

    private void HandleBrickDestroyed(BrickDestroyedEvent data)
    {
        // 这里【不再】需要 if (popupPrefab == null) return;
        //   Awake 已经保证了它非空 —— 否则这个组件根本不会被启用。
        //   多一个守卫会把后面的代码变成死代码,那个坑刚踩过。

        ScorePopup popup = Get();

        // 世界坐标 → 屏幕坐标。
        // Overlay Canvas 上的 UI 用【屏幕像素】定位,而砖块在【世界坐标】里。
        Vector3 screenPos = Camera.main.WorldToScreenPoint(data.position);
        screenPos.x += Random.Range(-30f, 30f);
        // 把"归还"这个动作当成参数传进去。
        // 对象只负责播动画,不负责决定自己的命运 —— 这就是 Action 委托的用法。
        popup.Play($"+{data.score}", data.color, screenPos, () => Release(popup));
    }

    // ==================== 池的三个动作 ====================

    /// <summary>借出一个可用的飘分。</summary>
    private ScorePopup Get()
    {
        ScorePopup popup;
        if (_available.Count == 0)
        {
            // 池子空了。临时补一个 —— 不报错,因为游戏还能继续。
            // 但这说明 prewarmCount 该调大了,所以给个警告让你知道。
            Debug.LogWarning($"飘分池不够用(预热 {prewarmCount} 个),临时新建。考虑调大 prewarmCount。");
            popup = CreateNewOne();
        }
        else popup = _available.Pop();
        popup.gameObject.SetActive(true);
        return popup;
    }

    /// <summary>归还。注意:不销毁,只是关掉它放回池子。</summary>
    private void Release(ScorePopup popup)
    {
        popup.gameObject.SetActive(false);
        _available.Push(popup);
    }

    /// <summary>真正创建一个。只在预热和扩容时调用。</summary>
    private ScorePopup CreateNewOne()
    {
        ScorePopup popup = Instantiate(popupPrefab, transform);
        popup.gameObject.SetActive(false);
        return popup;
    }
}
