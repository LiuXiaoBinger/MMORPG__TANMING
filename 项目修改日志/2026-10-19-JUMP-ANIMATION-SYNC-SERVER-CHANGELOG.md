# 2026-10-19 跳跃动画不同步：协议与服务端最小修复

## 用户问题与设计思路

A 跳跃时 B 能看到 A 上升，却播放走路动画。现移动协议已有 Y 坐标与移动标志，但缺当前跳跃段；服务端没有可转发的跳跃状态。依照架构职责先定义两现有协议尾字段，再按服务端职责仅原样转发；不用高度猜跳跃、不新增消息、服务器物理、排队或确认机制。

专题：`项目分析/JUMP_ANIMATION_SYNC.md`；原 `项目分析/JUMP_SYNC_DESIGN.md` 仅历史扩展设计，不作本轮实现依据。

## 修改文件

| 路径 | 本轮内容 |
|---|---|
| `LYF_Server/ProtoBuf/proto/RequestEntity.proto` | MoveOperationReq 尾部追加 int32 jump_count=8，0 落地/无跳跃、1～3 当前段 |
| `LYF_Server/ProtoBuf/proto/ResultEntity.proto` | SyncMoveNtf 尾部追加 int32 jump_count=9，同语义 |
| `LYF_Server/Network/Proto/RequestEntity.cs` | 原 protoc 重新生成，提供 JumpCount 属性及序列化 |
| `LYF_Server/Network/Proto/ResultEntity.cs` | 原 protoc 重新生成，提供 JumpCount 属性及序列化 |
| `LYF_Server/GameServer/Components/ROMoveComponent.cs` | 原通知初始化增加 JumpCount=action.JumpCount，补中文参数说明及透传边界注释 |
| `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` | 原 Network 工程独立构建后更新原客户端协议插件，未改插件meta或依赖 |
| `项目分析/JUMP_ANIMATION_SYNC.md` | 协议字段、服务端链路与当前验证边界 |

原全部源修改完整备份在 `_temp/jump-animation-sync-20261019/server/before/`。没有覆盖或回退其他已有修改。四入口和项目难点由主 Agent 汇总。

## 验证结果

- 现有 protoc 生成输出与两正式 C# 逐字节一致；51 个无关消息类保持原文，原字段号和消息号不变。
- Network Rebuild 0 错误/4 既有警告；GameServer 0 错误/11 既有警告；GateServer 0 错误/4 既有警告，全部输出/中间文件在 `_temp`。
- 正式程序集与组件、逻辑队列、网关、临时回环 TCP 专项 `JUMP_SYNC_PASS checks=80`。覆盖一/二/三段与落地的上行和通知 ProtoBuf 往返、服务器通知透传、网关上下行、原坐标/耗时/朝向/移动标志保持、原附近过滤及缺字段默认 0。
- 编译源与工程共 246 项 SHA 记录在 `compiled-source-hashes.json`；6 个正式协议/业务/插件路径的 SHA 见 `formal-input-manifest.json`；结果见 `validation.json`，实际回环输出见 `jump-runtime.log`。
- 正式 Network.dll 290304 字节，SHA256 `4429175E2D87C4F8D00CC6E78D6B178955AA7BAE6EAEEAD7428EDED718E6DBBF`，与独立 `network-bin/Network.dll` 相同。PDB 保留 `_temp/jump-animation-sync-20261019/server/network-bin/`，原插件没有正式 PDB 因而未新增；Game/Gate引用各自同源构建，不声称因调试路径不同的文件哈希完全相同。
- 源码限定 `git diff --check` 通过。测试夹具初次缺 System.Numerics 引用（CS0012），补引用后编译/运行通过；仅夹具引用修正，生产三工程未发生编译错误。

## 未解决与交接

未部署、未重启游戏服/网关、未操作 Unity Play，未验证正式两客户端画面或安装包。旧客户端默认不上报跳跃段，旧游戏服创建通知不会赋新字段；须配套使用新客户端协议与客户端动画接入、新游戏服协议和透传实现才会实际生效。网关本身是字节透传，没有新增业务代码。主 Agent 统一处理客户端测试、正式生效边界和四入口记录。
