using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    public GameObject bp;
    public int raw = 4;
    public int column = 7;
    public float topY = 1.8f;
    // Start is called before the first frame update
    void Start()
    {
        float stepX = bp.transform.localScale.x + 0.05f;
        float stepY = bp.transform.localScale.y + 0.05f;
        float startX = -(column - 1) * stepX / 2;

        for (int i = 0; i < raw; i++)
        {
            for (int j = 0; j < column; j++)
            {
                Vector2 position = new Vector2(startX + j * stepX, topY + i * stepY);
                Instantiate(bp, position, Quaternion.identity, transform);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
