# 双账号主城互见：服务端位置与入场顺序核对

## 2026-10-13 实际现象与边界

用户登录两个账号，之前互相看不见，截图中出现两个角色且质疑不能运动。本次服务端职责只核对当前在线会话、进入世界双方同步及移动协议，不添加AOI框架，不修改数据库或重启正在使用的服务。客户端改动及最终现场验收由对应职责另行记录。

## 实际在线证据

只读CLR读取GameServer进程61096，16:33:49启动，正式GameServer.exe为16:33:42生成。当前在线索引2项、会话索引2项：角色1为Gate1/Unity8，角色2为Gate1/Unity7，均Map1、Online、角色数据已装载；坐标分别67.47428/18.73892/78.14861与68.23897/18.73892/86.34387，相距约8.23米。主Agent此时Unity现场仅角色1，说明当前位置近不等于此前入场已经完成实体创建。

堆中还存在历史离线对象，不将这些对象误算当前在线：角色2旧Unity6的Pos为空，另角色1旧Unity2/3/4已离线。非挂起读取结果只代表采样瞬间，跨帧字段可能变化；两次采样在线角色、坐标与索引一致，LogicTick持续推进。

证据：`_temp/two-account-city-20261013/server/live-state-1.json`、`live-state-2.json`。诊断首次以64位宿主读取32位服务架构不匹配；改为已安装的x86 dotnet运行同一读取DLL后成功，不重启/注入/修改目标进程。

## 空坐标造成首次过滤的真实链

- `LYF_Server/CenterServer/DB/Modle/LoginModle.cs`：创角Pos为""，StartGame原样返回；这是历史默认数据，不属于本次新增。
- `LYF_Server/Network/Common/ExitUtils.cs`：空坐标解析为Vector3.Zero。
- `LYF_Server/GameServer/manger/RoleManager.cs`：HandleClientEnterWorld先BroadcastRoleEnterWorld，再SendNearbyRoles；两者依据同图及100米IsNearbyRole发送11100。客户端现出生点62.149/19.414/80.669距原点约103.75米，因此一方已移动到出生、一方快照还为空时，双方同步都被原距离规则过滤。
- `LYF_Server/GameServer/MessageHandler/Role/MoveOperationHandler.cs`：11180从复合会话找到在线角色后，直接调用正式ROMoveComponent；没有EnteredWorld状态门禁。
- `LYF_Server/GameServer/Components/ROMoveComponent.cs`：保存上报坐标后，SyncMove仅发送11181。客户端11181仅找已存在实体，不负责创建实体，因此首次11100漏掉后，后续坐标进入100米范围仍不能补建。

这解释了当前服务端两号已近、Unity只有本地角色。没有证据认为shared/Shader包能决定附近玩家协议同步；此前特效故障与该位置时序分开处理。

## 最小适配和实际验证

客户端在本地模型成功、初始化真实出生位置之后，先发送现有11180位置，再发送现有11070进入世界。StartGame已经注册在线角色和移动组件，所以原服务链即可在入场前保存出生坐标，再按原100米规则发送双方11100。无需服务端改默认出生常量、协议字段、动态AOI或重启GameServer。具体客户端UpdateMove首次强制上报细节由客户端开发/测试验收。

隔离测试使用当前正式GameServer.exe与Network.dll的副本及回环临时端口，在真实RoleManager/ClientMessageManager/MoveOperationHandler执行13项断言：空坐标解析原点、距离超100、先入场双方均无11100、首次位置上报可消费、只收到11181且不会11100补建、位置保存、再入场双方11100正确会话和身份。结果`ENTER_WORLD_POSITION_PASS checks=13`，原正式服务程序集与测试副本SHA256相同。

测试首次漏带Config出现技能规则加载错误，保留原日志；仅补齐临时Config后重跑，无该错误。专用测试编译0错/1条既有Protobuf版本匹配CS1702；CLR只读工具编译0错0警告。未新构建服务源码、不连接正式服务端口、不写数据库、不伪称正式双客户端重登已验收。

测试：`_temp/two-account-city-20261013/server/enter-world-position-runtime-final.log`；程序集证明：`deployed-assembly-hashes.json`。修改记录：`项目修改日志/2026-10-13-TWO-ACCOUNT-CITY-SERVER-CHECK-CHANGELOG.md`。
