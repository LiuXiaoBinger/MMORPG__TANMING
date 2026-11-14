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

---

# 第二轮迭代：按录屏重新对齐（2026-11-14 晚）

用户反馈四条：①滑行属于轻功、大图标固定是绝学；②轻功页布局不对；③页签不是胶囊式；④每个功能页都要有内容。随后提供了《一梦江湖》实机录屏（`D:/录屏/bandicam 2026-11-14 19-40-04-655.mp4`，67 秒）作为参考。

## 8.1 从录屏量出的真实结构

| 页面 | 结构 |
| --- | --- |
| 出战武功 | 顶部「配置/翻页/描述」工具行；人物两侧各 4 个节点（共 8 个）+ 1 个绝学大节点；人物左右各一个 1/2 技能组徽章；右侧 616 宽详情；最右 190 宽竖排导航 |
| 武功列表 | 左侧约 576 宽技能列表（图标 + 名称 + 等级，条目高约 120），立绘在中间透出，右侧同一个详情面板 |
| 出战轻功 | 左侧 3 个竖排节点 + 中央 200 直径大图标（当前选中技能）+ 右侧详情；没有工具行与徽章 |
| 秘笈 | 单页签，列表式，暂无数据 |

配色：深青蓝底、青白文字、伤害数值黄绿、标签橙色。

## 8.2 本轮改动

| 文件 | 变化 |
| --- | --- |
| `UIRoot.prefab` | TabRow 由 3 个胶囊页签改为 2 个「浅色矩形 + 剪影图标 + 深色字」子页签（300×77 / 285×77，y=270）；新增 `ToolRow`（配置/翻页/描述）、`BadgeRoot`（1/2 徽章）、`LightRoot`（轻功页：3 节点 + 中央大图标）、`ListRoot`（ScrollView + 竖向列表 + 自动隐藏滚动条 + `ItemTemplate`）；`NodeRoot` 扩为 9 个节点（8 普通 92px + 绝学 142px）；立绘移到 (-427,-100) 520×720；详情面板加宽到 616×668 并重排内部控件；右侧导航 6 项加宽到 190×104；`EmptyPage` 保留 |
| `SkillInfoWindow.cs` | 整体重写为「分类 + 子页」状态机：`SelectCategory`（武功/轻功/秘笈）与 `SelectPage`（出战/列表）；武功页按 8+1 铺节点（绝学按 Tag 固定占大节点）；轻功页收 `Type=3 滑行` 与 `Type=4 轻功`；列表页运行时复制模板条目并支持点击选中；秘笈页只显示空壳 |
| `SkillListItemWidget.cs`（新增） | 列表条目控件：图标、名称、`Lv.x · 两字标签` 与选中底色 |
| `SkillCtrl.cs` | 无新增改动（沿用上一轮的 `GetSkillTag`） |

## 8.3 验证记录

| 项目 | 结果 |
| --- | --- |
| 编译 | 通过；`SkillInfoWindow` 新增的 20+ 个字段与 `SelectListItem` 方法在运行时程序集中可反射到 |
| 预制体引用 | `SkillInfoWindow` 全部 `[SerializeField]` 引用非空（emptyFields=0），窗口 1580×880、14 个子节点 |
| 出战武功页 | Play 模式注入 10 条技能：左列 4 节点（普攻/突进/冲锋/爆发）、右列 4 节点（连招/群攻/护盾/未习得）、绝学金圈大节点、1/2 徽章、工具行、详情（名称/Lv/橙色标签/描述/使用条件）全部正常 |
| 出战轻功页 | 页签自动变「出战轻功/轻功列表」，左侧 3 节点、中央大图标显示选中技能、工具行与徽章隐藏、右侧详情正常，滑行与轻功都出现在轻功分类 |
| 武功列表页 | 条目从第一条开始、图标+名称+等级正常、滚动条自动隐藏、首条高亮、点击条目切换右侧详情（代码已绑定 `SelectListItem`） |
| 未验证 | ①真实登录态主城按 L 打开的完整链路（Play 只能进登录场景，数据为手工注入）②滚动到底部的表现 ③「配置/翻页/描述」「1/2 徽章」「重置技能点」仍是展示，未接协议 |

## 8.4 踩坑与结论

- **一梦江湖的客户端无法被脚本驱动**：窗口能切前台、进程未提权（`OpenProcess` 成功），但 `mouse_event` 与 `PostMessage(WM_LBUTTONDOWN)` 都被丢弃，猜测是 raw input 或反注入；最终改为「手动录屏 → 抽帧量比例」的方式取参考。
- **Unity 2023.2 在编辑器失焦时会挂起脚本编译**：`isCompiling` 会一直为 true 且 DLL 不更新（本例卡了 40 分钟）。恢复办法是先让 Unity 窗口获得焦点，再 `EditorApplication.UnlockReloadAssemblies()` / 进出一次 Play 模式清状态，然后 `CompilationPipeline.RequestScriptCompilation()`。
- **Play 模式下改 prefab 资产不会同步到场景实例**，预览迭代必须直接改运行时实例，最后再统一写回 prefab。
- Mask 用的 `MaskCircle01` 是菱形而非圆形；节点图标显示成白色菱形时，先看图标 sprite 是否已异步加载完成，再看 mask 形状。

---

# 第三轮迭代：全屏化 + 去技能点（2026-11-14 夜）

用户反馈：界面不好看、要求全屏显示（不再是居中窗口）、任务追踪要浮在技能面板之上、删掉「重置技能点」与「剩余技能点」。

## 9.1 改动

| 项目 | 变化 |
| --- | --- |
| 窗口尺寸 | `SkillInfoWindow` 从 1580×880 居中窗口改为 1920×1080 全屏；`BG_Frame` 圆角外框禁用，`BG_Inner` 改成无 sprite 的全屏纯色底（alpha 0.97） |
| 层级 | 窗口加 `Canvas`（`overrideSorting=true`、`sortingOrder=-10`）+ `GraphicRaycaster`，并把 `QuickPanel` 提到 MainView 最后一个子节点，使任务追踪稳定浮在技能面板之上。**Canvas 的 overrideSorting 在预制体里必须用 `SerializedObject` 写 `m_OverrideSorting` 才能持久化**，直接赋值在 inactive 对象上会被丢弃 |
| 删除元素 | `DetailPanel/Btn_Reset`（重置技能点）与 `Txt_Point`（剩余技能点）整节点删除；`SkillInfoWindow.SetHeader` 改为只接收 jobName，`RoleInfoCtrl` 调用同步修改 |
| 全屏坐标 | 标题「武学」(-620,470)、职业 (-430,460)、关闭 (890,468)；子页签 y=388；工具行 y=316；立绘 680×920 @(-480,-40)；左列节点 x=-880（y 180/60/-60/-180），右列 x=-60（y 260/140/20/-100），绝学 180px @(-260,-300)；详情卡片 600×660 @(450,40)；导航 190×96，x=860 |
| 装饰 | 新增 `PortraitHalo`（立绘背后冷色光斑 1000×1000，alpha 0.10）、`TitleLine`（标题下 1920×2 亮蓝分隔线）、`DetailBorder`（详情卡片外描边 612×672，蓝色 0.55） |

## 9.2 验证

- Play 模式注入 10 条技能数据后截图：全屏铺满、无窗口边框；左上任务追踪不再被压暗（浮在面板之上）；「重置技能点」「剩余技能点」已消失；立绘/节点/详情卡片/导航四块位置正常。
- 未验证：不同分辨率下的适配（当前按 1920×1080 固定坐标）、列表页与轻功页在 1920×1080 下的新坐标（本轮只重排了出战页与详情/导航）。

## 9.3 遗留

- 底部游戏技能栏与「绝学」节点仍有轻微重叠（HUD 恒在最上层），若要彻底避免需要把绝学再上移或全屏时隐藏 HUD。
- 左列最上方节点与左上任务面板小面积重叠，可再下移 20px。

---

# 第四轮：按录屏实测坐标重建（2026-11-14 深夜）

用户指出两个硬伤：①全屏后游戏 HUD（顶栏按钮、小地图、底部技能栏）全浮在技能界面之上；②技能节点被排成贴屏边的两条竖线，与实机不符。要求逐帧对照视频重排。

## 10.1 从录屏实测的坐标（1080 宽画面 → 1920×1080 canvas，×1.777 横向、×1.742 纵向）

| 元素 | 参考图坐标（1080×620） | canvas 坐标 |
| --- | --- | --- |
| 人物中心 | (320,360)，半身约 200×360 | (-391,-87)，显示 456×640 |
| 左列节点（4） | (105,327)(102,412)(127,482)(157,527) | (-775,-30)(-762,-165)(-722,-290)(-672,-415) |
| 右列节点（4） | (427,237)(482,307)(502,377)(507,452) | (-205,130)(-118,8)(-80,-120)(-70,-250) |
| 绝学大节点 | (382,442)，直径 75 | (-281,-230)，133px |
| 数字 1 / 2 | (175,415) / (430,315) | (-649,-183) / (-196,-9) |
| 详情面板 | x 570..960，y 130..600 | 中心 (399,-96)，693×818 |
| 竖排导航 | x 965..1080 | x 857，205×110 ×6 |

关键结论：**节点是沿人物轮廓的弧形错落，不是等距竖列**；左右两列各自的 x 随 y 递增/递减，靠前后遮挡形成"环绕"感。

## 10.2 改动

- 窗口 Canvas `sortingOrder` 从 -10 改为 **+100**，全屏面板盖住顶部按钮栏、小地图、底部技能栏，不再有 HUD 浮在界面上。
- 按上表重建全部坐标（标题 62px / 页签 311×78 / 工具行 y=232 / 人物 456×640 / 节点 98px 绝学 133px / 详情 693×818 / 导航 205×110）。
- 节点标签下移到圆环外 24px，避免被下一个节点遮挡。
- 顺带补上列表页与轻功页的全屏坐标；列表页立绘移到列表与详情之间（`SetPortraitPosition`）。

## 10.3 验证

- Play 模式注入 10 条技能数据截图：HUD 全部被盖住；左列 4 个节点（普攻/突进/冲锋/爆发）沿人物左侧弧形排列、标签全部可读；右列 4 个（连招/群攻/护盾/未习得）沿右侧弧形；绝学金圈在人物右下；1/2 徽章分列；详情卡片与导航位置与参考图一致。
- 未验证：列表页与轻功页的新坐标（本轮只截图看了出战页）；不同分辨率适配。

---

# 第五轮：RO 风格皮肤 + 列表/详情微调（2026-11-14 深夜）

用户要求「发技能界面采用 RO 项目风格」，并指出三处布局问题：轻功列表页两个页签错位、武功列表首条被页签压住、技能描述标题与正文贴在一起。

## 11.1 改动

| 位置 | 变化 |
| --- | --- |
| 顶部 | 新增 `RoTopBar`（1920×92，`UI_Common_Panel_Head_01` 九宫格，白色 0.95 alpha），标题「武学」压在条上 |
| 页签 | 两个子页签改为 `UI_Common_Bg_20Fillet_01` 圆角块，并去掉中间 18px 缝隙（SubTab_1 中心从 -311 改到 -330，与 SubTab_0 紧贴） |
| 详情卡片 | 底图换 `UI_Common_Bg_Normal_04`（RO 蓝银框）+ `DetailBorder` 同贴图蓝色描边 |
| 导航 | 6 项换 `UI_Common_Bg_20Fillet_01`，选中态由代码染白 |
| 列表页 | ScrollView 上提到 (-600,-64) 460×700，条目 440×108、图标 76、名称/Meta 右移到图标外侧；滚动条 x=232、宽 8 |
| 描述区 | 「◇技能描述」标题 y=222、正文 y=92 高 190，二者留 20px；分隔线 y=14、使用条件 y=-116 |

## 11.2 验证

- Play 模式实拍：顶部 RO 蓝渐变标题条、页签紧贴、详情蓝银边框、导航圆角块，人物与 8 节点（含绝学金圈）位置正常，名称不再压图标。
- 未验证：轻功列表页与秘笈页在本次皮肤下的观感（本轮只截了出战页）。

---

# 第六轮：收尾核对（2026-11-14 22:1x）

## 12.1 交付状态

- 第五轮改动已提交并推送：`1271f2d style: 武学面板改用 RO 皮肤，修页签缝隙/列表/描述间距`，与 `origin/codex/feature-development` 同步。
- 整条需求链共 10 次提交：`44f30ad` → `a04d15a` → `1baebc0` → `f0f8488` → `bd3b67f` → `00c3d07` → `db45257` → `253ed7f` → `1271f2d`，另加本轮收尾提交。
- 最终实拍：`_temp/portrait/ro_skin.png`（1920×1080 等比缩小）。深蓝底 + RO 蓝银描边 + 顶部 `Panel_Head_01` 云纹条、白色选中页签紧贴无缝、详情卡片蓝银框、人物立绘与 8 个弧形节点 + 绝学金圈、右侧 6 项竖排导航，HUD 全部被全屏面板盖住。

## 12.2 Unity 二次重排的 prefab 差异

- 提交后 Unity 编辑器又保存了一次 `UIRoot.prefab`，工作区出现约 12598 行 diff。逐项核对结论：
  - 对象数 **3996 → 3996**，`m_Name` 集合完全一致，无新增/删除节点；
  - 去掉 `fileID` 数值与 `--- !u!` 文档头后，真实属性差异仅 **8 行**：4 个 TMP 组件的 `m_fontSizeMin/Max` 由 `8 / 15` 改为 `12 / 22`（节点标签自适应字号范围上调）；
  - 其余全是 Unity 重新分配 local fileID 造成的行序重排（本项目 `SaveAsPrefabAsset` 的已知行为）。
- 处理方式：保留编辑器实际写出的版本并提交，避免外部回写文件导致编辑器内存态与磁盘不一致。

## 12.3 遗留与风险（**打包前必看**）

| 项 | 状态 |
| --- | --- |
| **HybridCLR 热更 DLL** | `Assets/GM_Game/Dlls/GM_Game.dll.bytes` 时间戳仍为 **11-12 00:10**，早于本轮新增的 `SkillListItemWidget.cs` 及 `SkillInfoWindow.cs` / `SkillViewData.cs` / `SkillCtrl.cs` / `RoleInfoCtrl.cs` 改动。编辑器内走 `#if !UNITY_EDITOR` 跳过加载所以能正常预览，但**真机包不会包含新类、预制体会 Missing Script**。打包前必须执行 Unity 菜单 `Build/BuildDll` 重新生成 `GM_Game.dll.bytes` 并提交。 |
| 页面观感 | 轻功页、武功列表页、秘笈页在 RO 皮肤下未实拍；列表滚到底部未测。 |
| 分辨率 | 全部坐标按 1920×1080 绝对像素写死，未做多分辨率适配。 |
| 业务链路 | 真实登录态主城按 `L` 打开面板的完整链路未跑；「配置 / 翻页 / 描述」、1/2 徽章、自创/双门派/失传武学仍为展示态，未接协议。 |

---

# 第七轮：技能节点点击无响应修复（2026-11-14 22:3x）

## 13.1 现象与需求

- 用户反馈：出战武功页的技能节点（突进/冲锋/爆发/连招/群攻/护盾/绝学等）看得见、图标和标签都正常，但鼠标点击没有任何反应，右侧详情不会切换。

## 13.2 排查证据

| 层次 | 检查结论 |
| --- | --- |
| 业务代码 | `SkillInfoWindow.BindButtons()` 已为 `_nodeButtons`(9) / `_lightButtons`(3) 全部 `AddListener`；序列化引用 9+3 个 `Button` 均有效，运行时 `interactable` 对有技能的节点为 `True`（两个「未习得」槽位为 `False`，符合设计）。 |
| Canvas | `SkillInfoWindow` 自带 `Canvas(overrideSorting=1, sortingOrder=100)` 与 `GraphicRaycaster`，`EventSystem.current` 存在，`worldCamera=UICamera`，射线层正常。 |
| 遮挡 | 全屏 `BG_Inner`/`BG_Frame` 虽 `raycastTarget=1`，但在 `NodeRoot` 之前渲染，不会盖住节点；`PortraitHalo`/`Portrait`/`DetailBorder`/`TitleLine` 均为 `raycastTarget=0`。 |
| **节点本身** | 扫描面板全部 22 个 `Button`：`Btn_Close`、`SubTab_0/1`、`Nav_0..5`、`ItemTemplate` 共 10 个自身带可命中 `Image`；**`Node_0..8`、`LightNode_0..2` 共 12 个根对象只有 `RectTransform + Button`，没有 `Graphic`，其全部子对象（Glow/Ring/Inner/IconMask/Tag）的 `raycastTarget` 都是 0**。 |
| 运行时复核 | Play 模式对 9 个节点中心做 `EventSystem.RaycastAll`：每个节点 `graphics=6, rayOn=0`，`rayHits=2` 且 `firstHit=BG_Inner`，事件沿命中对象向上找不到任何 `IPointerClickHandler`，因此 `Button.onClick` 永远不触发。 |

**根因**：UGUI 的 `Button` 本身不参与射线；射线必须先命中一个 `raycastTarget=1` 的 `Graphic`，再由 `EventSystem` 沿父链找到 `Button`。节点为了做圆形遮罩把所有可见 `Image` 的射线都关掉了，又没有给节点根补一个命中区，点击直接穿透到全屏底图。

## 13.3 修复

- 文件：`LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`（修改，无新增/删除文件）。
- 把这 12 个节点子对象 `Ring` 的 `Image.raycastTarget` 从 `0` 改为 `1`（`Ring` 尺寸与节点完全一致，98×98 / 绝学 133×133 / 轻功 100×100），命中区精确等于圆环范围，不依赖图标 sprite 是否加载完成，也不会像 `Glow` 那样溢出到相邻节点。
- 未改任何 `.cs`；`Button` 的 `m_TargetGraphic` 与颜色过渡逻辑保持原样。未习得槽位仍由 `interactable=false` 保持不可点。
- 同步处理：Python 精确改 12 行 YAML → `AssetDatabase.ImportAsset(ForceUpdate|ForceSynchronousImport)` 让编辑器加载新资产 → Play 模式当前实例单独改 12 个 Ring（Play 模式下资产改动不会回灌运行时实例）。改前备份 `_temp/UIRoot_before_raycast_fix.prefab`。
- 保留差异：`UIRoot.prefab` 相对上一提交还有 28 行与本次无关的属性变化（页签图标 `m_AnchoredPosition -68→-80`、`m_SizeDelta 34→28`、`m_fontSize 15→13`、autoSize `12/22→9/13` 等），是上一提交后编辑器二次保存产生的既有修改，按「不覆盖用户已有修改」原则原样保留；`git diff` 里的大段行序变化仍是 Unity 重新分配 local fileID 所致，归一化后真实属性差异共 40 行（本次 12 行 + 既有 28 行）。

## 13.4 验证

- Play 模式运行时：12 个 Ring 全部改为可命中；9 个武功节点 `firstHit=Ring`、`clickHandler=Node_0..Node_8`；对 `Node_1` 执行 `ExecuteEvents.Execute<IPointerClickHandler>` 模拟点击成功，详情随之切换。
- 预制体资产：`LoadPrefabContents` 读回校验 `raycastOn=12, raycastOff=0`，与磁盘一致。
- 未验证：真实鼠标物理点击（本轮用事件系统模拟）、轻功页 3 个节点的实际点击观感（同样已修但只做了静态与资产校验）、真机/非编辑器环境。

## 13.5 遗留

- 第六轮记录的 **HybridCLR `GM_Game.dll.bytes` 未重建** 仍然存在；本轮未改代码，没有新增类，但整条技能面板改动打包前仍需执行 Unity 菜单 `Build/BuildDll`。
- 「配置 / 翻页 / 描述」、1/2 徽章、自创/双门派/失传武学仍为展示态，未接协议。


