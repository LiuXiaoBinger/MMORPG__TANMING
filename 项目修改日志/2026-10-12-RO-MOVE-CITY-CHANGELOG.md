# RO 移动同步与主城性能修改记录

- 需求：按 RO 移动同步接口和成员命名接入本项目，关闭主城多余灯光，调查低帧率；按角色职责开发、测试与验收。
- 分工：move_client、move_server、city_performance 分别负责客户端、服务端与场景性能，主 Agent 仅协调和记录。
- 修改准备：更新 Docs/AI/CURRENT_TASK.md 明确需求与验收标准；业务开发前留存当前用户稿，不覆盖已有改动。
- 设计思路：沿用参考实际结构和调用时序，只作工程类型与既有网络链的最小适配，不添加额外门禁、等待、重试、代次或通用框架。
- 主城实际范围：LYF_MMO/Assets/GM_Game/Scenes/Scene_MainCity.unity；在当前用户稿的几何预制体实例内增加 54 条实时点光组件禁用覆盖，没有关闭父节点或修改共享预制体。
- 主城设计与验证：原生重载确认实时灯 54→0、活动阴影灯 13→0，保留 1 个无阴影烘焙方向光；剔除新增的 9336 字节覆盖后与用户 before 完全一致。活动启动场景及 dirty 状态保持，预览测试场景已关闭。
- 主城性能实证：整个场景约 2020 个活动 Renderer、8,345,721 个活动网格三角形；历史一次加载 9036 毫秒。前者不等于每帧可见提交，后者不是稳定帧耗时，未取得稳态 CPU/GPU Profiler，不宣称具体 FPS 增幅或唯一根因。
- 临时预览曾因 RenderTexture 清理顺序产生诊断；解除相机引用后重新出图成功，业务未因此修改，失败情况如实保留在分析。
- 性能分析：项目分析/MAIN_CITY_PERFORMANCE.md；本轮移动范围与最终验收见下节及服务端分日志。
- 临时证据：_temp/ro-move-city-20261012/，待产品经理确认后清理。

## 客户端移动开发交付

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`、`LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`；分析新增 `项目分析/RO_MOVE_SYNC_CLIENT.md`。
- 修改内容：玩家按 RO `Update`/`UpdateMove` 0.1 秒变化上报，Time 秒/位置米；主城 11181 解析到既有角色事件；正式移动组件补 `onSyncMoveEvent`/`LagSyncUpdate` 和 RO 同名跟随变量。协议/共享 DLL 由后端开发独占修改，客户端仅接入。
- 设计思路：使用已有 MEntity 同步字段，保留原水平/跳跃/技能根/程序位移一次碰撞提交。真实坐标变化决定 IsMoved，避免没有方向输入的纯 Y/技能根移动被 RO 停止分支忽略；没有新增网络队列、等待、代次、重试或管理层。
- 开发验证：当前 277 个完整生产源 GM_Game Rebuild 0 错/35 警告；24 移动专项通过，72 完整生产源配真实请求/通知 Protobuf 编解码，玩家两个同步函数及主城接收函数逐字提取。Unity、UI、传输、角色业务容器为薄边界，不代表真人联机和原生跳跃验证。
- 静态验证：三个源码新增差分没有 namespace、foreach、三目或弱类型业务载荷；中文用途注释补齐，未改 F 特殊技能、CD、跳跃许可或根运动资源。全部用户 before 在 `_temp/ro-move-city-20261012/client/`；末稿、277 编译输入、268 引用和72运行输入 SHA 清单分别保留。
- 未解决与交接：暂无开发专项失败；独立验收由测试审查 Agent 完成后主 Agent 汇总。没有 RO RTT/导航高度/飞行/传送带外部系统，直接使用目标高度与默认追赶系数；远端跳跃动画不扩协议。正式 TCP、多服务联机、现场视觉和安装包未验，未自动部署/重启。

## 集成验收与交接

- 服务端16正式路径：GameServer/Components/ROMoveComponent.cs、MessageHandler/Role/MoveOperationHandler.cs、Entity/Role/OnlineRole.cs、manger/RoleManager.cs、MessageHandler/ClientMessageManager.cs、GameServer.csproj；GateServer/Ctrl/GateMoveCtrl.cs、Ctrl/GateClientResponseForwarder.cs、GateServerApp.cs、GateServer.csproj；Network/Proto/RequestEntity.cs、ResultEntity.cs、Socket/com/NetDefine.cs；ProtoBuf/proto/RequestEntity.proto、ResultEntity.proto（以上均位于LYF_Server）；客户端LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll。设计、22项开发与备份详见 项目修改日志/2026-10-12-RO-MOVE-SYNC-SERVER-CHANGELOG.md 与 项目分析/RO_MOVE_SYNC_SERVER.md。
- 与客户端3源和主城1场景合计20正式路径；主更新Docs/AI四协作入口与本日志，root_verify.py等验证工具只存_temp。
- 最终独立验收：移动30、F/跳跃/CD/root28、游戏服/网关回环TCP22共80通过，客户端277源0错35警告，Game/Gate0错11/4旧警告；原51协议类保持且protoc复生成一致。主核962去重输入SHA零漂移，日志真实PASS标记对应当前文件。
- 用户强调灯光只关闭：独立精差确认deleted_objects=0、deleted_components=0，全部60灯组件仍在，仅54个实时点光enabled=false，随时可启用。活动烘焙主光/共享prefab/全部原参数保持。
- 暂无已复现源码阻断；正式Unity两客户端/多服务联机、墙体/实际远端表现、包/Bundle及稳态FPS仍未验证，不将临时TCP或预览图当现场验收。后续重新编译配套协议，从原登录入口回归并采Profiler确认灯光关闭后的剩余瓶颈；临时失败与成功证据保留，不部署重启或自动清理。
- 最终主验收已读取独立中文review-report.md、final-audit.json及真实运行日志，12类末稿/编译/引用/运行清单共962去重文件保持。独立首次临时XML与根运动时间序列失败已保留，仅修测试夹具，无业务返工。
