# 跳跃高度同步但远端播放走路动画

## 现象与协议证据

玩家 A 跳跃时，B 端能看到 A 上升，却仍播放走路动画。现有移动协议只传世界坐标、朝向、耗时与移动标志，没有传递跳跃状态。Y 坐标变化不能可靠区分跳跃、坡地与技能位移，因此不能据高度直接切跳跃动画。

旧 `项目分析/JUMP_SYNC_DESIGN.md` 属于未实现的扩展设计，本轮仅修复已有跳跃的远端动画，不引入服务器跳跃物理、独立消息或新状态管理层。

## 协议设计（架构职责先行）

| 消息 | 消息号与方向 | 新字段 | 含义 |
|---|---|---|---|
| MoveOperationReq | 11180，客户端→游戏服，原网关透传 | int32 jump_count = 8 | 0 表示落地/当前无跳跃，1～3 表示当前跳跃段 |
| SyncMoveNtf | 11181，游戏服→附近其他客户端 | int32 jump_count = 9 | 原样携带请求的当前跳跃段 |

两字段由原 protoc 生成 C# `JumpCount` 属性。保留旧字段号、坐标（米）、朝向（度）、Time（秒）和消息号。默认值为 0，旧字节仍能解析；旧服务不会主动转发新字段到新建通知，因此实际生效必须配套更新游戏服协议与实现、客户端协议和客户端跳跃接入。

## 服务端最小实现

正式链仍为 `GateMoveCtrl` → 原逻辑队列 → `MoveOperationHandler` → `ROMoveComponent.OnMoveStart` → `RoleManager.SyncMove`。仅在创建 `SyncMoveNtf` 时赋值 `JumpCount = action.JumpCount`；不改位置快照字符串、会话索引、范围广播、旧移动标志或位置结算。

关键文件：`LYF_Server/ProtoBuf/proto/RequestEntity.proto`、`LYF_Server/ProtoBuf/proto/ResultEntity.proto`、`LYF_Server/Network/Proto/RequestEntity.cs`、`LYF_Server/Network/Proto/ResultEntity.cs`、`LYF_Server/GameServer/Components/ROMoveComponent.cs`。

## 本轮验证记录

临时证据统一存放 `_temp/jump-animation-sync-20261019/server/`：

- 使用现有 `LYF_Server/ProtoBuf/bin/protoc.exe` 输出两份生成代码；与复制进正式目录的文件逐字节一致，除两个移动消息及反射描述外，51 个其他生成消息类保持原文。
- 现有 MSBuild 工程 Rebuild：Network 0 错误/4 既有警告，GameServer 0 错误/11 既有警告，GateServer 0 错误/4 既有警告；输出与中间目录均为 `_temp`。
- `JumpSyncSmoke.cs` 使用当前正式程序集、在线组件容器、真实消息队列、正式网关与网络封包，在临时回环 TCP 连接中通过 80 项断言。覆盖 1→2→3→0 的请求/通知 ProtoBuf 往返、正式移动组件原样广播、网关上下行、原坐标/朝向/秒耗时/移动标志保持、旧消息缺字段默认 0，以及原本人/远处/异图/离线过滤。
- 生成输出、51 个无关消息、246 份工程与编译源输入及正式插件哈希核对见 `validation.json`、`compiled-source-hashes.json`、`formal-input-manifest.json`；源码限定 `git diff --check` 通过。
- 客户端现有正式 `Network.dll` 更新为本轮协议构建产物（290304 字节），与独立 Network 输出字节一致；原目录没有 PDB，本轮不新增正式 PDB，调试产物保留 `_temp`。不替换 MMoRpgCommon 或 Protobuf 依赖。

专用夹具初次编译缺 `System.Numerics` 引用（CS0012），补测试编译引用后通过；生产工程没有该错误。夹具仍有项目既有 Protobuf 3.31.0/3.31.1 引用统一提示，使用原 GameServer 配置的绑定策略运行通过，未为本任务更换依赖。

本轮服务端没有部署或重启，也没有操作 Unity Play。回环测试证明协议与服务器透传，不代替正式双客户端跳跃画面验收；客户端动画切换与落地播放由客户端职责及主验收补充。

修改日志：`项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-SERVER-CHANGELOG.md`。

## 客户端接入与时序

1. 本地 `PlayerEntity.Update` 检测 `MoveComp.JumpCount` 与上次发送段号不同，立即调用原 `UpdateMove`。段号变化允许累计耗时为零；普通坐标上报仍保留原耗时、距离和节拍规则。
2. `MainCityMgr.SyncMoveHandle` 将通知段号传入原 `MMoveEventArgs`，事件克隆和回池一并处理该字段。
3. 远端 `MMoveComponent.onSyncMoveEvent` 对大于零的新段号调用 `ApplySyncJumpState`，设置既有跳跃状态并派发 `JumpStarted`。相同段号直接返回，避免每个移动包重播起跳动画。
4. 原 `MJumpComponent` 继续播放起跳、空中循环和落地阶段。远端不调用本地 `BeginJump`，不重复计算跳跃高度；位置继续由原影子追赶驱动。停止水平输入时仍允许空中纯 Y 追赶。
5. 收到段号零先记录目标状态，在影子实际抵达目标坐标时才派发 `JumpEnded`，避免远端仍在空中却提前走路。技能根位移已抵达同步点、残差为零的分支也交接该状态。
6. 落地后，本地按方向输入、远端按同步移动状态决定恢复走路或播放落地后待机。不改变既有技能动作优先级、动画资源或角色碰撞提交方式。

客户端五个源文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`、`LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/IEventArg.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`。

客户端开发侧使用现有工程的 280 份真实源 Rebuild，0 错误、38 条既有警告；冻结输入和末稿见 `_temp/jump-animation-sync-20261019/client/`。具体修改见 `项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-CLIENT-CHANGELOG.md`。

独立测试逐字提取生产方法，修前6项复现纯Y位移/Walk，修后37项通过，覆盖上述段变化发送、默认动作不覆盖、同段不重播、三段、落地抵达时序、纯Y/水平、技能根与程序位移、事件池与旧包默认零。专项保留真实Protobuf，模型动画为播放记录器，向量/碰撞/事件宿主为薄边界。独立280源编译0错误/39警告，其中较开发多的一条CS8032为独立环境原有Unity.Properties生成器缺System.Runtime6依赖警告，未修改依赖掩盖。详见 `_temp/jump-animation-sync-20261019/review/`。

主验收核对11条正式开发路径与527份去重编译/协议输入SHA一致，限定业务差分检查通过，证据 `_temp/jump-animation-sync-20261019/root-final-validation.json`。汇总日志：`项目修改日志/2026-10-19-JUMP-ANIMATION-SYNC-CHANGELOG.md`。

## 生效与验证边界

本轮只读检查当前 Play 已加载的类型：请求和通知都没有 `JumpCount` 属性，移动组件也没有 `ApplySyncJumpState`。这证明运行会话仍为旧代码，不能用磁盘源文件已修改或控制台没有错误推断当前画面已修复。证据为 `_temp/jump-animation-sync-20261019/root-live-loaded-before.txt`。

本轮未打断用户 Play、未保存场景、未部署或重启服务、未发布热更资源。后续需让两客户端加载新协议和业务代码，并让游戏服加载新协议和透传实现，再重登验证原地跳、移动中跳、二三段跳与落地停止。协议往返、回环 TCP 和生产方法专项各自证明对应代码链，不能替代正式双端动画画面、真实碰撞或网络延迟验收。
