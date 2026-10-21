# 2026-10-21 参考RO实现运行导航

## 用户要求与分工

开始写导航代码，类、函数和变量尽量跟RO一致，子Agent负责开发和代码检查。navigation_query_dev实现公共查询，navigation_client_dev接入客户端，navigation_review独立审查，主Agent协调验收和维护记录。

## 正式修改文件

本轮21条正式代码/配置/数据路径，既有6条修改、15条新增：

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MNavigationComponent.cs`：RO同名导航流程、跨帧路线/技能动作、到达与中断，保持原施法尾段。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`：PlayerUpdate消费路线、方向输入中断及必要SetMoving访问适配，保单次碰撞合并。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/MNavigationEventArgs.cs`：沿RO字段表达位置/目标/范围/动作，强类型复制与回收。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/TouchObject.cs`：地面点击入口，保UI、拖动和实体选择语义。
- `LYF_MMO/Assets/GM_Game/Scripts/Manger/SceneMgr.cs`：成功加载场景时调用导航地图加载。
- `LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj`：仅增5个公共查询源的Compile/Link，不复制代码或替换插件。
- `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigation.cs`及meta：纯C#最近面、邻接/接缝、A*、漏斗/门户高度交点。
- 同目录`MNavigationTriangle.cs`、`MNavigationPortal.cs`、`MNavigationEdge.cs`、`MNavigationPointComparer.cs`及各meta：必要强类型查询缓存和边/坐标比较。
- 同目录`MNavigationMgr.cs`及meta：沿RO入口读Windows随包数据并转换Unity/公共坐标；Geometry层地形射线。
- `LYF_MMO/Assets/StreamingAssets/NavigationData.meta`、`LYF_MMO/Assets/StreamingAssets/NavigationData/Scene_MainCity.navmesh.bytes`及文件meta：同份公共地图正式部署副本。

协作与专题文档：`Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md`；`项目分析/RO_NAVIGATION_IMPLEMENTATION.md`、`README.md`、`RO_PATHFINDING_ANALYSIS.md`、`UNITY_NAVIGATION_EXPORT.md`、`项目难点.md`；以及本日志。原三份共享格式源、根导航数据、场景、烘焙、Editor预览、协议和插件保持。

## 实现内容与设计取舍

沿RO的OnGetEvent/navigate/navToPos/navToTarget/TryGetPath/TryGetNextStep/onCloseTarget/Interrupt职责和字段，复用现有公共网格及移动组件，不另建角色更新/碰撞入口。当前无目标技能立即释放语义保持，显式NeedTarget导航才追近；NavAction为强类型技能复制件，避免池化事件同步回收后跨帧引用。UseAfterAction=false不执行动作。

实际主城270735顶点仅130227唯一XYZ，精确焊接仍4942分量、最大8582面。分块有约0.000015米坐标差和长短重叠边，最小按0.001米共线重叠、双端高度及两侧面判断建立门户。末稿1459无向接缝、244996有向连接、最大40682面，共4304分量；不擅自滤除原孤岛或推断全部剩余分量根因。三角A*配漏斗和过边插值高度，保ALL_CROSSINGS风格，完整可达才成功；并非复制RO Detour底层。面AABB筛候选避免首稿复杂投影全扫成本。

Windows使用StreamingAssets/NavigationData同名文件，场景成功后读入；重导后人工同步副本，未扩发布框架。路径只贡献水平移动，原跳跃/重力/根/程序位移仍合并至现有CharacterController入口。服务端只获得同源查询库，未扩运行加载、移动权威、动态障碍或离网连接。

真实接入修正：首稿遗漏RO原6米高度约束，独立审查打回后恢复，以7米回归；不改6米内水平到达语义。另现有MEntity.LateUpdate未分发普通组件，仅覆写将无法到达；在Nav.Update沿实际Move后顺序调用同名逻辑，不改通用生命周期。专题和难点记录详见`项目分析/RO_NAVIGATION_IMPLEMENTATION.md`、`项目分析/项目难点.md`。

## 验证结果

- 开发及独立完整客户端290源编译0错误38既有警告；真实Common工程0错误4既有警告。产物均留_temp，无正式DLL覆盖。
- 独立共享查询2090项通过，包含几何夹具与真实主城12接缝/9长路线、段落面采样；完整导航32项和真实实体派发7方法链10项通过。Move/Touch26项为独立重编复跑开发夹具，7方法逐字核源，不称独立设计，阶段断言不累加。
- 正式出生配置成功投影Y18.80104，附近三个方向路线完整可达，3/6/12路点；非当前运行主角位置或实际碰撞验证。
- 1941冻结输入1935保持，仅6授权旧文件变化。主root-final-validation五项通过，21交付文件存在，双方完整编译输入及末稿SHA一致，无额外删除/变化。
- 根公共地图与StreamingAssets副本逐字一致，SHA256 `4659a2c35d5fdbbdd0dbb084321328408f560d5fd239b084b3dfbc7cd3984b84`。原OnAfterAction尾段逐字保持；新增代码规则/中文注释、同源链接、meta及限定diff检查通过。
- 独立末稿`_temp/navigation-runtime-20261021/review/final-review.json`为PASS；主汇总`_temp/navigation-runtime-20261021/root-final-validation.json`。客户端MNav末稿SHA `8884c915497f75de4daa744b23a3a1af509dab6382811719674b521d5148807f`。

失败证据保留：7米高度修前复现使用开发原始前缀+冻结尾段重建，并非完整修前快照；首次测试夹具失败和provenance留review。末次UseAfterAction修改后主首次哈希审计失败，开发和独立重编、补false专项、更新清单后再次通过。不是忽略版本差异直接交付。

临时.NET性能仅本机观察：开发构建658毫秒、五长查询首26/后4～14毫秒；独立另一组构建约863毫秒、9长查询2～8毫秒，不是严格同输入基准或Unity帧率收益。早稿64～129毫秒查询已替代，不作末稿结论。

## 未解决问题与交接

无剩余源码审查阻断；真实Unity/Player物理、画面和联机仍待验。当前未提供Unity MCP，Editor.log与Unity GM_Game.dll时间早于本轮源，不能确认新代码原生编译/加载，不引用历史Console0。薄边界测试中的物理/动画/网络替代不等于正式运行结果。

正常让Unity编译重载，再沿既有流程进入主城验证地面点击、道路拐角、坡地、到达停步、方向接管和原联机位置同步。未主动启停Play、保存场景、替Common插件、重启服务或发布。以后重导地图同步StreamingAssets副本；新Common若复制进Unity，仍须确定共享类型唯一编译归属。

本轮没有Unity MCP恢复或原生网格显示恢复结果，上轮相关待验保持独立交接。临时文件全在`_temp/navigation-runtime-20261021`，未经产品经理确认不清理。
