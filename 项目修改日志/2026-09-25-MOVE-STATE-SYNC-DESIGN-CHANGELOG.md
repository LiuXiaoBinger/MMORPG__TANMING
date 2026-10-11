# 移动同步与状态同步设计文档变更记录

- 日期：2026-09-25
- 类型：设计文档新增（无业务代码改动）
- 产出文件：`项目分析/移动同步与状态同步设计.md`

## 修改文件

| 文件 | 操作 | 说明 |
| --- | --- | --- |
| `项目分析/移动同步与状态同步设计.md` | 新增 | 移动同步+状态同步+AOI 综合设计文档 |
| `Docs/AI/CURRENT_TASK.md` | 追加 | 本轮任务记录 |
| `Docs/AI/TEST_REPORT.md` | 追加 | 文档产出验证记录 |
| `项目修改日志/2026-09-25-MOVE-STATE-SYNC-DESIGN-CHANGELOG.md` | 新增 | 本日志 |

## 设计思路

1. **选型**：本项目采用状态同步而非帧同步。理由：MMORPG 实体数量大（玩家/怪物/宠物/投射物），服务端权威、带宽可控、反外挂强；不要求客户端与服务端跑确定性逻辑。
2. **同步分层**：
   - L1 移动同步（位置/朝向/速度）：客户端 10Hz 上报，服务端 10Hz 批量广播，他机延迟 100ms 插值播放。
   - L2 状态机同步（Idle/Move/Attack/SingSkill/BeHit/Stun/Dead/Born/Collect/Climb）：事件即时广播 + 进视野全量 + 5s 定期校准。
   - L3 属性同步（HP/MP）：增量随下一个广播帧 + 5s 全量校准。
   - L4 生命周期同步：AOI 跨格差集触发 EnterView/LeaveView。
3. **关键参数**：
   - 上报/广播频率 10Hz（与服务端 50ms 固定帧对齐，每 2 Tick 广播一次）。
   - 插值延迟 100ms（2 个广播间隔），缓冲深度 2~3 个快照。
   - 外推上限 200ms，超时停住。
   - 速度校验容差 1.2 倍配置上限；单 Tick 位移上限防瞬移。
   - 调和分级：<0.5m 忽略，0.5~2.0m 用 200ms Lerp 平滑回拉，>2.0m 瞬移回拉。
   - AOI 格子 15m，九宫格覆盖 45m×45m。
4. **AOI 演进**：用 `AoiGrid`（格子坐标→实体集合）+ `ViewManager`（跨格差集）替换现有 `RoleManager` 距离 100m 附近广播；过渡期旧逻辑标记 `[Obsolete]`。
5. **协议新增**：`MoveSync.proto` 定义 ReqMoveReport/NotifyMoveReconcile/NotifyMoveBroadcast/NotifyStateChange/NotifyAttrDelta/NotifyEntityEnterView/NotifyEntityLeaveView/EntitySnapshotData；协议码注册到 `GameMessageProtocolRegister`，Handler 放 `GameServer/MessageHandler/Move`、`Scene` 子目录。
6. **代码约束**：所有参考 C# 代码块遵守项目规范——无 namespace、用 for 不用 foreach、用 if/else 不用三目、中文注释；落地时按"一类一文件"拆分。

## 验证结果

- 文档 8 章节齐全，无空泛内容。
- 所有推荐数值均给出具体值与依据。
- proto 字段编号无重复（已逐个核对）。
- C# 代码块括号匹配、类名一致。
- 本次无业务代码改动，未触发编译；Unity Play Mode 未执行（属于落地阶段验收）。

## 未解决问题

- `ViewManager.CollectDiffNotInB` 参考实现中差集逻辑已简化；落地时按"一类一文件"拆分到 `AoiGrid.cs`、`ViewManager.cs`。
- 服务端 NavMesh 穿墙校验 MVP 不做，预留 `INavMeshQuery` 接口。
- 延迟补偿（命中回退）属于战斗系统，不在本文档范围，后续战斗同步文档单独设计。
- 投射物推荐客户端本地模拟，服务端只发生成/命中事件，落地时需与战斗系统对齐。
