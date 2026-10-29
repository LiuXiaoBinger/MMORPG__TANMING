# RO 副本 UI 移植与主题副本选择窗口接入

## 来源与范围

源工程 `UnityDemos/ROCharacterLightingDemo`（与 LYF 同源：Common/ThemeArround 图集、`Common SDF.asset` 的 GUID 完全一致，因此只做「补缺」而非整目录覆盖）。

## 修改内容

### 1. 资源移植（43 个文件）

- 14 个副本 UI 预制体 → `LYF_MMO/Assets/artres/Resources/UI/Prefabs/ROCharacterLightingDemo/`
  （DungeonCopyTips / DungeonCountDown / DungeonExtend / DungeonHintNormal / DungeonRegret /
  DungeonTarget / DungeonTimer / MazeDungeon / MazeDungeonWheel / MazeEnter /
  ThemeDungeonArround / ThemeDungeonResult / ThemeDungeonReward / ThemeDungeonTeam）
- 27 个缺失资源并入既有目录：新增图集 `Atlas/{Loot,NewTalk,PreyDungeon,Team,Theme,UiAnimation}.png`、
  贴图 `Texture/PrefabBg/RO_MiGong_BG{1,2}.png` 与 `Texture/Themearround/` 下 8 张副本大图、
  目录 `Texture/ThemearroundKing/`（9 张首领立绘）、`TMP/Common_{Battlefield,MapName}.mat`
- 副本配置 `ThemeDungeon/theme_dungeon.json`（9 个副本条目）
- LYF 已存在的 14 个资源未重复拷入；所有文件连同 `.meta` 拷贝，GUID 保持不变

### 2. 失效自定义脚本清理

移除 321 个 LYF 无法解析的 RO 自定义 MonoBehaviour（含 `ThemeDungeonArroundDriver`）。
GameObject 数量与层级结构前后不变，仅删除组件块并同步摘除 `m_Component` 条目。

判定依据：在 `LYF_MMO` 全工程（含 `Packages`/`Library/PackageCache`）范围反查 `m_Script.guid`，
未命中 `.cs`/`.dll` 的 `.meta` 才算失效。`fe87c0e1`(Image)、`5f7201a1`(Text) 一度被误判，
已按包内脚本修正保留。

### 3. Text → TextMeshProUGUI

`ThemeDungeonArround.prefab` 的 15 个旧 UGUI Text 全部换成 `TextMeshProUGUI`，
字体统一指向 `Font/Common SDF`（guid `8d1c33eb43cd7e24d9faf1a7087d2dd2`），
字段模板取自已转换的 `Task.prefab`。其余 13 个预制体不含旧 Text（其 Text 挂在旧版 dll 上）。

### 4. UIRoot 集成

- `UIRoot.prefab` / MainView 下新建空对象 `ThemeDungeonArroundWindow`（默认隐藏），
  其下挂 `ThemeDungeonArround` 预制体实例。MainView 子节点 15 → 16。
- `ThemeDungeonArround.prefab` **保留自身 Canvas / CanvasScaler / GraphicRaycaster**，未做剥离。
- `MainView.cs`：新增 `_themeDungeonArroundWindow` 字段、同名属性、`windowDic` 登记
- `EnumDefine.cs`：新增 `WindowType.ThemeDungeonArroundWindow`

### 5. Driver 逻辑迁移

`ThemeDungeonArroundDriver.cs` 的功能迁入 `ThemeDungeonArroundWindow.cs`（继承 `WindowBase`）：
配置加载、8 书位填充、上下翻页、选中高亮、详情区（大图 + 首领立绘 + 文案）、关闭。
按项目「一类型一文件」拆出 `ThemeDungeonEntry.cs`（数据）与 `ThemeDungeonConfig.cs`（JSON 根）。
遵循无命名空间、无 foreach、无三目运算符、中文注释。

## 行为

窗口默认隐藏，由 `windowDic[WindowType.ThemeDungeonArroundWindow]` 统一调度显隐。
运行时按 `_configResourcePath` 读 JSON，按 `_textureResourceRoot` 拼贴图路径，
`_playerLevel` 低于 `chapterLevel` 的书位显示锁定遮罩。挑战/确认按钮目前只输出反馈文本，
未接入进入副本的协议。

## 修复记录（移植后）

| 问题 | 根因 | 处理 |
| --- | --- | --- |
| UIRoot 显示 `(Broken Prefab Asset) / There are no GameObjects in the file` | 新增节点 fileID 随机取到 `[1e18, 1e19-1]`，其中 `9372901361956168199`、`9941163220825974821` 超过 `Int64.MaxValue`(9223372036854775807)，被截断成同一值造成重复标识符 | 改为 `5100000000000000001` / `5100000000000000002` |
| 误剥 ThemeDungeonArround 根 Canvas | 自行套用 DailyTask 惯例推断，未经确认 | 从备份 `.pre_canvas_strip.bak` 完整还原 |

## 验证

已验证（静态核对）：

- `UIRoot.prefab`：5570 个文档，无重复 fileID、无越界、无断引用（`component`/`m_Children`/`m_Father`/`m_GameObject`/`m_TransformParent`/`m_PrefabInstance` 全量校验）
- `ThemeDungeonArround.prefab`：根组件 `224/223/114/114`（Canvas 与 GraphicRaycaster 在位），旧 Text = 0、TMP = 15、失效 Driver = 0
- GUID 链路：UIRoot 引用源预制体 `6660df18b8eafb841a8e6768c309e79e` == `ThemeDungeonArround.prefab.meta`；`ThemeDungeonArroundWindow` 组件 script guid `1e3d5a39724943f39f5d077770e22daa` == 其 `.cs.meta`
- 节点命名：Window 空对象名 `ThemeDungeonArroundWindow`；预制体根名 `ThemeDungeonArround`，与脚本 `FindThemeDungeonRoot()` 的查找名一致
- 配置：`theme_dungeon.json` 顶层 `{"items":[...]}`，9 个条目，字段与 `ThemeDungeonEntry` 一一对应；18 个 `scenePic`/`painting` 路径拼 `UI/Texture/` 后全部命中实际文件
- API 一致性：`InitWindow`/`Show`/`ReFreshUI`/`CloseWindow` 与 `WindowBase` 的 virtual 签名完全匹配；`gameObject.Show()` 扩展方法基类自身即在使用

**未验证（必须实测）**：

1. **C# 未编译过**——Unity 当前停留在 Play 模式（`Editor.log` 最后一次 `ReloadAssembly` 在 23303 行，其后 77000+ 行全是运行时日志），不会触发重编译；现有 `Assembly-CSharp.dll` 是 01:11 的产物，不含本次新增类型。需退出 Play 模式让 Unity 刷新。
2. 13 个非 `ThemeDungeonArround` 预制体仍引用旧版 `UnityEngine.UI.dll`（guid `f70555f144d8491a825f0804e09c671c`），LYF 中不存在。未删除（它不属于「自定义脚本」），是否由 Unity 自动迁移待导入确认。
3. 新脚本 `.cs.meta` 为手工生成（与工程内既有 `.cs.meta` 同格式，仅两行），打开后请确认 GUID 未被重新分配。
4. `Scenes/RODungeon_Moshenta/Role_MJS_AlwaysLit/Editor/` 路径的 `CS2001` 为陈旧编译缓存残留（该目录已不存在，实际文件在 `Models/RoleFaber/Role_MJS_AlwaysLit/`），后续编译未再复现。

## 回退

- tag `backup/pre-dungeon-ui-port-20261029` → `0df9ff6`
- 逐文件备份：`_temp/backup_20261029_dungeonui/`（含 `UIRoot.prefab.bak`、
  `ThemeDungeonArround.prefab.pre_canvas_strip.bak`、`ThemeDungeonArround.prefab.pre_tmp.bak` 及 14 个原始预制体）
- 提交：`443d80d`（移植）、`effc1fe`（修复 UIRoot + 还原 Canvas），均已推送 `codex/feature-development`

## 关联文档

`项目分析/RO项目副本UI预制体清单-2026-10-29.md`（RO 副本 839 个预制体全量盘点与 LYF 对照）

---

## 追加修复（2026-10-29 11:xx）：全屏白色斜梯形

**现象**：主城 Play 后屏幕中间出现一块巨大的白色斜梯形，两侧留黑角，把整个界面盖白。

**根因（两层叠加）**：

1. `ThemeDungeonArround.prefab` 里 `ActivePanel/ScrollView` 的 Image **sprite 引用为空**（`m_Sprite: {fileID: 0}`），Unity 对空 sprite 的 Image 会画一整块纯白不透明矩形；该节点锚点为 `0,0~1,1` + `sizeDelta(0,124.96)`，即铺满全屏。同批还有 8 个 `Collider` 子节点与 `ChallengeDungeonBtn` 也是同样的白板。
2. `ScrollView` 自身带 `m_LocalRotation.x = -0.2419218`（**-28° X 轴倾斜**，RO 原版就有的设计，书位 Item1* 各带 +28° 抵消回正）。RO demo 用 `ScreenSpaceOverlay`（正交投影）所以只是压扁；LYF 主画布是 **Screen Space - Camera（透视投影）**，同一个 -28° 平面就被投成梯形。

RO 原版驱动 `ThemeDungeonArroundDriver.FixLostSpriteGraphics()`（第 228~258 行）专门处理这件事，注释里写得很清楚（"ScrollView 上那块 1821x1263 的白板"），移植时漏掉了。

**修复**：

- `ThemeDungeonArround.prefab`：把 10 个空 sprite 的 Image 的 `m_Color.a` 改为 0（`ScrollView`、`ChallengeDungeonBtn`、8 个 `Collider`），保留 `m_RaycastTarget: 1`，滚动与点击区域不变。逐字节改行，仅 10 行 diff。
- `ThemeDungeonArroundWindow.cs`：新增 `FixLostSpriteGraphics()`，在 `InitWindow()` 里 `CacheNodes()` 之后调用，运行时再把任何 `sprite == null && a > 0` 的 Image 透明化，作为兜底。
- `MainView.cs`：`InitView()` 里补 `_themeDungeonArroundWindow.InitWindow()`（原来只登记进 `windowDic` 从未初始化，所以看到的是未填充的裸预制体）。

**验证（Unity 编辑器内实测）**：

- 透明化前 `whiteOpaque=10`，修复后 `whiteOpaque=0`
- 全窗口 43 个 Graphic 里仅剩 `DownBtn` 的 Z 轴 180°（翻转箭头图标，平面旋转不会透视变形），无斜置可见控件
- 手动调 `InitWindow()`：`slots=8、thumbsWithTex=8、pageText=1 / 2、splashTex=UI_ThemeArround_Img_Fuben_beifangyibian a=1`，无异常
- 编译：仅既有 warning，无 error

## 追加清理（2026-10-29 12:0x）：RO 遗留死字体目录 UI/TMP

**现象**：Project 窗口 `Resources/UI/TMP` 下 `Common SDF` 带红标，旁边多了 `Common_Battlefield / Blueedge / MapName / Whiteedge / Yellowdge` 五个陌生材质。

**查明**：

- `UI/TMP/Common SDF.asset`（guid `938e4270be1e5904ca574c780766e00c`）是 RO 2021 年的旧版 TMP 字体，`m_Script` 指向 `guid 89f0137620f6af44b9ba852b4190e64e`，该脚本在当前 TMP 版本已不存在 → `LoadMainAssetAtPath` 返回 **NULL**（Missing Script，红标根因）。旧序列化格式（`m_fontInfo` / `hashCode`）。
- 5 个 `Common_*.mat` 只被这个死字体引用，全工程其余资源对它们的引用数 **全部为 0**（逐 guid 搜 `.prefab/.unity/.asset/.mat/.controller`）。
- 真正在用的字体是 `Resources/UI/Font/Common SDF.asset`（guid `8d1c33eb43cd7e24d9faf1a7087d2dd2`），实测 `TMP_FontAsset` 加载正常、材质 `Common Atlas Material`、Dynamic 模式；UIRoot 下 261 个 TMP 文本 **fontMissing=0**。
- 另发现 `UI/Font/Common SDF.asset` 工作区被 TMP 回写：图集 1024→2048、`m_GlyphTable` 从空表变成烤入 257 个字形（Dynamic 字体在编辑器渲染中文后的正常持久化）。

**处理**：

- 删除整个 `Assets/artres/Resources/UI/TMP/`（6 文件 + 6 meta + 目录 meta），删除前整目录备份到 `_temp/backup_20261029_tmp_dead/`。
- `UI/Font/Common SDF.asset` 回滚到提交版本（图集 1024、清空字形表）。
- 删除后实测：`tmpDirExists=False`、字体资产 `atlas=1024 mat=Common Atlas Material`、UIRoot 261 个 TMP 文本 `fontMissing=0`、Console 无新增报错（残留两条 `Global.BeginAnimationPreload` 的 YooAsset 资源包缺失与 `CameraMgr.Update` 空引用，均为 Play 期既有日志，与本次无关）。

**注意**：Dynamic 字体下次进 Play 用到中文时，TMP 仍会回写 `UI/Font/Common SDF.asset`；如果不想让它一直变脏，可把该资产的 Atlas Population Mode 改成 Static 并重新烘焙（本次未改，等确认）。

**提交**：`8a3b757`

## 追加修复（2026-10-29 12:2x）：主题副本窗口不在最上层

**现象**：进入主城后，主题副本界面被左侧聊天窗口、任务条目、底部技能栏压在上面。

**根因**：UGUI 的渲染顺序 = 同级节点的兄弟顺序（越靠后越上层）。移植时把 `ThemeDungeonArround` 插到了 `MainView` 的 `m_Children` **首位**，等于整个主界面里最底层。实测同级顺序：

```
0 RoleCurrtInfoWindow  1 ChatWindow_遗弃  2 MinMapWindow  3 FunctionMenu
4 SkillInfoWindow  5 KnapsackWindow_遗弃的  6 TalkWindow  7 NPCShopWindow
8 QuickTaskPanel  9 RoleAttriibuteWindow  10 ChatWindow  11 KnapsackWindow
12 SettingPanel  13 TaskWindow  14 DailyTaskWindow  15 ThemeDungeonArround ← 改后
```

**修复**：

- `UIRoot.prefab`：`MainView` 的 `m_Children` 里把 `{fileID: 3943980905731245043}`（即 ThemeDungeonArround 实例的 stripped RectTransform）从第 1 位移到末位。仅 1 行增 1 行删。
- `ThemeDungeonArroundWindow.cs`：新增 `BringToFront()`，在 `Show(true)` 里 `SetAsLastSibling()`，保证以后任何新加入 MainView 的窗口都不会再压住它。

**验证（Unity 编辑器内实测）**：

- `Show(true)` 后 `GetSiblingIndex() = 15/15`，为 MainView 末位
- 截 UICamera 到 RenderTexture 检查：书位卡片、页码 `1 / 2`、底部详情面板、左右翻页箭头全部完整显示，聊天/任务/技能栏不再覆盖
- 编译无 error，Console 无新增报错

**遗留（未改，待确认）**：窗口自身没有全屏不透明背景（`TD_Splash` 选中书位后才显示副本预览图），所以底层主城与 HUD 仍会透出来；空白区域点击也会穿透到下层。要改成模态需另加全屏透明阻挡层。

**提交**：`443e92e`

## 追加修复（2026-10-29 12:3x~13:0x）：详情面板乱码 + 文字溢出

**现象**：主题副本界面选中最左侧书位后，底部信息面板里的正文大面积显示成空心方框（豆腐块），而且文字排到面板外面。

**根因（两个独立问题）**：

### 1. 乱码 = TMP 动态字形图集被撑爆

- 全项目 UI 文本共需 **849 个不同字符（732 个汉字）**，用脚本扫 502 个 prefab/asset 的 `m_text` 加全部副本配置 JSON 统计得出。
- `Resources/UI/Font/Common SDF.asset` 是 **Dynamic** 模式，图集 `1024x1024`、`padding 9`。按 13~26px 字号算，每个字形连 padding 占 30~44px 见方，1024 图集只排得下约 **900 个字形**，扣掉已有字符后余量极小。
- 副本描述正文一条就有 **302 字**（`theme_dungeon.json` 的 `chapterContent`），一加载就把剩余空间吃光。TMP 在 Dynamic 模式下图集满后加不进新字形，**从某一处开始后续字符全部渲染成 missing glyph（方块）**——所以现象是"前半句正常、后半句全方块"。
- 已核对 `Common.TTF` 的 cmap：7741 个码位、6763 个基本汉字，配置文本 740 个唯一汉字 **缺字 0**，所以不是字体缺字，是图集容量。

### 2. 布局 = 文本框太小 + overflow 设成 Overflow

- `TD_Desc` 是 `420x90`、字号 14、`overflow = Overflow`。实测该文本 `preferredWidth/Height = 1078x230`，换行后需要约 11 行 × 21px，90 高的框装不下，Overflow 又不会裁切，于是直接糊到面板外面。
- 右下角状态文字 `TD_Status` 锚在 `x=470`、宽 900 右对齐，右边缘到 920 已贴屏幕边。

**处理**：

- `Common SDF.asset`：`m_AtlasWidth/Height` 1024 → **2048**（面积 4 倍，可容纳约 3600 个字形）。**不做预烘焙**——预烘焙会把 2048 图集以 typeless raw 内嵌进资产，文件从 8KB 涨到 **25.5MB**，Unity 重导入极慢（实测卡住 10 分钟以上），所以保持 Dynamic 运行时生成，资产仍是 7954 字节。
- `ThemeDungeonArroundWindow.BuildDetailPanel()`：面板 `660x250 → 820x280`、位置 `(-560,-380) → (-500,-380)`；BOSS 立绘 `150x200 → 160x210`、`x -245 → -320`；文字区宽度 `420 → 620`，标题/章节/首领纵坐标上移；描述框 `420x90/14 号 → 620x150/13 号`，`lineSpacing -8`，`overflow` 改 **Truncate**（再长也只裁在框内）；状态文字 `x 470 → 280`、宽 `900 → 640`。
- 新增 `ClearRuntimeNodes()`：`InitWindow()` 里先删掉上一轮生成的 `TD_*` 节点。预制体本身没有 `TD_` 前缀节点（已 grep 确认），所以清理只影响运行时创建物，重复初始化不再叠加。

**验证**：

- 修复图集后编辑器内实测：`TD_Desc` 的 295 个字符 **missingGlyph = 0**，字体 `characterTable = 892`，截图确认正文全部正常显示、无方块。
- 布局部分因 MCP 通道中断**未做运行时复测**（见下）。

**提交**：`1f0c988`（字体）、`2a98036`（布局）

## 事故记录：Unity 编辑器 MCP WebSocket 断连

- 预烘焙字体（25MB 资产）触发 Unity 大规模重导入期间，`MCP-FOR-UNITY` 的 WebSocket 被对端关闭，之后编辑器进入 **"Connection closed / Keep-alive failed" 无限重连刷日志**，CPU 长时间 4 核满载（日志 10.9MB，从 107420 行起全是重连堆栈）。
- 期间脚本编译与域重载**均已成功**（`Loaded All Assemblies 2.43s`、`Mono: successfully reloaded assembly`、无 `error CS`），只是 MCP 通道回不来。
- 恢复方式：让 Unity 编辑器窗口获得焦点，或重启编辑器。

---

## 追加修复（2026-10-29 14:4x~14:5x）：Console 一堆 MissingReferenceException

**现象**：Unity Console 被 `MissingReferenceException: The variable m_AtlasTextures of TMP_FontAsset doesn't exist anymore.` 刷屏；编辑器 4 个 CPU 核心长时间满载（`Unity.exe` CPU 累计 41800 秒），主线程卡顿；日志涨到 12MB。

**根因（两层，都是上一轮改字体时留下的）**：

1. **磁盘资产不自洽**：上一轮只把 `m_AtlasWidth/Height` 从 1024 改成 2048，但 `m_FreeGlyphRects` 还是 1024 图集时的 `1023×1023`，与实际图集尺寸对不上。
2. **编辑器内存态被污染**：在 Play 模式运行期间反复用 `git checkout` / `cp` 覆盖 `Common SDF.asset`，每次 Unity 重导入都会重建内嵌的图集 Texture2D，而内存里的 `TMP_FontAsset.atlasTextures` 数组只 append 新纹理、不清理旧槽。实测导入后数组变成 **4 个槽位：[0]=1×1、[1]=已销毁、[2]=2048×2048、[3]=已销毁**。TMP 每帧在 `TMP_MaterialManager.GetFallbackMaterial` 里访问已销毁的纹理 → 抛异常。因为 `MainView` 下有 `ScrollView`（`ScrollRect.LateUpdate` → `ForceUpdateCanvases()`），每帧强制重建全部 290 个 TMP 文本 → 一帧几十条异常。

**修复**：

- `Common SDF.asset`：`m_FreeGlyphRects` 的 `1023×1023` → `2047×2047`（`m_AtlasWidth/Height` 已为 2048），资产恢复自洽：2048 图集 + 空字形表 + 1×1 占位纹理 + Dynamic 模式。
- 编辑器内 `Resources.UnloadAsset(fontAsset)` + `AssetDatabase.ImportAsset(path, ForceUpdate|ForceSynchronousImport)` 重建实例，把内存里 4 个槽位的脏数组重置为 `[0]=1×1` 单槽。
- 清空 Console。

**验证（Play 模式实测）**：

- Console **error 条目 0 条**（清空后进 Play 20 秒内无任何新增 error）
- 字体加载：`atlas=2048x2048`、`atlasTextures.Length=1`、`glyphs=321`、Dynamic
- `UIRoot` 下 290 个 TMP 文本 `fontMissing=0`
- 主题副本窗口 `TD_Desc`：rect `620×150`、字号 13、`overflow=Truncate`，275 字符 **missingGlyph=0**，无方框
- 截图确认：无白色斜梯形、书位缩略图正常、底部信息面板正文清晰完整、页码 `1 / 2`、窗口 `sibling=15/15` 在最上层

**教训**：

- **绝不能在 Play 模式运行期间从磁盘覆盖 TMP 字体资产**。Unity 会重导入并销毁内嵌图集纹理，而 TMP 的 `atlasTextures` 数组不会收缩，残留的已销毁引用会每帧抛 `MissingReferenceException`，只要场景里有 `ScrollRect` 就会放大成刷屏。
- 手改 TMP 字体 YAML 时，`m_AtlasWidth`/`m_AtlasHeight`、`m_FreeGlyphRects`、内嵌图集 `Texture2D` 的 `m_Width/m_Height` 三者必须同步，否则资产不自洽。

**提交**：`dff4a86`

**附带发现（未改，待确认）**：`ActivePanel/ChallengeDungeonBtn`（原版"挑战玩法"入口）是 `207×200`、锚点居中、`anchoredPosition=(3,73)` 的透明按钮，`raycastTarget=1`，正好压在书位列表正中间，会挡住中间两列书位的点击；它的文字"挑战玩法"叠在卡片之间，视觉上也突兀。

---

## 追加修复（2026-10-29 15:0x）：副本描述仍大面积方块

**现象**：上一条把图集从 1024 放大到 2048 后，`达纳托斯之战`（第 7 个书位）的描述里仍然有一大片空心方框，且是「同一行里正常字和方块交替」——前半句正常、越往后方块越多。

**根因：图集容量算错了，2048 根本不够**

之前判断"1024 只能放约 900 字形"是错的，实际数字要从字体参数算：

| 参数 | 值 | 含义 |
|---|---|---|
| `m_FaceInfo.pointSize` | **90** | SDF 采样点阵高 90 像素 |
| `m_AtlasPadding` | **9** | 每侧留 9 像素 |
| 单个汉字实际占用 | **90 + 2×9 = 108 像素见方** | |
| 2048 图集可排 | `2048 / 108 ≈ 18` 列 × 18 行 ≈ **324~400 个字形** | 只有窄字符（标点、字母）能塞得更多 |

实测证据：改多图集前，`TMP_FontAsset.glyphTable.Count = 372` 就已经把 2048 图集顶满。

而界面上要显示的全部字符量：8 条副本描述合计 **2589 字、830 个不同字符**。图集满之后 TMP 不再添加新字形，新字直接渲染成 missing glyph 方块；已经烤进图集的字仍正常，所以出现"正常字和方块交替"的观感。

**修复**：`Common SDF.asset` 的 `m_IsMultiAtlasTexturesEnabled` `0` → `1`（开启多图集）。TMP 在图集排满时自动新建下一张，不再受单张容量限制。资产文件仍是 **7954 字节**，因为 Dynamic 模式的图集纹理是运行时生成的，不落盘（`m_ClearDynamicDataOnBuild: 1` 保证打包时也会清掉）。

**验证（Play 模式实测）**：

- 一次性渲染全部 8 条描述：`2589 字符`、其中 `2162 个汉字`、**`missingGlyph = 0`**
- 图集自动扩到 **3 张 2048×2048**（`atlasTextures.Length = 4`，第 4 槽为待用空槽），字体累计烘焙 `830` 个字符
- 逐个点击 8 个书位，详情面板实测缺字数：

  ```
  Item1         han=230 miss=0    Item1 (4)  han=252 miss=0
  Item1 (1)     han=266 miss=0    Item1 (5)  han=155 miss=0
  Item1 (2)     han=260 miss=0    Item1 (6)  han=261 miss=0  ← 用户截图的「达纳托斯之战」
  Item1 (3)     han=264 miss=0    Item1 (7)  han=256 miss=0
  ```

- 截图确认：`达纳托斯之战` 详情面板标题、章节、首领、正文全部正常显示，无方块
- 退出 Play 后资产未被回写污染（仍 7954 字节、git diff 只有 1 行）

**提交**：`a48dd57`

**教训**：TMP Dynamic 图集的容量不是按"字符数"算的，要按 `pointSize + 2×padding` 算单个字形的实际像素占位。写 `900 个字形`这种估计值会误导决策，必须以 `glyphTable.Count` 实测为准。

---

## 16. ChallengeDungeonBtn 改为「进入副本」空壳按钮，ChallengeName 跟随选中刷新

### 需求

用户明确要求：`ChallengeDungeonBtn` 按「进入副本按钮」实现（空壳即可），并把按钮上的 `ChallengeName` 文本改成随当前选中的副本实时刷新。

> 说明：RO 原版里这个按钮的实际语义是「挑战玩法」入口，点击打开 `Theme_Challenge` 界面（Normal/Hard 两档难度、日周限次、周奖励进度），与"选书→开书→进入剧情副本"主线无关。本次按用户要求改为"进入选中副本"，原版挑战模式的接入点保留注释位。

### 现场数据（Play 模式实测）

书位是 8 个 155×155 的方块环绕中心排布，按钮在正中央：

```
Item1        local=(-415, 126)      Item1 (4)  local=(427, 127)
Item1 (1)    local=(-219, -11)      Item1 (5)  local=(228, 203)
Item1 (2)    local=(-3,   -68)      Item1 (6)  local=(2,   227)
Item1 (3)    local=(230,  -33)      Item1 (7)  local=(-219, 203)
ChallengeDungeonBtn  local=(3, 73)  size=207x200
```

- 按钮纵向范围 y ∈ [-27, 173]，与正下方书位 `Item1 (2)`（y ∈ [-146, 10]）重叠约 37px。
- `ActivePanel` 子节点顺序里 `ChallengeDungeonBtn` 排在 `ScrollView` **之前**（即渲染在下层），重叠区由书位优先响应，两侧点击互不阻断，因此**不调整层级、不挪位置**，保持 RO 原布局。

### 代码改动（`GM_Game/Scripts/UI/Main/Window/ThemeDungeonArroundWindow.cs`）

1. 新增字段 `_challengeName`（TMP 文本）、`_challengeGroup`（CanvasGroup）
2. `CacheNodes()` 缓存 `ChallengeName`
3. 新增 `SetupChallengeButton()`：把 `ChallengeName` 从 RO 原版的 `overflowMode=Truncate`（长名字被直接切掉）改为 `Overflow` + 自动缩放 12~28 号；给按钮挂 `CanvasGroup` 供置灰。幂等，重复 `InitWindow` 安全。
4. 新增 `RefreshChallengeButton()`：有选中 → 写入副本名、文字纯白、整体 alpha 1；无选中 → 显示"请选择副本"、文字转暗灰、alpha 0.4
5. `Select()` 末尾调用刷新；`ShowPage()` 空数据分支也调用
6. `ShowPage()` 翻页时增加"选中项不在当前页则自动改选本页第一个"，否则按钮会残留上一页的副本名
7. `OnChallengeClicked()` 重写为空壳实现：
   - 未选中 → "请先选择一个副本"
   - 等级不足 → "等级不足，{副本名} 需要 Lv.{需求等级}"
   - 通过 → "进入副本：{副本名}（协议待接入）"
   - 真正的进入副本协议等服务器接口就绪后在此发起

### 编译验证

`Library/ScriptAssemblies/GM_Game.dll` 于 15:54:17 重新产出，`Editor.log` 全文 `error CS` 计数为 **0**。

### 未验证

Play 模式下的实际点击效果与文字刷新**未测量**：本轮 `refresh_unity` 强制同步重编译后 Unity 编辑器主线程进入 4 核满载、MCP ping 无响应（`server-launch-8082.log` 每 45 秒"Plugin registered → Ping 失败 → disconnected"循环），需重启编辑器或重新聚焦后补测。

### 教训

Play 模式下不要用 `refresh_unity(force + compile request)` 强制同步重编译，会触发「Reloading assemblies for play mode」与「after forced synchronous recompile」叠加，编辑器卡死、MCP 桥接随之断掉。改脚本后等编辑器自己检测重编译即可。
