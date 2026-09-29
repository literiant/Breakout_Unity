using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [Header("身份识别")]
    [Tooltip("小球的 Tag。只有这个 Tag 进入死亡区才算数")]
    public string ballTag = "Ball";
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(ballTag)) return;
        Debug.Log("球掉出去了");
        if (GameController.Instance != null)
            GameController.Instance.OnBallLost();
    }
}
