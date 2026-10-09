using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 单个飘分文字 —— 挂在 TMP 文本预制体 "ScorePopup" 上。
/// 职责:播一次"向上飘 + 淡出"的动画,播完【通知调用方】。
///
/// ⚠️ 它自己不 Destroy —— 因为对象池要把它收回去复用。
///    "播完之后干什么"由调用方通过 onFinished 回调决定:
///      池化版 → () => Release(popup)
///      非池化 → () => Destroy(popup.gameObject)
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ScorePopup : MonoBehaviour
{
    [Header("动画")]
    [Tooltip("向上飘多少屏幕像素")]
    public float riseDistance = 70f;

    [Tooltip("整个动画持续多少秒")]
    public float lifetime = 0.8f;

    [Tooltip("起始缩放,结束时回到 1.0")]
    public float startScale = 0.5f;

    private TMP_Text _text;
    private Color _color;
    private Action _onFinished;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    /// <summary>播一次飘分动画。onFinished 在动画结束时被调用(通常是"归还给池子")。</summary>
    public void Play(string content, Color color, Vector3 screenPosition, Action onFinished)
    {
        Debug.Log($"飘分复用,实例ID = {GetInstanceID()}");
        // ========== 重设全部状态 ==========
        // 池化的代价:借出来的对象带着上一次的残留。
        // 所以这里每一行都必须重设,不能有任何"上次留下的"东西。
        _text.text = content;

        // alpha 强制设为 1:不让关卡颜色的透明度传染过来
        _color = color;
        _color.a = 1f;
        _text.color = _color;

        transform.position = screenPosition;
        transform.localScale = Vector3.one * startScale;

        _onFinished = onFinished;

        // ⚠️ 池化后这句是必须的。
        //    只要有任何一个流程提前把对象还回池子,上一次的协程就可能还在跑;
        //    两套插值同时改同一个 transform,画面会抽搐。
        //    先杀干净再开新的,成本近乎为零。
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * riseDistance;

        // ⚠️ 起点和终点必须在【循环外】算好。
        //    写在循环里现读 transform.localScale,每帧读到的都是上一帧被改过的值 ——
        //    起点和终点会一起往下漂,结果就是"两三帧内缩成一个点"。
        //    这和 BreakAnimation 是同一个套路:循环外定两端,循环里只插值。
        Vector3 startScaleVec = Vector3.one * startScale;
        Vector3 endScaleVec = Vector3.one;      // 结束时回到原尺寸

        float t = 0f;
        while (t < lifetime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / lifetime);        // 进度 0 → 1

            transform.position = Vector3.Lerp(startPos, endPos, k);
            transform.localScale = Vector3.Lerp(startScaleVec, endScaleVec, k);

            Color c = _color;
            c.a = 1f - k;                                  // 淡出
            _text.color = c;

            yield return null;
        }

        // 播完了 —— 不销毁自己,把决定权交给调用方
        _onFinished?.Invoke();
    }
}
