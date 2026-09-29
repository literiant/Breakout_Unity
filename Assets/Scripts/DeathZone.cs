using System;
using System.Security.Cryptography;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Ball")) return;
        Debug.Log("球掉出去了");
        collision.GetComponent<BallController>().ResetBall();
    }
}
