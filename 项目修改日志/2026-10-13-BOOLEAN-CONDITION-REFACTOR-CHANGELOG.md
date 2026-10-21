# 全项目布尔并排判断不超过 3 项重构（客户端 + 服务端，75 文件 / 191 处）

- 日期：2026-10-13
- 类型：全局代码风格重构（不改业务逻辑）
- 规范依据：拆分规范 `_temp/boolcond-refactor/PATTERN.md`；项目约束见 `AGENTS.md`。
- 说明：本日志为本次重构的统一归档，替代开发批次 H、I 过程中产生的两份临时服务端日志（`2026-10-13-SERVER_BOOLCOND_REFACTOR_CHANGELOG.md`、`2026-10-13-SERVER-BOOLCOND-REFACTOR-AGENT-I-CHANGELOG.md`），其内容已并入本日志。

## 一、用户需求与规则

用户要求：“这种并排判断不要超过三个，整个项目修改”。点名示例为 `MSkillTargetMgr.CanAttack` 中
`IsValidTarget(entity) && IsEnemy(entity) && LocalRole != null && 距离判断` 的四连条件。

规则：
- 每个布尔表达式（`if/while/do/for` 条件、`return`、局部 `bool` 初始化/赋值、Lambda 体）中，布尔判断操作数最多 **3 个**，即最多包含 **2 个 `&&`/`||` 组合**。
- 仅靠换行或外层括号包裹、但一个表达式内仍有 4 个以上判断项，不算通过。
- 必须保持原判断顺序、短路语义、副作用、取反、混合 `&&`/`||` 优先级与执行结果完全一致。

## 二、扫描范围（先盘点后执行）

### 纳入扫描的第一方目录（共扫描 499 个 C# 文件）
- 客户端 `LYF_MMO/Assets/GM_Game/Scripts`（264）
- 客户端 `LYF_MMO/Assets/Boot`（2：HotUpdateView.cs、Load.cs）
- 客户端 `LYF_MMO/Assets/GM_Game/Editor/Skill`（8 个第一方文件；其下 ActionEditor 子目录 36 文件排除）
- 客户端 `LYF_MMO/Assets/Editor`（5 个第一方工具；Editor/Editor 子目录排除）
- 服务端 `LYF_Server/CenterServer`、`GameServer`、`GateServer`、`LoginServer`、`MMoRpgCommon`
- 服务端 `LYF_Server/Network/Common`、`Socket`、`LubanManage/LubanMgr.cs`、`LubanManage/Parse/*`
- 服务端 `LYF_Server/Tests`（3）

### 明确排除（未扫描、未修改）
- 第三方：`Plugins/UniRx`（240）、`GM_Game/Plugins/DOTween`（9）、`HighlightPlus`（11）、`artres/Resources`（10）、`Scenes/MainCity_Res/Scripts`（3）、`Assets/Editor/Editor`（6）
- 生成代码：`HybridCLRGenerate`、`Network/LubanManage/Codes`（Luban 生成）、`LubanLib`（第三方库）、`Proto`（protobuf 生成）、`Properties`、`obj/bin`
- Unity 包目录 `Packages`
- 参考工程 `F:\UnityPro\ro`
- 带 auto-generated 标记自动跳过 3 个文件：`PlayerInput.cs`、`LoginServer/Grpc/Message.cs`、`MessageGrpc.cs`。

## 三、扫描方法与结果

- 在 `_temp/boolcond-analyzer` 建立 .NET 9 控制台，引用 `Microsoft.CodeAnalysis.CSharp` 4.12.0，用 Roslyn 语法树精确识别布尔链：
  - 对每个 `&&`/`||` 二进制节点（及 `and/or` 模式节点）向上找链根（穿透括号/强制转换），统计链根子树内布尔操作符数；
  - 不深入 Lambda/匿名方法/本地函数/Invocation/ObjectCreation/Initializer/Query/插值字符串，其内部表达式各自独立计链；
  - 链内布尔操作符 ≥3（即操作数 ≥4）记为违规。
- 最终结果：**499 文件扫描，3 个生成代码跳过，191 处违规**。结果明细：`_temp/boolcond-analyzer/scan-result.json`（字段 File/StartLine/EndLine/OpCount/OperandCount/Operands/Text）。
- 191 处按互斥文件边界划分为 A–I 九个批次，逐批委派开发子 Agent 修改；每批生成精确文件配置（`agent-x-config.json`）与文件清单（`agent-x-files.txt`）。

## 四、处理方式（三种）

1. **具名 bool 分组**（纯读取、无空引用风险的条件）：把判断项按语义分组，用英文 camelCase 命名的 `bool` 变量承载；每个新变量自身 ≤3 项，最终表达式 ≤3 项。
2. **提前 return / continue / break / throw**（含副作用或长 `||` 守卫链）：把长链按原顺序切成连续的早退块，每块 ≤3 项；天然保持短路，保证空对象解引用、副作用调用只在守卫通过后发生。
3. **分层分组**（超长链）：先分成若干 ≤3 项的语义组，再二次组合，每层 ≤3 项。

等价性硬要求：判断顺序、短路、副作用调用次数/时机、取反、混合括号全部不变。

## 五、完整修改文件清单（75 个，按批次）

### 批次 A：技能运行时（5 文件，22 处）
| 文件 | 处理 |
|---|---|
| SceneEntity/Skill/MArtSkill.cs（13 处） | 表现完成、FireEvents、表现行校验、fxEvent 26 项链、audioEvent 11 项链、segment 校验、窗口重叠、chargeData 校验、`ghost()`/`PlayRuntimeEffect()`/`TriggerAnim`/`OwnsRootMotion` 空引用守卫链：具名分组 + 逐级早退；fxEvent/segment 先判空守卫 |
| SceneEntity/Skill/MSkill.cs（4 处） | `Fire`、`OwnsDisplacement`、`OnProcessTimer`、`TriggerAnim` 守卫链：早退 return，`core/entity.Skill/args` 判空前置 |
| SceneEntity/Skill/MSkillChargeData.cs（2 处） | IsValid 14 项链、关键帧 11 项链：分层分组；`CurveZ!=null`、`index>0` 下标守卫保留 |
| SceneEntity/Skill/MSkillCore.cs（1 处） | CanStart 9 项链：identity/cooldown/cost 三 3 项分组 |
| SceneEntity/Skill/MSkillFactory.cs（2 处） | CreateCoreByEffect `TryGetValue(out effect)`：三段早退，out 仅在 effects 非空后赋值一次；FindPresentations：row 判空 + 早退 continue |

### 批次 B：动作 / 位移 / 移动组件（4 文件，19 处）
| 文件 | 处理 |
|---|---|
| SceneEntity/Component/Action/MDefaultActionComponent.cs（5 处） | 战斗待机姿势、跑速同步、动画机就绪、脚步点表：具名分组；`FootSteps` 8 项守卫链逐组早退（返工删除一处重复声明的 `stillMoving`） |
| SceneEntity/Component/Action/MSkillComponent.cs（5 处） | CanStart 宿主守卫、活动槽确认、ApplyServerStart、Cease 清理、`_skill.Fire` 副作用：`hostBlocked`/`identityMismatch` 短路分组，Fire 仅在身份匹配后调用一次 |
| SceneEntity/Component/MDisplacementComponent.cs（5 处） | Update/onStart/onStop/ConsumeDelta 守卫、段完成判定：早退链；混合括号 `(StopAtDistanceLimit && 距离)` 保留 |
| SceneEntity/Component/MMoveComponent.cs（4 处） | 走跑刷新、TryJump、落地判定、stopMove 补发：早退 + 具名分组 |

### 批次 C：3D 模型 / 跳跃 / 状态机（7 文件，18 处）
| 文件 | 处理 |
|---|---|
| SceneEntity/3DModel/AsyncObj/MAnimationMgr.cs（1 处） | JSON 结构边界字符：具名分组 |
| SceneEntity/3DModel/AsyncObj/MAnimator.cs（4 处） | OverrideAnim/ReleaseAnim（含 `TryGetValue(out)`）、过渡等待、NeedPreload 9 项：嵌套 if + 早退 + 三状态分组 |
| SceneEntity/3DModel/AsyncObj/MAnimatorStateCompatibility.cs（1 处） | 行走态枚举：具名分组 |
| SceneEntity/3DModel/SkillRootMotionCollector.cs（1 处） | OnAnimatorMove 守卫：早退，`_skill` 非空后才调 OwnsRootMotion |
| SceneEntity/3DModel/SoundComponent.cs（4 处） | Play/PlayAudioInstance 9 项/BeginLoad/Dispatch 时间窗：早退 + 嵌套 if |
| SceneEntity/Component/MJumpComponent.cs（4 处） | Update、TryPrepareJumpAnimations、三段短名、TryPlayPendingState：早退链 |
| SceneEntity/Component/State/MStateMachine.cs（3 处） | LateUpdate 嵌套 if、两处位掩码实体类型：具名分组早退 |

### 批次 D：通用 / 全局 / 管理器 / 场景实体（9 文件，16 处）
| 文件 | 处理 |
|---|---|
| Common/MCommonFunctions.cs（4 处） | 线段快速排斥、2D/3D 包围盒、含边框矩形：x/y/z 轴具名分组 |
| Common/MTimerMgr.cs（2 处） | Update 中 `deltaTInvalid`、SetTimer 中 `callbackInvalid/intervalInvalid`：具名分组 |
| FunlModule/Shop/ShopMgr.cs（1 处） | 7 项守卫：network/shopParam/role 三 3 项分组，`roleInvalid` 内部短路 |
| Global.cs（2 处） | IsValidMainRoleInfo、IsSameLoadedRole：逐级判空提前 return |
| Manger/MainCityMgr.cs（1 处） | 加载回调实体校验：worldReady/entityReady 分组 |
| Manger/NpcManager.cs（1 处） | IsCurrentRequest：lifecycleStale/entityInvalid 分组 |
| SceneEntity/Entity/PlayerEntity.cs（2 处） | 移动上报（内层 7 项）、LoadRoleData：分层 + 具名分组 |
| SceneEntity/MEntity.cs（1 处） | IsMovable：basicMovable/stateAllowsMove，混合括号保留 |
| SceneEntity/SceneEntityWorld.cs（2 处） | Add、Remove 目标清除：entityInvalid 分组 + 三级非空守卫 |

### 批次 E：客户端控制器（10 文件，23 处）
| 文件 | 处理 |
|---|---|
| controller/InputController/PlayerInputCtr.cs（3 处） | 移动取消时间混合链、物理技能键 3 项早退、IsSkillKey 13 项分层 |
| controller/InputController/TouchObject.cs（2 处） | IsPointerOverUI 副作用：null 守卫后单独 if；自身/非敌/无效目标 continue：具名分组（纯谓词） |
| controller/RoleController/RoleCtrlBase.cs（1 处） | 5 状态枚举：3 组具名 |
| controller/UIController/CreateRoleCtrl.cs（9 处） | 全部 `||` 空引用守卫链（MainRoleInfo.BaseInfo、requestedGlobal.SceneWorld、LoginInfo 等）：顺序早退 |
| controller/UIController/KnapsackCtrl.cs（1 处） | 窗口/角色守卫：早退 |
| controller/UIController/LoginCtrl.cs（1 处） | 5 个字符串空判断：3+2 分组 |
| controller/UIController/MSkillTargetDefine.cs（1 处） | IsValidTarget 7 项：三段守卫链 |
| controller/UIController/MSkillTargetMgr.cs（1 处） | **CanAttack：先守卫再求值**——非敌人提前 return，无本地玩家提前 return，距离最后计算（见下） |
| controller/UIController/ShopCtrl.cs（1 处） | 窗口/全局守卫：早退 |
| controller/UIController/SkillCtrl.cs（3 处） | ApplySkillCores/OnSkillKey 相同守卫、OnSkillCastResult 中 OnSkillCoreSync 副作用：外层 3 项守卫 + 内层嵌套 if 调用 |

### 批次 F：功能模块 / 正式 UI / 客户端测试（11 文件，22 处）
| 文件 | 处理 |
|---|---|
| FunlModule/Bag/BagTransition.cs（1 处） | IsExpandableType：isBaseType/isResourceType |
| FunlModule/Bag/RoItemComponent.cs（5 处） | 五处守卫链：顺序早退（GetContainer 等调用顺序保持） |
| FunlModule/Bag/RoItemContainer.cs（2 处） | CheckIn、FindItem continue：早退；GetItemUID 调用次数保持（原文两次） |
| FunlModule/RoleCount/RoleCountComponent.cs（3 处） | 三处循环 continue：早退，IsRoleDataReady 在 Owner 判空后 |
| UI/Main/Window/KnapsackWindow.cs（1 处） | countOnly：双非空守卫块内比三字段 |
| UI/Main/Window/RoleCurrInfoWindow.cs（1 处） | previous 判空守卫后比 SkillId/Level/IconPath |
| UI/Main/Window/SkillInfoWindow.cs（2 处） | 刷新判断 3 组、循环 continue 早退 |
| UI/Main/Window/Widget/SkillSlotWidget.cs（1 处） | Lambda 内 cooldownInvalid 累积式守卫精确复现短路 |
| UI/test/Editor/SkillVerification/SkillPresentationBuildVerification.cs（2 处） | 两处 throw 守卫：早退 |
| UI/test/Editor/YCombatIntegrationContractTests.cs（2 处） | 两处断言链：下标 Count 守卫 + 早退 |
| UI/test/test.cs（2 处） | 两处 while 等待：`while(true)` + 顺序早退 continue/break |

### 批次 G：客户端编辑器（5 文件，22 处）
| 文件 | 处理 |
|---|---|
| Editor/ConvertChatPrefabToTMP.cs（2 处） | 7 个 null 与 4 个命名空间判断：具名分组 |
| Editor/DrawCallAssistantUtil.cs（1 处） | RenderType/尺寸/缩放：具名分组 |
| GM_Game/Editor/Skill/SkillEditorAssetTools.cs（8 处） | 混合分组、多处 throw 守卫、SameFx 12 项：早退 + 分层，异常消息逐字保持 |
| GM_Game/Editor/Skill/SkillEditorAudioMotionSetup.cs（1 处） | presentationId：具名分组 |
| GM_Game/Editor/Skill/SkillEditorAudioMotionTools.cs（10 处） | 17 项白名单、根运动/音效同步、CurvesEqual、混合 `A&&(B||(C&&D))`：早退 + 嵌套 if，消息逐字保持 |

### 批次 H：服务端 Center/Game 核心（9 文件，28 处）
| 文件 | 处理 |
|---|---|
| CenterServer/DB/Modle/CentRoleModel.cs（2 处） | 5 项守卫早退、枚举 IsExpandableType 分组 |
| CenterServer/DB/Task/CRoleWriteTask.cs（6 处） | 含 `itemUids.Add` 副作用、两处 `TryGetValue(out)`、EquipInfo 空引用守卫：早退 throw + 独立 if，副作用末段执行 |
| GameServer/Components/ROSkillComponent.cs（5 处） | mainRoleInfo 嵌套、JobId/Type 混合、CurrHp/CurrMp、row 6 项：早退 + 嵌套（baseMatch 守卫后才解引用 row） |
| GameServer/Components/RoItemComponent.cs（4 处） | 四处守卫：早退 + 枚举分组 |
| GameServer/Components/RoleCountComponent.cs（1 处） | 守卫链早退 |
| GameServer/Components/RoleSkillComponent.cs（1 处） | 守卫链早退 |
| GameServer/Ctrl/Game_LoginCtrl.cs（1 处） | ret 判空前置：isStartGameSuccess/hasCompleteMainRole，else-if 链保持 |
| GameServer/Entity/Role/RoleNetSender.cs（2 处） | request 判空、result/_inFlightRequest 守卫：早退 |
| GameServer/manger/RoleManager.cs（6 处） | CanDispose 纯谓词分组、pendingRole 判空 + TryGetValue、4 处守卫早退/continue |

### 批次 I：服务端处理器 / 服务 / 网关 / 公共库 / Network / Tests（15 文件，21 处）
| 文件 | 处理 |
|---|---|
| GameServer/MessageHandler/Knapsack/OpenKnapsackGridHandler.cs（1 处） | Gate 来源守卫 + 链路守卫：早退 |
| GameServer/MessageHandler/Knapsack/RoleKnapsackInfoHandler.cs（1 处） | 来源守卫 + Package.Data 守卫：早退 |
| GameServer/MessageHandler/Role/EnterWorldHandler.cs（1 处） | 同 OpenKnapsack：早退 |
| GameServer/MessageHandler/Role/RoleSkillInfoHandler.cs（1 处） | role/response 判空 + State/RoleId：早退 |
| GameServer/MessageHandler/Shop/RequestBuyShopItem.cs（3 处） | 7 项 Command.Session 嵌套、商品项、角色守卫：早退 |
| GameServer/MessageHandler/Skill/SkillCastHandler.cs（2 处） | 入口守卫、SendResult 守卫：早退 |
| GameServer/Service/item/BagTransition.cs（1 处） | IsExpandableType：具名分组 |
| GameServer/Service/item/ItemDataListDB.cs（1 处） | 纯 && 字段比较：三变量分组 |
| GameServer/Service/item/RoItemContainer.cs（2 处） | CheckIn、FindItem：早退；GetItemUID 调用次数保持 |
| GateServer/Ctrl/GateClientResponseForwarder.cs（1 处） | 消息码分组：具名 |
| MMoRpgCommon/Item/ItemBase.cs（1 处） | IsMoney：base/premium 分组 |
| Network/LubanManage/LubanMgr.cs（2 处） | 14 项 throw 链拆 5 段、9 项混合链分层：消息逐字保持 |
| Network/LubanManage/Parse/ItemConfigBase.cs（1 处） | 背包类型：具名分组 |
| Tests/SkillAuthoritySmoke.cs（2 处） | 断言链：startNotice 判空守卫 + 早退 |
| Tests/SkillConfigSmoke.cs（1 处） | 9 项含 `TryGetValue(out presentation)`：4 段早退，out 后才解引用 |

## 六、CanAttack 等价改写（用户点名示例）

最初具名变量无条件提前求值，会在 `LocalRole/entity/Global/SceneWorld` 为空时丢失原短路保护（空引用）。最终改为“先守卫、再求值”，与原表达式逐项等价：

```csharp
public bool CanAttack(MEntity entity)
{
    // 目标是当前世界中有效的敌对单位；IsValidTarget 内部已含全局与世界非空判断。
    bool isEnemyTarget = MSkillTargetDefine.IsValidTarget(entity) &&
        MSkillTargetDefine.IsEnemy(entity);
    // 非敌对目标不满足时，后续本地玩家读取与距离计算都不得执行，保持原短路语义。
    if (!isEnemyTarget)
    {
        return false;
    }
    // 本地玩家已在当前场景中。
    bool hasLocalRole = Global.Instance.SceneWorld.LocalRole != null;
    // 本地玩家不存在时原表达式短路返回 false，距离表达式不得解引用 LocalRole。
    if (!hasLocalRole)
    {
        return false;
    }
    // 目标位于本地玩家十八米选择半径内。
    bool isWithinChooseDistance =
        (entity.Position - Global.Instance.SceneWorld.LocalRole.Position).sqrMagnitude <=
        MaxChooseDistance * MaxChooseDistance;
    return isWithinChooseDistance;
}
```

## 七、验证结果

### 1. 全量复扫
- 命令：`Set-Location '_temp/boolcond-analyzer'; dotnet run -- 'scan-config.json' 'scan-after-all.json'`
- 结果：`扫描文件：499，跳过生成代码：3，违规：0`。即 191 → 0。

### 2. 客户端编译（MSBuild 2022 Professional，独立编译工程）
- 工程：`_temp/boolcond-refactor/compile-GM_Game/GM_Game.csproj`（280 个 Compile 条目，含 DOTween/HighlightPlus 20 个第三方源，目标框架 v4.7.1、LangVersion 9.0）。
- 结果：**0 个错误，39 个警告**。
- 首编曾出现 1 个错误（MDefaultActionComponent.cs `stillMoving` 重复声明 CS0128），已由开发批次 B 修复后复编通过。
- 警告对照：以 2026-10-12 同模板基线（38 警告、0 错误）为基准，按“文件+警告码”去重对比，**警告集合完全一致（25 个去重签名）**，无新增警告码/文件；原始数量差 1 仅为编译宿主对 CS0436 类型冲突重复计数所致。

### 3. 服务端编译（MSBuild 2022 Professional，解决方案整体）
- 对象：`LYF_Server/LYF_Server.sln`。
- 结果：**0 个错误，12 个警告**。
- 警告均为既有代码质量提示（CS0162/0649/0169/0114/0108/4014，位于 ShopCondition、OnlineRole、ShopStatic、ItemDesc、enum/Item、LoginCtrl、RoItemContainer 等），与本次布尔拆分无关；无新增可归因于本次重构的警告。

### 4. 独立复审（三名只读复审子 Agent）
- 复审 R1（客户端核心 A–D，75 处）：**全部等价**。
- 复审 R2（客户端控制器/功能/UI/编辑器 E–G，67 处）：**全部等价**。
- 复审 R3（服务端 H–I，49 处）：**全部等价**。
- 191 处逐条核对：判断顺序、短路、空引用守卫、副作用调用次数/时机、混合括号与取反均保持。

## 八、第三方 / 参考工程未改动确认

- 未修改第三方代码（UniRx、DOTween、HighlightPlus、LubanLib、protobuf 等）、生成代码（HybridCLRGenerate、Luban Codes、Proto）、Packages 目录。
- 未修改参考工程 `F:\UnityPro\ro`。
- 本次修改仅涉及上述 75 个第一方业务源文件及本日志、Docs/AI 协作记录。

## 九、未解决问题 / 遗留说明

1. 少量“纯查询谓词提前求值”经复审判定等价（如 TouchObject 自身命中后仍调用 IsEnemy/IsValidTarget、MainCityMgr/SceneEntityWorld 多一次纯字典查询、Game_LoginCtrl 的 hasCompleteMainRole）。这些方法均为只读、无副作用、无空引用，返回值与可观察行为不变；如需严格保持“短路不调用”，可后续再改为早退式，但当前不构成行为差异。
2. 服务端 `Network/LubanManage/LubanMgr.cs` 在 14 项 throw 链首段，当 `effect == null` 时拼接异常消息会取 `effect.Id`——这是**重构前即存在**的潜在空引用，本次按“不重构无关逻辑”原样保留，未在本次范围内修复；是否单独处理由用户决定。
3. 仅完成编译/静态验证与代码级等价复审，未在 Unity 编辑器内做运行时回归；建议后续在编辑器中做一次进入场景、选目标、施法、移动的冒烟验证。
