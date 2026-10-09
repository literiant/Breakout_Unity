using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 单个飘分文字 —— 挂在 TMP 文本预制体 "ScorePopup" 上。
/// 职责:生成后向上飘、同时淡出,飘完销毁自己。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ScorePopup : MonoBehaviour
{
    [Header("动画")]
    [Tooltip("向上飘多少屏幕像素")]
    public float riseDistance = 70f;

    [Tooltip("整个动画持续多少秒")]
    public float lifetime = 0.8f;

    [Tooltip("初始缩放。小于 1 = 从小弹出来")]
    public float startScale = 0.5f;

    private TMP_Text _text;
    private Color _color;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    /// <summary>由生成器调用:填内容、上色、定位,然后开始飘。</summary>
    public void Setup(string content, Color color, Vector3 screenPosition)
    {
        Debug.Log($"飘分已生成,屏幕位置 = {screenPosition}");
        _text.text = content;

        // ⚠️ alpha 强制设为 1。关卡里的砖块颜色理论上不透明,但万一有人调了透明度,
        //    飘分文字会跟着一起变透明 —— 那样就看不见了。这里不让它被传染。
        _color = color;
        _color.a = 1f;
        _text.color = _color;

        transform.position = screenPosition;
        transform.localScale = Vector3.one * startScale;

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * riseDistance;
        Vector3 VstartScale = transform.localScale * startScale;
        Vector3 VendScale = transform.localScale;
        float t = 0f;
        while (t < lifetime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / lifetime);        // 进度 0 → 1

            transform.position = Vector3.Lerp(start, end, k);

            Color c = _color;
            c.a = 1f - k;                                  // 淡出
            _text.color = c;

            // TODO(填空练习):让它"弹出来"——
            //   缩放从 startScale 涨到 1.0,用同一个 k 插值。
            //   提示:和 BreakAnimation 里的 Vector3.Lerp 是同一个套路
            transform.localScale = Vector3.Lerp(VstartScale, VendScale, k);
            yield return null;
        }

        Destroy(gameObject);                               // 必须是最后一句 —— 销毁会终止协程
    }
}
