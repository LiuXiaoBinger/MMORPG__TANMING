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
