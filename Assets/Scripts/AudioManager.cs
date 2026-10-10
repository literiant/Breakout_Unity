using UnityEngine;

/// <summary>
/// 音效播放器 —— 挂在场景里的空物体 "AudioManager" 上。
///
/// ⚠️ 它【不是单例】,也不需要是 —— 没有任何人需要找到它。
///    它只是一个纯听众:订阅事件 → 放声音。
///
/// 【核心设计:一组 AudioSource 当"喇叭池"】
///   之前只有一个 AudioSource + PlayOneShot,36 块砖同时碎 = 36 个声音叠加 → 削波(爆音)。
///   现在用 N 个喇叭轮流用:**全忙的时候就丢弃这次播放** —— 这就是天然的限流。
///
///   这和 ScorePopupSpawner 是同一个"池"模式,但目的不同:
///     飘分池  —— 为了【省掉创建销毁的开销】
///     喇叭池  —— 为了【限制同时发声的数量】
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("音量")]
    [Range(0f, 1f)]
    public float masterVolume = 0.6f;

    [Header("喇叭池")]
    [Tooltip("同时最多几个声音。满了就丢弃新的 —— 这就是限流")]
    public int voiceCount = 8;

    [Tooltip("每次播放的音高随机浮动(±)。让重复音效不单调")]
    [Range(0f, 0.3f)]
    public float pitchJitter = 0.08f;

    [Header("音色参数")]
    public float brickBreakVolume = 0.5f;
    public float brickDamageVolume = 0.35f;
    public float paddleFrequency = 520f;
    public float wallFrequency = 380f;
    public float ballLostStartFreq = 700f;
    public float ballLostEndFreq = 120f;

    private AudioSource[] _voices;
    private int _nextVoice = 0;        // 轮询游标

    private AudioClip _brickBreak;
    private AudioClip _brickDamage;
    private AudioClip _paddleHit;
    private AudioClip _wallHit;
    private AudioClip _ballLost;
    private AudioClip _win;
    private AudioClip _lose;

    void Awake()
    {
        BuildVoices();
        BuildClips();
    }

    /// <summary>建一组子物体,每个挂一个 AudioSource。这样它们各有独立的 clip / pitch。</summary>
    private void BuildVoices()
    {
        _voices = new AudioSource[Mathf.Max(1, voiceCount)];

        for (int i = 0; i < _voices.Length; i++)
        {
            var go = new GameObject($"Voice_{i}");
            go.transform.SetParent(transform, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;

            // ⚠️ Spatial Blend = 0 表示【2D 音效】:音量与声源位置无关。
            //    本游戏是固定视角的 2D,球飞到角落时声音不该变小。
            src.spatialBlend = 0f;

            _voices[i] = src;
        }
    }

    private void BuildClips()
    {
        _brickBreak = SfxLibrary.Noise("BrickBreak", 0.14f, brickBreakVolume, 0.22f);
        _brickDamage = SfxLibrary.Tone("BrickDamage", 260f, 0.05f, brickDamageVolume);
        _paddleHit = SfxLibrary.Tone("PaddleHit", paddleFrequency, 0.09f, 0.45f);
        _wallHit = SfxLibrary.Tone("WallHit", wallFrequency, 0.06f, 0.3f);
        _ballLost = SfxLibrary.Sweep("BallLost", ballLostStartFreq, ballLostEndFreq, 0.45f, 0.4f);
        _win = SfxLibrary.Tone("Win", 880f, 0.5f, 0.35f);
        _lose = SfxLibrary.Sweep("Lose", 400f, 80f, 0.7f, 0.4f);
    }

    // ==================== 订阅 ====================

    void OnEnable()
    {
        GameEvents.BrickDestroyed += HandleBrickDestroyed;
        GameEvents.BrickDamaged += HandleBrickDamaged;
        GameEvents.PaddleHit += HandlePaddleHit;
        GameEvents.WallHit += HandleWallHit;
        GameEvents.BallLost += HandleBallLost;
        GameEvents.GameEnded += HandleGameEnded;
    }

    void OnDisable()
    {
        // ⚠️ 一个都不能漏。静态事件跨场景存活,漏掉哪个,哪个就会在重开后
        //    去调用"已销毁对象"的方法 —— 你已经亲眼见过那个 MissingReferenceException。
        GameEvents.BrickDestroyed -= HandleBrickDestroyed;
        GameEvents.BrickDamaged -= HandleBrickDamaged;
        GameEvents.PaddleHit -= HandlePaddleHit;
        GameEvents.WallHit -= HandleWallHit;
        GameEvents.BallLost -= HandleBallLost;
        GameEvents.GameEnded -= HandleGameEnded;
    }

    // ==================== 回调 ====================

    private void HandleBrickDestroyed(BrickDestroyedEvent data) => Play(_brickBreak);
    private void HandleBrickDamaged(BrickDamagedEvent data) => Play(_brickDamage);
    private void HandlePaddleHit(Vector2 position) => Play(_paddleHit);
    private void HandleWallHit(Vector2 position) => Play(_wallHit);
    private void HandleBallLost() => Play(_ballLost);
    private void HandleGameEnded(bool won) => Play(won ? _win : _lose);

    // ==================== 播放 ====================

    /// <summary>
    /// 用一个空闲的喇叭播放。**全忙就丢弃这次播放。**
    ///
    /// 这个"丢弃"不是妥协,而是本设计的核心:
    ///   36 块砖同时碎 → 只有 8 个喇叭在响 → 听起来是"一片碎裂声",
    ///   而不是 36 个声音叠加到削波(爆音)。
    /// </summary>
    private void Play(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource voice = TakeFreeVoice();
        if (voice == null) return;      // 全忙 → 丢弃

        voice.clip = clip;
        voice.volume = masterVolume;

        // 音高随机浮动一点点。同一个音效连播时,听感不会像"复读机"。
        voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);

        voice.Play();
    }

    /// <summary>从上次用到的位置开始轮询,找第一个空闲的喇叭。找不到返回 null。</summary>
    private AudioSource TakeFreeVoice()
    {
        for (int i = 0; i < _voices.Length; i++)
        {
            // 取模 = 环形绕回。这样每次从不同位置起步,喇叭被均匀使用
            int index = (_nextVoice + i) % _voices.Length;

            if (!_voices[index].isPlaying)
            {
                _nextVoice = (index + 1) % _voices.Length;
                return _voices[index];
            }
        }
        return null;
    }
}
