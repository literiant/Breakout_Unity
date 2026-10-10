using UnityEngine;

/// <summary>
/// 砖块碎裂粒子 —— 挂在场景里一个带 ParticleSystem 的空物体上。
///
/// 设计要点:【一个粒子系统管所有碎屑】,而不是每块砖生成一个特效。
///   每次碎裂只是"移到那个位置 → 发射 14 颗",零 Instantiate、零 Destroy、零池管理。
///
/// 这是对"对象池"那一课的补充:池化不是唯一答案,有时候"一个系统管所有"更干净。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class BrickBurstEffect : MonoBehaviour
{
    [Header("单次粒子数")]
    [Tooltip("每块砖碎裂时迸发多少颗碎屑")]
    public int burstCount = 14;
    [Tooltip("砖块受到伤害但不碎裂时迸发多少颗碎屑")]
    public int DamagedBurstCount = 4;

    [Header("碎屑")]
    [Tooltip("初速度范围(世界单位/秒)")]
    public Vector2 speedRange = new Vector2(2f, 6f);

    [Tooltip("碎屑尺寸范围")]
    public Vector2 sizeRange = new Vector2(0.06f, 0.16f);

    [Tooltip("碎屑存活时间(秒)")]
    public float lifetime = 0.6f;

    [Tooltip("重力影响。1 = 正常下落,0 = 悬浮")]
    public float gravity = 1.2f;

    [Header("渲染")]
    [Tooltip("渲染层级。要比砖块大,碎屑才会盖在砖块上面")]
    public int sortingOrder = 5;

    private ParticleSystem _ps;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();

        // ============================================================
        // ⚠️ 为什么必须写 `var main = _ps.main;` 这一步?
        //
        //   `ParticleSystem.main` 是【只读属性】,返回一个 MainModule 结构体。
        //   C# 不允许直接改"属性返回的结构体"的成员:
        //
        //       _ps.main.playOnAwake = false;     // ❌ CS1612:不是变量,不能改
        //
        //   所以必须先把结构体取到局部变量再改 —— 这一步是【语法要求】。
        //
        // ⚠️ 但【不要写回】。这一点和 Vector3 / Vector2 完全不同:
        //
        //   粒子模块的结构体内部【持有 ParticleSystem 的引用】,
        //   它的 setter 是直接写回粒子系统本身的。所以:
        //
        //       _ps.main = main;                  // ❌ CS0200:只读属性,不能赋值
        //                                         //    —— 你刚遇到的正是这个错
        //
        //   对比三种 struct:
        //     Vector3   —— 纯值类型,改副本【不生效】  (transform.position.x = ...)
        //     Vector2   —— 纯值类型,改副本【不生效】  (rb.velocity.Set(...))
        //     粒子模块  —— 持宿主的 struct,改副本【生效】,而且无法写回
        //
        //   判断依据不是"是不是 struct",而是:**这个 struct 的 setter 干了什么。**
        // ============================================================

        var main = _ps.main;
        main.playOnAwake = false;
        main.loop = false;                                          // 不循环:只在碎裂时手动发射
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
        main.startLifetime = lifetime;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = Color.white;                              // 真正的颜色每次发射时再定
        main.gravityModifier = gravity;

        // ⚠️ 必须是 World。因为我们的做法是"把系统移到砖块位置再发射"。
        //    Local 模式下,已经飞出去的碎屑会跟着系统一起被搬走 ——
        //    结果就是所有碎屑都从屏幕某个固定点喷出来。
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = _ps.emission;
        emission.enabled = false;                                   // 关掉自动发射,只在我们叫它的时候发

        var shape = _ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;            // 球体 = 向四面八方均匀喷
        shape.radius = 0.06f;                                       // 从一个小点里炸出来
        shape.radiusThickness = 1f;                                 // 只从球面发射

        // 渲染层级:让碎屑盖在砖块上面
        var psRenderer = GetComponent<ParticleSystemRenderer>();
        psRenderer.sortingOrder = sortingOrder;
    }

    void OnEnable()
    {
        GameEvents.BrickDestroyed += HandleBrickDestroyed;
        GameEvents.BrickDamaged += HandleBrickDamaged;
    }

    void OnDisable()
    {
        GameEvents.BrickDestroyed -= HandleBrickDestroyed;
        GameEvents.BrickDamaged -= HandleBrickDamaged;
    }

    private void HandleBrickDestroyed(BrickDestroyedEvent data)
    {
        // 让碎屑和砖块同色。同样:取到局部变量改,但【不写回】。
        var main = _ps.main;
        main.startColor = data.color;
        // 把粒子系统搬到砖块位置,然后发射。
        // ⚠️ 因为 simulationSpace = World,搬动系统【不会】影响已经飞出去的碎屑。
        transform.position = data.position;
        _ps.Emit(burstCount);
    }
    private void HandleBrickDamaged(BrickDamagedEvent data)
    {
        var main = _ps.main;
        main.startColor = data.color;
        transform.position = data.position;
        _ps.Emit(DamagedBurstCount);
    }
}
