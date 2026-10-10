using UnityEngine;

/// <summary>
/// 程序化音效生成器 —— 用代码"合成"出 AudioClip,不依赖任何素材文件。
///
/// ⚠️ 这是【占位方案】,不是玩具做法:
///    以后你换成真实素材时,只需要把 AudioClip 的引用换掉,
///    AudioSource 那一整套播放代码一行都不用动。
///
/// 所有音效都遵循同一个公式:
///     采样点[i] = 波形(相位) × 包络(时间) × 音量
/// </summary>
public static class SfxLibrary
{
    private const int SampleRate = 44100;   // 每秒采多少个点。CD 音质 = 44100

    /// <summary>
    /// 正弦音 + 线性衰减包络 —— "叮""咚"这类有音高的声音。
    /// </summary>
    /// <param name="frequency">频率(Hz)。越大音越高。440 = 标准音 A</param>
    /// <param name="duration">时长(秒)</param>
    public static AudioClip Tone(string name, float frequency, float duration, float volume = 0.4f, bool square = false)
    {
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (float)i / count;     // 包络:从 1 线性衰减到 0
            if (!square)
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            else data[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume);
        }
        return Build(name, data);
    }

    /// <summary>
    /// 频率滑音 —— "嗖""唰"这类有下坠/上升感的声音。
    /// </summary>
    public static AudioClip Sweep(string name, float startFreq, float endFreq,
                                  float duration, float volume = 0.4f)
    {
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];

        // ⚠️ 频率在变的时候不能直接写 sin(2π·f(t)·t) —— 相位会跳变,听出"喀"的杂音。
        //    正确做法是【累加相位】:每一帧把当前频率贡献的相位增量加上去。
        float phase = 0f;

        for (int i = 0; i < count; i++)
        {
            float k = (float)i / count;
            float freq = Mathf.Lerp(startFreq, endFreq, k);   // 频率随时间滑

            phase += 2f * Mathf.PI * freq / SampleRate;       // 累加相位,保证连续

            float envelope = 1f - k;
            data[i] = Mathf.Sin(phase) * envelope * volume;
        }

        return Build(name, data);
    }

    /// <summary>
    /// 白噪声 + 指数衰减 + 【低通滤波】—— "啪""碎"这类打击音。
    /// 噪声没有音高,听起来像"材质"而不是"音符"。
    ///
    /// ⚠️ 为什么要滤波:
    ///   纯白噪声包含【所有频率】,听起来是"嘶——",又刺耳又廉价。
    ///   真实的撞击声是被材质吸收过的 —— 高频先被吃掉。
    ///   一个一阶低通就能把"电子噪声"变成"木头/塑料碎裂"。
    /// </summary>
    /// <param name="smooth">滤波强度。越接近 1 越亮(越刺耳),越小越闷。0.25 左右比较像木头</param>
    public static AudioClip Noise(string name, float duration, float volume = 0.4f, float smooth = 0.25f)
    {
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];

        float filtered = 0f;   // 滤波器的"记忆" —— 上一次的输出

        for (int i = 0; i < count; i++)
        {
            float k = (float)i / count;

            // 指数衰减包络。比线性衰减更像真实撞击:起始猛、尾巴短。
            // 原来的线性衰减会让声音"拖"到最后才断,听感发糊。
            float envelope = Mathf.Exp(-6f * k);

            // 一阶低通(指数移动平均):
            //   filtered 慢慢"追"当前的噪声值。smooth 越小追得越慢,高频被削得越多。
            float noise = Random.Range(-1f, 1f);
            filtered += (noise - filtered) * smooth;

            data[i] = filtered * envelope * volume;
        }

        return Build(name, data);
    }

    /// <summary>把采样数组打包成一个 AudioClip。</summary>
    private static AudioClip Build(string name, float[] samples)
    {
        // 参数依次是:名字、采样点数量、声道数(1 = 单声道)、采样率、是否流式加载
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);   // 把数据灌进去
        return clip;
    }
}
