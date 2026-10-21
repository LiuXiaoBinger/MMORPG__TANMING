# 参考RO的运行导航接入

日期：2026-10-21。源码、完整编译与独立专项通过；正式Unity/Player运行尚未验收。

## 2026-10-21补充：专用测试与NPC初始化

正式仍以NPC/任务业务触发，点地测试现可在Play中通过“工具/导航/测试/启用点地导航（仅本次运行）”临时开启，退出Play自动清理。主城原来未调用NpcManager.Initialize，现由MainCityMgr.Start接入已有管理器，配置中有一个QuQu。测试步骤、累计逐项文件清单与当前验证边界统一见`项目分析/导航代码文件清单与测试说明.md`；本条更新下文“当前没有测试入口”的历史状态。

## 实测返工后的当前入口与查询

用户已明确游戏不需要点击地面导航。正式TouchObject已移除ClickGround派发，保留原角色/敌人选择；新增NPC点击沿现有MNavigationEventArgs追近原NpcEntity，到原三维10米范围后调用NpcCtrl.OpenTalk。窗口仍由原控制器打开，不新增UI生命周期或任务系统。任务以后可用已有显式目的点事件，当前没有任务按钮或测试入口。

真实位移无动画的根因是本地主角默认动作只认HasDirectionalInput且IsMoving，导航刻意不伪装按键，因此仍选Idle。现在默认动作在原IsMoving成立时同时识别有效导航，沿原Walk/Run及普通/战斗槽位播放。方向输入、施法和跳跃仍走原链。

独立复现还发现，RO原预测跨多路点接口接入本项目单次CharacterController.Move后，会把折线合成跨角弦；碰撞未抵达时索引又提前消费。当前仅用实际位置消费，局部扫描同向共线门户以保持速度，单次位移止于真实转角。未修改MMove提交入口、原技能发送尾段或增加卡住重试。

截图区域已通过静态Prefab父子变换匹配中央台阶(60.61,18.02,25.27)、蓝屋顶(80.967,18.659,39.813)等地标，再用09:29:25历史上报位置(77.05026245,18.73891640,73.20010376)扫描附近361个候选。候选终点是估计网格，不能当用户精确射线位置。道路X68、Z41→39的8面/7条门户全部同高18.7061958且完全共享，真实2米直路却因长细三角重心左右摆动累计20.0648米费用，促使旧查询绕楼。

现PathFind沿RO运行版rogamelibs/thirdparty/recast/DetourNavMeshQuery.cpp约1119～1164流程：首次访问邻面，把当前节点水平投到共享门户，比例夹在0.1～0.9，保存_positions节点位置，按节点间三维距离计g、到终点距离乘0.999计h，并在终面追加末段。重心保留为接缝几何/门户方向用途，不能再用于路径计价。此为自有三角查询沿RO职责修正，仍非完整Detour移植，不承诺所有路线全局最短。

临时同输入对照361候选的路径/直距大于1.4者234→0；正式末稿随后交独立重播同区候选和既有查询，不以随机路径落网覆盖代替路线合理性。地图、烘焙、碰撞配置和接缝规则保持；角色台阶0.3米/烘焙爬高0.75米虽有差异，该段同高边已连通，不能据此归因本次绕楼。

本轮实际修改TouchObject.cs、NpcCtrl.cs、MDefaultActionComponent.cs、MNavigationComponent.cs、MNavigation.cs，以及MNavigationTriangle.cs的Center一行用途注释。双方末稿290源编译0错误/38条既有警告，Common真实73输入_temp副本0错4旧警告；主核1720冻结1714保持、全部编译输入与末稿一致。独立49综合/18移动/2380查询及宏观道路前后各23项通过，200路线45844段覆盖0失败。2380和修前2090是同夹具随路点数量变化，不累加。最终边界以Docs/AI/TEST_REPORT.md顶部为准。证据_temp/navigation-business-fix-20261021；日志项目修改日志/2026-10-21-NAVIGATION-BUSINESS-FIX-CHANGELOG.md。

实际使用：让Unity正常编译重载，沿Boot/登录进入主城，在Game窗口点击NPC；角色接近时应播走跑，到达开原对话，WASD接管则取消该次到达。点击空地不移动。当前无Unity MCP，未操作Play或保存场景，未把薄边界专项当真实动画/物理实测。下文初版点地入口及验证数字均为历史实现，以上述当前入口及本轮记录为准。

## 需求与范围

用户要求开始实现导航，类名、函数名、变量名尽量与RO一致，并由子Agent开发及检查。参考源码根目录为`F:/UnityPro/ro/Ro_cs422nb`，既有解析为`项目分析/RO_PATHFINDING_ANALYSIS.md`。

沿RO职责分离：`MNavigationComponent`管理目标、路径消费和到达，`MNavigationMgr`适配地图查询，`MMoveComponent`继续负责每帧位移与现有碰撞/跳跃/同步。底层使用已经导出的公共三角网，纯C#查询同时编入Unity和.NET4.8公共工程；不会将自有格式冒称为RO Detour格式。

当前可操作闭环为NPC点击导航与方向键中断；初版点地入口已按用户实测后的产品要求移除。当前正式技能全部无目标模式，保留原释放语义。协议、服务端移动权威、动态障碍、跨图自动导航及重新烘焙不在本轮范围。

## 实施与验收

navigation_query_dev开发公共查询，navigation_client_dev完成客户端接入，navigation_review独立审查，主Agent核末稿、保护文件与记录。证据统一`_temp/navigation-runtime-20261021`，末稿独立结论为PASS。

主城拓扑调查发现：270735个顶点仅130227个完全相同XYZ的唯一位置，按几何边统计有129853条单侧边、121039条双侧边。开发与独立审查确认，仅精确坐标焊接仍产生4942个连通分量，最大8582面；实际分块边界出现约0.000015米坐标差，不能把这些边当成互不相连的岛。当前查询采用0.001米内共线且正长度重叠、双端高度匹配、两侧三角中心分居边两侧的最小接缝连接。末稿1459无向接缝、244996有向连接，最大连通40682面、共4304分量；不凭分量数量判定其他区域为烘焙故障，不修改或删原面。

路径在三角图上A*搜索，沿走廊用漏斗提取转折，再输出穿越共享边的交点及边上插值高度，尽量对应RO的ALL_CROSSINGS路点语义。底层不是移植Detour库；不可达返回语义和查询限制需以末稿明确。

客户端采用`StreamingAssets/NavigationData/<场景名>.navmesh.bytes`随Windows包部署；复制自项目根公共文件并核字节一致，不用开发机绝对路径。场景成功加载时读取。原导出工具保持，后续重导应同步部署文件，当前不新增发布框架。

独立审查发现客户端首稿到达判断遗漏RO高度限制，已打回恢复`SELECT_TARGET_DISTANCE_HEIGHT=6.0f`（参考`rogamelibs/rocommongamelibs/utility/common_define.h:12`）。专项以7米高度差复现和复验；RO在6米以内仍按水平距离到达，本轮保留其语义，不声称任何高度差均阻止到达。共享查询的多高度拓扑隔离与业务层的到达范围是不同验证项。

生命周期接入调查还发现，现有`MEntity.LateUpdate`没有调用基类组件分发，仅覆写导航组件的`LateUpdate`不会进入正式更新链。最小适配在导航组件`Update(int)`中调用同名帧后逻辑，使用现有容器中移动先于导航的执行顺序；不改实体通用生命周期或其他组件。独立7段逐字生产方法动态专项10项通过，真实顺序为`MEntity.Update → special Move → MSceneObject → MObject → 普通容器 → Nav.Update`，导航到达在模型更新前执行。

## RO命名与必要差异

| 职责 | 本项目沿用的RO名称 | 必要接入差异 |
| --- | --- | --- |
| NPC输入 | `TouchObject.JudegeType`、`ClickNpc` | 当前Input System、UI过滤与原实体选择保持；点地入口移除，NPC走现有目标事件与OpenTalk |
| 导航参数 | `MNavigationEventArgs`、`Pos`、`NeedTarget`、`Target`、`UpperRange`、`LowerRange`、`CustomRange`、`UseAfterAction`、`NavAction` | `NavAction`保留现有强类型技能载荷，不复制RO的object参数数组 |
| 导航流程 | `MNavigationComponent.OnGetEvent`、`navigate`、`navToPos`、`navToTarget`、`calcTargetPos`、`TryGetNextStep`、`onCloseTarget`、`closeTargetAfter`、`OnAfterAction`、`Interrupt` | 现有正式无目标技能继续立即进入原施法链；显式目标导航才追近 |
| 路线状态 | `_pathPoints`、`_currentPathIdx`、`_isNavigation`、`_destination`、`_tarPosition`、`_target`、`_naviWithTarget`、`_upperRange`、`_lowerRange`、`_customRange`、`_afterAction`、`_useAfterAction` | 目标引用当前实体世界的原对象，技能复制件在到达或中断后回收 |
| 查询适配 | `MNavigationMgr.singleton`、`OnSceneLoaded`、`TryGetPath`、`TrySetHeight`、`TryRaycastTerrain` | 读取公共文件，不经SWIG/C++；保留`_cppNav`名方便对照，其类型实际为纯C#查询器 |
| 网格查询 | `MNavigation.PathFind`、`TrySetHeight` | 自有三角邻接+A*，完整可达才成功，不提供RO底层的部分路径成功语义 |
| 位移提交 | `MMoveComponent.PlayerUpdate` | 路点只贡献水平位移，继续合并原重力、跳跃、技能根位移并走一次既有碰撞入口 |

普通路径查询的最近面半盒沿RO为XYZ `(4,8,4)`米，既有网格已按角色规格烘焙，不再缩边。路径失败中断并释放动作，不能把未到达当到达。未移植RO的自动战斗、NPC/跨图编排、动态障碍及空气墙体系。

## 正式文件与地图更新

新增共享查询`MNavigation.cs`、`MNavigationTriangle.cs`、`MNavigationPortal.cs`、`MNavigationEdge.cs`、`MNavigationPointComparer.cs`，均在`LYF_MMO/Assets/GM_Game/Scripts/Navigation/`，由`LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj`同源链接。Unity适配器`MNavigationMgr.cs`仅编入客户端，不链接服务端。

修改现有`MNavigationComponent.cs`、`MMoveComponent.cs`、`MNavigationEventArgs.cs`、`TouchObject.cs`、`SceneMgr.cs`，沿已有事件、输入、普通组件和场景成功回调接入。既有`OnAfterAction`发送/预表现尾段逐字保留。

当前主城正式副本为`LYF_MMO/Assets/StreamingAssets/NavigationData/Scene_MainCity.navmesh.bytes`，与`NavigationData/Scene_MainCity.navmesh.bytes`逐字一致，SHA256为`4659a2c35d5fdbbdd0dbb084321328408f560d5fd239b084b3dfbc7cd3984b84`。以后地图烘焙并从原工具重导后，将对应公共文件同步至StreamingAssets同名文件；重新进入该场景时读取。当前同步是明确的人工制作步骤，不自动新增发布框架。

## 末稿验证与实际使用边界

| 验证 | 结果 | 边界 |
| --- | --- | --- |
| 完整客户端290源 | 开发/独立0错误、38既有警告 | 使用当前Unity引用的离线编译，非Editor原生加载 |
| 真实Common.csproj | 0错误、4既有警告 | 产物留_temp，未替正式DLL |
| 独立查询2090项 | 绕障、断岛、上下层、坡面、接缝和段落面采样通过；真实主城12接缝/9长路线通过 | 不代表所有地图点可达 |
| 完整客户端导航32项 | 首末帧、动作标志、事件池、到达/中断、切图与RO7米高度回归通过 | Unity物理/网络为替代边界 |
| 真实更新链10项 | 7段生产方法逐字相同，移动先于导航 | 动态隔离执行，不是Unity真实帧 |
| Move/Touch26项 | 独立重编复跑通过，7方法逐字核源 | 复用开发夹具，不称独立设计 |
| 正式出生配置附近查询 | 投影与X+10/Z+10/X-8米三路通过，3/6/12路点 | 未读取当前运行主角位置 |
| 主最终保护审计 | 1941冻结1935保持，仅6授权旧文件变化；21正式路径与末稿编译SHA一致 | 不覆盖项目任意外部并发操作 |

主城开发临时.NET单次构建658毫秒、五长路线首26/后4～14毫秒；独立另一组构建约863毫秒、9长路线2～8毫秒。前后并非严格同一输入基准，不能写Unity帧率收益。路径定位按面AABB筛候选后精算，避免首稿每次全量复杂投影的64～129毫秒成本。

源码验收通过后，应正常让Unity完成脚本重载，再从既有登录/场景流程进入主城，验证地面点击、道路拐角、坡地贴地、到达停步、方向键接管及联机位置同步。当前工具没有Unity MCP，Editor日志与GM_Game脚本DLL时间早于新源，未确认原生加载；未主动启停Play、保存场景、运行正式Player、替插件或发布。

服务器已有同源查询库，可使用`new MNavigation(MNavigationMapSerializer.Load(filePath))`及`PathFind`；本轮没有把文件加载/寻路接入游戏服运行模块或移动权威。Unity仍引用旧Common插件，未将新Common复制进Unity；后续更新插件仍须遵守共享类型唯一归属。

独立结论`_temp/navigation-runtime-20261021/review/final-review.json`，主汇总`_temp/navigation-runtime-20261021/root-final-validation.json`。修前高度复现用开发原始前缀和冻结施法尾段重建，provenance与首次夹具失败保留，不伪称完整修前快照。

修改记录：`项目修改日志/2026-10-21-RO-NAVIGATION-CHANGELOG.md`。
