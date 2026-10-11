# 2026-09-27 RO 技能子类与大剑特效

## 需求与依据

用户要求对照 RO 解析并融合技能子类。用户最终确认 1 号大剑下落是特效，飞行物暂不开发。参考分析见 `项目分析/RO_SKILL_SUBTYPE_ALIGNMENT.md`；协作与验收入口为 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。

## 修改范围

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：抽象生命周期父类，保存 UUID、计时、核心引用和池清理。
- 同目录 `MArtSkill.cs`、`MArtSkill.cs.meta`：承接动画，消费 10070 原剪辑的两条特效事件，使用既有粒子配置；实例归还具体子类池。
- 同目录 `MSkillFactory.cs`、`SkillLogicType.cs`、`SkillLogicType.cs.meta`：当前配置均借出 Art；保留 RO 类型编号，未实现没有配置支撑的组合或引导分派。
- `LYF_MMO/GM_Game.csproj`：登记新增源文件。
- `LYF_MMO/Assets/GM_Game/Prefabs/Role/Role_MJS.prefab`、`Role_MJS_Other.prefab`、`Role_JX.prefab`、`Role_JX_Other.prefab`：仅将旧 `RoleAnimaBehaviour` 组件启用位改为 0，保留组件及其他已有差异。
- `_temp/ro-skill-pool-tests`：扩展正式源码链接的纯内存夹具；所有测试与构建产物留在根目录 `_temp`。
- `项目分析/RO_SKILL_SUBTYPE_ALIGNMENT.md` 与两份协作入口：记录分析、边界和验收结果。

## 实现思路

RO 的普通表现由 `MArtSkill` 承担，弹体由其对接弹体管理器，不存在独立“飞行技能”子类。`MCombineSkill` 按配置串联子技能，`MChannellingSkill` 处理引导阶段；本项目现有数据均未定义这些行为，因此本轮只接实际使用的 Art 分支。

10070 读取已加载动画中的 `PlayEffect` 事件和 `Jx_Skill04.asset` 引用，播放蓄力及落剑粒子，保持共享配置只读。粒子按自身配置寿命延迟销毁，避免服务器结束通知截断尾迹。Q 仍用 `UsingTimes` 轮换原动作；原表、协议、动画和粒子资产保留。

## 审查与验证

客户端开发、独立代码审查和纯内存测试分别由子 Agent 完成。首轮发现四份正式预制体旧事件处理器仍启用，已返工关闭并逐组件复核。客户端与独立构建均为 0 错误、42 条既有警告；首轮纯内存夹具 148 项断言通过。

第二轮审查发现剪辑加载超过动作状态时长可能漏播特效，已打回并修复：10070 独立持有原路径 `MAnimationMgr.ClipInfo`，旧施法存活时继续更新事件，回收时对称解除引用。未注册跨池复用的完成回调，不依赖会被下一技能替换的动画槽。`MSkill.Reset` 清理说明同步修正。

最终独立静态复审通过；独立构建 0 错误、42 条既有警告，产物 `_temp/skill04-independent-review/cold-final`。纯内存夹具最终 171 项断言通过、退出码 0，日志 `_temp/ro-skill-pool-tests/result.log`。新增回归覆盖实际组件动作槽结束和旧列表移交、同槽新技能替换、冷资源到达、拒绝和池复用不补发、剪辑引用最终归零。目标技能代码禁语法扫描无命中，预制体差异空白检查通过，仅行尾提示。

## 未解决与交接

实际按键的目标 UID 为零，大剑沿用角色前方 10 单位落点；没有接入敌人锁定或伤害结算。已经播放的粒子在服务器拒绝后仍播放有限尾迹。冷加载补播只适用于 UUID 实例仍存活的情况；服务器结束、拒绝或超时已回收后不补发。真实 Unity 画面、挂点、YooAsset 冷加载和四服联调未验收，由用户在已有模拟角色场景手动确认；没有新增生成角色、按键探针或飞行物组件。
