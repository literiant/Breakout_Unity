using UnityEngine;

/// <summary>
/// 程序化贴图生成器 —— 用代码"画"出 Sprite,不依赖任何素材文件。
///
/// 和 SfxLibrary 是完全平行的两件事:
///   SfxLibrary    : 采样点[] → AudioClip
///   SpriteFactory : 像素[]   → Texture2D → Sprite
///
/// 两者都是【用代码生成资源】。好处是零素材依赖,而且可以【随机化】——
/// 每一块砖的裂纹、每一个箭头的形状都可以不一样,这是手画素材做不到的。
/// </summary>
public static class SpriteFactory
{
    /// <summary>
    /// 生成一个朝上的三角形箭头。
    ///
    /// 纹理坐标原点在【左下角】(0,0)。所以 y 越大越靠近箭头尖端 ——
    /// 越往上,三角形的半宽越小。
    /// </summary>
    /// <param name="pixelsPerUnit">Sprite 的"每单位多少像素"。设成 height 时,箭头世界高度正好是 1 个单位</param>
    public static Sprite CreateArrowSprite(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0f, 0f, 0f, 0f);
        var pixels = new Color[width * height];

        float centerX = (width - 1) * 0.5f;

        for (int y = 0; y < height; y++)
        {
            // y = 0(底部)时半宽最大;y = height-1(尖端)时半宽为 0
            float t = (float)y / (height - 1);
            float halfWidth = (1f - t) * (width * 0.5f);

            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Abs(x - centerX);

                // 在三角形内部就上色,外面就透明
                pixels[y * width + x] = (dx <= halfWidth) ? color : clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // 参数依次是:纹理、取哪块矩形、轴心(pivot)、每单位多少像素
        // ⚠️ 轴心选 (0.5, 0) = 底部中心 —— 这样箭头会绕【底部】旋转,
        //    看起来就是"以球为中心向外指"。
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), height);
    }
}
