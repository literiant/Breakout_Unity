# Breakout · Unity 2D

![游戏演示](Docs/demo.gif)

> 一个从零手写的 Unity 2D 打砖块,3 关。
> **游戏内没有任何美术 / 音频素材文件** —— 图形是引擎内置方块 + 代码逐像素画出的瞄准箭头,
> 音效全部由代码实时合成。

**技术栈**:Unity 2022.3 · C# · 2D 物理 · 零第三方插件

---

## 怎么玩

| 操作 | 按键 |
|---|---|
| 左右移动挡板 | `←` / `→` |
| 调整发射角度 | `↑` / `↓`(不按则随机发射) |
| 发射小球 | `空格` |
| 暂停 | `P` |
| 重新开始 | `R` |

**用挡板边缘击球,反弹角更刁钻** —— 打砖块的核心技巧就在这里。

---

## 技术亮点

### 1. 事件驱动:砖块不认识游戏管理器

`BrickController` 被打碎时只做一件事:**广播"我碎了"**。
计分、飘分文字、碎屑粒子、摄像机震动、音效 —— 五个互不相干的系统各自订阅。

**可验证的结果**:后来新增"飘分文字"功能时,`BrickController` **一行都没有改**。

`GameEvents.cs` · `BrickController.cs` · `CameraShake.cs`

### 2. 有对象池,但知道什么时候不该用

同一个"需要很多个"的需求,三种不同的答案:

| 对象 | 方案 | 理由 |
|---|---|---|
| 飘分文字 | **对象池**(`Stack<T>` + 开局预热) | 生命周期短、创建频繁、外形固定 |
| 碎屑粒子 | **一个 `ParticleSystem`** | 能在任意位置发射,比池子更干净:零分配、零管理 |
| 砖块 | **不池化** | 数量可控、一次性生成,加池子只会让代码更复杂 |


`ScorePopupSpawner.cs` · `BrickBurstEffect.cs`

### 3. 零素材:音效和图形都是代码生成的

| | 做法 |
|---|---|
| **音效** | 代码合成 `AudioClip` —— 正弦 / 方波 / 白噪声 + 指数包络 + **一阶低通滤波** |
| **瞄准箭头** | 代码逐像素绘制 `Texture2D`,轴心设在底部以便绕球旋转 |
| **关卡** | `ScriptableObject` + 字符串图案掩码,运行时可生成 / 清空砖墙 |

**为什么噪声要滤波**:纯白噪声听起来是廉价的"嘶——"。真实的撞击声被材质吸收过,
高频先被吃掉 —— 一个一阶低通就能把"电子噪声"变成"木头碎裂"。

`SfxLibrary.cs` · `SpriteFactory.cs` · `LevelData.cs`

### 4. 代码里没有静默失败

配置缺失时**一定会在 Console 大声报错,并停用自己**(`enabled = false`)。
理由是踩过三次同样的坑:关卡数据是空数组 → 静静地生成 0 块砖,游戏开始了,什么都没有。

> 守卫要分清挡住的是「别人的事」(业务条件,可以静默)还是「我的事」(配置故障,必须吼),
> 而且**吼完要止损**,否则会变成每帧刷屏。

---

## 运行

**[▶ 在 itch.io 下载](https://ITCH_IO_URL_HERE)** ← *发布后替换成真实地址*

或:解压 `Builds/` 下的压缩包,双击 `Breakout.exe`(Windows x64)。
首次运行 Windows 可能提示 SmartScreen,选择"仍要运行"即可。

> **关于 70MB 的体积**:Unity 引擎有固定的体重下限 —— `UnityPlayer.dll` 30MB +
> .NET 类库 27MB + Mono 运行时 9MB,和游戏内容无关。
> 本项目自身的美术 / 场景 / 数据加起来只占约 3.5MB。

---

## 项目结构

```
Assets/
├── Scripts/     17 个 C# 文件,约 2000 行
├── Levels/      3 个关卡(ScriptableObject)
├── Prefabs/     Brick.prefab / ScorePopup.prefab
├── Physics/     Bouncy.physicsMaterial2D(弹性 = 1,摩擦 = 0)
└── Scenes/      SampleScene.unity
```

| 脚本 | 职责 |
|---|---|
| `GameController.cs` | 分数 / 生命 / 关卡流程 / 暂停 / 胜负判定 |
| `GameEvents.cs` | 静态事件中心(委托 + `event` + 载荷结构体) |
| `BrickController.cs` | 耐久 / 受伤变暗 / 碎裂动画(协程) |
| `BrickSpawner.cs` | 按 `LevelData` 生成 / 清空砖墙 |
| `BallController.cs` | 小球物理 + 出射角校正(防"太平"轨迹)+ 瞄准 |
| `PaddleController.cs` | 挡板移动 + 按击球点决定反弹角 |
| `ScorePopupSpawner.cs` / `ScorePopup.cs` | 飘分文字对象池 |
| `BrickBurstEffect.cs` | 碎屑粒子(单系统管所有碎屑) |
| `CameraShake.cs` | 事件驱动的摄像机震动(`LateUpdate` + 防漂移) |
| `SfxLibrary.cs` / `AudioManager.cs` | 程序化音效 + 音源池(忙则丢弃) |
| `SpriteFactory.cs` | 程序化贴图 |
| `LevelData.cs` | 关卡数据 |
| `AimIndicator.cs` | 瞄准箭头显示 |
| `DebugCheats.cs` | 编辑器专用调试快捷键(`#if UNITY_EDITOR` 包裹) |

---

## 已知限制

- 没有存档(进度不会保留)
- 没有设置菜单(音量 / 难度不可调)
- 只有 3 关
- 球偶尔会卡在挡板和墙壁之间(2D 物理的经典边界情况)

---

## 开发方式

这个项目同时是一次 **「如何验证 AI 生成的代码」** 的实践。

代码注释里大量出现 `⚠️` 标记,记录的不是"这行干什么",而是
**「这行为什么必须这么写、不这么写会怎样」** —— 每一条都来自一次实际跑出来的结果,
而不是"看起来应该这样"。

把踩过的坑归成四类:没跑过的断言 / 跨边界搬运规则 / 流程没走到底 / 缺省即沉默。
其中一类只在配置错误时才会暴露,所以项目里所有的 `null` 守卫都遵循同一个约定:
**业务条件可以静默,配置故障必须吼**。
