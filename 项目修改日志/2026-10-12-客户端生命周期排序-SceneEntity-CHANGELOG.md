# 客户端生命周期方法顺序重排 — SceneEntity 模块修改日志

- 日期：2026-10-12
- 执行人：客户端开发子 Agent（受主 Agent 委派）
- 范围：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/` 下 SceneEntity 模块 22 个含 Unity/帧驱动生命周期函数的 C# 脚本
- 原则：纯排版整理，只调整类内成员书写顺序，零逻辑改动；不修改方法体、不增删注释、不改条件编译块、不改 BOM。
- 工具：专用 Roslyn 语法树重排工具 `_temp/lifecycle_reorder_tool/.../ReorderTool.dll`，写盘前自动多重备份到 `_temp/lifecycle_run/A_scene/backup`。

## 一、重排后统一的类内结构（每个类内部从上到下）

1. using 引用（不动）；
2. 类型声明及类级 XML 注释；
3. 成员变量区：字段（含 [SerializeField]）、const、只读字段、event、委托、属性，保持原相对顺序；
4. 构造函数；
5. 生命周期函数区，固定时序：Awake→OnEnable→Start→FixedUpdate→Update→LateUpdate→OnDisable→OnDestroy（文件中出现哪些排哪些；同名重载按方法名聚合、重载间保持原序）；
6. 其他方法（公共/私有/非核心 Unity 消息/接口实现），保持原相对顺序；
7. 嵌套类型（类内部 class/struct/enum/interface）移到类末尾。

说明：本次按“方法名”识别生命周期，带参数/不同返回值的同名方法（如 `Update(int)`、`Update(float)`、`LateUpdate(float)`、`Start()`）同样上移并按方法名聚合；`OnAnimatorMove`、`OnDestroying`、`OnReset`、状态机 `OnStart/OnUpdate/OnStop` 等非标准 Unity 消息仍留在业务方法区。

## 二、逐文件清单

基目录：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/`

| 相对路径 | 所含生命周期方法 | 实际移动（Moved） |
|---|---|---|
| 3DModel/AsyncObj/MAnimator.cs | Update | Update |
| 3DModel/MModel.cs | Update(float)、LateUpdate(float) | LateUpdate |
| 3DModel/SkillRootMotionCollector.cs | OnDisable、OnDestroy | OnDisable、OnDestroy |
| 3DModel/SoundComponent.cs | Update | Update |
| Component/Action/MSkillComponent.cs | Update(int)、Update(float) | Update（两个重载聚合上移） |
| Component/base/EntityComponentBase.cs | Update(int)、Update(float)、LateUpdate(float) | Update、LateUpdate |
| Component/base/EntityComponentContainer.cs | Update(int)、LateUpdate(float) | Update、LateUpdate |
| Component/EntityProjectileComponent.cs | Update(int) | 本就符合（未移动） |
| Component/MDisplacementComponent.cs | Update(int)、Update(float) | Update（两个重载聚合上移） |
| Component/MIdleComponent.cs | Update(int) | Update |
| Component/MJumpComponent.cs | Update(int) | Update |
| Component/MMoveComponent.cs | Update(int) | Update |
| Component/State/MStateMachine.cs | Update(int)、Update(float)、LateUpdate(float) | Update、LateUpdate |
| Entity/PlayerEntity.cs | Update(int) | Update |
| MEntity.cs | Update(int)、LateUpdate(float) | Update、LateUpdate |
| MObject.cs | Update(int)、LateUpdate(float) | Update、LateUpdate |
| MSceneObject.cs | Update(int) | 本就符合（未移动） |
| ROObject.cs | Update(int)、LateUpdate(float) | Update、LateUpdate |
| SceneEntityWorld.cs | Update(int)、LateUpdate(int) | Update、LateUpdate |
| Skill/MArtSkill.cs | Start、Update(float) | Start、Update |
| Skill/MSkill.cs | Start、Update(float) | Start、Update |
| Skill/SkillRuntimeFxLifetime.cs | LateUpdate、OnDestroy | LateUpdate、OnDestroy |

## 三、验证结果

### 工具内置 5 项写盘前校验（全部通过才写盘）
1. 代码 token 多重集合前后一致（代码本体零增删改）——通过；
2. 注释 / 预处理指令 / 禁用文本多重集合一致（注释与条件编译块零丢失）——通过；
3. 语法错误诊断不新增——通过；
4. 各类型成员定义数量守恒——通过；
5. 重排后顺序符合约定——通过。

report.json 全部 22 个元素 `Warnings` 均为空，无任何告警。

### 逐文件人工回读结论
对全部 22 个文件逐一 Read 回读，确认：
- 成员变量 / 属性位于类内最前；
- 生命周期函数紧随构造函数之后，并按 Awake→…→OnDisable→OnDestroy 标准时序排列；同名重载（如 `Update(int)`/`Update(float)`）按方法名聚合且重载间原序保留；
- 各方法上方的特性、XML 文档注释、普通注释均未脱离所属方法，随方法整体移动；
- 无方法被截断、无方法粘连；
- 嵌套类型已移到类末尾：`MAnimator.PreparedClipApplyResult`(enum)、`MAnimator.MAnimInfo`(class)、`SoundComponent.LocomotionCacheEntry`(class)、`MEntity.MoveType`(enum)、`MArtSkill.EArtsSkillTimerCb`(enum) 均位于类末尾；
- 条件编译块完整保留：`MMoveComponent.PlayerUpdate`/`onMoveEvent` 内的 `#if true/#endif`、`#if false/#endif` 配对完整，未被拆散。

### 大括号 / 条件编译配对结论
工具基于 Roslyn 语法树切分并通过“语法诊断不新增 + 成员数量守恒”校验，回读未发现大括号失配或 `#if/#endif` 错位；`#region/#endregion`（MEntity、MSceneObject 等）随所属成员整体保留、配对正确。

### 编译确认（需用户在 Unity Editor 中执行）
客户端命令行环境无法运行 Unity 编译，本次仅完成纯文本顺序重排与上述静态/人工校验。**请用户在 Unity Editor 中做一次编译确认**，确保无编译错误。

## 四、SKIPPED / FAILED / 需人工确认文件
- SKIPPED：无；
- FAILED：无；
- 需人工确认：无逻辑异常文件。仅两点提示（非问题）：
  1. `EntityProjectileComponent.cs`、`MSceneObject.cs` 的生命周期方法本就位于正确位置，工具未产生实际位置移动（Moved 为空），内容保持原样；
  2. 本次未改动任何业务逻辑，行为应与重排前完全一致。

## 五、本模块汇总

- 清单文件总数：22
- Status=OK：22（其中实际发生位置移动 20 个；Moved 为空、本就符合未移动 2 个：EntityProjectileComponent.cs、MSceneObject.cs）
- Status=NOCHANGE：0（工具对本就符合的文件仍记为 OK、Moved 为空，未单独标记 NOCHANGE）
- Status=SKIPPED：0
- Status=FAILED：0

备份目录：`_temp/lifecycle_run/A_scene/backup/`（写盘前原文镜像备份）
结果报告：`_temp/lifecycle_run/A_scene/report.json`
