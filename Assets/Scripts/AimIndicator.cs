using UnityEngine;

/// <summary>
/// 发射瞄准箭头 —— 挂在 Ball 的子物体 "AimArrow" 上。
/// 职责:把 BallController 里那个瞄准角度"显示出来";球发射后自动隐藏。
///
/// 它【不读输入、不管角度】—— 那些都是 BallController 的事。
/// 这个脚本只做一件事:把状态画出来。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AimIndicator : MonoBehaviour
{
    [Header("外观")]
    [Tooltip("箭头的颜色")]
    public Color color = new Color(1f, 1f, 1f, 0.55f);

    [Tooltip("箭头长度(世界单位)")]
    public float length = 1.5f;

    [Tooltip("渲染层级。要比【球】高 —— 箭头根部压在球身上,不设的话谁盖谁是不确定的")]
    public int sortingOrder = 10;

    private SpriteRenderer _sr;
    private BallController _ball;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        // ⚠️ 不用 Inspector 手拖 —— 【层级的父子关系本身就是这条信息】。
        //    手拖等于把已知的信息再人工声明一遍,于是出现两个真相源:
        //      真源:箭头挂在 Ball 下面
        //      副本:Inspector 里的引用
        //    两者不一致时,症状就是下面那个静默 bug。
        _ball = GetComponentInParent<BallController>();

        // ⚠️ 这是【故障】,不是业务条件 —— 必须大声失败。
        //
        //    而且要【止损】:把自己关掉。
        //    日志只说一次,组件直接停摆 —— 而不是留一个每帧空转、
        //    每帧可能抛 NullReferenceException 的东西在场景里刷屏。
        if (_ball == null)
        {
            Debug.LogError("AimIndicator 必须挂在 Ball 的子物体上", this);
            enabled = false;
            return;
        }

        // 箭头是"朝上"的三角形。轴心在底部中心,所以旋转时绕的是球心。
        _sr.sprite = SpriteFactory.CreateArrowSprite(32, 48, color);
        _sr.sortingOrder = sortingOrder;
    }

    void Update()
    {
        // 注意:这里【不再】需要 if (_ball == null) return;
        //   Awake 已经保证了 _ball 非空 —— 否则这个组件根本不会被启用。
        //
        //   多一个守卫在这里,反而会把它【后面所有的代码】变成死代码,
        //   包括那些本该在出错时暴露问题的访问。那个坑刚踩过。

        // 球已经发射出去了 → 箭头没有意义,藏起来
        if (_ball.IsLaunched)
        {
            if (_sr.enabled) _sr.enabled = false;
            return;
        }

        if (!_sr.enabled) _sr.enabled = true;

        // ⚠️ 旋转方向:箭头默认朝 +Y(0, 1)。
        //    想去方向 (sin a, cos a),绕 Z 轴需要转【-a】度,不是 +a。
        //    推导:(0,1) 逆时针转 θ 得到 (-sinθ, cosθ),要它等于 (sin a, cos a) → θ = -a
        transform.localRotation = Quaternion.Euler(0f, 0f, -_ball.AimAngle);

        transform.localScale = Vector3.one * length;
    }
}
