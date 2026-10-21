# 跳跃期间技能与普攻限制

## 需求与最小接入

用户要求角色跳跃时不能释放技能和普攻。复用当前 `MEntity.IsJump`，仅在 `MSkillComponent.CanStart(int skillId, float realtimeSeconds, int currentMp)` 的既有实体有效性条件加入 `Entity.IsJump`，没有新增状态、延迟执行、输入缓存或协议。

`SkillCtrl.RequestAuthoritativeCast` 先调用该校验，之后才读取下一施法 UUID、发出 11170 和启动预测。`MSkillComponent.BeginPrediction` 也调用该校验，因此直接预测和全部快捷键共用限制。Q 普攻与其他技能没有分立施法入口。

## 起跳与落地时序

`MMoveComponent.TryJump` 调用 `BeginJump`，后者在派发起跳事件前同步设置 `_isjump = true`。二段和三段跳维持同一状态。`PlayerUpdate` 在下降、接地且经过起跳保护时间后调用 `EndJump`，同步清除该状态，此后按原学习等级、MP、动作权限及 CD 判断能否释放。

因此起跳当帧至下落接地期间拒绝新的施法；已存在的技能收尾、权威结果同步和持续 CD 倒计时保持原流程。拒绝输入不发包、不建立预测 CD、不推进动作段、不触发技能动画。跳跃期间的输入不缓存，落地需重新按键。

## 后端边界

当前服务端 `GameServer/SceneEntity/MoveComponent.cs` 是空的组件壳，在线角色、技能权威组件与现有协议没有可用于判断真实跳跃的状态源。本轮完成客户端限制，没有虚构服务器跳跃字段或新增协议。服务器原有 MP/CD、等级和施法状态校验保持。

## 验证与交接

开发将冻结前源码与末稿分别用于同一真实技能输入契约：冻结前 Q 在起跳当帧仍创建请求/表现，契约失败；末稿 27 条针对性断言通过。运行链接 50 个正式生产源，包含完整 SkillCtrl、protobuf、实体、Move、Component、Core 与计时链；InputSystem、状态机、CharacterController、资源和网络交付为专用测试薄边界，未执行 Unity PlayMode 或真实服务器联机。

272 个真实 GM_Game 源码隔离 Rebuild 为零错误、37 条既有警告。开发 27 条与独立 93 条本轮专项断言均通过，均受上述薄边界限制，最终验收以 `Docs/AI/TEST_REPORT.md` 为准。正式修改日志为 `项目修改日志/2026-10-12-JUMP-CAST-BLOCK-CHANGELOG.md`，本轮临时证据保留在 `_temp/jump-cast-block-20261012/`，不清理、不挂测试组件到正式场景。
