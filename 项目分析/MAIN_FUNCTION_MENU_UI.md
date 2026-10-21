# 主界面左上功能菜单

## 需求与范围

用户指定将设置入口放到左上角，并参考一梦江湖把背包、设置等入口组成可以展开、收起的功能区，之后明确使用RO图标，并要求开关改为箭头、按钮背景透明。当前仅整合已有背包、角色、技能和设置窗口，不实现设置选项效果或新增玩法。

## 布局与交互

- `MainView/FunctionMenu` 固定距画布左上角 24 个参考像素，采用现有 1920×1080 画布适配。
- 默认仅显示朝下的RO箭头，展开后箭头朝上、下方显示344×92横排入口组；点击任一入口后保持展开，只有再次点击箭头才手动收起。箭头显示28×20，按钮仍保留108×38点击区，开关底板完全透明，不再显示“功能 +”“收起 -”文字。
- 用户随后要求展开组也透明，现入口组底板与四入口各自底板的Image alpha均为0，展开仅显示箭头、四个RO图标与中文；原按钮及射线区域保留，没有删除组件。
- 四项依次为背包、角色、技能、设置，各保留中文名称和独立图标。原右上设置按钮的对象和组件身份保留，迁入展开组；设置窗口正文不变。
- 实际正式预制体的 `RoleCurrtInfoWindow` 已在底部技能HUD，左上无常驻头像。本轮不移动HUD、小地图或其他窗口，菜单在弹窗之前绘制。

## 现有UI模式接入

`MainView` 保存序列化按钮、入口组和箭头RectTransform，仅提供 `ToggleFunctionMenu`、`SetFunctionMenuVisible` 展示操作；`InitView` 将菜单恢复为收起。`MainCtrl` 成对注册、注销五个按钮事件。显隐直接读取容器，箭头以0度朝下、180度朝上，没有新增动画、持久化或状态字段。

背包、角色、技能入口分别调用既有 `MainUIKeyHandler("B"/"I"/"L")`，保持背包和属性窗口原有镜头调用。设置继续通过 `ShowMainWindow(WindowType.SettingPanel)` 打开。窗口仍由 `MainView` 登记到现有窗口字典，没有新增UI管理器、业务类型或窗口生命周期。

## 顶部关闭入口检查（2026-10-20）

正式 `UIRoot.prefab` 的背包、角色属性、技能三个顶部Button原来存在但持久监听为空，现补绑定各自窗口的 `CloseWindow`。背包与角色属性分别重写关闭：先调用基类隐藏，再走原 `CameraMgr.KnapsackWindowAngle(this)`、`RoleAttrWindowAngle(this)` 的关闭分支，避免界面隐藏后镜头仍保留打开状态。四个功能入口仅取消自动收起，初始化收起及箭头手动切换保持。

| 界面 | 检查结论与处理 |
| --- | --- |
| 背包 `KnapsackWindow/BG/Btn_Close` | 补持久关闭绑定；隐藏后同步原背包镜头状态 |
| 角色 `RoleAttriibuteWindow/btn_close` | 补持久关闭绑定；隐藏后沿原分支恢复角色镜头 |
| 技能 `SkillInfoWindow/pan_right/Image/tex_close` | 补基类 `CloseWindow` 持久绑定 |
| 设置、NPC对话、NPC商城 | 已绑定 `CloseWindow`，保持原监听 |
| 服务器列表 | 已绑定 `OnCloseBtnClicked`，经 `CloseRequested`、Loginview注册及LoginCtrl返回当前服务器窗口；保持原流程 |
| 注册 | 已绑定返回按钮 `OnBackBtnClicked`，经 `BackRequested` 及登录控制器返回登录窗口；保持原流程 |
| 购买确认弹窗 | 已绑定 `CloseDialog`，走原销毁及管理器回调流程；保持原流程 |
| 登录、当前服务器、创建/选择角色 | 无顶部关闭设计的流程视图，不新增退出行为 |
| 底部角色技能HUD、小地图、加载、文字/物品提示与Widget | 常驻展示或非独立关闭窗口，不新增关闭按钮 |
| 遗弃背包/聊天、设置内未接业务的静态按钮 | 不属于正式顶部关闭缺口，保留原资源与范围 |

本轮仅修改 `MainCtrl.cs`、`KnapsackWindow.cs`、`RoleAttriibuteWindow.cs` 及 `UIRoot.prefab`。开发与独立完整281源编译均0错误、38既有警告；独立18项审查通过，5530对象无增删，Root只有三个Button的 `m_OnClick` 变化、5527块逐字保持。

原生在失活父节点下克隆六个正式主城窗口，六个持久监听均正确指向克隆窗口，调用后均隐藏，源缺脚本0。只将临时克隆监听改为EditorAndRuntime，不改正式资源；相机单例临时指向PreviewScene失活组件并在finally恢复，PreviewScene已关闭。当前主城Play尚未加载两个新override，所以该结果只证明资源接线与既有隐藏调用，不能证明新镜头逻辑已在原生运行。

另外逐字提取14段生产方法，28项菜单与镜头状态专项通过，覆盖四入口开关后菜单保持展开、顶部关闭后原镜头状态与800/460偏移切换、恢复保存的轨道/朝向/纵轴参数、B/I重开及箭头手动收起。Unity对象、窗口数据刷新及补间渲染为薄边界，不代表正式主城画面或包端测试。证据 `_temp/ui-close-menu-stay-20261020/client`，日志 `项目修改日志/2026-10-20-UI-CLOSE-MENU-STAY-CLIENT.md`。本轮不启停Play、不保存场景、不构建或发布；新代码正常编译并重新加载UIRoot后再验正式点击及镜头画面。

## 图标来源

### 当前：RO原生CommonIcon功能图标

用户随后明确要求“使用RO游戏的图标”。当前四入口直接引用项目已有 `LYF_MMO/Assets/artres/Resources/UI/Atlas/CommonIcon.png`，该PNG及meta与RO参考项目 `F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/artres/Resources/UI/Atlas/CommonIcon.png`、`.meta`逐字一致。

| 功能 | RO原生Sprite名称 | fileID | 原Sprite像素尺寸 |
| --- | --- | --- | --- |
| 背包 | `UI_Commonicon_Icon_top_Bag.png` | 21300682 | 19×22 |
| 角色 | `UI_Commonicon_Icon_top_Role.png` | 21300664 | 23×19 |
| 技能 | `UI_Commonicon_Icon_top_Skil.png` | 21300668 | 25×20 |
| 设置 | `UI_Commonicon_Icon_top_Setting.png` | 21300666 | 20×21 |

四图GUID均为 `6cc2d0922412f38498c6300249ed9e82`，Image保持白色RGBA(1,1,1,1)，仅开启保持比例，原58×44图案矩形、入口/面板尺寸和业务交互不变。没有重新裁图、重绘、Tint改色或新增整图集。旧 `UISprites/MainMenu` 四裁图按保留要求继续存在，当前菜单已无其引用。

RO最新版 `MainButtonTemplate.showButton` 实际通过 `CommonUIFunc.GetMainPanelAtlasAndIconByFuncId` 将配置名追加 `_Main.png` 并从 `IconMain` 动态加载。该图集本项目未引入；本轮采用同样来自RO、且本项目已存在的CommonIcon对应功能图标，不能将其描述为RO最新IconMain动态图标。

上一阶段RO入口图标替换仅改UIRoot的四个Image块，各改Sprite引用及保持比例，共8字段；其余5526块逐字保持。该阶段原生展开预览见 `_temp/ro-main-menu-icons-20261020/client/preview-expanded.png`，400×190局部见 `preview-menu-crop.png`。原生读到四个精确Sprite名、保持比例true及白色Tint，正式源菜单缺脚本0。该阶段未改源码，未重复业务编译，未新增发布。日志见 `项目修改日志/2026-10-20-RO-MAIN-MENU-ICONS-CLIENT.md`。

上一阶段RO入口图标替换验收末态是Boot非Play、运行新方法存在，未主动启停Play。Boot的dirty标志为true；当时初始state资源未提供dirty字段，无法确认何时变脏，因此没有擅自清标志或保存场景。已冻结的主城场景磁盘SHA保持，Boot仅记录该阶段末态磁盘SHA，不宣称具有前后对照。隔离预览临时移除克隆业务组件并手动切展示，不声称正式点击验收。

### 当前开关：RO收展箭头与透明点击底板

开关复用已有 `LYF_MMO/Assets/artres/Resources/UI/Atlas/Common.png` 中的 `UI_Common_Shouzhanjiantou_03.png`，GUID为 `22781455b3685ea4bae5d7ef77d885ea`、fileID为21300460、源Sprite为20×14。PNG及meta与RO原件逐字一致；原图朝下，展开仅绕Z轴旋转180度。

原Label节点直接改为Arrow Image，保留对象身份，不新增对象。透明底板仍由原Button的Image承接射线，alpha=0、raycastTarget=true；箭头raycastTarget=false，不拦截父按钮。箭头开发阶段只修改开关，四入口图标、按钮底板、入口组背景和设置正文当时保持；后续组背景透明按下一段单独实施。

### 最新：展开组与四入口底板透明

用户截图进一步要求消除展开组深色背景。仅将FunctionMenuContent及KnapsackButton、RoleButton、SkillButton、SettingButton的五个背景Image颜色alpha归零，RGB、Sprite、尺寸、射线及Selectable设置均保持；四个图标、中文、箭头及代码未改。

此阶段5530资源对象无增删、5525块逐字保持，精确差分只有上述五个颜色alpha。原生一次展开隔离图目检无深色块，四入口Image仍raycastTarget=true，内容组背景保持raycastTarget=false。末图 `_temp/main-menu-content-transparent-20261020/client/preview-expanded-crop.png`，日志 `项目修改日志/2026-10-20-MAIN-MENU-CONTENT-TRANSPARENT-CLIENT.md`。纯资源修改不重复编译；预览是克隆布局，未新增正式业务点击验收或发布。

本次开发完整281源编译0错误、38既有警告。正式Root5530对象无增删，仅5块必要修改、5525块逐字保持；MainView仅局部替换字段、注释及两条角度赋值，前后CRLF90处、LF125处一致，无关代码保持。

原生两态预览的 `GraphicRaycaster` 在108×38点击区的中心、近左上及近右下均命中开关，两态共6项为true；透明底板可点击，源默认收起、缺脚本0。两张局部图在 `_temp/main-menu-arrow-20261020/client/preview-expanded-crop.png`、`preview-collapsed-crop.png`。隔离克隆直接切显隐和箭头角度，没有正式主城业务点击或包端测试。本次原生主城Play、运行程序集未含新箭头字段，未强制停止或刷新；主城dirty在测试前后均false，未保存任何正式场景。日志见 `项目修改日志/2026-10-20-MAIN-MENU-ARROW-CLIENT.md`。

### 历史：首次一梦江湖风格裁图（已被RO图标替代）

图标来自项目已有 `LYF_MMO/Assets/GM_Game/UISprites/ExtractedMenuUi`。原素材保留不变；裁出图案并交给现有字体显示中文，避免携带原素材的不同功能名称。

| 新图标 | 原图 | 左、上、右、下裁切坐标 |
| --- | --- | --- |
| `MainMenu/Menu_Knapsack.png` | `menu-r3-c1.png`，包裹 | 19、1、68、39 |
| `MainMenu/Menu_Role.png` | `menu-r1-c1.png`，人物 | 13、6、74、48 |
| `MainMenu/Menu_Skill.png` | `menu-r4-c2.png`，双剑 | 21、3、83、43 |
| `MainMenu/Menu_Setting.png` | `menu-r2-c7.png`，齿轮 | 26、1、89、44 |

坐标以原图左上为原点，右、下不包含边界像素。仅在新裁图去除原图边缘的3个红色像素和22个黄色像素；未重绘图案。四图使用透明PNG、无Mipmap、默认不压缩导入。完整原图GUID/SHA、新图GUID/SHA和裁切信息留在 `_temp/main-menu-integration-20261020/client/icon-sources.json`。

入口组和四入口底板保留对 `Assets/artres/Resources/UI/Atlas/Common1.png` 的 `UI_Common_Bg_10Fillet_01.png` 引用，GUID `c46212db0fcbdd547a2a319e0ab42aba`、fileID `21300168`，现均alpha=0；开关底板也透明。入口中文复用项目现有字体，未新增字体或外部素材。

## 首次菜单集成的历史验证与边界

开发完整281源Rebuild为0错误、38条既有警告。正式Root按YAML对象精确修改，保留原5472对象，仅7个既有对象作必要修改、增加58个对象；原设置窗口及其内容未改。

Unity原生只导入本轮四图和Root，不刷新脚本、不保存场景。在PreviewScene内克隆正式菜单、底部HUD与小地图，移除克隆的业务组件后渲染1920×1080展开、收起两图；图标、中文、位置及不重叠已目检。正式菜单原生检查16个Transform、5个Button、0缺脚本。

首次菜单集成验收时用户在主城Play，反射显示运行中的MainView/MainCtrl没有当时的新方法，因此没有声称真实Play点击已通过。该阶段预览仅改变克隆容器显隐和提示文字，没有替代正式控制器做现场业务验收。正常重新加载对应脚本及UIRoot后，需从正式主城点击开关及四入口验收。该阶段末Console错误/警告0，主城dirty=False，PreviewScene已关闭；没有构建、发布或启停Play。

独立末稿281源编译0错误、38既有警告、无新增警告签名，24项审计通过；设置子树2304对象逐字保持，主另核12正式路径、两方281编译源SHA与10保护输入一致，汇总15项通过。证据 `_temp/main-menu-integration-20261020/root-final-validation.json`，预览裁图 `_temp/main-menu-integration-20261020/menu-preview.png`。

修改记录见 `项目修改日志/2026-10-20-MAIN-MENU-CHANGELOG.md`、`项目修改日志/2026-10-20-MAIN-MENU-CLIENT.md`，独立结论见 `项目修改日志/2026-10-20-MAIN-MENU-REVIEW.md`。正常结束当前Play、等待脚本编译并重新加载UIRoot后进行正式点击验收；包端需按原流程配套更新热更代码和资源。
