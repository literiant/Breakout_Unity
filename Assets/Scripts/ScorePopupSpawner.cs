using UnityEngine;

/// <summary>
/// 飘分文字生成器 —— ⚠️ 必须挂在 Canvas 的子物体上。
///
/// 这个脚本是【事件系统价值】的证明:
///   它是凭空出现的, BrickController 一行代码都没为它改过。
///   砖块根本不知道飘分文字的存在 —— 它只管喊"我碎了"。
/// </summary>
public class ScorePopupSpawner : MonoBehaviour
{
    [Header("预制体")]
    [Tooltip("带 ScorePopup 脚本的 TMP 文本预制体")]
    public ScorePopup popupPrefab;

    void OnEnable()
    {
        // 订阅:从现在起,每次砖块碎裂都会调用 HandleBrickDestroyed
        GameEvents.BrickDestroyed += HandleBrickDestroyed;

    }

    void OnDisable()
    {
        // ⚠️ 退订,而且必须和订阅成对出现。
        //    静态事件跨场景存活 —— 场景重载时旧的 spawner 已经销毁,
        //    但订阅列表里还留着它。不退订的话,下次事件触发会去调用一个
        //    "已销毁对象"的方法,然后 Accessing transform 就炸了。
        //
        //    用 OnEnable / OnDisable 而不是 Awake / OnDestroy:
        //    物体被临时禁用再启用时,订阅关系也能自动正确地建立和解除。
        GameEvents.BrickDestroyed -= HandleBrickDestroyed;
    }

    private void HandleBrickDestroyed(BrickDestroyedEvent data)
    {
        Debug.Log($"收到事件,popupPrefab = {popupPrefab}");
        if (popupPrefab == null)
        {
            Debug.LogError("ScorePopupSpawner:还没有指定 popupPrefab", this);
            return;
        }

        // ⚠️ 世界坐标 → 屏幕坐标。这是 UI 和游戏世界之间唯一的桥。
        //    Overlay Canvas 上的 UI 用【屏幕像素】定位,而砖块在【世界坐标】里。
        //    直接把世界坐标赋给 UI,飘分会出现在屏幕外的莫名其妙的地方。
        Vector3 screenPos = Camera.main.WorldToScreenPoint(data.position);

        ScorePopup popup = Instantiate(popupPrefab, transform);
        popup.Setup($"+{data.score}", data.color, screenPos);
    }
}
