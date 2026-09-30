using UnityEngine;

/// <summary>
/// 摄像机震动 —— 挂载到场景中的 "Main Camera" 上。
/// 职责:收到震动请求后,让摄像机在原地附近随机抖动一小段时间,然后自动归位。
/// 用法:任何脚本调 CameraShake.Instance.Shake(强度, 时长);
/// </summary>
public class CameraShake : MonoBehaviour
{
    /// <summary>单例入口。整个场景只有一台主摄像机,天然适合单例。</summary>
    public static CameraShake Instance { get; private set; }

    private Vector3 _basePosition;   // 摄像机的"家" —— 永远基于它算偏移
    private float _intensity;        // 震动幅度(世界单位)
    private float _duration;         // 本次震动总时长(秒),0 表示"没在震"
    private float _elapsed;          // 已经震了多久

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 记下摄像机的原始位置。
        // ⚠️ 震动永远是"从家出发的临时偏移",绝不能基于当前位置累加 ——
        //    否则每帧的随机偏移会攒起来,摄像机会越飘越远,最后跑出场景。
        _basePosition = transform.position;
    }

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

        // Random.insideUnitCircle:在半径 1 的圆内随机取一个点(Vector2)
        Vector2 offset = Random.insideUnitCircle * (_intensity * falloff);

        transform.position = _basePosition + new Vector3(offset.x, offset.y, 0f);
    }
}
