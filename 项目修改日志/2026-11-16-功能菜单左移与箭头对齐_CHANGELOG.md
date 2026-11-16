# 顶部功能菜单左移与收起箭头对齐

- 日期：2026-11-16
- 分支：`codex/feature-development`
- 提交：`1ae26c5`（预制体改动；日志与协作记录另起一次提交）
- 目标：左上角「背包 / 角色 / 技能 / 设置 / 任务 / 日常 / 副本」整行太靠右，收起箭头也没贴到最左；本次把整行左移，并让箭头与快捷人物隐藏按钮（`Btn_Fold`）落在同一列。

## 一、需求与落地

| 用户要求 | 落地方式 |
| --- | --- |
| 功能菜单太靠右 | 入口组容器 `FunctionMenuContent` 的 x 由 116 改 44，整行左移 72px |
| 缩放（收起）箭头没靠在最左边 | 箭头在其按钮内的 x 由 54 改 14，中心落到屏幕 x=38 |
| 箭头与快捷人物隐藏按钮对齐 | 折叠态 `Btn_Fold` 中心同为 x=38，两者同一列（箭头 24~52，按钮 21~55） |

定位结论（编辑器态核实）：

- 功能菜单根 = `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` 的 `Canvas/MainView/FunctionMenu`，锚点/轴心均为左上 (0,1)，`anchoredPosition=(24,-24)`。
- 收起箭头 = `FunctionMenu/FunctionMenuButton/Arrow`，尺寸 28×20，中心对齐；`FunctionMenuButton` 是 108×38 的全透明点击区（`m_Color` alpha 0、`m_RaycastTarget` 1）。
- 七个入口 = `FunctionMenu/FunctionMenuContent` 下的 `KnapsackButton / RoleButton / SkillButton / SettingButton / TaskButton / DailyTaskButton / ThemeDungeonButton`，每个 80×84、间距 2px。
- 快捷人物隐藏按钮 = `QuickPanel.prefab` 实例里的 `QuickTaskPanel/Btn_Fold`，34×34、轴心居中；折叠态 `anchoredPosition=(20,-225.31)`，父级 `QuickTaskPanel` 偏移 18，故中心落在屏幕 x=38。
- 主画布 1920×1080、`MatchWidthOrHeight` 匹配宽度，1 UI 单位即 1 像素，左上锚点的 `anchoredPosition` 可直接当屏幕坐标读。

## 二、文件清单

### 修改

| 文件 | 具体变化 |
| --- | --- |
| `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` | ① `FunctionMenuContent`（RectTransform `&9200000000000000003`）`m_AnchoredPosition` x 116 → 44；② `FunctionMenuButton`（`&9200000000000000007`）`m_SizeDelta` x 108 → 44；③ 其子节点 `Arrow`（`&9200000000000000012`）`m_AnchoredPosition` x 54 → 14。共 3 行数值，其余字段、组件、`.meta` 均未动 |

### 新增

| 文件 | 用途 |
| --- | --- |
| `项目修改日志/2026-11-16-功能菜单左移与箭头对齐_CHANGELOG.md` | 本文件：文件清单、坐标对比、验证与边界 |

## 三、坐标前后对比（屏幕像素，左上角为原点）

| 元素 | 改前 | 改后 | 说明 |
| --- | --- | --- | --- |
| 箭头中心 x | 78 | **38** | 与 `Btn_Fold` 中心 38 同列 |
| 箭头左右边界 | 64~92 | 24~52 | 与 `Btn_Fold` 的 21~55 基本重合 |
| 「背包」按钮左边缘 | 146 | **74** | 整行左移 72px |
| 「背包」图标中心 x | 186 | 114 | 与箭头间距由 54px 收到 22px |
| 箭头点击区 | 24~132（108 宽） | 24~68（44 宽） | 原 108 宽会压住左移后的第一个按钮，故一并收窄 |
| 箭头竖直中心 y | 116 | 116 | 未动，仍在按钮组中轴 |

## 四、设计取舍

1. **只改数值，不改结构**：`MainView.SetFunctionMenuVisible` 只负责显隐与箭头旋转（展开 90°、收起 −90°），不读坐标，所以位置调整不需要动脚本。
2. **点击区必须一起收窄**：`FunctionMenuButton` 是透明但可点击的 108×38 区域。若只把箭头移到左边、点击区仍留 108 宽，它会盖住左移后的「背包」按钮（74~154）前 58px，造成点背包变成收起菜单。收窄到 44 宽后两者不再重叠。
3. **箭头不对齐按钮组左边缘而是对齐隐藏按钮中心**：用户要求「跟快捷人物隐藏按钮对齐」，`Btn_Fold` 中心在 38、左右 21~55；箭头 28 宽居中放 38，落在 24~52，视觉上就是同一列。
4. **按钮组起点取 74**：箭头右边界 52，留 22px 间隙，既贴左又不与箭头视觉粘连。

## 五、验证结果（编辑器态，Unity 2023.2.20f1c1）

- 预制体读回：`FunctionMenuButton.sizeDelta=(44,38)`、`Arrow.anchoredPosition=(14,-19)`、`FunctionMenuContent.anchoredPosition=(44,-46)`，与改后资产一致。
- 实例化 `UIRoot.prefab` 后换算世界坐标（画布左上角为原点）：箭头中心 `x=38`、`KnapsackButton` 中心 `x=114`；改前分别为 78 与 186。
- 控制台 0 错误；临时场景用后即关，活动场景已恢复 `Assets/Boot/Scene_Boot.unity`，未保存任何场景改动。

## 六、未验证与遗留

- **未取实机 Play 截图**：本次 Unity 处于编辑态且不在主城场景，临时场景没有相机、Overlay 画布不参与 Game View 渲染，截图只拍到天空，无法作为视觉证据；因此只做预制体坐标实测。重新按 Play 进主城即可看到效果。
- **预制体另有既有未提交改动**：`UIRoot.prefab` 工作区还有 413 行先前改动（配色、贴图与若干节点坐标，非本次产生）。本次用「HEAD + 本次 3 行」单独入索引，只提交这 3 行，其余改动保留在工作区未提交。
- 箭头外观（白 `>` 图标）与尺寸 28×20 未改；若要与参考图那种圆角卷轴手柄一致，需另换贴图。
