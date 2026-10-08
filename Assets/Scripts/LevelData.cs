using UnityEngine;

/// <summary>
/// 一行砖块的配置。
///
/// [System.Serializable] 让这个【普通 C# 类】能被 Unity 序列化,
/// 于是它就能作为数组元素出现在 Inspector 里,展开成一组可编辑字段。
/// 不加这个特性,Unity 不会保存它的数据,Inspector 里会是一片空白。
/// ⚠️ 和 [Header] / [Tooltip] 一样:Unity 只序列化【字段】,不序列化属性。
/// </summary>
[System.Serializable]
public class BrickRowConfig
{
    [Tooltip("这一行的图案。'X' = 普通砖,'H' = 硬砖,'.' 或空格 = 留空。" +
             "长度可以和 columns 不同:超出长度的格子一律视为空")]
    public string pattern = "XXXXXXXXX";

    [Tooltip("这一行砖块能挨几下")]
    public int health = 2;

    [Tooltip("打碎一块加多少分")]
    public int score = 1;

    [Tooltip("这一行砖块的颜色")]
    public Color color = Color.white;
}

/// <summary>
/// 关卡数据 —— 一个可以存成资产文件(.asset)的【纯数据】对象,不挂在任何 GameObject 上。
///
/// 和 Prefab 的区别:
///   Prefab            = GameObject 的模板(有 Transform、组件、渲染)
///   ScriptableObject  = 纯数据的模板(只有字段)
///
/// 新建关卡:Project 窗口右键 → Create → Breakout → Level
/// </summary>
[CreateAssetMenu(fileName = "Level_", menuName = "Breakout/Level")]
public class LevelData : ScriptableObject
{
    [Header("网格")]
    [Tooltip("列数。决定砖块的横向排布和间距计算")]
    public int columns = 9;

    [Tooltip("砖块之间的缝隙(世界单位)")]
    public float gap = 0.05f;

    [Tooltip("最上面一行砖块中心的 y 坐标")]
    public float topY = 2.8f;

    [Header("每一行(数组第 0 个 = 最上面那行)")]
    [Tooltip("数组长度就是行数。要加一行就点 + 号")]
    public BrickRowConfig[] rows;

    /// <summary>
    /// Unity 在【新建】这个资产时会调用 Reset。
    /// 不填默认数据的话,新建出来的关卡是空的(rows = 空数组),会一块砖都生成不出来。
    /// </summary>
    void Reset()
    {
        FillWithDefault();
    }

    /// <summary>
    /// 把当前内容替换成一份能直接跑的示例关卡。
    /// 在 Project 窗口选中这个资产 → Inspector 右上角齿轮 → 点它。
    /// (已存在的资产不会被 Reset 自动填充,所以需要这个手动入口)
    /// </summary>
    [ContextMenu("填充默认关卡(会覆盖现有内容)")]
    private void FillWithDefault()
    {
        columns = 9;
        gap = 0.05f;
        topY = 2.8f;

        rows = new BrickRowConfig[4];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new BrickRowConfig
            {
                pattern = "XXXXXXXXX",
                health = 2,
                score = 1,
                // 4 行从红渐变到青,一眼能看出每行是独立的
                color = Color.Lerp(Color.red, Color.cyan, i / 3f),
            };
        }
    }
}
