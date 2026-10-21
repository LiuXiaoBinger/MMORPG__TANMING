# 2026-10-20 落地后新方向输入的移动动画交接修复（客户端）

## 需求与范围

用户反馈跳跃后再按方向键，角色先保持待机姿态水平移动，过一段时间才开始走路。本轮仅修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs` 的 `Update`，保留已有跳跃同步及技能退场修复。不修改协议、主包 AOT 接口、服务器、角色资源或输入规则。

## 根因与设计

静止落地时，`OnJumpEnded` 将 `_isEnding` 设为真，启动落地表现。若方向键在这之后才按下，`MMoveComponent.onMoveEvent` 已设置方向输入及水平移动态，开始事件也标记了动画刷新，但 `MDefaultActionComponent.PlayAnim` 被落地动画保护提前返回。状态机随即清掉本次刷新标记，直至 `MJumpComponent.Update` 等完整落地剪辑结束才再次交接。当前 `Anim_JX_Jump_End` 元数据时长为 0.83333343 秒；这是可能的等待窗口，不是已测玩家端实际等待耗时。

在落地表现阶段每帧检查已有有效移动态：本地主角要求 `IsMoving` 与 `HasDirectionalInput`，远端沿用同步的 `IsMoving`。开始移动时先调用现有 `ClearJumpPresentation` 清掉落地保护，再通过原 `ForceToDefault` / `MarkTriggered` 交还默认动作，在正常动画调度阶段选择 Walk 或 Run。该分支位于技能接管判断之后、待播落地处理之前，不重播落地声音，不中断空中动作。

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`：仅在 `Update` 插入 16 行，使用现有字段、方法及状态机入口，没有新增类型、字段、接口或等待机制。
- 本文为客户端修改记录；独立验收和最终交接由主 Agent 汇总到 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md` 及本轮总日志。

## 开发验证

- 已按顺序读取四份协作入口及客户端职责；本轮修改前保存源码到 `_temp/landing-walk-slide-20261020/client/before/MJumpComponent.cs`。
- 本轮完整 280 份真实 `GM_Game` 源码 `Rebuild`：退出码 0、0 错误、38 份既有警告。日志 `_temp/landing-walk-slide-20261020/client/GM_Game-build.log`，编译输入哈希 `_temp/landing-walk-slide-20261020/client/compiled-source-hashes.json`。
- 末稿 `MJumpComponent.cs` SHA256：`96609F87AED28D589D584B9928C5CDF79C11BDEFD526A5F964FDDDD9385302D5`。
- 新代码仅使用中文说明、清晰分支；布尔表达式最多三项，无三目、`foreach`、命名空间声明和测试入口。
- 主 Agent 已核当前 283 份去重输入哈希与本轮证据一致，且本轮业务源码只插入 16 行；证据 `_temp/landing-walk-slide-20261020/root-final-validation.json`。
- 独立生产方法专项修前 32 项、修后 59 项均通过：前者验证旧问题与原有行为，后者覆盖落地后新方向输入的走跑交接、本地无方向残留、远端移动、空中和技能保持；不是两份数量相加的修后结果。使用真实 `MStateMachine.LateUpdate` / `MDefaultActionComponent.PlayAnim` 链，实体/Animator 边界为薄记录器，不代表 Unity 原生或正式双客户端画面通过，夹具 0.2 秒不作正式角色落地时长。
- 独立 280 源 `Rebuild`：0 错误、39 份既有警告；最终审查 14 项全真，末稿 SHA 保持。独立记录 `项目修改日志/2026-10-20-LANDING-WALK-SLIDE-INDEPENDENT-REVIEW.md`，证据 `_temp/landing-walk-slide-20261020/review/final-audit.json`。

## 验证边界与后续

本轮未操作 Unity MCP、未停止用户 Play、未构建或发布热更资源、未启动或覆盖打包客户端、未重启服务。新修复只有更新配套的客户端热更代码后才生效；已有主包与协议配套要求继续保持，本轮没有新增协议/AOT 变更。需要后续正式游戏验证静止落地后新按方向键、持续移动落地、Shift 跑步及远端观察的画面效果。
