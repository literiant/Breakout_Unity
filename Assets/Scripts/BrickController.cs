using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 砖块控制器 —— 挂载到 "Rectangle" 物体上(它就是砖块原型)。
/// 职责:被小球撞到就销毁自己。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))] // 没有碰撞体就自动补一个 —— 砖块靠它才可能被球撞到
[RequireComponent(typeof(SpriteRenderer))]

public class BrickController : MonoBehaviour
{
    [Header("身份识别")]
    [Tooltip("小球的 Tag。只有这个 Tag 撞过来才算数,防止被其他东西误拆")]
    public string ballTag = "Ball";
    [Header("最大撞击次数")]
    public int strikeCount = 2;
    private SpriteRenderer _sr;
    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }
    /// <summary>
    /// 撞到东西时调用。Unity 2D 会把碰撞事件【同时发给碰撞双方】,
    /// 所以哪怕砖块没有 Rigidbody2D,只要球有,砖块这边也能收到这个回调。
    /// </summary>
    void OnCollisionEnter2D(Collision2D col)
    {
        // col.gameObject 是"撞过来的那一方"。
        // CompareTag 比 col.gameObject.tag == "Ball" 更推荐:它不产生字符串临时对象(GC 友好)。
        if (!col.gameObject.CompareTag(ballTag)) return;
        strikeCount--;
        _sr.color = new Color(0.45f, 0.45f, 0.45f);
        // 销毁整个砖块物体。
        // 注意:Destroy 不是立即执行的,它把销毁请求排到【本帧末尾】统一处理。
        // 所以这一帧里 gameObject 其实还活着,下一帧才真正消失。
        if (strikeCount == 0)
            Destroy(gameObject);
    }
}
