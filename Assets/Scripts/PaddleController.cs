using UnityEngine;

/// <summary>
/// 挡板控制器 —— 挂载到场景中的 "Paddle" 物体上。
/// 职责:读取玩家的左右输入,让挡板沿 X 轴移动,并限制在屏幕范围内。
/// </summary>
public class PaddleController : MonoBehaviour
{
    [Header("移动参数")]
    [Tooltip("挡板最大移动速度,单位:Unity 单位 / 秒")]
    public float moveSpeed = 8f;

    [Header("左右边界(世界坐标 X)")]
    [Tooltip("挡板中心能到达的最左位置")]
    public float leftLimit = -6.5f;

    [Tooltip("挡板中心能到达的最右位置")]
    public float rightLimit = 6.5f;

    /// <summary>
    /// Update:每一渲染帧调用一次(帧率不固定,可能是 60 次/秒,也可能是 144 次/秒)。
    ///
    /// 为什么挡板移动写在 Update,而不是 FixedUpdate?
    ///   1. 这里要做的事是"读键盘输入"。输入是玩家在【真实时间】里做出的动作,
    ///      必须每渲染帧都问一次,才能跟手、不丢按键。
    ///   2. FixedUpdate 走的是【物理时钟】(默认每 0.02 秒一次),它和渲染帧不同步,
    ///      有时一帧里跑 0 次,有时一帧里跑 2 次。拿它读输入会出现"漏按"。
    ///   3. 而且这里我们直接改的是 Transform,根本没经过物理引擎,不需要物理时钟。
    ///
    /// 记忆口诀:输入 + 非物理表现 → Update;刚体受力 / 设速度 → FixedUpdate。
    /// </summary>
    void Update()
    {
        // ========== (1) 读输入 ==========
        // Input.GetAxis("Horizontal") 返回 -1 ~ 1 之间的浮点数:
        //   按 A / ←  → 数值逐渐变到 -1
        //   按 D / →  → 数值逐渐变到  1
        //   松开按键  → 数值平滑回到  0
        // 注意"逐渐 / 平滑"这几个字 —— 它自带一段缓动,所以手感更柔和。
        // 如果我们想要的是"按下瞬间到底、松开瞬间归零"的街机手感,
        // 那就应该改用 Input.GetAxisRaw("Horizontal")(只返回 -1 / 0 / 1)。
        float inputX = Input.GetAxis("Horizontal");

        // ========== (2) 算位移 ==========
        // 本帧移动距离 = 方向 × 速度 × 本帧耗时
        // Time.deltaTime = 上一帧到这一帧花了多少秒。
        // 乘上它之后,位移的含义就从"每帧移动多少"变成了"每秒移动多少"。
        // 于是 30 帧的电脑和 144 帧的电脑,挡板跑出来一样快。
        // 这就是几乎所有 Update 里的移动都要乘 Time.deltaTime 的原因。
        float deltaX = inputX * moveSpeed * Time.deltaTime;

        // ========== (3) 应用位移 ==========
        // transform.Translate(x, y, z) 的意思是"在当前位置上再叠加一个位移量"。
        // 注意:它默认按【本地坐标系】移动。挡板没有旋转,本地轴 = 世界轴,所以这里没问题;
        // 但如果物体被旋转过,就必须写成 transform.Translate(deltaX, 0f, 0f, Space.World)。
        transform.Translate(deltaX, 0f, 0f);

        // ========== (4) 限位 ==========
        // 先读出当前坐标。为什么要用一个临时变量接一下?
        // 因为 transform.position 返回的是一个【结构体副本】(Vector3 是 struct),
        // 直接写 transform.position.x = 5f 会编译报错 —— 不允许修改一个"临时返回值"。
        // 正确做法:取出来 → 改副本 → 整体写回。
        Vector3 pos = transform.position;

        if (Input.GetKeyDown(KeyCode.R))
            pos.x = 0f;

        // Mathf.Clamp(值, 最小, 最大):把值夹在区间内,超出就取边界值。
        // 它等价于:
        //   if (pos.x < leftLimit)  pos.x = leftLimit;
        //   if (pos.x > rightLimit) pos.x = rightLimit;
        // 但一行就能写完,而且不会漏掉边界情况。
        pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);

        // 把夹好的坐标整体写回 Transform,物体才会真正出现在新位置。
        transform.position = pos;
    }
}
