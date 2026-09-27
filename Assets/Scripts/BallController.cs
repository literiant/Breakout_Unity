using UnityEngine;

/// <summary>
/// 小球控制器 —— 挂载到 "Ball" 物体上。
/// 职责:开局时给小球一个初速度,之后完全交给物理引擎自己跑。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]   // 没有刚体就自动补一个,避免 GetComponent 拿到 null
public class BallController : MonoBehaviour
{
    [Header("发射参数")]
    [Tooltip("发射速率(单位/秒),只决定快慢")]
    public float launchSpeed = 6f;

    [Tooltip("发射角度(度):0=正右方,90=正上方")]
    public float launchAngle = 60f;

    // 缓存刚体引用。GetComponent 有查找开销,别在每帧里反复调用。
    private Rigidbody2D rb;

    void Awake()
    {
        // 生命周期顺序:Awake → OnEnable → Start → Update / FixedUpdate
        // 在 Awake 里拿引用,能保证之后任何地方用 rb 都有效。
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 角度 → 单位方向向量。Unity 的三角函数吃的是【弧度】,不是角度。
        float rad = launchAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        // 速度 = 方向 × 速率
        rb.velocity = dir * launchSpeed;
    }
    private float accumulator = 0f;
    public float maxSpeed = 10f;
    public float speedUpInterval = 1f;
    public float speedUpMultiplier = 1.05f;
    void FixedUpdate()
    {
        accumulator += Time.fixedDeltaTime;
        if (accumulator > speedUpInterval)
        {
            float magV = rb.velocity.magnitude;
            float newMagV = Mathf.Min(magV * speedUpMultiplier, maxSpeed);
            if (newMagV > magV + 0.001f)
                rb.velocity = rb.velocity.normalized * newMagV;
            accumulator -= speedUpInterval;
        }
    }

    // ★ 重点:这里【没有】FixedUpdate,也【不该】有。
    //
    // 为什么设速度不需要 FixedUpdate?
    //   因为 velocity 是刚体自己的【状态】,赋一次值,物理引擎就会在之后的
    //   每个物理步里按它继续积分位移。我们不需要每帧去"推"它。
    //
    // 那 FixedUpdate 到底什么时候非用不可?
    //   —— 当你需要【持续地】修改物理状态时。例如:
    //       · 每帧给刚体施加力:rb.AddForce(...)
    //       · 让速度随时间变化:rb.velocity *= 1.01f
    //       · 手动读取输入来控制物理物体(如角色移动)
    //   这些操作必须踩在物理时钟上,用固定步长执行,结果才稳定可复现。
    //
    // 一句话:改一次状态 → Start/Update 里做;持续改状态 → FixedUpdate 里做。
    // 注意:你【不会】在 Update 里改 transform 来移动带刚体的物体 ——
    //       那等于绕过物理引擎直接改结果,会和引擎自己的积分打架,表现为抖动/穿墙。
}
