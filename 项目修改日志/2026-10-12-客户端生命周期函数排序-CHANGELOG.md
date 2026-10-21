# 修改日志：客户端生命周期函数排序（总）

- 日期：2026-10-12
- 执行人：客户端开发子 Agent（3 个模块并行）+ 独立校验子 Agent；OrganizeAgent 负责拆解、委派、验收与记录
- 范围：`LYF_MMO/Assets/GM_Game/Scripts` 下 **57 个**含 Unity 生命周期函数的业务脚本（不含 `UI/test` 测试脚本）
- 性质：**纯排版重排，零逻辑改动**。只调整类型内部成员的先后顺序，不修改任何方法体、字段初始化值、注释内容、特性内容；方法顺序在 C# 中不影响运行时行为。

## 一、分模块日志（详细逐文件说明见这三份）

- [SceneEntity 模块（22 个）](2026-10-12-客户端生命周期排序-SceneEntity-CHANGELOG.md)
- [UI 模块（18 个）](2026-10-12-客户端生命周期排序-UI-CHANGELOG.md)
- [其余模块（17 个）](2026-10-12-客户端生命周期排序-其余模块-CHANGELOG.md)

## 二、重排后统一的类内结构（从上到下）

1. `using` 引用（不动）；
2. 类型声明及类级注释；
3. **成员变量区**：字段（含 `[SerializeField]`）、`const` 常量、只读字段、`event` 事件、委托、属性 Property，保持原有相对顺序；
4. **构造函数**（静态/实例构造函数，紧跟成员变量）；
5. **核心生命周期函数区**，固定按 Unity 标准执行时序排列：
   `Awake → OnEnable → Start → FixedUpdate → Update → LateUpdate → OnDisable → OnDestroy`
   （文件中只出现其中哪些就排哪些；同名重载按方法名聚合，重载之间保持原相对顺序）；
6. **其他方法**：公共方法、私有方法、非核心 Unity 消息回调（如 `OnTrigger`/`OnCollision`/`OnAnimatorMove`/`OnApplicationFocus`/`OnValidate`/`OnPointerClick` 等）、接口实现，整体保持原文件中的相对先后顺序；
7. **嵌套类型**（类内部定义的 class/struct/enum/interface）统一移到类的最末尾。

识别口径（用户已确认）：按方法名识别这 8 个生命周期函数，**同名但带参数或不同返回值的也算**。因此项目中由游戏主循环手动驱动的 `Update(int)`/`Update(float)`/`LateUpdate(float)` 等 Tick 方法同样上移并按方法名聚合；接口（`ROObject`）中的同名方法同样处理。

## 三、重排文件清单与移动的生命周期方法

### SceneEntity 模块（22 个，路径相对 `Scripts/SceneEntity/`）

| 文件 | 移动的生命周期方法 |
| --- | --- |
| 3DModel/AsyncObj/MAnimator.cs | Update |
| 3DModel/MModel.cs | LateUpdate |
| 3DModel/SkillRootMotionCollector.cs | OnDisable、OnDestroy |
| 3DModel/SoundComponent.cs | Update |
| Component/Action/MSkillComponent.cs | Update |
| Component/base/EntityComponentBase.cs | Update、LateUpdate |
| Component/base/EntityComponentContainer.cs | Update、LateUpdate |
| Component/EntityProjectileComponent.cs | 本就符合（无移动） |
| Component/MDisplacementComponent.cs | Update |
| Component/MIdleComponent.cs | Update |
| Component/MJumpComponent.cs | Update |
| Component/MMoveComponent.cs | Update |
| Component/State/MStateMachine.cs | Update、LateUpdate |
| Entity/PlayerEntity.cs | Update |
| MEntity.cs | Update、LateUpdate |
| MObject.cs | Update、LateUpdate |
| MSceneObject.cs | 本就符合（无移动） |
| ROObject.cs（接口） | Update、LateUpdate |
| SceneEntityWorld.cs | Update、LateUpdate |
| Skill/MArtSkill.cs | Start、Update |
| Skill/MSkill.cs | Start、Update |
| Skill/SkillRuntimeFxLifetime.cs | LateUpdate、OnDestroy |

### UI 模块（18 个，路径相对 `Scripts/UI/`）

| 文件 | 移动的生命周期方法 |
| --- | --- |
| CreateRole/Window/CreateRoleWindow.cs | 本就符合（无移动） |
| Loading/LoadingView.cs | Start、OnEnable |
| Main/Window/ChatWindow.cs | 本就符合（无移动） |
| Main/Window/KnapsackWindow.cs | Awake、OnEnable、Update、OnDestroy |
| Main/Window/MinMapWindow.cs | 本就符合（无移动） |
| Main/Window/NpcShopWindow.cs | Update、OnDestroy |
| Main/Window/RoleAttriibuteWindow.cs | NOCHANGE（未写盘） |
| Main/Window/SkillInfoWindow.cs | Update |
| Main/Window/Widget/BuyItemWidget.cs | 本就符合（无移动） |
| Main/Window/Widget/EquipSlotWidget.cs | 本就符合（无移动） |
| Main/Window/Widget/GoodsItemWidget.cs | OnDestroy |
| Main/Window/Widget/SkillSlotWidget.cs | OnDestroy |
| Style/InputStyle01.cs | 本就符合（无移动） |
| Style/UIDragWidget.cs | 本就符合（无移动） |
| Style/UISmoothTransitionMatValue.cs | OnDestroy |
| Style/WindowTitleDragHandle.cs | 本就符合（无移动） |
| Tips/BuyDialog.cs | Awake、OnDestroy |
| UIRoot.cs | OnDestroy |

### 其余模块（17 个，路径相对 `Scripts/`）

| 文件 | 移动的生命周期方法 |
| --- | --- |
| Common/MTimerMgr.cs | Update |
| Common/Utils/GhostEffect.cs | OnEnable、Update、OnDisable |
| Common/Utils/MinMapHelp.cs | NOCHANGE（未写盘） |
| controller/S_CameraController.cs | 本就符合（无移动） |
| controller/camerctrl/S_CameraController.cs | 本就符合（无移动） |
| controller/InputController/PlayerInputCtr.cs | Awake、OnEnable、Update、OnDisable、OnDestroy |
| controller/RoleController/RoleCtrlBase.cs | Awake、Start、Update |
| controller/UIController/KnapsackCtrl.cs | Update |
| FunlModule/Bag/RoItemComponent.cs | Update |
| FunlModule/Component/RoleComponentBase.cs | Update |
| FunlModule/Component/RoleComponentContainer.cs | Update |
| FunlModule/Role/SceneWorldSystem.cs | Update、LateUpdate |
| Global.cs | Awake、Update、LateUpdate、OnDestroy |
| Manger/CameraMgr.cs | Awake、OnEnable、Update、OnDisable、OnDestroy |
| Manger/MainCityMgr.cs | OnDestroy |
| Manger/NpcManager.cs | OnDestroy |
| Role/RoleAnimaBehaviour.cs | 本就符合（无移动） |

> 说明：标注“本就符合（无移动）”的文件 Status=OK，工具仅做了成员间空行规整，生命周期方法本就在字段之后、索引未变；标注 NOCHANGE 的 2 个文件内容完全未改动、未写盘。

## 四、实现方式

- 使用 Roslyn（`Microsoft.CodeAnalysis.CSharp` 4.11.0）语法树精确定位每个成员节点边界，避免纯正则在嵌套大括号、字符串、插值、注释、特性上的误判。
- 移动成员时，将其作为整体块处理：连同上方特性（Attribute）、XML 文档注释、普通注释，以及包裹方法的 `#if...#endif` 条件编译指令一起移动。
- 对 `#if...#endif` 区域做配对分析：被同一条件块包裹的多个成员作为不可拆分的原子块整体移动，保证 `#if`/`#endif` 不被拆散；`#region/#endregion` 随其所属成员移动并保持配对。
- 工具工程、扫描结果、备份与报告均在 `_temp`（`lifecycle_reorder_tool/`、`scan_result.tsv`、`lifecycle_lists/`、`lifecycle_run/`、`verify_independent/`），未散落到项目目录。

## 五、验证结果

### 开发工具内置写盘前校验（每个文件，全部通过才写盘）

1. **代码 token 多重集合前后一致**：代码本体零增、删、改；
2. **注释/预处理指令/条件编译禁用文本多重集合一致**：注释与条件编译块零丢失；
3. **语法错误诊断不新增**：重排前后解析错误完全相同；
4. **成员定义数量守恒**：各类型方法/字段/属性/构造/嵌套类型数量一致；
5. **重排后顺序符合约定**：成员区→构造→生命周期（按时序）→其他方法→嵌套类型。
此外，写盘前原文已镜像备份到 `_temp/lifecycle_run/<模块>/backup/`；文件 UTF-8 BOM 有无严格保持。

### 独立第三方校验（独立 Python 实现，未复用开发工具）：结论 PASS

- **55/55 个写盘文件代码本体完全一致**（独立词法归一化后按 token 多重集合对比），无缺备份；
- **55/55 括号与预处理器配平**：`{}`/`()`/`[]` 配平，`#if/#endif`、`#region/#endregion` 计数一致、配对完整；
- 排序正确性全部通过（GhostEffect.cs 曾被简化正则误报，人工目检确认为误报，外层类顺序 OnEnable→Update→OnDisable 正确）；
- 人工 Read 目检 12 个文件（含 Global、GhostEffect、ROObject、PlayerInputCtr、UISmoothTransitionMatValue、MStateMachine 等），成员区在前、构造次之、生命周期居中、其他方法在后、嵌套类型在类尾，注释/特性无脱离、无截断或粘连；
- 2 个 NOCHANGE 文件确认本就符合；
- git 只读核对：抽样 diff 显示代码行逐字保留、仅位置移动（外加更早“注释补充”任务新增的中文文档注释），无整段逻辑改写；未做提交或还原。
- 独立校验证据：`_temp/verify_independent/`（`verify.py`、`verify_result.json`、`独立校验报告.md`）。

## 六、汇总与遗留

- 实际处理 **57 个文件**：**55 个写盘重排**（其中生命周期方法发生位置移动的 41 个，仅空行规整、方法本就在字段后的 14 个），**2 个 NOCHANGE** 未改动；
- **零 SKIPPED、零 FAILED**，静态检查与独立校验全部通过；
- 唯一待用户跟进：客户端命令行无法运行 Unity 编译，**请在 Unity Editor 中做一次编译确认**（本次为纯顺序调整，预期零编译风险）；
- 透明说明：`UI/Style/UISmoothTransitionMatValue.cs` 内 `namespace IndieImpulseAssets` 为第三方导入资产原有，本次未改动其命名空间与逻辑（项目“禁止新增 namespace”约束不针对该历史第三方文件）。
