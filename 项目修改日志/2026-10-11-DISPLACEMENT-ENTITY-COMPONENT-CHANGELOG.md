# RO 位移组件实体挂接纠正

- 状态：已完成，独立审查与测试均通过，主已核末稿证据。
- 用户纠正：RO 由实体 `_displacementComp = MComponentMgr.singleton.CreateComponent<MDisplacementComponent>(this)` 挂接组件，先前将驱动由 MMoveComponent 构造并持有偏离该结构。本次按实体组件结构改正。
- 分工：displacement_entity_develop 修改业务，displacement_entity_review 独立检查，主负责范围、验收和正式记录；失败交开发返工。

## 范围与设计

最终业务范围六份源码：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEntity.cs`、同目录 `Component/MDisplacementComponent.cs`、`Component/MMoveComponent.cs`、`Skill/MArtSkill.cs`、`Component/base/EntityComponentContainer.cs`，以及 `LYF_Server/MMoRpgCommon/SceneEntity/EntityComponentType.cs`。MComponentMgr、EntityComponentBase 无需修改。实际客户端引用 `LYF_MMO/Assets/GM_Game/Plugins/Common/MMoRpgCommon.dll`，与 `Plugins/Net/MMoRpgCommon.dll` 两份已同步为同次共享源码构建产物；两meta/GUID保持，不新增PDB。共八个业务路径，已按末稿manifest复核。

不会借用旧组件枚举伪装位移组件；追加 `Displacement=32`，旧值保持。共享文件既有namespace及程序集身份保持，只补新枚举成员，不新增namespace声明或迁移外部类型。

组件继承 EntityComponentBase，实体通过现有 MComponentMgr.CreateComponent 挂普通容器，统一初始化、卸载和池复用；移动组件不构造或拥有位移驱动。为保持当前专用 Move→Buff→Skill→Machine 顺序及末帧提交，普通容器新增明确的运动优先更新入口，仅推进挂接的位移组件，原普通更新跳过该阶段，实体在专用移动更新前调度。不在 ConsumeDelta 内偷推进，不由移动组件再次 Update 位移。

保此前绝对时钟、冻结动作时长/计时会话/施法身份、冷加载与同步换 GO、三曲线策略和碰撞一次提交；已有动画 Collector 与时间线到期职责保持。正式作者稿、Runtime、资源、场景、协议不自动修改；当前用户3007001已F13/end111/F49，禁止恢复历史F60。

分析更新到 `项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md`；四协作入口维护当前状态。中间证据统一 `_temp/displacement-entity-20261011/`，未经产品经理确认不清理。

## 验收计划

独立核真实组件创建/容器查询/同帧唯一更新、完整实体 Update 调度、真实池释放重绑和停止；重跑受影响的63运动及92生命周期，不复用旧155数宣称本轮通过。编译真实客户端与共享程序集、检查新枚举旧值兼容、必要DLL一致性、中文注释和禁止语法、当前资产保护；未改的导出链按输入SHA说明原证据适用，不无理由重做全部原生导出。

## 最终修改与验收

- MEntity.cs:268 在 OnBeforeInitialize 的 Created 阶段通过真实 CreateComponent 挂接；DisplacementComp 与容器查询返回同一实例。实体 Update:285 先调用 Components.UpdateBeforeMovement，销毁及复用准备清快捷引用。
- MDisplacementComponent 继承 EntityComponentBase，构造接受 ROObject 宿主，ComponentType 独立为32；Initialize、Shutdown、OnDetachFromHost 清旧运动。候选直接卸载原等后续更新清理，主审要求立即 Stop，开发补正并通过新增卸载/重挂回归。
- MMoveComponent 删除位移对象的创建、持有、更新及回收，只从 Entity.DisplacementComp 消费一次差量，与输入、重力、动画根合并一次碰撞提交；侧碰撞停止实体组件。MArtSkill 的 charge/StopCharge 改用实体组件，FireEvents/MTimerMgr/OnProcessTimer 职责保持。
- EntityComponentContainer:119 的优先阶段按类型键查询已挂位移并推进，普通阶段跳过该类型；未引入每帧临时集合或闭包，保专用 Move 先于 Skill 自然结束，末帧先提交后离槽。
- 独立189项运行断言通过：63运动、92本轮重新执行的生命周期、34新增真实实体装配/调度/回收。直接链接45完整生产文件，包括实体继承链、管理器、组件基类、容器及真实技能/计时器/模型/移动/工厂/池/Collector；状态机与Unity原生、配置及资源为薄边界。
- 独立271份GM_Game源码 Rebuild 零错误/37既有警告；65份共享源码 Rebuild 零错误/4既有警告。共享哈希66项为65源码加1项目文件。人工逐声明中文注释与禁止语法检查通过；客户端无namespace，共享保持既有外部类型身份。
- 2796前置保护路径中2789不变，7授权变化为五客户端源及两DLL，无新增、删除或越界；共享枚举没有事前字节快照，单列该限制。原工具完整文本与可靠旧DLL反射共同核实旧0–31和导出类型保持，仅新增Displacement=32。两正式DLL字节相同且等于开发同次产物；独立构建按契约核验，不要求因构建元数据不同的DLL字节相等。
- 主重算开发8末稿、336编译源、独立8末稿、271客户端输入、66共享输入及45运行输入，共734项SHA零差异，root-manifest-check.json=PASS。两独立报告及final-audit.json已读取。
- 本轮没有新跑原生导出；旧170输入已有8处后续授权差异，不将旧64项当本轮当前输入验收。当前F13/end111/F49作者稿及Runtime、资源、meta/GUID、场景保持，不自动发布。

## 证据、未解决问题与交接

独立证据位于 `_temp/displacement-entity-20261011/review/`：independent-review.md、independent-test-report.md、final-audit.json、runtime-final.log、运行/客户端/共享输入SHA、shared-assembly-contract.json、previous-native-input-comparison.json；开发证据在同任务 develop/，主重核为 root-manifest-check.json。夹具XML编码、数学接口、重复碰撞组件和断言类型等失败仅修_temp，日志保留。

无剩余已复现代码阻断。真实Unity PlayMode、角色画面、设备听感、原生墙体及Animator精确末帧/跨窗、服务器与Bundle尚未现场验证。下一步使用原正式技能入口体验；不继续扩大已通过测试，不自动改资产或挂正式测试组件。临时证据保留，待产品经理确认清理。分析见 `项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md`，交接与验收见 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。
