# 2026-10-21 导航业务入口纠正与运行问题返工

## 用户要求与范围

用户实测点击地面导航角色移动没有动画，且容易走到死路；明确游戏不需要点地导航，只在点击NPC或后续点击任务后导航。本轮移除正式点地入口、修导航动作接入并沿既有NPC实体和对话最小接近；保留显式目的点事件供后续业务调用，不实现任务系统、自动战斗或跨图。

## 分工与文件范围

- navigation_business_fix：TouchObject、MNavigationComponent、MMoveComponent、MDefaultActionComponent的必要改动，现有NPC对话接入需先核接口；不改通用UI管理或技能发送流程。
- navigation_deadend_diagnosis：Navigation目录查询与Mgr只在复现故障后修改，不猜修网格、不重烘焙、不修改地图/预制体。
- navigation_business_review：独立冻结、审查、末稿编译及生产方法回归，不改业务代码。
- 主Agent维护Docs/AI四份入口、本日志及项目分析下对应专题/难点，协调范围及最终验收。

最终实际修改以下6个文件（均从项目根开始）：

| 文件 | 最小变更 |
| --- | --- |
| LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/TouchObject.cs | 删除ClickGround正式派发，增加NPC目标点击；10像素拖动限制仅用于NPC，原角色选择保持 |
| LYF_MMO/Assets/GM_Game/Scripts/controller/RoleController/NpcCtrl.cs | 提取原10米TalkDistance供导航共用，补OpenTalk注释；窗口沿原控制器 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs | 原IsMoving前提下承认导航为移动来源，沿原Walk/Run槽位 |
| LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MNavigationComponent.cs | 原NPC三维距离到达后开对话；按实际位置消费路点、同向共线可连续、单次位移止于转角 |
| LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigation.cs | 沿RO运行版门户进入位置计算A*费用，新增每面节点位置缓存，保原邻接/投影/漏斗 |
| LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationTriangle.cs | 仅Center字段一行中文注释改为接缝/门户几何用途 |

MMoveComponent、MJumpComponent、PlayerInputCtr及原OnAfterAction尾段逐字保持。无新增业务类型或正式测试入口，协议、两份bytes、场景、烘焙、碰撞参数不改。全部中间文件位于_temp/navigation-business-fix-20261021，不覆盖正式DLL、bin或启动服务。

文档修改：Docs/AI/PROJECT_CONTEXT.md、CURRENT_TASK.md、DECISIONS.md、TEST_REPORT.md，本日志，项目分析/RO_NAVIGATION_IMPLEMENTATION.md、项目分析/README.md及项目分析/项目难点.md。修改记录与专题互相按项目根相对路径引用。

## 已知事实与设计约束

默认动作本地主角的ShouldPlayMovementAnimation要求HasDirectionalInput且IsMoving，导航没有方向按键因此仍选择Idle。最小修正应承认当前导航为移动来源，保留原走跑槽位及技能、跳跃优先级，不把导航伪装为按键。

现有NpcEntity/NpcCtrl/OpenTalk可复用，NpcEntity.Position为隐藏的配置字符串字段，世界坐标必须通过MEntity读取。NPC到达不得新建UI生命周期或任务框架。

初期排查同时核碰撞前消耗路点、一帧跨多转角却只直线提交，以及角色台阶0.3米与现有烘焙爬高0.75米差异。规格不同只是事实；随后已证截图同区道路连续同高，不能据规格差异认定本次绕楼根因。

用户随后提供截图并说明“点击红色，按照绿色那边移动”：中央道路尽头台阶脚为目标，实际绕向左侧树林和蓝屋顶北侧。原图备份_temp/navigation-business-fix-20261021/user/incorrect-stair-route.png，随后按真实地标和历史位置定位同区候选，证据及修正见下文，不能将其他路线的测试当精确用户点击回放。

独立冻结完整导航组件已复现转角消费问题：L路线(0,0)→(0.1,0)→(0.1,1)单帧0.3米预算被合成跨角直线；实际碰撞仅到(0.05,0)时下一帧丢失首角。查询Agent还从真实主城提取面47073→122057的路线，18个转角中0.3米跨角弦离开实际网格水平覆盖。上述证明消费有缺陷，仍不直接解释用户截图的大绕行。

## 截图区域绕楼根因与修正

只读解析669份嵌套Prefab父子变换，匹配中央台阶(60.61,18.02,25.27)、蓝屋顶(80.967,18.659,39.813)和两排柱。按实际协议解析621份历史移动上报，取09:29:25位置(77.05026245,18.73891640,73.20010376)，扫描同区361候选。终点为区域估计网格而非用户精确屏幕射线，不能宣称精确点击回放。

道路X68的Z41→39切线经过8面，7条邻接均为原共享边、Y18.7061958完全同高，实际2米直路被重心间距计成20.0648米；三角重心在横向左右摆动，旧A*因此选择绕楼。该段“缺缝”和“目标吸屋顶”候选原因被同高直接邻接及地面投影证据排除，未焊接或重烘焙。

对照RO运行版rogamelibs/thirdparty/recast/DetourNavMeshQuery.cpp约1119～1164，在首次访问邻面时将当前节点水平投到门户，比例夹0.1～0.9，按保存的节点位置计g/h及终面末段；启发系数沿原0.999。先临时副本对照，再落正式两查询源；三角重心仍用于接缝两侧/门户排序。未设计新导航框架，不声称自有库是完整Detour。

同一输入候选的路线水平路程对比（单位米）：

| 候选终点XZ | 修前 | 修后 |
| --- | ---: | ---: |
| 68 / 33 | 77.4351 | 41.8481 |
| 68 / 39 | 57.0793 | 35.9979 |

两条路线对应三维折线长分别79.1048→43.4029和58.0784→36.7542米，与水平路程分列，不混口径。361候选前后可达数均354，其中水平路程/直距大于1.4者由234变0；该阈值仅本轮局部观察，不作为全图最短或物理可达保证。正式场景、导航bytes和角色碰撞配置保持。

## 验证与未解决

主复核独立1720路径，1714保持，仅上述6源变化，双方290实际源输入SHA均匹配当前末稿；Common隔离副本73份输入也逐份匹配。两份地图SHA仍4659a2c3…3984b84，场景/预制体保持。root-final-validation全部检查通过。

- 开发最终client/final-with-query和独立review/final：290实际源各0错误、38条既有警告。Common以真实工程73份输入绝对化后构建_temp隔离副本，0错误、4条既有警告；未直接运行正式路径工程。
- 独立完整Nav/Mgr/Touch/NpcCtrl、真实事件池及逐字提取默认动作选态49项通过：动画切入/持续/走跑、到达待机、NPC三维10米与10.005米边界、换NPC、中断/断岛不开对话、空地/UI/拖动与原角色选择、无目标技能保持。
- 独立重提取MMove等7个生产方法18项通过，包含方向接管、原技能根位移/跳跃及单次碰撞提交；修前冻结Nav与动画方法3项重播证明缺陷。薄边界替代Unity物理/Animator/设备/网络，非正式Player运行。
- 正式查询末稿同夹具2380断言通过，初版2090因真实路点数量变化变为2380，不累加；200条真实主城路线45844段均在实际A*走廊，水平边界容差2毫米/高度容差2厘米。该覆盖检查不能单独证明路线合理，所以另跑宏观道路前后回归。
- 独立宏观道路最终23项通过，用冻结原查询复现同高2米误费及绕楼，再以当前查询核同候选缩短且不越到屋外侧。具体日志review/macro-before/tests.log、macro-final/tests.log；完整结论review/final-review.json。
- 首轮客户端漏必要using报1错，已补并保留失败日志，正式末稿两方重编通过。未清理_temp，也未把前期或临时副本输出当正式末稿证据。

当前代码修正及隔离回归完成，尚需用户正常重载后在Unity主城实测NPC点击、走跑、到达对话、WASD接管和实际碰撞/联机。工具无Unity MCP，未自动启停Play、保存场景或替正式DLL。截图精确世界终点未取得，修的是已独立复现的同区重心误费和路点消费缺陷，不宣称全图死路全部消失。

主要证据：root-final-validation.json；client/development-report.md、final-with-query/combined-audit-result.json；review/final-review.json、final-audit.json及各末稿编译/专项；query/final-query-report.json、local-comparison.json、local-before-after.png和road-sections-x68.json，均位于本轮_temp。

本轮复杂问题持续记录在项目分析/项目难点.md；实现与使用边界见项目分析/RO_NAVIGATION_IMPLEMENTATION.md。文档引用均为项目根相对路径，临时证据不自动清理。
