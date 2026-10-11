# RO 普攻三段动画轮换对照

日期：2026-09-27。用户要求 Q/10010 普攻使用已有 Anim_MJS_Atk01、02、03，严格参考 RO。本文为只读源码对照及实施范围，验收记录见 Docs/AI/TEST_REPORT.md，修改日志见 项目修改日志/2026-09-27-RO-NORMAL-ATTACK-COMBO-CHANGELOG.md。

## 源码依据

参考根目录：F:/UnityPro/ro/Ro_cs422nb。

| 行为 | 源文件与行号 | 实际语义 |
| --- | --- | --- |
| 本地轮换 | clientcode/CSProject/MoonClient/Skill/MSkillCore.cs:99-128 | UsingTimes 在距 fire_time 超过 10 秒时置零，否则递增，对当前武器表现列表长度取模 |
| 表现选择 | 同文件:196-221 | GetDataByWeaponType 按 using_times 索引选择完整表现数据 |
| 本地时间 | 同文件:757-767 | 权威冷却同步或固定 CD 更新 fire_time |
| 发包后预表现 | clientcode/CSProject/MoonClient/Skill/MNavigationComponent.cs:383-413 | Send 成功后启动表现，传递 UsingTimes |
| 本人确认 | clientcode/CSProject/MoonClient/Skill/MSkillComponent.cs:955-974 | UUID 匹配后 ServerFire 更新索引并确认旧实例，不重复播放 |
| 远端表现 | 同文件:978-1005 | 开始广播携带 UsingTimes，远端以该索引播放 |
| 状态结束与实例结束分离 | 同文件:415-425、718-723 | 动画状态结束把当前技能移入 _skills 等待通知，清当前引用 |
| 可释放校验 | MSkillCore.cs:171-186、MSkillComponent.cs:281-284 | 按单技能/组 CD 校验，不额外锁定所有未确认实例 |
| 未确认回收 | clientcode/CSProject/MoonClient/Skill/MSkill.cs:456 | 技能表现时长加 2 秒超时；已确认实例加 10 秒 |
| 服务端轮换 | rogamelibs/entity/skill/skill_core.cpp:585-610、1099-1119 | CheckUsingTimes 检查间隔并递增/重置，GetUsingTimes 对表现列表长度取模 |
| 服务端窗口 | rogamelibs/table/global_table_manager.cpp:253 | SkillComboTime 默认 6 秒；与客户端硬编码 10 秒不同，保留原参考语义 |
| 服务端推进位置 | rogamelibs/entity/action/singing_component.cpp:558 | 成功进入施法流程时 CheckUsingTimes |
| 广播字段 | rogamelibs/entity/skill/skill.cpp:1182 | SkillStartInfo 发送 using_times |
| 组合技区别 | clientcode/CSProject/MoonClient/Skill/SkillLogic/MCombineSkill.cs:58-78、110-121 | CombineSkillList 自动推进子技能，本任务不采用 |

## 本项目最小实施范围

1. 保留旧技能主表与代码，新增 Q/10010 的表现变体 1、2，对应 Atk02、Atk03；变体 0 仍为 Atk01，各段 DurationMs 使用真实动画时长。
2. MSkillFactory 装配同效果、职业、模型和默认武器的全部变体，按 VariantIndex 排序。MSkillCore 保存本地轮换计数与最后释放时间；首次为 0，连续有效输入为 0→1→2→0，客户端间隔超过 10 秒返回 0。
3. 本次 MSkill 保存独立的索引、剪辑和时长快照；确认更新核心轮换计数不改变已经播放的剪辑。动画结束清当前表现槽位，旧实例按 RO 移入等待回包/结束的列表。
4. 删除单个未确认请求阻断后续动画结束后的释放这一限制。所有回包按 ClientCastSeq、ServerCastId 命中旧实例；旧预测超时回滚不能覆盖更新一次释放的 CD。
5. 开始广播增加 using_times，远端按权威索引选择动画，本地匹配预测后只确认。请求仍只发送序号与技能 ID，不信任客户端索引。
6. 服务端成功施法按 RO 默认 6 秒窗口推进，选择本段表现数据；Q 的权威动作占用时长使用本段 DurationMs，避免 Atk03 被通用 CastMs=2167 提前结束。
7. Q 现有 CooldownMs=500、MpCost=20 保留。其他技能数值、既有角色生成流程及正式资源保留。

## 操作与验收边界

- 每次有效 Q 输入释放一段；当前动作期间按键不缓存，不强制自动三段连放，不新增按住重复机制。
- 编译与静态审查必须覆盖客户端、协议生成、服务端及配置装载；独立审查发现问题后由开发角色返工。
- 用户自己在 Game View 手动检查 Atk01→02→03→01、超过窗口重置、动作期间移动阻断、Atk03 完整播放。禁止恢复自动角色/键盘探针。
- 真实联机需要检查本人不重播、其他角色正确三段、旧回包不覆盖新动作、MP/CD 按现有配置结算。

## 未决事项

RO 客户端 10 秒与服务端 6 秒窗口存在差异；本任务明确按各自源码移植，通过权威开始索引确认后续计数，不擅自统一窗口。纯编译不能证明手动动画或真实四服表现通过。
