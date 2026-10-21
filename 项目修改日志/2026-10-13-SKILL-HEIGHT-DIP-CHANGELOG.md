# 施法模型下沉排查（2026-10-13）

- 用户报告施法模型向下移动后马上复原，确认本地和远端两边都有；具体技能与阶段继续核对。
- 主协调客户端与独立审查，使用Unity MCP只读分层采样和临时隔离动画评估；业务/资源是否需要修改以实际证据决定。
- 正式45秒只读样本4082行，两角色均待机、世界Y恒18.73892，未捕获施法，不计故障复现。
- 原生正式OtherPrefab/Humanoid固定根评估8技能及战斗Idle×根开关，共18组：根Y均0，原生deltaY均0；若干技能含下蹲姿态、双脚最低值均高于根，不据骨盆变低直接判整角色下沉。
- 随后通过现有MSkillClassicsHandler正常入口请求当前角色已学10040，记录8秒Entity/Model/Animator/Hips/Feet高度；不强改状态或坐标，不绕过施法校验。
- 首次工具高度采样因工程内Global同名编译失败，改为按GM_Game程序集反射定位后成功；没有修改业务来解决工具类型冲突。
- 证据统一在 `_temp/skill-height-dip-20261013/`，正式记录与最终结论待追加；未保存场景、修改动画资产、发布热更或重启服务器。

## 最终定位与修改

- 用户补充所有技能且两端均有，并要求看施法后不能继续。10040正常请求被MP0拦截未入动作，不计下沉复现；改走正常10080入口捕获真实退场：根Y恒18.73892、真实Slider_F被提前替为空Skill、左脚低于根0.41695米，约55毫秒后Idle恢复。
- 前轮InState改为RO最新请求语义解决快速停走，但额外资源释放入口漏掉对旧语义依赖；本轮仅改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/AsyncObj/MAnimator.cs` 中TryReleasePendingSkillAnim，读取原生实际Skill采样状态并保留过渡保护。InState、其他方法、根运动、资产、协议、服务端保持，没有加字段、队列或框架。
- 末稿SHA256：D7FDACE77C8F26003B4DAC59354291DA9211E1B0CECC1DAF3BD35F8CEF37639D。
- 协作记录修改：Docs/AI/PROJECT_CONTEXT.md、CURRENT_TASK.md、TEST_REPORT.md；客户端/独立/后端职责日志和专题分析按根目录归档，详细路径见下方引用。

## 验证结果

- 开发280源Rebuild0错38旧警告，独立280源0错39旧警告/警告签名保持；两生产方法逐字抽取修前10项复现/回归、末稿10项通过。独立final-audit PASS，方法外逐字保持、两方280编译输入无漂移、19保护保持，20客户端冻结输入仅MAnimator变化。
- 主最终核两方编译与前轮四保护文件共564条/280去重路径SHA全一致，两生产方法文本与测试末稿相同、原生结果PASS，见_temp/skill-height-dip-20261013/root-final-hash-check.json；限定源码/入口/职责文档git diff --check退出0。
- 用户自然退出Play后Boot非编译，加载IL已包含原生状态查询且无InState调用。原生正式OtherPrefab/Humanoid/Controller与真实MAnimator隔离：最新请求FIdle/nativeSkill真/transition假缝隙仍保clip，12过渡帧保clip，真正退出后释放；20帧最低脚高0.1100968米，PASS。Console错误警告0。
- 此原生测试为Edit PreviewScene、独立元数据引用装配，不改变正式共享缓存、不走网络或全技能生命周期；18组完整动画固定根检查仅辅助定位，不计全技能退场联机验收。
- MP现场客户端Default/空CurrentSkill/CD0/CanSkill真，收费技能因MP0无法开始，零费用F已实际成功。后端两角色MP0/500、无活动技能、逻辑时钟正常20Hz；DB500/500。当前部署IL仅按表扣费、处理器一次TryCast；无回蓝实现，未获得完整消耗序列，不断言一次扣光500或历史重放。

## 遗留与交接

- 源码与隔离验证已通过，修后正式两端全技能及安装包尚未验；包按现有流程更新热更DLL/资源。未发布、重启、保存场景或清理临时。
- 不擅自补MP、修改数据库/消耗、移除MP校验或添加回蓝玩法；用户明确回蓝规则后再处理。当前已证直接原因，历史消耗过程尚未还原。
- 分析：项目分析/SKILL_EXIT_CLIP_RELEASE_HEIGHT_DIP.md、项目分析/SKILL_STUCK_SERVER_MP_DIAGNOSIS.md。
- 职责日志：项目修改日志/2026-10-13-SKILL-HEIGHT-DIP-CLIENT-CHANGELOG.md、项目修改日志/2026-10-13-SKILL-HEIGHT-DIP-INDEPENDENT-REVIEW.md、项目修改日志/2026-10-13-SKILL-STUCK-SERVER-READONLY-CHANGELOG.md。
