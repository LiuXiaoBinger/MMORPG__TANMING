# 2026-09-27 RO 参考技能模块修改日志

## 目标与设计

参考 `F:/UnityPro/ro/Ro_cs422nb/技能系统解析.md` 及 RO 源码，为本项目建立技能定义、等级效果、职业表现和单次施法的分层链路。方案存于 `项目分析/RO_SKILL_SYSTEM_DESIGN.md`。服务端按已学等级、职业、HP/MP、施法状态和冷却做权威校验；客户端只预测动画，结果以服务器回包校正。当前场景战斗实体链尚未接通，因此本期 `TargetMode=0` 只表示无目标施法表现，不表示命中或伤害。

## 修改文件与内容

- 数据：新增 `Luaban/MiniTemplate/Datas/#SkillEffectInfo.xlsx`、`#SkillPresentationInfo.xlsx`，各 5 条职业 1 一级技能数据；生成 `LYF_Server/Network/LubanManage/Codes/SkillEffectInfo.cs`、`TbSkillEffectInfo.cs`、`SkillPresentationInfo.cs`、`TbSkillPresentationInfo.cs` 及同名 `.bytes`。`Tables.cs`、`LubanMgr.cs`、`Network.csproj` 登记新表和完整 16 张运行时表，兼容既有 `PresentTable`/职业模型配置。原 `#SkillInfo.xlsx` 与 `tbskillinfo.bytes` 未修改。
- 协议：`LYF_Server/ProtoBuf/proto/RequestEntity.proto`、`ResultEntity.proto` 新增 11170 施法请求/结果、11171 开始、11173 结束及错误码；`RoleSkillInfoRet` 追加 `role_id=3` 以绑定中心服查询角色。同步 `LYF_Server/Network/Proto/RequestEntity.cs`、`ResultEntity.cs`、`Socket/com/NetDefine.cs` 和 Unity `Assets/GM_Game/Plugins/Net/Network.dll`，保留旧消息字段和命令号。
- 服务端：新增 `LYF_Server/GameServer/Components/RoleSkillComponent.cs`、`MessageHandler/Skill/SkillCastHandler.cs`、`GateServer/Ctrl/GateSkillCtrl.cs`、`GameServer/manger/PendingEnterWorldRequest.cs`；修改 `OnlineRole.cs`、`RoleManager.cs`、`RoleSkillInfoHandler.cs`、`ClientMessageManager.cs`、`GameServer.csproj`、`GateServerApp.cs`、`GateClientResponseForwarder.cs`、`GateServer.csproj`、`CenterServer/DB/Modle/CentRoleModel.cs`。实现权威扣 MP/设 CD、请求序号去重、施法起止、同地图附近表现通知、11080 装载与重连 EnterWorld 重放。11080 在 GameServer 核对复合会话角色 ID 后才转发/装载，防旧角色列表串给新角色。
- 客户端：新增 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillCore.cs`、`MSkill.cs`、`MSkillFactory.cs`、`SceneEntity/event/eventclassArg/SkillStartedEventArgs.cs` 及 Unity `.meta`；修改 `SceneEntity/Component/Action/MSkillComponent.cs`、`SceneEntity/3DModel/AsyncObj/MAnimator.cs`、`SceneEntity/Entity/PlayerEntity.cs`、`controller/UIController/SkillCtrl.cs`、`controller/RoleController/RoleCtrlBase.cs`、`UI/Main/Window/RoleCurrInfoWindow.cs`、`Widget/SkillSlotWidget.cs`。按角色 ID/连接代次缓存早到 11080，按请求序号匹配预测结果、按服务器施法 ID 匹配开始/结束；失败回滚，成功同步 MP/CD，无表现资源也保持活动施法互斥，动画与 UI 冷却资源按生命周期释放。
- 测试：新增 `LYF_Server/Tests/SkillConfigSmoke.cs`、`SkillAuthoritySmoke.cs`、`SkillRoleSnapshotSmoke.cs`，均不加入正式服务工程。测试构建输出与临时协议描述文件存放于项目 `_temp/`。协作测试证据见 `Docs/AI/TEST_REPORT.md`；角色隔离专测另见 `项目修改日志/2026-09-26-RO-SKILL-ROLE-SNAPSHOT-TEST-CHANGELOG.md`。

## 验证结果

- Network、GameServer、GateServer、CenterServer、GM_Game 最终构建均为 0 错误；Network 正式输出复制 16 张 Luban bytes。客户端插件与服务端 `Network.dll` SHA256 均为 `AF805591BFC54F124315A474C0450FACA195861E357025954DB9A3A7BF3DE54B`。
- 配置运行时测试通过旧技能/等级效果/表现五组映射；权威组件测试覆盖成功扣 20 MP、重传幂等、未装载、冷却、未学、非法 ID、MP 不足和死亡；同会话 A→B 迟到 11080 专项拒绝 A、接受 B。当前协议描述中的 `RoleSkillInfoRet.role_id` 为字段 3。目标业务文件禁用语法与差异空白检查通过。
- 审查首次打回 11080 角色身份竞态、客户端换角旧缓存、无表现活动施法及技能槽 CD 时钟；对应开发 Agent 已返工，测试 Agent 复审结论为通过，无剩余 Block/Critical。实际四服与 Unity Play Mode 未执行，不能把编译或组件测试视为联机验收。

## 未解决问题与交接

本期不实现命中、伤害、Buff、投射物、引导/多段、技能升级和 MP/CD 重连持久化；组 CD、非零目标模式、自然结束通知次数及双客户端广播仍缺运行覆盖。需先在正式场景和数据库环境完成四服/Unity 联调，随后实现权威场景目标与数值结算。原有表格、角色技能数据库行、旧 UI 与用户未提交修改均保留；`_temp` 由产品经理确认后再清理。

## 2026-09-27 直接主城启动返工

### 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/controller/RoleController/MainRoleCtrl.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/Role/RoleAnimaBehaviour.cs`

### 修改内容与设计思路

- 主角控制器启动时幂等补建 `UIRoot.MainCtrl`，并在 `UIRoot` 不存在时跳过主界面快捷键注册，避免直接运行 `New Scene` 或角色对象脱离正式 UI 时空引用。
- `RoleAnimaBehaviour.PlayEffect` 继续拒绝失活组件收到的延迟动画事件。审查确认动画结束不依赖 `AnimaEnd`：正式 MSkill 状态由 `MSkillComponent.OnUpdate` 在归一化动画完成后 `Finish`，`MStateMachine.UpdateSeconds` 再切回默认状态；服务器结束由 11173 通过 `ApplyServerEnd` 回收施法实例。因此未恢复已注释的旧 `AnimaEnd` 状态切换，避免旧 FSM 与正式技能状态机重复写入。

### 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug`：0 错误，38 个既有警告。
- `git diff --check`：通过。
- Unity Play Mode 重启后运行态查询：`mainCtrl=True input=True playing=True`；`MainView active=True`，技能槽组件数量为 11。
- 重启后的 Unity Console 错误/警告：0 条。此前出现的 `CameraMgr.Update` 与 `RoleCtrlBase.Update` 空引用来自旧 Play Mode 启动过渡，干净重启后未复现，未改动其非技能代码。

### 未解决问题与交接

- 直接运行场景仍依赖模拟资源包和角色表现数据；四服联机、命中结算和数据库持久化不在本次返工范围。

## 2026-09-27 无 TCP 本地施法与技能 UI 刷新返工

### 修改文件与设计思路

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`：新增 `BeginLocalCast`，沿用已学技能核心的状态、MP 和 CD 校验，直接分发真实技能表现事件并登记本地展示 CD；动作结束后释放本地施法实例。联网预测和权威结果路径保持独立。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs`、`MainCtrl.cs`：无 TCP 时快捷键走本地施法，有连接时仍发送 11170；统一服务器绑定与输入键大小写，并提供按当前角色 ID 校验的强类型已学技能快照装载入口。正式业务不生成全学习模拟数据。
- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/SkillInfoWindow.cs`：仅技能稳定展示内容变化时重建条目，CD 刷新不再作废异步条目回调。按快照索引记录加载状态，窗口启用期间即使没有新快照也只对缺失条目隔 1 秒重试；成功条目按快照顺序排列。
- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/RoleCurrInfoWindow.cs`：用 `_slotBindings` 比较 ID、等级、图标路径，仅绑定变化时清理和重新加载图标；同技能仅更新冷却，归零时清遮罩。各缺失槽位独立异步加载，首个失败不阻断后续槽位，替换已销毁槽位时清除旧绑定缓存。

### 验证结果与交接

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output _temp/skill-input-offline-build --verbosity quiet -clp:ErrorsOnly`：0 错误、38 条警告。
- 上述五个目标业务文件未命中禁用的 `namespace`、`foreach`、三目和弱类型载荷声明；针对目标文件的 `git -c core.autocrlf=false diff --check` 通过。全工作树 `git diff --check` 仍提示其他既有资源和场景文件的尾随空白，未改动这些用户文件。
- Unity Play Mode 离线按键、真实 Animator 剪辑、图标可见及冷却复测待测试 Agent 完成；四服真实联网、命中伤害和 Buff 仍未覆盖。测试快照由专用 `test.cs` 生成并序列化为 11080 回包，再通过 `UIRoot.Instance.MainCtrl.LoadSkillResponse(payload)` 注入；本地表现路径不扣 MP，权威扣费仍属于联网路径。

### 11080 协议回包复用入口

- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs`：网络回调与离线注入共用 `TryParseRoleSkillInfo` 对 `RoleSkillInfoRet` 的正式 protobuf 解析和结果校验。网络路径继续校验派发连接代次并支持角色晚创建时暂存；离线路径通过 `ApplyRoleSkillInfoPayload(ByteString)` 在本地主角已存在时按 RoleId 装载，不伪造连接代次。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`：新增 `LoadSkillResponse(ByteString)` 作为正式主界面的协议字节装载入口，供专用测试脚本在模拟登录后投递同格式技能回包。模拟角色的 HP/MP 和全学习数据仅由 `test.cs` 构造，正式业务代码不生成测试数据或自动按键。
- 验证：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output _temp/skill-input-offline-build --verbosity quiet -clp:ErrorsOnly` 为 0 错误、38 条警告；两个控制器目标文件的 `git -c core.autocrlf=false diff --check` 通过，禁用语法扫描无命中。模拟登录、正式回包刷新和用户手动按键仍待 Unity Play Mode 验收。

## 2026-09-27 技能动画覆盖槽延迟释放

### 修改文件与设计思路

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：单次施法结束时请求延迟归还 Skill 剪辑，避免 Animator 尚处于 Skill 状态时立即换回占位剪辑。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs`：等待 Skill 状态及过渡实际退出后归还剪辑租约；新施法登记同一槽位时取消旧挂起项，旧 Location 的结束请求不能释放新绑定；模型 Reset 清除挂起状态并统一释放全部剪辑。未修改 MJS 的 `.anim`、FBX、Controller 或正式预制体。
- 逐帧驱动核对：`MEntity.Update` 调用 `MSceneObject.Update`，后者执行 `MModel.Update`，最终进入 `MAnimator.Update`；旧注释块中的第二次模型更新不是当前唯一入口。

### 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output _temp/skill-root-motion-build --verbosity quiet -clp:ErrorsOnly`：0 错误、38 条既有警告。目标文件禁用语法扫描无命中，`git -c core.autocrlf=false diff --check` 通过。
- Unity Editor 内存临时 Animator 实验：`Role_MJS` 的 Skill 槽装入 `Anim_MJS_Skill01` 后，播放中立即恢复占位槽，当前剪辑由 `Anim_MJS_Skill01` 变为 `Skill`；保持覆盖直至离开 Skill 后再归还，期间剪辑保持 `Anim_MJS_Skill01`，离开后为 `Idle`。临时实例已销毁，场景未保存或修改。Unity 脚本刷新后 Console 错误为 0。

### 未解决问题与交接

- 本次只修正剪辑释放时序；技能动作本身的下蹲和跳跃曲线保留。用户在 Game View 手动按 Q/E/R/F/1 的最终视觉复验仍待完成；不把临时 Animator 实验等同于完整本地施法验收。四服联机与战斗结算仍属于原技能模块后续范围。
- 协作记录同步：`Docs/AI/CURRENT_TASK.md` 与 `Docs/AI/TEST_REPORT.md` 已追加模型根/骨骼采样、提前恢复占位槽的复现、新技能加载成功和失败路径的验证，以及手动 Game View 视觉验收仍待确认的范围。

### 新剪辑失败路径返工与复验

- 审查发现新施法接管 Skill 槽位后，若候选剪辑加载或安装失败，旧 ready 剪辑仍保留，但其延迟释放请求此前已失效。`MAnimator.cs` 现在暂存旧 ready 的 Location；候选成功安装时废止旧请求，失败时恢复旧剪辑的延迟释放。候选尚未结束准备时不归还同槽资源，模型 Reset 同时清除暂存路径。
- 再次构建 `GM_Game.csproj`：0 错误、38 条既有警告；目标文件禁用语法扫描无命中，差异空白检查通过。
- Unity Editor 内存临时 Animator 失败注入：新 Location 无可加载资源时，仍处于 Skill 且当前剪辑为 `Anim_MJS_Skill01`，绑定路径回到旧 ready；转入 Idle 后旧 Skill 绑定已释放。临时对象已销毁，场景 `isDirty=false`，Console 错误 0。用户手动按键的完整视觉复验仍待完成。
- 成功替换对照：旧 Skill01 待释放时安装 Skill02，当前剪辑变为 `Anim_MJS_Skill02` 且新绑定保留；新施法请求结束后，在 Skill 期间仍保留 Skill02，切到 Idle 才释放。临时对象已销毁，未修改正式资源。

## 2026-09-27 施法期间水平移动阻断

### 修改文件与设计思路

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`：技能状态明确关闭 `IsActiveMovable`；有效技能开始后立即调用移动组件清除上一帧方向和水平速度。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`：Skill 状态单独禁止接收水平移动输入，即使角色仍在跳跃；逐帧合成位移时将 Skill 的水平分量置零，但继续执行垂直重力、CharacterController.Move 和落地检测。停止移动同步清除模型水平速度，不触碰垂直速度。
- `PlayerInputCtr` 已有每约 33 毫秒读取当前按键并重发移动事件的路径；技能期间输入被移动组件拒绝，动作结束仍按住方向时可由该路径重新启用移动，因此未修改输入控制器或联网预测协议。

### 验证结果与未解决问题

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output _temp/skill-movement-lock-build --verbosity quiet -clp:ErrorsOnly`：0 错误、38 条既有警告。两个目标文件禁用语法扫描无命中，`git -c core.autocrlf=false diff --check` 通过。
- Unity 编辑器脚本刷新和重编译结束，Console 错误 0；正式场景保持 Edit Mode。测试 Agent 仍需在 Play Mode 手动核对按住方向施法、空中施法落地、动作结束时保持按键恢复移动，以及普通移动和本地预测表现。

### 跳跃表现优先级审查返工

- 审查发现空中施法落地时 `MJumpComponent.OnJumpEnded` 可能播放 Jump_End 或强制切回默认状态；Jump_End 已播放后进入 Skill 时，旧 `_isEnding` 和待播项可能遗留。修改文件新增 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`：技能状态接管表现时清除跳跃动画阶段和待播项；技能期间的起跳/落地事件只清理表现，不切动画或动作状态；待播动画也不能覆盖 Skill。
- `MMoveComponent.EndJump`、CharacterController 垂直位移、接地判定保持原物理结算。三文件重新构建 0 错误、38 条既有警告；禁用语法扫描无命中，差异空白检查通过；Unity 脚本重编完成且 Console 错误 0。空中施法与 Jump_End 中施法的 Play Mode 视觉回归仍交测试 Agent 验收。

## 2026-09-27 技能按键本地表现与联网请求解耦

### 修改文件与设计思路

- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs`：按键入口只检查绑定技能和当前角色，再按技能快照来源启动施法。通过 `MainCtrl.LoadSkillResponse` 直接装载的快照启用本地施法会话，按键立即调用 `BeginLocalCast`；正式网络 11080 快照仍调用独立的预测与 11170 发送方法，保留原序号、重试、超时及权威结果处理。
- 网络快照会话中传输不可用时仍使用本地表现；传输恢复后再次按键可按原协议请求权威结算。切入直接注入的本地会话时撤销旧待确认预测并清除其序号，迟到的权威结果不覆盖当前 MP/CD；本地角色旧开始/结束广播不接管本地会话表现，其他角色广播照常处理。Mock 数据和测试键位仍只由专用 `UI/test/test.cs` 构造，未修改正式场景、预制体或技能表。

### 验证结果与未解决问题

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/skill-input-simplify/build --verbosity quiet -clp:ErrorsOnly`：0 错误、38 条既有警告。
- 目标文件 `git diff --check` 通过；静态扫描未命中 `namespace`、`foreach`、三目运算符、`dynamic` 或 `object[]` 业务载荷。
- 需在 Unity Game View 使用现有模拟角色手动按 Q/E/R/F/1 核对动画和 UI 冷却；真实四服联机的 11170/11171/11173 回包顺序仍待联调。本轮未把代码构建视为画面验收。
- 独立复审补充发现快速重连可先切换连接代次、再于下一帧清理旧预测，导致技能核心保留待确认状态。代次切换现在先对当前角色回滚旧预测；角色直接切换时清除旧序号，回包应用前再核对当前角色与技能快照 RoleId。断线前已发出的请求仍保留过期序号供迟到 11170 校正原会话 MP/CD；直接注入的本地会话会清除这项资格。
- 返工后重新构建 `GM_Game.csproj`：0 错误、38 条既有警告；目标文件禁用语法扫描无命中，差异空白检查通过。
