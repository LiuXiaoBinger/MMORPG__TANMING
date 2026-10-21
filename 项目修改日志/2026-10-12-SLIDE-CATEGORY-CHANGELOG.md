# 滑行残影使用Excel技能类别

日期：2026-10-12。状态：已完成。

最终交接补充：并发成员整理造成当前文件与历史SHA不同，保留用户当前整理，历史清单不改写。MArtSkill的35个成员声明、函数体及相邻注释与已测副本逐字相同，仅成员排列和五空行不同。当前稿另行272源Rebuild零错/37既有警告，主核当前末稿与编译输入273项SHA一致；类别专项沿用已测14项，不扩测。追加证据在`_temp/slide-category-20261012/client/current-ordered/`、`current-final-audit.json`和`member-order-evidence.json`。

用户要求不使用`if (SkillId == 10080)`识别滑行，使用Excel中的技能类别。已核正式`#SkillInfo.xlsx`生成的`cfg.SkillInfo.Type`注释明确1普攻/2技能攻击/3滑行/4轻功；`MSkill.SkillType`是Art等逻辑类型，不是该类别。

唯一业务修改`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`：FireEvents一次注册时从现LubanMgr获取主表，用非空且Type==3条件登记原残影，补具体中文类别说明。无需新增Core字段/Initialize参数或新的枚举/框架，不更改源表/schema/协议、后端ID身份接入或输入绑定。

roll_client_develop负责业务修改，主只协调、核对和记录；冻结当前before，保最右F、约5米、白残影、0.5秒CD、跳跃限制及其他用户成果。

验证：272客户端源Rebuild零错/37旧警告；复用63正式源与原薄Unity/网络边界，本轮14断言通过。不同ID10081/Type3完整21影、10080/Type2无影、正式10080/Type3保持原影/白四0.2、500ms CD、667ms动作、根1.458485/空Charge。样例只内存生成表行，不改正式表或数据库。首临时XML编码名utf8假失败仅修测试为utf-8，保首日志，不返改业务。

主精确字节审计只有原if变为注释/具名主表局部/类别条件，其他字节及Core/MSkill保持；272编译输入SHA一致。无已复现剩余问题，未正式PlayMode联机，开发末稿及专项源/边界清单留证。

证据 `_temp/slide-category-20261012/`，分析更新 `项目分析/F_ROLL_SLIDE_DESIGN.md`。不清临时、不自动提交上传。
