# 移动同步与状态同步方案复核修改日志

## 修改日期

2026-09-26

## 需求目标

结合 LYFMMORGP 当前客户端、GateServer、GameServer 和公共实体层源码，复核 `项目分析/移动同步与状态同步设计.md` 是否可落地；将不可行或与项目现状不符的内容直接修订到文档中。本次不修改业务代码。

## 修改文件

* `项目分析/移动同步与状态同步设计.md`
* `项目修改日志/2026-09-26-MOVE-STATE-SYNC-DESIGN-REVIEW-CHANGELOG.md`
* `Docs/AI/CURRENT_TASK.md`
* `Docs/AI/TEST_REPORT.md`

## 修改内容与设计思路

1. 新增“项目现状复核与强制修订”章节，结论为方案方向有条件可行，但原方案不能直接开发。
2. 明确服务器采用“轻量校验后确认客户端位置”，本阶段不模拟 Unity 场景物理，不承诺穿墙、坡面高度或精确落点校验。
3. 记录 `SceneManager.Update` 为空、`EntityWorld` 未接入正式生命周期、`OnlineRole.SceneUid` 未正式绑定等 P0 前置缺口。
4. 将身份链固定为 `GateSessionId + UnitySessionId -> OnlineRole -> SceneUid -> EntityWorld`，禁止信任客户端角色 ID 或实体 UID。
5. 将移动上报调整为开始/停止/跳跃/技能等边沿即时上报，持续移动每 150~200ms 上报；服务器仍可每 100ms 批量广播变化快照。
6. 删除按固定 50ms 限制网络包位移的设计，改用服务器单调接收时间、实际收包间隔和约 1 秒滑动窗口校验。
7. 调整本地纠偏为 `MMoveComponent` 内处理，避免直接写 Transform 绕过 CharacterController；MVP 不宣称输入历史回放。
8. 补充三段跳的阶段、段数、垂直速度、起跳和落地边沿同步要求。
9. 修正 AOI Enter/Leave 差集方向，要求按地图与场景实例隔离并双向维护可见关系；旧 100m 同步与新 AOI 必须二选一。
10. 状态同步复用 `MMoRpgCommon.StateDefine`，Idle/Walk/Run 从移动信息派生；离散状态增加 `state_seq`，取消无条件 5 秒全量广播。
11. 协议提案增加 `scene_instance_id`、`move_session_id`、`snapshot_seq`、服务器时间、移动/跳跃阶段和状态序号，并移除与项目不一致的 `package LYFMMORGP`。
12. 补全 GateServer 上下行注册、路由和下行白名单，明确 GameServer 每协议一个 `IGameMessageHandler` 以及 `OnlineRole.SendToClient` 的发送责任。
13. 将实施顺序改为世界生命周期与协议接线优先，再验证基础移动主链，最后引入 AOI、状态和压测。
14. 将原第 6 节代码标记为已弃用的历史示意，防止把与当前接口不一致的参考代码复制到项目。

## 验证结果

* 独立架构审查 Agent 已完成只读复核，关键结论已合并到文档。
* 已核对 `GameGlobal`、`SceneManager`、`EntityWorld`、`OnlineRole`、`RoleManager`、`ClientMessageManager`、GateServer 转发链及客户端移动组件接口。
* Markdown 代码围栏数量为偶数，标题结构可解析。
* `git diff --check` 未发现本次目标文档的空白格式错误。
* 本次仅修改设计和协作文档，未执行客户端或服务端编译，也未执行 Unity Play Mode。

## 未解决问题与交接

* 第 6 节保留早期代码仅用于追溯，已经明确弃用；正式编码前应根据第 0 节重新输出具体类接口。
* AOI 格子尺寸、视野半径、上报间隔、广播间隔和可见人数上限仍需通过主城并发压测确定。
* Buff 同步尚未形成完整协议，本方案明确延后处理。
* 下一阶段若进入业务实现，必须按 P0 开始，并分别委派客户端、后端、代码审查和测试 Agent。
