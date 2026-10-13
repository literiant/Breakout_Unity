using UnityEngine;

/// <summary>
/// 死亡区 —— 挂在 "Walls/DeathZone"(屏幕下方的 BoxCollider2D 触发器)上。
///
/// 职责:球掉出场地时【只广播这个事实】。
/// 扣命、重置球、震动、音效 —— 全都由订阅者自己决定。
/// 它不认识 GameController,也不认识 CameraShake。
/// </summary>
public class DeathZone : MonoBehaviour
{
    [Header("身份识别")]
    [Tooltip("小球的 Tag。只有这个 Tag 进入死亡区才算数")]
    public string ballTag = "Ball";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;

        GameEvents.RaiseBallLost();
    }
}
