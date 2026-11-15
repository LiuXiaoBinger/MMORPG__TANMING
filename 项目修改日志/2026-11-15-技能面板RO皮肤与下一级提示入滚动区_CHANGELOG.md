# 技能面板 RO 皮肤统一 +「下一级」提示收进描述滚动区

- 日期：2026-11-15
- 分支：`codex/feature-development`
- 需求来源：用户指出 `Txt_NextLevel` 控件应放入项目的滚动条中，并要求把技能面板设计成 RO 项目 UI 风格。
- 方向确认（本轮交互选择）：**保留现有全屏武学布局、所有节点坐标与业务结构，只换 RO 皮肤**；改动范围 **整个技能面板**。

## 一、问题定位

1. 第九轮（`2026-11-14-技能面板一梦江湖布局重构_CHANGELOG.md` 第 15 节）按一梦江湖实机把描述做成 `DetailPanel/DescScroll`（`ScrollRect + Viewport(RectMask2D) + Content(VerticalLayoutGroup + ContentSizeFitter)`），Content 里收了 `Txt_Sec1 / Txt_Desc / Divider_2 / Txt_Sec2 / Txt_Cond` 五个对象，右侧 `DescScrollbar`。
2. 但参考实机需求第 3 条明确写「长文本（白色叙述 / 橙色数值 / 青蓝词条 / **绿色下一级**）需要滚动查看」，实现却把绿色「下一级(角色N级可提升)」做成 `NormalUpgradeRoot` 下的固定独立控件 `Txt_NextLevel`，不随描述滚动 —— 这是本轮要纠正的错位。
3. 面板大部分视觉元素已使用 RO 图集 `Assets/artres/Resources/UI/Atlas/Common1.png`（guid `c46212db0fcbdd547a2a319e0ab42aba`）的九宫格：外框/详情卡片 `UI_Common_Bg_Normal_04`、页签与竖排导航 `UI_Common_Bg_20Fillet_01`、标题徽记 `UI_Common_Panel_Head_01` 等。仍为纯色 `Image`（`m_Sprite: {fileID: 0}`）的是第九轮新增的升级区、描述滚动条，以及列表滚动条、铜币按钮、标题分隔线等共 12 处，视觉语言不统一。

## 二、文件清单（新增 0、修改 3、删除 0）

| 文件 | 用途与具体变化 |
|---|---|
| `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` | ① `DetailPanel/NormalUpgradeRoot/Txt_NextLevel` 整体移到 `DetailPanel/DescScroll/Viewport/Content` 末尾，成为文档流第 6 个子对象；RectTransform 改 `anchorMin=(0,1)`、`anchorMax=(1,1)`、`pivot=(0.5,1)`、`anchoredPosition=(0,0)`、`sizeDelta=(0,30)`，`m_Father` 指向 Content。② 12 处纯色 Image 换成 RO 图集九宫格并设 `m_Type=1(Sliced)`，同时统一蓝银/青蓝配色（清单见第三节）。③ **未增删任何 GameObject、组件或文档**，文档数 7606 保持不变。 |
| `LYF_MMO/Assets/GM_Game/Editor/SkillUpgradeUIBuilder.cs` | ① 新增 `RoAtlasPath` 与 5 个 RO 配色常量、`RoSprites` 缓存。② `Build()` 增加 `LoadRoSprites / RescueNextLevel / ApplyRoSkin` 三步，并把原有的三目取值改为 if/else。③ `BuildScroll()` 的 `saved[]` 增加 `Txt_NextLevel`，并在 Content 文档流末尾 `MoveInto` 该控件。④ `BuildNormalUpgrade()` 删除 `Txt_NextLevel` 的创建（改由滚动区持有）。⑤ 新增 RO 皮肤 region：`LoadRoSprites`、`RescueNextLevel`、`ApplyRoSkin`、`FindImage`、`ApplyRoSprite`、`TintImage`。 |
| `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/SkillInfoWindow.cs` | ① `_normalUpgradeRoot` / `_nextLevelText` 字段注释改为滚动区语义。② `EnsureUpgradeRefs()` 中 `_nextLevelText` 的查找路径由 `ChildText(normal, "Txt_NextLevel")` 改为 `ChildText(detail, "DescScroll/Viewport/Content/Txt_NextLevel")`。③ 新增私有方法 `SetNextLevelActive(bool)`；`RefreshUpgradeBlock()` 在 `skill == null` 时隐藏该行、在绝学页隐藏该行、普通技能页显示该行 —— 因为控件父节点已不再是随之显隐的 `NormalUpgradeRoot`。 |

另外本轮在 `_temp/ui-ro-skin-20261115/` 生成只读分析、落盘与校验脚本及证据（`inspect_panel.py`、`apply_skin.py`、`verify_skin.py`、`panel_structure.json`、`apply_skin_report.json`、`verify_result.json`、`sprite_ids.json`、`UIRoot.prefab.before_ro_skin.bak`），按项目规则属临时产物，不入库。

## 三、RO 皮肤逐项清单（12 处）

| 控件路径（相对 `SkillInfoWindow`） | RO 九宫格 | 配色 |
|---|---|---|
| `DetailPanel/DescScrollbar` | `UI_Common_Bg_progress_01` | (0.22, 0.34, 0.50, 0.55) |
| `DetailPanel/DescScrollbar/Sliding Area/Handle` | `UI_Common_Btn_Scrollbar_01` | (0.72, 0.86, 1, 0.95) |
| `DetailPanel/NormalUpgradeRoot/Btn_Upgrade` | `UI_Common_Btn_Normal_01` | (0.42, 0.62, 0.88, 1) |
| `DetailPanel/UltimateUpgradeRoot/Btn_ExpItem` | `UI_Common_Btn_Medium_01` | (0.30, 0.48, 0.72, 1) |
| `DetailPanel/UltimateUpgradeRoot/ExpBarBg` | `UI_Common_Bg_progress_01` | (0.18, 0.26, 0.38, 1) |
| `DetailPanel/UltimateUpgradeRoot/ExpBarBg/ExpBarFill` | 保持无 sprite（宽度由 `anchorMax` 控制，避免九宫格压缩变形） | 改 (0.42, 0.72, 0.95, 1) |
| `DetailPanel/UltimateUpgradeRoot/Btn_UltimateUpgrade` | `UI_Common_Btn_Normal_01` | (0.42, 0.62, 0.88, 1) |
| `DetailPanel/UltimateUpgradeRoot/ExpItemHint` | `UI_Common_Bg_Tips_01` | (0.09, 0.13, 0.20, 0.97) |
| `CurrencyBar/Btn_UpgradeAll` | `UI_Common_Btn_Medium_01` | (0.36, 0.56, 0.82, 1) |
| `ListRoot/ScrollView/Scrollbar` | `UI_Common_Bg_progress_01` | (0.22, 0.34, 0.50, 0.35) |
| `ListRoot/ScrollView/Scrollbar/Sliding Area/Handle` | `UI_Common_Btn_Scrollbar_01` | (0.72, 0.86, 1, 0.85) |
| `TitleLine` | `UI_Common_Bg_qujianjianbian_01` | (0.62, 0.72, 0.92, 0.55) |

保持不变：`BG_Inner` 与 `RoTopBar` 仍为纯色不透明底（前者用于盖住 3D 场景，后者是第八轮为避免 360×78 云纹条被拉到 1920 宽失真而刻意去掉 sprite 的结论）；标题、页签、竖排导航、环形节点、详情卡片、列表条目等已使用 RO 图集的元素不动；所有坐标、锚点、层级与业务代码结构不动。

## 四、设计思路

- 控件迁移复用项目既有的 `DescScroll/Viewport/Content` 文档流（`VerticalLayoutGroup` + `ContentSizeFitter`），不需要新建滚动容器；Content 由 5 个子对象变 6 个，内容高度随 TMP 首选高度自动增加，仍超过 460 视口高度，因此保持可滚动。「下一级」作为文档流最后一行，符合参考实机。
- 编辑器构建器与落盘 prefab 保持严格一致：构建器可重复执行，执行前 `RescueNextLevel` 会把控件从 `NormalUpgradeRoot`（旧位置）或 `DescScroll/Viewport/Content`（新位置）救回 `DetailPanel`，避免随旧滚动区或升级区一起被销毁；`BuildScroll` 重新收进 Content；`BuildNormalUpgrade` 不再创建该控件。
- 皮肤统一走 `Common1.png` 按 sprite 名加载，构建器运行时用 `AssetDatabase.LoadAllAssetsAtPath` 缓存，避免手改 YAML 的 fileID；落盘则用按对象名/路径定位的精确行替换，只改 `m_Sprite` / `m_Color` / `m_Type` 与一对父子引用。

## 五、验证

| 项目 | 结果 |
|---|---|
| prefab 结构 | 文档数 `7606 → 7606`、GameObject `1872 → 1872`、技能面板节点 `210 → 210`，无增删；`git diff --numstat` 仅 `41 / 41` 行 |
| 引用完整性 | fileID 无重复；本地 `{fileID: X}` 引用 0 处断链；`Txt_NextLevel` 的 `m_Father` 指向 Content，`Content.m_Children` 6 项、`NormalUpgradeRoot.m_Children` 3 项 |
| 字段核对 | 12 处皮肤的 `m_Sprite` / `m_Color` / `m_Type` 逐项比对全部命中；迁移后 anchor/pivot/sizeDelta 符合文档流要求 |
| 自动校验 | `_temp/ui-ro-skin-20261115/verify_skin.py`：`checks=20 failed=0` |
| C# 静态检查 | 两个 .cs 花括号/圆括号均配平；新增代码无 `foreach`、无命名空间、无三目运算符、无 `interface`、无超过 3 项的布尔表达式；`SkillUpgradeUIBuilder.cs` 原有三目 `font` 取值已改 if/else |
| 编辑器编译 | Unity 编辑器当前在运行，`Editor.log` 近期无 `error CS`；**本轮未主动触发 Unity 重新编译，也未在 Play 模式实拍** |

## 六、未验证 / 遗留

- **未做 Play 模式真实渲染截图**：滚动到底部时「下一级」行是否随文档流出现、12 处九宫格在窄滚动条（6px / 4px）上的 Sliced 观感、列表页与轻功页/秘笈页皮肤，均需 Unity 内实测。
- Unity 编辑器当前开着：磁盘 prefab 已更新，需让编辑器刷新资源后查看；若正处于 Play 模式，磁盘资产不会同步到当前场景实例，需退出 Play 再进。也可在 Unity 菜单 `Tools/Skill/Build Upgrade UI` 重建一次，验证构建器与落盘结果一致。
- 历史遗留不变：`Assets/GM_Game/Dlls/GM_Game.dll.bytes` 未重建（打包真机前必须跑 `Build/BuildDll`）；多分辨率按 1920×1080 绝对像素写死；升级/心得仍是本地 UI 模拟未接协议；「背景小青点」未消除。
- 本轮未改动脚本之外的 `.meta`，未新增或删除任何文件。

## 七、Git 回执

（提交与推送后补记）
