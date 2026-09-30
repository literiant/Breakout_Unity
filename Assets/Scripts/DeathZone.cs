using UnityEngine;

/// <summary>
/// 死亡区 —— 挂载到 "Walls/DeathZone"(屏幕下方的 BoxCollider2D 触发器)上。
/// 职责:球掉出场地时给一次震动反馈,并通知 GameController 处理后续规则。
/// </summary>
public class DeathZone : MonoBehaviour
{
    [Header("身份识别")]
    [Tooltip("小球的 Tag。只有这个 Tag 进入死亡区才算数")]
    public string ballTag = "Ball";

    [Header("反馈")]
    [Tooltip("球掉出去时的摄像机震动幅度")]
    public float shakeIntensity = 0.3f;

    [Tooltip("球掉出去时的摄像机震动时长(秒)")]
    public float shakeDuration = 0.35f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;

        // 只负责"报告事实"和给反馈,规则由 GameController 决定
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeIntensity, shakeDuration);

        if (GameController.Instance != null)
            GameController.Instance.OnBallLost();
    }
}
