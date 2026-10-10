using UnityEngine;

/// <summary>
/// 音效播放器 —— 挂在一个带 AudioSource 的场景空物体上。
///
/// ⚠️ 它【不是单例】,也不需要是 ——
///    没有任何人需要找到它。它只是一个纯听众:订阅事件 → 放声音。
///    (对比 GameController:砖块要能找到它,所以必须有单例。
///     事件系统让"单向通知"的场景彻底摆脱了单例。)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("音量")]
    [Range(0f, 1f)]
    [Tooltip("总音量。调低可以缓解多个音效同时播放时的削波(爆音)")]
    public float masterVolume = 0.6f;

    [Header("音色参数")]
    [Tooltip("碎砖音的音量")]
    public float brickVolume = 0.5f;

    [Tooltip("撞挡板音的频率(Hz),越大越脆")]
    public float paddleFrequency = 520f;

    [Tooltip("掉球滑音的起止频率")]
    public float ballLostStartFreq = 700f;
    public float ballLostEndFreq = 120f;

    private AudioSource _source;

    private AudioClip _brickBreak;
    private AudioClip _paddleHit;
    private AudioClip _ballLost;
    private AudioClip _win;
    private AudioClip _lose;

    void Awake()
    {
        _source = GetComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false;

        // ⚠️ Spatial Blend = 0 表示【2D 音效】:音量与声源位置无关,永远一样响。
        //    本游戏是固定视角的 2D,球飞到屏幕角落时声音不该变小。
        //    设成 1(3D)的话,声音会随 AudioListener 与声源的距离衰减,听起来会莫名其妙地变轻。
        _source.spatialBlend = 0f;

        BuildClips();
    }

    /// <summary>开局生成所有音效。一次性成本(几毫秒),之后复用同一批 AudioClip。</summary>
    private void BuildClips()
    {
        _brickBreak = SfxLibrary.Noise("BrickBreak", 0.12f, brickVolume);
        _paddleHit = SfxLibrary.Tone("PaddleHit", paddleFrequency, 0.09f, 0.45f);
        _ballLost = SfxLibrary.Sweep("BallLost", ballLostStartFreq, ballLostEndFreq, 0.45f, 0.4f);
        _win = SfxLibrary.Tone("Win", 880f, 0.5f, 0.35f);
        _lose = SfxLibrary.Sweep("Lose", 400f, 80f, 0.7f, 0.4f);
    }

    // ==================== 订阅 ====================

    void OnEnable()
    {
        GameEvents.BrickDestroyed += HandleBrickDestroyed;
        GameEvents.PaddleHit += HandlePaddleHit;
        GameEvents.BallLost += HandleBallLost;
        GameEvents.GameEnded += HandleGameEnded;
    }

    void OnDisable()
    {
        // ⚠️ 一个都不能漏。静态事件跨场景存活,漏掉哪个,哪个就会在重开后
        //    去调用"已销毁对象"的方法 —— 你已经亲眼见过那个 MissingReferenceException 了。
        GameEvents.BrickDestroyed -= HandleBrickDestroyed;
        GameEvents.PaddleHit -= HandlePaddleHit;
        GameEvents.BallLost -= HandleBallLost;
        GameEvents.GameEnded -= HandleGameEnded;
    }

    // ==================== 回调 ====================

    private void HandleBrickDestroyed(BrickDestroyedEvent data) => Play(_brickBreak);
    private void HandlePaddleHit(Vector2 position) => Play(_paddleHit);
    private void HandleBallLost() => Play(_ballLost);
    private void HandleGameEnded(bool won) => Play(won ? _win : _lose);

    /// <summary>
    /// ⚠️ 用 PlayOneShot 而不是 Play。
    ///
    ///   source.Play()          → 打断当前正在播的,同时只有 1 个声音
    ///   source.PlayOneShot(c)  → 叠加播放,可以同时响多个
    ///
    ///   球一秒撞三块砖,用 Play 的话你只能听到最后一块 —— 前面两块被掐断了。
    ///   代价是会叠加,音量过大会削波。这也是为什么 masterVolume 默认只有 0.6。
    /// </summary>
    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        _source.PlayOneShot(clip, masterVolume);
    }
}
