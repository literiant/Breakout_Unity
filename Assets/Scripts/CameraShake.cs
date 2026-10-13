using UnityEngine;

/// <summary>
/// 摄像机震动 —— 挂在场景中的 "Main Camera" 上。
///
/// 它是一个【纯听众】:订阅事件 → 震一下。没有任何人需要找到它,
/// 所以【不需要单例】—— 和 AudioManager 完全一样。
///
/// 震动的幅度【集中在这里】配置,而不是散在 36 份砖块预制体实例上。
/// 震动是"全局手感参数",应该只有一个调参的地方。
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("各事件的震动幅度(设成 0 = 不震)")]
    public float brickDamagedShake = 0.04f;
    public float brickDestroyedShake = 0.13f;
    public float paddleHitShake = 0.06f;
    public float ballLostShake = 0.30f;

    [Header("时长(秒)")]
    public float shortDuration = 0.12f;
    public float ballLostDuration = 0.35f;

    private Vector3 _basePosition;   // 摄像机的"家" —— 永远基于它算偏移
    private float _intensity;
    private float _duration;         // 0 表示"没在震"
    private float _elapsed;

    void Awake()
    {
        // 记下摄像机的原始位置。
        // ⚠️ 震动永远是从家出发的临时偏移,绝不能基于当前位置累加 ——
        //    否则每帧的随机偏移会攒起来,摄像机会越飘越远。
        _basePosition = transform.position;
    }

    // ==================== 订阅 ====================

    void OnEnable()
    {
        GameEvents.BrickDamaged += HandleBrickDamaged;
        GameEvents.BrickDestroyed += HandleBrickDestroyed;
        GameEvents.PaddleHit += HandlePaddleHit;
        GameEvents.BallLost += HandleBallLost;
    }

    void OnDisable()
    {
        // ⚠️ 静态事件跨场景存活,一个都不能漏。
        GameEvents.BrickDamaged -= HandleBrickDamaged;
        GameEvents.BrickDestroyed -= HandleBrickDestroyed;
        GameEvents.PaddleHit -= HandlePaddleHit;
        GameEvents.BallLost -= HandleBallLost;
    }

    // ==================== 回调 ====================

    private void HandleBrickDamaged(BrickDamagedEvent data) => Shake(brickDamagedShake, shortDuration);
    private void HandleBrickDestroyed(BrickDestroyedEvent data) => Shake(brickDestroyedShake, shortDuration);
    private void HandlePaddleHit(Vector2 position) => Shake(paddleHitShake, shortDuration);
    private void HandleBallLost() => Shake(ballLostShake, ballLostDuration);

    // ==================== 震动 ====================

    /// <summary>
    /// 请求一次震动。同时来多个请求时:幅度取更大的、时长取"剩余时长"和"新时长"里更长的。
    /// 这样小震动不会把正在播的大震动打断。
    /// </summary>
    public void Shake(float intensity, float duration)
    {
        _intensity = Mathf.Max(_intensity, intensity);
        _duration = Mathf.Max(duration, _duration - _elapsed);   // 剩余的 vs 新的,取长
        _elapsed = 0f;                                           // 计时重开
    }

    /// <summary>
    /// LateUpdate:所有 Update 都跑完之后才执行。
    /// 摄像机依赖"其他物体这一帧最终在哪",所以必须最后算。
    /// </summary>
    void LateUpdate()
    {
        if (_duration <= 0f) return;   // 没在震 —— 不碰摄像机

        _elapsed += Time.deltaTime;

        if (_elapsed >= _duration)
        {
            // 震完了:精确归位,并清零状态,让下一次 Shake 从干净状态开始
            transform.position = _basePosition;
            _duration = 0f;
            _intensity = 0f;
            return;
        }

        // 衰减系数:刚开始震 = 1,结束时 = 0
        float falloff = 1f - (_elapsed / _duration);

        // Random.insideUnitCircle:在半径 1 的圆内随机取一个点
        Vector2 offset = Random.insideUnitCircle * (_intensity * falloff);

        transform.position = _basePosition + new Vector3(offset.x, offset.y, 0f);
    }
}
