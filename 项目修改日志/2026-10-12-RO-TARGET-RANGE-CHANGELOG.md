# RO 自动选敌距离核对与校正

- 用户先举10米技能选20米的经验例子，随后明确要参考RO真实计算；本轮不实现攻击距离乘二。
- 事实：RO FindEnemyBySkillBtn使用MPlayerInfo.MaxChooseDistance，来源为全局MaxChooseRange，默认18米，副本可OverrideChooseDis。技能UpRange另计算真正射程，导航负责到达后施法。本项目此前30米是简化参数，不是RO表默认。
- 最小业务范围：委派target_client只改MSkillTargetMgr.cs的MaxChooseDistance 30→18及相关中文注释；独立target_review编译、参考核对与原生18米选择专项。主不改业务，不增加寻路、目标结算、配置框架或服务器协议。
- 主维护Docs/AI四入口、项目分析/RO_TARGET_SELECTION_DISTANCE.md及当前实施分析；临时备份、脚本、构建、证据全部在_temp/ro-target-range-20261012。
- 实际业务文件：LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MSkillTargetMgr.cs，仅三行替换（30f→18f及两中文范围注释），保原字节BOM/换行；没有改其他业务/表/资源。精差client/client-final.diff、改前备份和末稿/280编译源/268引用SHA齐。
- 正式文档：Docs/AI四入口、项目分析/RO_TARGET_SELECTION_DISTANCE.md、项目分析/RO_TARGET_SELECTION_IMPLEMENTATION.md顶部当前规则提示及本日志；全部工具/备份/构建/测试清单位于_temp/ro-target-range-20261012。

## 验证结果

- 独立280源Rebuild零错38既有警告，原生完整末稿业务DLL28项通过：17.99/18含边界/18.01拒，技能自动/点击/真实Tab/瞄准共用方法/超距清理，优先当前/敌友死亡过滤；五攻击技能统一18，隔离内存AtkDistance1仍选17米、100仍拒18.01并恢复，F轻功不选。审查PASS无业务返工，不累计旧93项。
- 主root_verify.py核7清单/开发7RO参考/2073保护共2440去重文件SHA全部一致；原生DLL=本轮独立编译并有28项运行标记，只有授权单源变化。root-manifest-check.json PASS。无受影响以外的Timer/根/后端/HUD重测。
- 独立报告_temp/ro-target-range-20261012/review/review-report.md、final-audit.json，真实native-range.log；开发ro-range-reference-evidence.json含表与关键源码SHA/行。无本轮已复现未解决业务故障。

## 边界与交接

- 本轮按RO普通默认18米，不实现技能范围乘二；RO副本覆盖、Buff射程与导航追近未移植。当前无目标攻击只目标/朝向辅助，选中不代表伤害可及；主城仍无正式刷怪。
- 原生模型为专用GO，Global启动禁用/隔离世界初值反射设置，查询用未初始化SkillCtrl并读真实bytes；未正式联机/异步资源包/目标伤害/安装包，未部署重启发布。下一步重新运行正式游戏查看选择入口。
- 证据保留待产品经理确认清理。
