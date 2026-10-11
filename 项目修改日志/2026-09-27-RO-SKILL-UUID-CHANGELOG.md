# 2026-09-27 RO SkillUuid 技能链整改

## 目标与依据

用户要求按 RO 现成技能链实现，不再维护本项目自加的双施法 ID。RO 的单次施法 `skill_uuid` 由客户端生成，并贯穿请求、预表现、开始和结束通知。源码对照与适配边界见 `项目分析/RO_SKILL_UUID_ALIGNMENT.md`。

## 修改文件与内容

- 客户端：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs` 生成 1 至 9999 循环 UUID；`Scripts/controller/UIController/SkillCtrl.cs`、`Scripts/SceneEntity/Skill/MSkill.cs`、`MSkillCore.cs`、`MSkillPrediction.cs`、`Scripts/SceneEntity/Component/Action/MSkillComponent.cs`、`Scripts/SceneEntity/event/eventclassArg/SkillStartedEventArgs.cs` 统一用同一 UUID 配对请求、预表现、结果、开始及结束通知。开始通知可先于结果到达而确认已有动作，不重播；Q 连击旧段仍按各自 UUID 回收。删除无动画纯占位状态及按技能 ID 等结果再播放的分支。
- 服务端：`LYF_Server/GameServer/Components/RoleSkillComponent.cs` 和 `GameServer/MessageHandler/Skill/SkillCastHandler.cs` 原样沿用客户端 UUID，删除服务器 ID 分配、最大序号门槛与历史结果缓存；`ProtoBuf/proto/RequestEntity.proto`、`ResultEntity.proto` 及 `Network/Proto/RequestEntity.cs`、`ResultEntity.cs` 统一协议字段，废弃结果包旧字段号 5；`Tests/SkillAuthoritySmoke.cs` 更新专用验证。生成后的 `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` 与服务端同步。
- 文档：新增 `项目分析/RO_SKILL_UUID_ALIGNMENT.md`；在 `项目分析/RO_SKILL_SYSTEM_DESIGN.md` 顶部标出旧双 ID 章节已失效；更新 `Docs/AI/CURRENT_TASK.md` 和 `Docs/AI/TEST_REPORT.md`。
- 保留已有 `SkillInfo` 主表、效果表、表现表、模拟学习包及 Q 连击 700 毫秒状态配置，未改非目标资源或场景。

## 设计边界

当前 `SkillCastRet` 承担已有网络链路的拒绝、MP 和 CD 同步，不作为动画开始门槛。RO 通过请求 RPC 回复、SkillStart、SkillCoreFlows 分担这些职责；直接删除本项目结果包会丢失现有 UI 和拒绝回滚数据。当前可释放的五个动画技能都有表现配置，因此清理了没有真实 RO 载体对应的无动画虚拟占用。

## 验证与交接

- GameServer 隔离编译 0 错误、11 条既有警告；客户端最终隔离编译 0 错误、38 条既有警告。正式客户端 `Network.dll` 与服务端构建产物 SHA-256 一致。
- 独立测试重新编译运行 `SkillAuthoritySmoke` 通过；目标协议/源码禁用语法及差异检查通过；无动画占位返工独立复审通过。详情见 `Docs/AI/TEST_REPORT.md`。
- 未解决：Unity Game View 的真实按键、Q 连击视觉与移动阻断、四服联机顺序及远端广播尚未实测。此轮仅证明已有无目标动画技能链的 UUID 对齐，不代表 RO 的命中、伤害、Buff、投射物或完整表现时间轴已经移植。
