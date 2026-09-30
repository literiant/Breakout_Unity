using UnityEngine;

public class BrickSpawner : MonoBehaviour
{
    [Header("生成源")]
    [Tooltip("要复制的砖块预制体")]
    public BrickController brickPrefab;
    [Header("网格布局")]
    [Tooltip("行数(从 topY 往下长)")]
    public int rows = 4;
    public int columns = 9;
    public float topY = 3f;
    public float gap = 0.05f;
    public int maxBrickScore = 3;
    // Start is called before the first frame update
    void Start()
    {
        float stepX = brickPrefab.transform.localScale.x + gap;
        float stepY = brickPrefab.transform.localScale.y + gap;
        float startX = -(columns - 1) * stepX / 2;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                Vector2 position = new Vector2(startX + j * stepX, topY - i * stepY);
                BrickController brick = Instantiate(brickPrefab, position, Quaternion.identity, transform);
                brick.gameObject.name = $"brick[{i}][{j}]";
                brick.brickScore = Mathf.Max(maxBrickScore - i, 1);
            }
        }
        if (GameController.Instance != null)
            GameController.Instance.RegisterBricks(rows * columns);
    }
}
