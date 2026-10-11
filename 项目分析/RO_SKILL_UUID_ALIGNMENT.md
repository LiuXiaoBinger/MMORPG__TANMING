# RO 施法 UUID 对照与当前项目边界

## 源码依据

- RO 客户端 `clientcode/CSProject/MoonClient/Entity/MPlayer.cs:213-224` 的 `NextSkillUUID` 在 1 至 9999 内循环。它是单次施法实例标识，与技能表的 `SkillTable.SkillUuid` 配置标识不同。
- RO 客户端 `Skill/MNavigationComponent.cs:367-412` 用同一个 UUID 发送请求并启动本地预表现；`Skill/MSkillComponent.cs:965-1005` 收到开始通知后按 UUID 找已有实例，找到时确认而不重播；`:366-381` 的结束事件按 UUID 查当前及已退出动作槽的实例。
- RO 服务端 `roserver/gameserver/protocol/action/rpcc2g_singingrequest.cpp:35-70` 从请求中解出 UUID 并原样传给施法事件；`rogamelibs/entity/skill/skill.cpp:70,641,1153,1173` 将它用于施法实例及开始、结束通知。
- RO 客户端 `Skill/MSkillCore.cs:112-118` 的连击本地重置窗口为 10 秒；服务端 `rogamelibs/entity/skill/skill_core.cpp:585-611` 使用默认 6 秒组合窗口。两端数值本来不同。

## 本项目对应实现

- 本地 `PlayerEntity.NextSkillUUID` 按 RO 的 1 至 9999 循环生成。`SkillCastReq`、`SkillCastRet`、`SkillCastStartNtf` 和 `SkillCastEndNtf` 共用 `skill_uuid`；服务端直接回传请求值，不另分配施法 ID，也不使用严格递增序号或历史结果缓存。
- 本地预表现与服务器开始通知按同一个 UUID 确认；已退出动作槽的 Q 连击旧段保留至对应结束通知，不能由后一段或不同 UUID 的通知回收。
- 2026-09-27 生命周期返工补充：旧实例列表并非仅供 Q 连击使用。任意活跃技能退出动作槽后均移入 `List<MSkill> _skills` 并继续逐帧更新；已学技能 ID 单独放入 `_learnedSkillIds`。重复开始事件必须在状态转移前按当前及列表 UUID 拦截，避免自转移影响新的当前技能。修改与返工证据见 `项目修改日志/2026-09-27-RO-SKILL-LIFECYCLE-CHANGELOG.md`。
- `SkillCastRet` 是当前项目网络链路的适配：失败反馈、权威 MP 和冷却快照仍由它交付。RO 将这些职责分配在请求 RPC 回复、SkillStart 和 SkillCoreFlows 中。只删除此消息会使现有界面及拒绝回滚失去数据来源，因此当前保留；它不作为动画开始的前置条件。
- 当前可释放的动画技能为 10010、10040、10050、10060、10070，均有表现配置。没有表现时不建立虚假的“无动画施法占用”；以后若要支持无动画技能，应参照 RO 的 `MSkillComponent.FireSkillNoAnim` 创建真实载体，并先补齐对应数据和规则。
- 当前场景加载与同角色重复登录回包会复用已经建立的本地玩家；角色会话清理才销毁它。现有调用链未显示同连接切场景时重建玩家并重置 UUID 的路径。

## 验证边界

以上对照仅覆盖本项目已有的无目标动画技能、学习列表、施法、连击和 MP/CD 同步。RO 的命中、伤害、Buff、子弹、引导和表现时间轴需要对应数据与服务端场景结算，不能仅凭当前动画施法链宣称已经实现。
