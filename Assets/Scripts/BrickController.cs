using System.Collections;
using UnityEngine;

/// <summary>
/// 砖块控制器 —— 挂载到预制体 "Assets/Prefabs/Brick.prefab" 上。
/// 职责:被球撞到时变暗、扣除耐久;耐久耗尽后播放碎裂动画再销毁自己。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BrickController : MonoBehaviour
{
    [Header("身份识别")]
    [Tooltip("小球的 Tag。只有这个 Tag 撞过来才算数")]
    public string ballTag = "Ball";

    [Header("耐久")]
    [Tooltip("能挨几下。撞满次数后碎裂")]
    public int brickHealth = 2;

    [Header("分值")]
    [Tooltip("撞碎后加多少分")]
    public int brickScore = 1;

    [Header("碎裂动画")]
    [Tooltip("碎裂动画总时长(秒)。0 = 瞬间消失")]
    public float breakDuration = 0.18f;

    [Range(0f, 1f)]
    [Tooltip("碎裂结束时缩到原尺寸的百分之几")]
    public float breakScale = 0.2f;

    [Header("撞击反馈")]
    [Tooltip("被撞但没碎时的摄像机震动幅度")]
    public float shakeOnHit = 0.05f;

    [Tooltip("碎裂时的摄像机震动幅度")]
    public float shakeOnBreak = 0.13f;

    private SpriteRenderer _sr;
    private BoxCollider2D _collider;

    private int _currentHealth;
    private Color _fullColor;        // 满血时的颜色(预制体上设的那个)
    private bool _isDying = false;   // 正在播碎裂动画 —— 期间不再响应任何碰撞

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _collider = GetComponent<BoxCollider2D>();

        // 初始化全部放在 Awake,不放 Start。
        // ⚠️ 原因:Awake 在 Instantiate 的那一刻【同步】执行,而 Start 要等到这一帧稍后。
        //    砖块是在 BrickSpawner.Start() 里生成的,极端情况下物理回调可能先于 Start 到达,
        //    那时若 _currentHealth 还是 0、_fullColor 还是黑色,变暗计算就会出错。
        _currentHealth = brickHealth;
        _fullColor = _sr.color;
    }

    /// <summary>
    /// 由 BrickSpawner 在 Instantiate 之后立刻调用,写入"这一行特有"的耐久 / 分值 / 颜色。
    ///
    /// ⚠️ 为什么必须有这个方法,而不能让生成器直接改 public 字段:
    ///    Instantiate 会【同步】执行新对象的 Awake,而 Awake 里已经把
    ///    _currentHealth = brickHealth、_fullColor = _sr.color 这两份"快照"算好了。
    ///    之后再改 brickHealth 字段,_currentHealth 不会跟着变 —— 它已经拷走了。
    ///    所以必须有一个显式入口,把字段和快照一起刷新。
    ///
    /// 这是"实例化后再配置实例"的标准做法:凡是 Awake 里做过初始化的脚本,
    /// 想事后改它的初始值,都得留一个这样的入口。
    /// </summary>
    public void Setup(int health, int score, Color color)
    {
        brickHealth = health;
        brickScore = score;

        _currentHealth = health;   // 刷新 Awake 里拷贝出去的快照
        _fullColor = color;        // 同上
        _sr.color = color;         // 立刻上色,先看到颜色再谈打几下
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_isDying) return;
        if (!col.gameObject.CompareTag(ballTag)) return;

        _currentHealth--;

        if (_currentHealth <= 0)
        {
            Break();
            return;
        }

        // 没碎:按剩余血量整体压暗。
        // ⚠️ 基准是 _fullColor 而不是"当前颜色" —— 当前颜色每次都不同,累乘会越算越偏。
        // ⚠️ alpha 固定写 1:否则 Color 乘法会把透明度也一起乘掉,砖块会越撞越透明。
        float ratio = (float)_currentHealth / brickHealth;
        _sr.color = new Color(_fullColor.r * ratio, _fullColor.g * ratio, _fullColor.b * ratio, 1f);

        // 广播"被撞但没碎" —— 音效要轻响、粒子要少量。
        // 注意:飘分和计分的订阅者【不】监听这个事件,所以它们不会误触发。
        GameEvents.RaiseBrickDamaged(new BrickDamagedEvent
        {
            position = transform.position,
            color = _fullColor,
        });

        Shake(shakeOnHit);
    }

    /// <summary>碎裂:通知计分、关掉碰撞体、播动画,最后在协程末尾销毁自己。</summary>
    private void Break()
    {
        _isDying = true;

        // 广播出去:砖块不认识 GameController,也不认识飘分文字 —— 它只报告"我碎了"。
        // 谁关心谁自己订阅。以后加音效、成就、统计,都不用回来改这里。
        GameEvents.RaiseBrickDestroyed(new BrickDestroyedEvent
        {
            position = transform.position,
            score = brickScore,
            color = _fullColor,
        });

        // ⚠️ 立刻关掉碰撞体。
        //    否则这 0.18 秒动画期间球还能再撞上来一次,而"已死"的砖块再被撞会重复加分。
        _collider.enabled = false;

        Shake(shakeOnBreak);

        StartCoroutine(BreakAnimation());
    }

    /// <summary>
    /// 调试用:立刻打碎这块砖,走【完整的】碎裂流程(动画 + 计分 + 广播)。
    ///
    /// 为什么不直接 Destroy:那样不会触发事件,分数不涨、剩余砖块数不减、
    /// 过关判定永远不触发 —— 那测的就不是真实流程了。
    /// </summary>
    public void ForceBreak()
    {
        if (_isDying) return;
        Break();
    }

    /// <summary>
    /// 碎裂动画。协程:每帧缩小一点、淡出一点,跑完再销毁。
    /// </summary>
    private IEnumerator BreakAnimation()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * breakScale;
        float randomZ = Random.Range(-25f, 25f);
        float t = 0f;
        while (t < breakDuration)
        {
            t += Time.deltaTime;                           // 累加真实时间
            float k = Mathf.Clamp01(t / breakDuration);    // 进度 0 → 1

            transform.localScale = Vector3.Lerp(startScale, endScale, k);

            Color c = _sr.color;
            c.a = 1f - k;                                  // 淡出
            _sr.color = c;

            float currentZ = Mathf.Lerp(0f, randomZ, k);
            transform.localRotation = Quaternion.Euler(0f, 0f, currentZ);

            yield return null;                             // 交还控制权,下一帧从这里继续
        }

        Destroy(gameObject);                               // 必须是最后一句 —— 销毁会终止协程
    }

    private void Shake(float intensity)
    {
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(intensity, 0.12f);
    }
}
