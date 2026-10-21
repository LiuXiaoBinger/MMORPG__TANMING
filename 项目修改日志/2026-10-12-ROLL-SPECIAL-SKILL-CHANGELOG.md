# 2026-10-12 F滑步恢复特殊技能流程

- 用户纠正：“改回去吧，这个也算一个技能，但是是个特殊技能，特殊判断一下就行”。覆盖前一本地移动动作需求，恢复原技能请求、预测、CurrentSkill、CD和服务器同步。
- 客户端开发已核前次末稿SHA并定向撤销自己的7源差异，恢复前次before字节；没有回退用户其他修改，SceneMgr获授权的一字符拼写修复保持。证据`_temp/roll-special-skill-20261012/`。
- 主表已有CanSlideCancel，语义为当前技能能否被滑步取消；10010/10040/10050为1，10060/10070/10080为0。特殊判断按新技能Type3与旧技能CanSlideCancel==1实现，不额外设计强断所有攻击或修改数值。
- 新业务范围：客户端`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`的CanSkill；后端`LYF_Server/GameServer/Components/RoleSkillComponent.cs`的TryCast。普通新技能沿InteruptSkill，滑步学习/职业/MP/CD/跳跃限制及请求/预测/广播保持；服务器Type3准入替代10080硬编码。
- roll_movement_check负责客户端，roll_special_backend负责服务器，roll_local_move_review独立审查。当前开发和验证进行中，不预记通过；正式分析`项目分析/F_ROLL_SLIDE_DESIGN.md`待补最新职责。
- 不新增类型、状态、框架、协议、表或资源，不发布或操作用户当前进程，临时文件保留。

## 开发末稿与验证

- 客户端新逻辑仅CanSkill增加主表Type3分支及中文注释，约8行判断；其余六源与本地方案之前的before严格字节一致，未保留TrySlide、SlideAction、双拥有者、一级显示或本地路由。服务端新逻辑仅TryCast前移一次主表读取、使用旧CanSlideCancel和替换类别准入，后续请求/结算/通知代码保持。
- 客户端272完整生产源Rebuild零错误、37既有警告。GameServer及Network/MMoRpgCommon Rebuild零错误、11既有警告；递归编译清单为233源/工程输入。开发报告分别为本轮证据目录`develop/client-report.md`、`backend/development-report.md`。
- 独立客户端63真实生产源24项专项通过：实表三个允许取消攻击、不同ID的Type3特殊规则、Type2原名单、Q→F真实请求及预测换槽、CanSlideCancel0拒绝、自滑步不提前重启、CD/跳跃/未学习保持、667ms动作/21原白残影与后续F请求。
- 后端真实生产程序集63项通过：允许/不允许取消、同UUID与拒绝时原活动保留、普通攻击不借滑步许可、MP/CD不退、500ms自断仍拒/667ms后续可放、学习/职业/死亡/MP门禁保持。所有校验通过才调用原旧End入口，再返回结果；handler发结果→新Start顺序仅源码核对，测试没有真实网关，不冒称线上广播录制。
- 主已读实际报告、运行日志与必要源码差异，核272客户端编译输入、233后端编译源/工程、1后端末稿、29后端程序集/配置/夹具、63专项生产源及42客户端配置/夹具SHA均一致，证据`_temp/roll-special-skill-20261012/root-manifest-check.json`。
- 后端首临时测试依赖绑定版本不符仅修_temp配置，首失败保留；客户端旧55项零网络动作测试已不适用于当前需求。约5米动画资产未改，沿既有原生采样证据，不重跑无变化的美术/测距。
- 独立最终`review/review-report.md`及`review/final-audit.json`均PASS，无需返工，主已读取并验收。四协作入口/分析同步，前次本地移动选择明确标历史；无剩余已复现阻断。下一步正式游戏重新运行体验F，安装包需同步客户端热更和游戏服代码；未正式场景/联机/包验收，不发布或清理临时文件。
