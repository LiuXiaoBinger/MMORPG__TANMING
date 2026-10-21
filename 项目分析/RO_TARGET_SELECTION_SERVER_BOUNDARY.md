# RO 三种选敌的服务端边界

本轮用户要求移植点击选中、瞄准切换和技能自动选敌，沿用 RO 类名、成员和方法命名，简化复杂依赖。服务端职责先核查实际释放链与实体身份；结论是本轮可以只接客户端选目标与朝向辅助，无需改变正式技能协议、服务端技能或配置。

## 已核实际数据

源表 `Luaban/MiniTemplate/Datas/#SkillEffectInfo.xlsx` 与正式生成表 `LYF_Server/Network/LubanManage/Tb/tbskilleffectinfo.bytes` 都包含六个一级技能效果。按当前 Luban `ByteBuf.ReadUint` 压缩整数规则读取生成数据，六行、每行十八字段，与源表逐值相等且完整消费二进制。证据：`_temp/ro-target-selection-20261012/server/config-boundary.json`。

| 技能主表 ID | 效果 ID | TargetMode | 最小/最大距离、半径、目标数、命中与伤害字段 |
|---|---|---|---|
| 10010 | 2001001 | 0 | 全部 0 |
| 10040 | 2004001 | 0 | 全部 0 |
| 10050 | 2005001 | 0 | 全部 0 |
| 10060 | 2006001 | 0 | 全部 0 |
| 10070 | 2007001 | 0 | 全部 0 |
| 10080 | 2008001 | 0 | 全部 0 |

当前没有需目标的正式技能。`MSkillCore.CanStart` 与 `ROSkillComponent.CanSkill` 仍只支持 `TargetMode == 0`。当前字段注释仅定义“0 无目标表现、其他本期拒绝”，不能自行赋予非零值完整 RO 敌方/友方/地面模式。

## RO 与本项目的接口接入差异

RO 的 `MSkillCore.NeedTarget` 来自主表 `SkillTargetRangeType == ESTR_Target`，不是本项目 `TargetMode` 的现成等价枚举。RO `MNavigationComponent.OnAfterAction` 对需目标技能上传 `oArg.Target`，并在 `Common` 内打包施法 UUID 与厘米坐标；本项目 `SkillCastReq` 只有 `skill_uuid` 和 `skill_id`。

本项目正式服务端链仍是 `SkillCastHandler` 从复合会话找到 `OnlineRole`，调用其 `RoleSkillComponent : ROSkillComponent` 校验学习、职业、MP、CD、当前动作和组合段，回结果后广播开始。当前 `FireSkill` 只执行表现生命周期，没有命中伤害；客户端选出目标不代表服务器开始对其结算。

客户端 `MEntity` 已有 `_targetEntity`，`MSkillEventArgs`、`MNavigationEventArgs` 与表现实例已具备目标/坐标载荷入口，可用本轮最小接入让三种选择保存当前实体并在攻击前调整朝向。Type1/Type2优先当前有效敌人，缺失时自动选敌；没有敌人仍保留当前无目标施法。Type3滑步和轻功不使用攻击自动选敌转向。

## 敌友与世界创建边界

`EntityType` 是位标记。本轮可把普通怪物、精英和首领（Monster/Elite/Boss）作为当前敌方候选；主城其他玩家 Role、NPC、宠物、佣兵等不能因“不是自己”就成为敌人。现阶段没有正式 PvP/阵营关系系统，不能假装已经沿用 RO 完整敌友关系管理器。

客户端 `SceneWorldSystem` 的正式主城链创建本地 Player、服务器同步的其他 Role，`NpcManager` 创建 NPC。怪物/精英/Boss 有真实实体类型和 Builder/工厂入口，但当前生产源码未发现刷怪或怪物网络同步创建调用。主城没有敌人时，瞄准和自动选敌返回无候选是合理结果，不为展示选敌效果擅自在正式场景添加假怪物或把其他玩家认作敌人。

客户端 `SceneEntityWorld` 用本地 `_nextUid` 分配运行时 UID；它不等于持久角色 `RoleId`，也不能视作跨端统一实体 ID。后端 `OnlineRole.BindSceneEntity` 目前仅定义，未找到正式调用；后端 `SceneEntity/MonsterEntity` 与工厂仍属于未绑定正式在线角色世界的历史结构。因此本轮不上传客户端 UID，不扩目标伤害、刷怪、导航或 AOI。

## 验证与交接

本服务端分工只读业务源码、协议、源表与生成表，无服务端代码/协议/DLL/表/场景改动，无需重复服务端编译。配置二进制与源表一致、目标模式与零命中字段已检查；没有运行正式多服务或实际怪物场景。

客户端开发与独立审查继续验证点击实体、瞄准循环、攻击自动选择、无敌仍可施法、滑步不抢目标朝向及原 MP/CD/组合/移动行为。服务端原权威释放链保持。修改记录：`项目修改日志/2026-10-12-RO-TARGET-SELECTION-SERVER-BOUNDARY-CHANGELOG.md`。
