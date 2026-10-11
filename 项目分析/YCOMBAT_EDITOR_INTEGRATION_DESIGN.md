# YCombatEditor 融合方案（历史初审设计）

本文保留实施前的设计与审查依据，首期仅角色根粒子、兼容性待验证等描述属于当时阶段。2026-10-07 实施已扩展到五种特效定位、正式技能资产与施法规则；当前实现和用法以 `项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md`、`项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md` 为准。验证结果见 `Docs/AI/TEST_REPORT.md`，修改记录见 `项目修改日志/2026-10-07-YCOMBAT-EDITOR-INTEGRATION-CHANGELOG.md`。

## 结论与证据

YCombatEditor 源码位于 `D:/unitypro/JNBJ/YCombatEditor`，版本为 `0.2.0-preview.2`，MIT 许可。它有完整的 `SkillEditorWindow`、帧时间轴、`SkillConfigSO` 资产保存、预览、`ActionConfigValidator` 和 `ActionPlayer`；适合用作本项目的**技能表现创作工具**。它并不导出本项目的 Luban 表，也不负责服务器施法、MP/CD、伤害或 `skillUuid`。

当前不能直接安装并宣称融合完成：包的 `package.json` 声明 Unity 最低 `6000.0`，唯一公开验证基线为 `6000.3.6f1`，本项目为 `2023.2.20f1c1`。包的全局 `Global` 与本项目 `Assets/GM_Game/Scripts/Global.cs` 同名；独立程序集本身可隔离，但跨程序集适配代码的类型解析必须实编确认。Y 的 `ActionClipAnimator` 独立控制 Animator，直接挂在现有角色上会与 `MAnimator`/`MSkillComponent.PlayAnim` 争用播放权；`ActionRootMotionCollector` 也可能接管根运动，不能在正式预制体上直接启用。

源码依据：`YCombatEditor/Editor/Window/SkillEditorWindow.cs:153`（菜单）、`SkillEditorWindow.Timeline.cs:370`（轨道）、`SkillEditorWindow.Persistence.cs:16,43,93`（新建/加载/保存）、`SkillEditorWindow.Preview.cs:457`（预览）、`Runtime/Data/SkillConfigSO.cs:9`、`Runtime/Core/ActionPlayer.cs:158,329`、`Runtime/Core/ActionContracts.cs:91`。上述相对路径的根为 `D:/unitypro/JNBJ`。包的 `Documentation~/AgentGuide.md` 明确：`TryBeginExternal` 只报告外部生命周期，不派发普通动画/特效/攻击轨道。

## 接入边界

1. **保留创作端命名和行为**：在兼容性门禁通过后，以独立 UPM 包引入原有 `SkillEditorWindow`、`SkillConfigSO`、`ActionTiming`、`ActionConfigValidator`、`ActionEditorAgent`。原包许可证随包保留，包版本固定，绝不改原仓库或 `Library/PackageCache`。本项目新增桥接类型使用全局命名空间与中文注释。
2. **保留现有权威链**：`SkillInfo`、`SkillEffectInfo`、`SkillPresentationInfo` 和 Luban 生成物原样保留；服务器 `RoleSkillComponent` 决定学习、费用、CD、目标和施法状态。客户端 `MSkillFactory → MSkillCore → MSkillComponent → MSkill/MArtSkill` 继续持有 `skillUuid`、预测、当前与旧施法列表和动画状态入口。Y 的 `skillCD`、`energyCostOnActivate`、攻击伤害字段只能作为编辑参考，不能回写权威数据。
3. **首期只接无损表现子集**：强制 `UsesExplicitTiming=true`。`SkillConfigSO.animSegments` 仅接受一段 `startFrame=0`、`clipStartFrame=0`、`clipEndFrame=0`、`blendInFrames=0` 的完整剪辑，且必须与旧表 `ClipLocation` 所指资源一致；旧 `skillClip` 允许为空或指向这同一剪辑，不一致则拒绝，导出始终以 `animSegments` 为准。`fxList` 仅接受单次触发的角色根节点粒子。具体字段白名单见下文；其余运行字段只要非空或非默认，一律逐项报错并停止该资产发布。`tracks` 只控制编辑器显示，隐藏轨道也必须检查对应数据列表，不能借隐藏来绕过校验。多动画段留待专门实现与审查分段播放。
4. **不挂双播放器**：正式角色不添加 `ActionPlayer`、`ActionClipAnimator`、`ActionRootMotionCollector` 或默认位移后端。Y 编辑器预览只用其隔离预览实例；正式游戏按 `MSkillComponent.PlayAnim → MSkill.TriggerAnim` 播动画。尤其不能用 `TryBeginExternal` 冒充完整轨道调度。

## 数据映射与导出

现有 `SkillPresentationInfo.Id` 是每条表现的唯一主键，行内已有 `EffectId`、`ProfessionId`、`ModelKey`、`WeaponType`、`VariantIndex`、`ClipLocation`、`DurationMs`、`SkillStateTimeMs`。新增独立的编辑绑定资产，以 **PresentationInfo.Id → SkillConfigSO** 映射，不修改旧表字段或已生成行。导出前核对目标行完整五元组、技能归属及片段资源，禁止只靠 `SkillConfigSO.skillID` 或文件名匹配。`skillID` 仍填旧 `SkillInfo.Id` 供编辑显示，不能替代效果和变体键。

新增项目自己的只读运行表现资产，按表现行 ID 定位，保存已校验的动画 Location 和以秒表示的粒子事件、粒子资源 Location、角色根节点相对变换及寿命。编辑器用 Unity `AssetDatabase` 取得实际资源引用和位置，不手写 GUID/YAML。**Location 字符串不构成 Unity 资产依赖**：YooAsset 收集规则须显式纳入每个导出的粒子预制体及运行表现资产；构建后用资源清单逐一验证 Location 有效且粒子能加载，缺项使构建失败。运行时由 `MSkillFactory` 在选择原表现行后可选加载该资产，`MSkillData` 保存对应表现数据，`MArtSkill` 按每个施法实例的时钟派发事件。2026-10-08按用户要求移除旧事件兼容：配置未就绪时等待本次锁定的表现ID，按施法经过秒数补发；无效配置报可定位错误且不读取旧动画事件。音频首期不导出，现有 `MArtSkill` 没有音频派发和句柄管理。

导出字段采用白名单：`skillID`、名称/说明/拥有者等编辑标识、`timelineFrameRate`、上述完整单段 `animSegments`、`tracks` 的显示布局，以及 `fxList` 中 `contentKind=VisualEffect`、非空 `particleSystem`、非负 `keyNumber`、`endKeyNumber=0`、`audioClip=null`、`followCharacter=true`、`useWorldSpace=false`、`playbackSpeed=1`、`customLifetime=0`、`audioRandomize=false` 和相对 `offset/rotation/scale`。粒子默认时长按本项目粒子寿命计算；若作者需要其他倍率、世界空间或自定义寿命，当前导出器报错，不近似。`attackList`、`phase2AttackList`、`phase2FxList`、`jumpList`、`flowNodes`、`cameraCues`、`moveSegmentList`、`projectileList`、`hitFxList`、`warningCueList`、`superArmorList`、`adjustMotionList`、`trailToggleList`、`cancelList`、`interactionWindows` 以及任何新增运行列表必须为空；旧 `moveCurve` 必须保持默认的零位移曲线或经验证等价的零位移曲线，`totalMoveDistance=0`，拒绝任何非零位移。`exitFrame=0`、`moveCancelFrame=-1` 等行为字段必须为默认。`skillCD`、能量和伤害数值不导出权威数据，导出校验需提示其被忽略；运行字段白名单随包版本固定，升级包时重新审查，不能默认接受新字段。

导出生成按表现行 ID 查询的只读清单，记录运行资产的 YooAsset Location；既有 `MSkillFactory.CreateCore` 保持同步查表，不在其中等待异步资源。技能列表同步后按已学效果和表现行预取对应运行资产及全部粒子依赖；缓存持有 YooAsset 句柄、就绪/失败状态和引用计数，加载回调只更新以表现行 ID 为键的缓存，不写可能已回池的核心/技能实例。每次 `MSkill.Fire` 快照本段表现ID、动画和原表时长；配置未就绪时由该实例Update等待原ID，完整校验后补发到点事件，不读取已换段的核心选择或旧事件。技能实例和已生成粒子分别持有必要租约，全部结束后才释放依赖句柄；切场景清缓存前先使旧实例失效并等待粒子尾迹清理。加载失败记录资源键，不回退旧事件。构建和回归须覆盖冷加载、晚到、施法中断、技能回池与场景退出，确认句柄计数归零。

`ActionTiming` 的显式时间轴帧率决定 `frame / timelineFrameRate` 秒；首期不接受任何剪辑裁剪或混合。导出必须验证事件帧和触发秒数都不超过旧 `DurationMs`，资源存在、剪辑时长与旧表一致；同帧多粒子按配置顺序各派一次，跨帧更新补发越过的所有事件，每条事件每个施法实例仅执行一次。`SkillStateTimeMs` 继续来自原表，不用 Y 的 `exitFrame`、`RuntimeEndFrame` 或 `skillCD` 改写服务器和 Q 的 700 毫秒接段窗口。Q 的三段各有独立绑定，按既有 `VariantIndex` 轮换，不能写成一个 Y `Jump` 或 `Flow`。10070 当前蓄力特效依赖 `EffectPos` 挂点、落剑依赖目标 UID 与模型高度；当前Y宿主已接入NamedSocket/TargetTop定位语义；2026-10-08删除运行旧事件链，10070统一导出两条FX，默认第4帧蓄力/第9帧落剑（30FPS），不同时消费旧事件。`RoleAnimaBehaviour` 保持失活，原资源与代码保留。

## 实施顺序与验收

| 阶段 | 工作 | 通过条件 |
| --- | --- | --- |
| 0. 兼容性门禁 | 在 `_temp` 内的隔离 Unity 2023.2 工程试装固定版本包，验证 UPM 版本限制、Runtime/Editor 编译、`Global` 解析、建资产/保存重开、动画及双特效预览。保留原始包不变；若仅元数据版本门槛阻止试装，可在隔离副本调低声明来做**实验**，不得以此视为已兼容。 | 所有检查有日志与实际编辑器结果。失败则不触碰正式项目，改评估独立 Unity 6 创作工程加受控导出，或另行评估项目升级 Unity 6；不得偷偷扩大到全项目引擎升级。 |
| 1. 编辑端接入 | 在门禁通过后以固定本地 UPM 依赖接入，新增绑定资产与导出/校验工具，保持 `SkillEditorWindow`、`SkillConfigSO` 等原名。桥接程序集尽量不直接声明包内 `Global` 嵌套类型；若实编仍冲突，先给出局部重命名或独立程序集方案并复审。 | 编辑器能按原菜单打开，旧表不变，绑定能保存/重开，合法完整单动画/角色根粒子导出稳定；隐藏轨道仍被检查，音频、裁剪/混合、目标/挂点 FX 等不支持字段明确失败。 |
| 2. 客户端表现接入 | 客户端开发子 Agent 只扩展运行资产预取、表现数据查找和 `MArtSkill` 的每实例粒子调度，不更换 `MSkillComponent`/`MAnimator` 施法和动画主链。 | Q 三段及 700ms 接段不变；10070 蓄力和落剑通过统一轨道各一次；同帧/越帧粒子各一次；冷加载按锁定原段ID补发，禁止旧事件回退；结束、拒绝、回池和场景退出后句柄归零。 |
| 3. 独立审查 | 测试 Agent 做 Unity 编译、编辑器保存/预览、YooAsset 打包、Game View 真实按键；审查 Agent 核对数据主键、时钟、资源释放、双播和规范。 | 任一门禁失败即打回对应设计/开发 Agent 修订，再按同一清单复验；没有实机证据时只报告静态/编译通过。 |

## 待决约束

本设计不改业务代码或表格。正式实施前必须先取得 Unity 2023 兼容性结果；如果包确需 Unity 6 API，应单独对比“受控兼容分支”与“Unity 6 独立创作工程”，不能在当前游戏里盲目复制源码。包内部已有命名空间、`foreach` 与通用载荷，必须作为**未修改的第三方包**隔离，不移入本项目业务源码；兼容分支若需修改包源码，先单独评估本项目规范和升级维护成本。Y 的高级轨道后续逐项接入，每项都要先确定本项目的权威来源和运行处理器。
