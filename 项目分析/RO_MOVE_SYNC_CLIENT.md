# RO 移动同步客户端接入

本轮用户要求按 RO 项目编写移动同步，接口与变量尽量一致，不追加未来玩法或防御框架。

## 参考和正式调用链

- `F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Entity/MPlayer.cs` 的 `Update` 在本帧实体移动后，按厘米位置变化、移动状态变化或朝向变化，在超过 0.1 秒时调用 `UpdateMove`。`UpdateMove` 上传当前位置、朝向和 `MoveComp.CostTime`，然后清除累计秒数与同步时间。
- 同项目 `Components/Common/MMoveComponent.cs` 的 `PlayerUpdate(float deltaTime)` 在方向判定前累计秒数。`onSyncMoveEvent` 保存目标、朝向、耗时、移动状态，`LagSyncUpdate` 按估算速度追赶，抵达后采用 `keep_moving` 维持或结束动画。普通地面分支抵达后不继续外推坐标。
- 本项目正式 `PlayerEntity.Update` 调用原实体更新与角色数据更新后接入这一同步判断；技能发送前原有 `PlayerEntity.UpdateMove` 空入口现在直接上传本段位置，未改技能调用链。
- `MainCityMgr` 注册 11181，解析后查找已有场景角色，转换为池化 `MMoveEventArgs`，沿原 `MEvent_Move` 事件进入正式 `MMoveComponent.onSyncMoveEvent` 和 `LagSyncUpdate`。

## 必需适配

1. 本项目逻辑更新使用整数毫秒，移动上报使用 RO 的秒：`CostTime` 在现有 `PlayerUpdate` 以 `deltaTime * 0.001f` 累加，在网络消息 `Time` 原样发送。现有水平移动、跳跃、技能动画根与程序位移仍合成为一次碰撞移动。
2. 直接使用 `MEntity` 已有 `sysnOldPos`、`_face`、`_lastSyncTime`；仅玩家增加 RO 原有 `_isMoving` 上次上报状态，未增加同步管理器、队列、确认、代次或重试。
3. 当前协议采用 `MoveOperationReq`/11180、`SyncMoveNtf`/11181，保留 `Dx/Dy/Dz/Face/Time/IsMoving/IsMoved` 语义，使用本项目 Protobuf 强类型字段，替代 RO `Common` 位打包。协议和 Network.dll 由服务端开发负责。
4. 本项目已有跳跃和技能根运动，位置比较包含 Y；`IsMoved` 表示本上报时间段确有位置变化或方向输入。单纯用方向输入会把纯 Y/技能根位移标成未移动，RO 的远端停止分支将不追赶，已用真实消息与远端组件专项确认，故采用该最小适配。
5. 本项目没有 RO 导航高度接口、RTT 管理器或飞行/传送带/公共载具体系，使用服务器目标 Y 和 RO 默认 1.1 追赶系数，不创建这些外部系统。现有模型持有 Quaternion，按原 90/2 度阈值和 0.2 插值处理水平转向。
6. 主城实例与已加载角色模型对应 RO 已进入场景的发送条件，创角不发送世界移动。未给移动添加额外角色数据门禁。
7. 远端影子停止不将整个移动组件置为禁用，因为本项目远端技能程序位移也由该组件消费；原技能位移消费保持原样。

## 文件范围和验证

仅修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs` 三个既有业务源，没有新业务类型或正式测试挂载。

开发实际 277 源 GM_Game Rebuild 为 0 错、35 警告；对照原 37 警告，本轮补完影子字段使用减少旧未读字段警告，原预留 `_predictedTime` 赋值出现一个未读警告，未为消警增加假计算。24 项专项通过，执行完整 72 个生产源（含真实 MMoveComponent、MEntity、事件、容器、协议编解码），玩家 `Update`/`UpdateMove` 与主城 `SyncMoveHandle` 逐字提取。

专项包含秒/米协议语义、0.1 秒节流、静止不发包、方向与停止包、只转向、Y 同步、技能发送前同步、RO 影子追赶/目标抵达/停止朝向/零耗时/微小目标/回收。Unity 原生、UI、Socket 传输、角色业务容器是薄边界；Quaternion 测试边界只执行水平数学，CharacterController 边界模拟脚下地面挡住向下位移，不声称原生跳跃或真人联机通过。

本轮客户端证据在 `_temp/ro-move-city-20261012/client/`，统一日志在 `项目修改日志/2026-10-12-RO-MOVE-CITY-CHANGELOG.md`。未重启/部署服务器、打包发布或修改 F 技能、CD、跳跃门禁、根运动资源；当前不接远端跳跃动画协议，位置高度通过原坐标同步。
