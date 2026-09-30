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
    void OnCollisionEnter2D(Collision2D col)
    {
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
    public void ModifySpeed(float amount)
    {
        CurrentSpeed = Mathf.Clamp(CurrentSpeed += amount, 0f, launchSpeed * 1.5f);
    }
}
