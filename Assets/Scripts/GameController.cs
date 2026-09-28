using UnityEngine;

/// <summary>
/// 全局游戏管理器 —— 挂载到场景中的空物体 "GameController" 上。
/// 职责:持有跨物体的全局状态(分数、生命),并提供唯一访问入口。
/// </summary>
public class GameController : MonoBehaviour
{
    /// <summary>
    /// 单例入口。任何脚本都可以用 GameController.Instance 拿到它。
    /// { get; private set; } = 外部只能读,只有本类能改。
    /// </summary>
    public static GameController Instance { get; private set; }

    [Header("分数")]
    [Tooltip("当前分数。属性而非字段 —— 外部只能通过 ModifyScore 修改")]
    [SerializeField]
    private int _score;
    public int Score { get => _score; private set => _score = value; }

    void Awake()
    {
        // 单例守卫:如果已经有实例了,就把自己销毁掉。
        // 为什么可能出现两个?比如切场景时旧的不销毁、或者手滑在场景里放了两份。
        // 注意这里用 != null 而不是 is null —— 因为这是 UnityEngine.Object。
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // 自杀,让先来的那个继续当老大
            return;                // 关键:return 掉,别往下执行
        }

        Instance = this;
    }

    /// <summary>加分。分数只能通过这里增长,不允许外部直接赋值。</summary>
    public void ModifyScore(int amount)
    {
        Score += amount;
        Debug.Log($"当前分数:{Score}");
    }
}
