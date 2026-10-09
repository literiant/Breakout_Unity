using System;
using UnityEngine;

/// <summary>
/// 砖块被打碎时广播出去的数据。
///
/// 事件【应该携带接收者需要的信息】,而不是让接收者自己去查 ——
/// 飘分文字需要知道"在哪、多少分、什么颜色",所以一次性打包发出去。
/// </summary>
public struct BrickDestroyedEvent
{
    public Vector2 position;   // 砖块位置 —— 飘分文字要在这里冒出来
    public int score;          // 这一块值多少分
    public Color color;        // 砖块颜色 —— 飘分文字跟它同色,视觉上能对上
}

/// <summary>
/// 全局事件中心。
///
/// 为什么需要它:
///   之前 BrickController 直接调 GameController.Instance.OnBrickDestroyed() ——
///   砖块【必须认识】GameController。想加一个新反应(飘分、音效、成就)就得改砖块。
///
///   现在砖块只喊一句"我碎了",谁关心谁自己订阅。砖块不认识任何人。
///
/// ⚠️ 静态事件是【跨场景存活】的:场景重载不会清空订阅列表。
///    所以订阅者必须在 OnDisable 里退订,否则会留下指向"已销毁对象"的悬挂引用。
/// </summary>
public static class GameEvents
{
    /// <summary>砖块被打碎。</summary>
    public static event Action<BrickDestroyedEvent> BrickDestroyed;

    /// <summary>球掉出场地。</summary>
    public static event Action BallLost;

    // ⚠️ 用方法而不是让外部直接 Invoke —— event 关键字保证了外部【不能】直接触发,
    //    只有这两个方法能广播。这就是 event 相比普通委托字段的全部价值:
    //    "谁都能听,但只有主人能说"。
    public static void RaiseBrickDestroyed(BrickDestroyedEvent data)
    {
        // ⚠️ 这里的 ?. 是【正确】的。
        //    Unity 对象不能用 ?. 判空,是因为 ?. 绕过 Unity 重载的 ==。
        //    但 Action<T> 是纯 C# 委托,不是 UnityEngine.Object —— 它没有重载 ==。
        //    没有订阅者时直接 Invoke() 会 NullReferenceException,所以 ?. 必须写。
        BrickDestroyed?.Invoke(data);
    }

    public static void RaiseBallLost()
    {
        BallLost?.Invoke();
    }
}
