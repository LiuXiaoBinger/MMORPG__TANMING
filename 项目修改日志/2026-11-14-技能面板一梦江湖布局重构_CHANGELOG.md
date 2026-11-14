# 技能面板（武学）一梦江湖布局重构

- 日期：2026-11-14
- 分支：`codex/feature-development`
- 目标：把原「技能面板」列表式界面改成一梦江湖「武学」式布局（人物立绘 + 环形技能节点 + 右侧详情 + 右侧竖排导航），视觉风格沿用本项目 RO（蓝银）UI 图集，并保证文字清晰可读。

## 一、需求拆解

| 用户要求 | 落地方式 |
| --- | --- |
| 做出图 2 的面板布局 | 顶部标题条 + 三个页签 + 左侧立绘环形节点 + 右侧详情 + 右竖排导航 |
| 门派图像换成魔剑士画像（AI 美化） | Unity 取证渲染主角半身像 → 图生图重绘 → 导入为 Sprite 并做椭圆渐隐 |
| 技能有两个字描述 | 环形节点下方显示两字定位标签（普攻/突进/冲锋/爆发/连招/绝学…），详情右上同样显示 |
| 秘籍先做空壳 | 「秘笈」分类页：标题 + 说明 + 4 个空槽位，其余内容全部隐藏 |
| 布局用一梦江湖、风格用 RO，界面清晰 | 全部使用 `Common1.png` 的 `UI_Common_*` 九宫格；深色底 + 蓝银描边 + 21~30px 字号 |

## 二、文件清单

### 新增

| 文件 | 用途与变化 |
| --- | --- |
| `LYF_MMO/Assets/GM_Game/UISprites/Skill/MJS_Portrait.png` | 魔剑士门派立绘贴图，760×1050，AI 重绘后裁掉右下角水印，并用椭圆 alpha 蒙版做四边渐隐，便于贴进深色面板时无硬边 |
| `LYF_MMO/Assets/GM_Game/UISprites/Skill/MJS_Portrait.png.meta` | 导入设置：Sprite(Single)、关闭 Mipmap、开启 AlphaIsTransparency、最高 2048、高质量压缩 |
| `LYF_MMO/Assets/GM_Game/UISprites/Skill.meta` | 新建 `Skill` 目录的目录元文件（Unity 生成） |
| `项目修改日志/2026-11-14-技能面板一梦江湖布局重构_CHANGELOG.md` | 本文件 |

### 修改

| 文件 | 具体变化 |
| --- | --- |
| `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab` | `SkillInfoWindow` 节点由 1100×780 改为 1580×880；删除原 3 个子节点（Line / pan_right / Scroll View 列表），写入新面板 10 个子节点：`BG_Frame`、`BG_Inner`、`TopBar`(标题+职业+剩余技能点)、`Btn_Close`、`TabRow`(3 页签)、`Portrait`、`NavRoot`(6 导航)、`DetailPanel`(11 控件)、`NodeRoot`(6 个环形节点)、`EmptyPage`(秘笈空壳)；并把 `SkillInfoWindow` 组件的全部引用重新绑定 |
| `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/SkillInfoWindow.cs` | 整体重写：不再异步创建 `SkillItemWidget` 列表，改为按分类筛选技能快照、铺到 6 个环形节点（末位为绝学金圈）、点击节点切换右侧详情；新增页签/竖排导航分类切换、秘笈空壳页、关闭按钮；保留 `SetHeader(jobName, points)` 供 `RoleInfoCtrl` 调用 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/ViewModel/SkillViewData.cs` | 新增 `SkillType`（技能表 Type）、`Tag`（两字定位标签）、`MpCost`（消耗内力）、`Range`（有效距离米）四个只读字段与构造参数，供面板显示使用条件 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs` | 两处 `new SkillViewData(...)` 补齐新字段；新增 `GetSkillTag(SkillInfo)`：按魔剑士技能编号给出两字标签（10010 普攻 / 10040 突进 / 10050 冲锋 / 10060 爆发 / 10070 绝学 / 10080 滑行 / 10090 轻功 / 10100 连招 / 10110 群攻 / 10120 护盾），未收录技能按 Type 回退 |

## 三、界面结构与坐标（窗口 1580×880，中心为原点）

| 区块 | 位置/尺寸 | 说明 |
| --- | --- | --- |
| 外框 `BG_Frame` | 全屏拉伸 | `UI_Common_Bg_Normal_04`，色 (0.23,0.29,0.39) 作为 RO 银边 |
| 内底 `BG_Inner` | 内缩 7px | `UI_Common_Bg_20Fillet_01`，色 (0.052,0.068,0.098)，**改成不透明**避免身后场景透出 |
| 标题条 `TopBar` | 1540×66，y=386 | `UI_Common_Panel_Head_01`；左「武 学」34px、副标「剑 修」20px；右「剩余技能点：N」22px 金色 |
| 页签 `Tab_0..2` | 175×50，y=322，x=-640/-455/-270 | 选中蓝块白字 (0.24,0.42,0.72)，未选中深块灰蓝字 |
| 立绘 `Portrait` | 500×690，(-280,-55) | 椭圆 alpha 渐隐，直接融入深色底（不加卡片边框） |
| 节点 `Node_0..5` | 96×96（绝学 132×132） | 光晕 + 外圈 + 深色内圆 + 圆形遮罩图标 + 下方两字标签；绝学为金圈金字 |
| 详情 `DetailPanel` | 470×710，(405,-55) | 图标/名称/Lv/两字标签 + 分隔线 + 技能描述 + 使用条件 + 重置技能点按钮 |
| 竖排导航 `Nav_0..5` | 130×86，x=715，y=288…-192 | 武功/轻功/秘笈 可点，自创/双门派/失传武学 置灰不可点 |
| 秘笈空壳 `EmptyPage` | 1400×690 | 标题「秘 笈」+「秘笈系统尚未开放，敬请期待」+ 4 个空槽位 + 底部提示；进入该页同时隐藏立绘与节点 |

## 四、立绘生成流程（可复现）

1. Play 模式下克隆当前主角（`Role_MJS(Clone)`），抬高 300m 避开主城几何体，关闭武器/火焰等特效 Renderer。
2. 临时三点光（暖白主光 1.05、冷蓝补光 0.45、青色轮廓光 1.6）+ 临时相机 30° FOV 渲染 1200×1500 RenderTexture，读回 PNG 作为参考图（`_temp/portrait/mjs_ref_3q.png`、`mjs_ref_bust.png`）。
3. 图像生成（图生图，参考图 = 半身像）：提示词限定银白长发 + 黑金轻甲 + 冷调逆光 + 深蓝墨色背景 + 国风半写实，输出 1024×1536。
4. `py -3.10` + Pillow：裁掉底部 66px 水印 → 缩放 760×1050 → 椭圆 alpha 蒙版（rx=0.40W、ry=0.46H，0.62 内全不透明，1.0 处全透明）→ 保存进 `Assets`。
5. Unity `TextureImporter` 设为 Sprite 并 `SaveAndReimport`。

## 五、验证

| 项目 | 结果 |
| --- | --- |
| 编译 | Unity 重新编译通过，无 `error CS`（`SkillInfoWindow` 新字段可在程序集中反射到） |
| 预制体结构 | `UIRoot.prefab` 载入后 `SkillInfoWindow` 1580×880、10 个子节点，组件 22 个 `[SerializeField]` 引用全部非空（脚本检查 emptyFields=0） |
| 运行期数据绑定 | Play 模式手工调用 `SetHeader("剑 修",40)` + `ReFreshUI(6 条 SkillViewData)`：节点图标异步加载成功、详情显示「剑气攻击/Lv.1/普攻」、描述内 `<color=#F00>` 百分比正常变红、使用条件「0.5 秒 / 3 米 / 20」正确 |
| 分类切换 | `SelectCategory(2)` 切到秘笈：立绘与节点隐藏、空壳页显示、页签与竖排导航同时高亮「秘笈」 |
| 其他窗口 | 对比 UIRoot 中 `KnapsackWindow/ChatWindow/RoleAttriibuteWindow/NPCShopWindow/DeathWindow/EquipmentDetailWindow/SkillInfoWindow` 节点数量，改动前后一致，未丢失其他窗口 |
| 未验证 | ①未在真实登录态主城中按 L 键完整走一遍（Play 只能进登录场景，面板数据为手工注入）②「重置技能点」「自创/双门派/失传武学」按设计不接业务，无服务端协议 ③技能点仍沿用角色等级（协议无独立技能点字段，沿旧逻辑） |

## 六、已知取舍

- **UIRoot.prefab 差异很大**：面板是用 `PrefabUtility.LoadPrefabContents` + `SaveAsPrefabAsset` 写入的，Unity 会按自己的顺序重排整个 YAML，因此 git diff 显示约 7.8 万行变更（新增文档 335 个），并非删除其他窗口；已用文档数与窗口节点数核对。
- 环形节点固定 6 个槽位（第 6 个为绝学大节点），技能多于 6 个时只显示前 6 个；分类筛选按技能表 `Type`：武功页收 Type 1/2/3，轻功页收 Type 4。
- 页签与竖排导航指向同一组分类（武功/轻功/秘笈），保持与一梦江湖「右侧分类 + 顶部子页」的视觉对应，但首版只做出战页，没有再做「列表页」。
- 「重置技能点」按钮沿用旧界面：只做展示，未接协议。

## 七、中间产物

- `_temp/portrait/`：渲染参考图、AI 输出、渐隐脚本 `make_portrait.py`、各轮预览截图（`preview_01~03`、`edit_v3~v5`、`final_skill2`、`play_panel`、`play_secret`、`play_opaque`）。
