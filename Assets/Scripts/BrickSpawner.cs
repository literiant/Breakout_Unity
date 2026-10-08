using UnityEngine;

/// <summary>
/// 砖块生成器 —— 挂在场景里的空物体 "BrickSpawner" 上。
///
/// 职责:按关卡数据生成砖墙;提供清空接口。
/// ⚠️ 它【不主动】在 Start 里生成 —— 什么时候生成第几关,由 GameController 决定。
///    这样就不依赖 Unity 的脚本执行顺序了。
/// </summary>
public class BrickSpawner : MonoBehaviour
{
    [Header("砖块预制体")]
    [Tooltip("用组件类型而不是 GameObject —— Instantiate 的泛型推断会直接返回 BrickController")]
    public BrickController brickPrefab;

    /// <summary>
    /// 按关卡数据生成一整面砖墙,返回【实际生成了多少块】(有留空的格子,所以不一定等于 rows × columns)。
    /// 调用前请先 ClearBricks()。
    /// </summary>
    public int SpawnLevel(LevelData level)
    {
        if (level == null || level.rows == null) return 0;

        // 砖块尺寸从【预制体】读 —— 单一真相源,不在关卡数据里再写一份
        float brickWidth = brickPrefab.transform.localScale.x;
        float brickHeight = brickPrefab.transform.localScale.y;

        float stepX = brickWidth + level.gap;
        float stepY = brickHeight + level.gap;
        float startX = -(level.columns - 1) * stepX / 2f;   // 让整排关于 x = 0 对称

        int count = 0;

        for (int row = 0; row < level.rows.Length; row++)
        {
            BrickRowConfig config = level.rows[row];

            for (int col = 0; col < level.columns; col++)
            {
                // 图案掩码:超出图案长度的格子一律留空
                if (col >= config.pattern.Length) continue;

                char cell = config.pattern[col];
                if (cell == '.' || cell == ' ') continue;   // 留空

                float x = startX + col * stepX;
                float y = level.topY - row * stepY;         // 从上往下排

                BrickController brick = Instantiate(
                    brickPrefab, new Vector2(x, y), Quaternion.identity, transform);

                int health = config.health;
                Color color = config.color;
                // TODO(填空练习):如果 cell 是 'H',把 health 改成 config.health * 3(硬砖)。
                //   跟着想一想:硬砖的颜色是不是也该不一样?
                if (cell == 'H') health *= 3;
                // ⚠️ 必须调 Setup(),不能直接改 brick.brickHealth。
                //    Instantiate 已经【同步】跑过 Awake,而 Awake 里把 brickHealth 拷成了
                //    _currentHealth、把颜色拷成了 _fullColor。改源字段不会刷新那两份副本。
                brick.Setup(health, config.score, color);

                brick.gameObject.name = $"brick[{row}][{col}]";
                count++;
            }
        }

        // ⚠️ 大声失败。
        //    rows 是空数组、或者 pattern 里一个 'X' 都没有 —— 都会走到这里。
        //    这两种都是【配置错误】,不是正常状态。如果不报错,游戏会静静地开始、
        //    一块砖都没有、玩家完全不知道发生了什么。
        //    第二个参数传 level 会把报错绑定到那个资产上,Console 里点一下就能高亮它。
        if (count == 0)
            Debug.LogError(
                $"关卡 '{level.name}' 生成了 0 块砖。检查:rows 数组是不是空的?pattern 里有没有 'X'?",
                level);

        return count;
    }

    /// <summary>销毁当前所有砖块。进下一关之前调用。</summary>
    public void ClearBricks()
    {
        // ⚠️ 倒序遍历。
        //    正序遍历时,一旦循环途中有子物体真的被移除,后面所有元素的索引都会往前挪,
        //    于是你会漏掉一批物体。倒序删则不会影响"还没访问到的那些索引"。
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // ⚠️ Destroy 是【延迟到帧末】执行的,所以这一帧里新旧砖块会短暂共存。
        //    视觉上无影响(位置重叠),下一帧就干净了 —— 但别指望 ClearBricks 之后
        //    立刻用 childCount 拿到 0。
    }
}
