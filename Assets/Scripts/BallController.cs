using UnityEngine;

/// <summary>
/// 小球控制器 —— 挂载到 "Ball" 物体上。
/// 职责:开局时给小球一个初速度,之后完全交给物理引擎自己跑。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]   // 没有刚体就自动补一个,避免 GetComponent 拿到 null
public class BallController : MonoBehaviour
{
    [Header("依赖挡板")]
    [Tooltip("在未发射时,附着于挡板上侧")]
    public PaddleController paddle;
    [Tooltip("墙的 Tag。撞到时广播 WallHit 事件")]
    public string wallTag = "Wall";
    private string paddleTag;
    [Header("发射参数")]
    [Tooltip("发射速率(单位/秒),只决定快慢")]
    public float launchSpeed = 6f;
    // 缓存刚体引用。GetComponent 有查找开销,别在每帧里反复调用。
    private Rigidbody2D rb;

    void Awake()
    {
        if (paddle == null)
            Debug.LogError("BallController:没有指定挡板!", this);
        // 生命周期顺序:Awake → OnEnable → Start → Update / FixedUpdate
        // 在 Awake 里拿引用,能保证之后任何地方用 rb 都有效。
        rb = GetComponent<Rigidbody2D>();
    }

    [Header("反弹参数")]
    [Tooltip("出射最大偏角(度),从【竖直方向】量起。0=永远垂直弹回,60=最多斜 60°")]
    [Range(0f, 80f)]
    public float maxBounceAngle = 60f;

    [Header("角度校正")]
    [Tooltip("球与水平方向的最小夹角(度)。防止球陷入'太平'的轨迹")]
    [Range(0f, 45f)]
    public float minAngleFromHorizontal = 20f;
    [Tooltip("球与水平方向的最大夹角(度)。防止球陷入'太直'的轨迹")]
    [Range(70f, 90f)]
    public float maxAngleFromHorizontal = 80f;
    public float CurrentSpeed { get; private set; }      // 速率由我们自己维护(单一真相源)

    private ContactPoint2D[] contactBuffer = new ContactPoint2D[4];   // 预分配,避免每次碰撞都产生垃圾
    private Vector2 startPosition;
    private bool isLaunched = false;
    void Start()
    {
        paddleTag = paddle.tag;
        CurrentSpeed = launchSpeed;
        startPosition = rb.position;
        ResetBall();
    }
    void Update()
    {
        StickToPaddle();
        HandleInput();
    }

    /// <summary>
    /// 角度校正 —— 每个物理步检查一次球的方向。
    ///
    /// ⚠️ 为什么放 FixedUpdate 而不是 Update:
    ///   这是在【改刚体的速度】,不是读输入、也不是做表现。刚体操作必须跟物理时钟走。
    ///   Update 是渲染帧(间隔不固定),FixedUpdate 是固定步长(默认 0.02s) ——
    ///   这正是本项目的高频考点:**输入/表现 → Update;刚体受力/速度 → FixedUpdate。**
    ///
    ///   顺带:写在 FixedUpdate 里就意味着"任何原因导致的速度变化都会被校正",
    ///   不管是撞挡板、撞墙、还是撞砖块 —— 不需要给每个碰撞回调都加一行。
    /// </summary>
    void FixedUpdate()
    {
        // ⚠️ rb.velocity 是属性,整体赋值 ✅;改它的成员(如 rb.velocity.x = ...)❌ 不生效。
        //    (Vector2 是 struct,这个坑你在 L2 踩过:rb.velocity.Set(0, 0))
        rb.velocity = ClampAngle(rb.velocity, minAngleFromHorizontal, maxAngleFromHorizontal);
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        // 撞到墙:只【报告事实】,不改变球的运动 —— 反弹交给物理引擎。
        // 墙面是 Untagged 的话这个分支不会进,记得给三面墙打上 Wall 标签。
        if (col.gameObject.CompareTag(wallTag))
        {
            GameEvents.RaiseWallHit(rb.position);
            return;
        }

        if (!col.gameObject.CompareTag(paddleTag)) return;

        // 留出足够空间;GetContacts 用预分配数组,不产生堆分配(GC 友好)
        int count = col.GetContacts(contactBuffer);

        bool hitFromAbove = false;
        for (int i = 0; i < count; i++)
        {
            if (contactBuffer[i].normal.y > 0.5f) { hitFromAbove = true; break; }
        }
        if (!hitFromAbove) return;   // 撞的是侧面/底面 → 交给物理引擎自己处理

        float halfWidth = col.collider.bounds.extents.x;   // bounds 是世界空间
        if (halfWidth <= 0.0001f) return;                  // 防除零

        // 撞击点相对挡板中心的偏移,归一化到 [-1, 1]
        float offset = (rb.position.x - col.transform.position.x) / halfWidth;
        offset = Mathf.Clamp(offset, -1f, 1f);

        // 角度从竖直方向量起 → 用 (sin, cos)。
        // cos(θ) 在 |θ|<90° 时恒正,所以球【结构上】不可能向下飞,无需额外 clamp。
        float rad = offset * maxBounceAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));

        rb.velocity = dir * CurrentSpeed;   // 用自己维护的速率,不现取

        // 广播出去 —— 球只管"我撞到挡板了",谁想听(音效、粒子、连击)自己去订阅
        GameEvents.RaisePaddleHit(rb.position);
    }
    public void ResetBall()
    {
        isLaunched = false;
        transform.position = startPosition;
        rb.velocity = Vector2.zero;
    }
    private void LaunchBall()
    {
        isLaunched = true;
        float rad = Random.Range(30f, 150f) * Mathf.Deg2Rad;
        rb.velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * CurrentSpeed;
    }
    private void HandleInput()
    {
        if (!isLaunched && Input.GetKeyDown(KeyCode.Space))
            LaunchBall();
    }
    private void StickToPaddle()
    {
        if (isLaunched) return;
        //使用rb.position会有延迟拖影，而直接修改transform.position不会
        transform.position = new Vector2(paddle.transform.position.x, rb.position.y);
    }
    /// <summary>
    /// 把速度方向校正到"离水平至少 minAngleDeg 度"。
    ///
    /// 为什么需要它 —— 这是本包的核心:
    ///   球撞墙和撞砖是【纯物理反射】:bounciness = 1、friction = 0,
    ///   入射角恒等于反射角,**角度永不衰减**。
    ///   所以球一旦被弹成浅角,就会一辈子保持浅角 —— 打不出变化,而且浅角还容易穿透薄碰撞体。
    ///
    ///   物理引擎不管"好不好玩",它只管守恒。**手感是程序的责任。**
    ///
    /// 做法(避开角度归一化的所有麻烦):
    ///   归一化方向 (x, y) 满足 x² + y² = 1,所以 |y| = |sin θ|(θ = 与水平方向的夹角)。
    ///   "夹角太水平" 等价于 "|y| 太小" —— 不用碰 atan2,也不用处理角度绕圈。
    ///   把 |y| 抬到最小值,再用勾股恒等式反推 x。
    /// </summary>
    private static Vector2 ClampAngle(Vector2 velocity, float minAngleDeg, float maxAngleFromHorizontal)
    {
        float speed = velocity.magnitude;
        if (speed < 0.0001f) return velocity;              // 几乎静止 → 不碰它

        Vector2 dir = velocity / speed;                    // 归一化方向

        float minY = Mathf.Sin(minAngleDeg * Mathf.Deg2Rad);
        float maxY = Mathf.Sin(maxAngleFromHorizontal * Mathf.Deg2Rad);
        // 保留原来的方向感:球是往上还是往下、往左还是往右
        float signY = dir.y >= 0f ? 1f : -1f;
        float signX = dir.x >= 0f ? 1f : -1f;

        float newY;
        float newX;
        if (Mathf.Abs(dir.y) < minY)
        {
            newY = minY * signY;
            newX = Mathf.Sqrt(1f - minY * minY) * signX; // 勾股:x² + y² = 1}
        }
        else if (Mathf.Abs(dir.y) > maxY)
        {
            newY = maxY * signY;
            newX = Mathf.Sqrt(1f - maxY * maxY) * signX;
        }
        else return velocity;
        return new Vector2(newX, newY) * speed;            // 速率不变,只改方向
    }

    public void ModifySpeed(float amount)
    {
        CurrentSpeed = Mathf.Clamp(CurrentSpeed += amount, 0f, launchSpeed * 1.5f);
    }
}
