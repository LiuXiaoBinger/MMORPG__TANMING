# F翻滚/滑步与旧F技能移至2

日期：2026-10-12。状态：开发与独立验收完成。

用户要求原Anim_JX_Slider_F用于位移翻滚/滑步，查RO或本项目图标，F翻滚，原F技能改2。已找到现成动画、Skill_330600紫色图标与10080滑行配置；优先沿现技能接口、动画根运动/碰撞和权威施法补齐缺少配置，不造通用框架。

roll_reference_design负责参考/动画/图标事实，roll_client_develop负责客户端及表现资产，roll_binding_backend负责源配置/绑定/权威类型接入；主负责协调、独立验收和记录。

范围实施前明确，保全部当前未提交成果；数据库先只读。原F→2要以真实绑定接入，不能只换文字；不擅增无敌/气力玩法，现跳跃禁止技能保持。

实际修改范围：

- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`：现有专用模拟11080将10060由F改2，新增10080/F；不新增正式业务Mock。
- `LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAssetTools.cs`：仅在原动画目录门禁中增加用户指定滑行动画的精确路径。
- `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/SkillConfigSO_3008001.asset`、`SkillEditorBinding_3008001.asset`及meta，`LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3008001.asset`及meta：新增完整120Hz滑步作者、绑定和真实导出表现。
- `LYF_Server/CenterServer/DB/Modle/CentRoleModel.cs`：已有10080回包固定F、等级至少1；10060旧F改2。仅规范11080回包，不写活数据库。
- `LYF_Server/GameServer/Components/RoleSkillComponent.cs`：沿原权威施法允许已接入的10080/Type3，原学习/职业/状态/MP/CD检查保持。
- `Luaban/MiniTemplate/Datas/#SkillInfo.xlsx`、`#SkillEffectInfo.xlsx`、`#SkillPresentationInfo.xlsx`：10080耗蓝改0及描述同步，新增2008001/3008001。保原表结构样式与旧行业务值。
- `LYF_Server/Network/LubanManage/Tb/tbskillinfo.bytes`、`tbskilleffectinfo.bytes`、`tbskillpresentationinfo.bytes`：正常Luban生成并仅同步必要共享数据；未改schema/生成类/客户端DLL。
- `项目分析/F_ROLL_SLIDE_DESIGN.md`与四份`Docs/AI`协作记录：记录参考、规则、验证与交接；补回CURRENT_TASK已完成气力修复的缺失标题，不改历史事实。

设计沿原10080身份、现成动画和图标，动作667毫秒、原6秒CD、零MP无伤害；单Animation根窗口[0,80/120)秒、倍率1，ChargeList为空。沿当前朝向滑步，Collector→MMove→实体碰撞唯一提交，不复活旧SliderState，不增加无敌、气力玩法、输入缓存或新协议/组件。

已完成客户端272源Rebuild零错/37旧警告、两服务Rebuild零错，真实Unity Validate→Export→ValidatePublished七项通过；正式Runtime与导出字节一致。原生真实Avatar独立采样前滑约3.43米、转向90度方向随面向，不将此数作为正式距离保证。HUD闲态/CD原生四图完成，主目检F位移图标/2旧攻击/双满资源及暗怒气通过。

独立24项专项链接59生产源，覆盖F/2真实输入/请求、冷表现/根窗口、根量唯一消费/提交、667ms收尾保CD、跳跃/下降拒绝及落地恢复。后端19项权威施放、7项真实ORM11080只读回包通过，三源表/真实Luban回读及三正常产物一致；活DB前后SELECT相同。首临时依赖/编码/解析失败只修测试，日志保留。

独立保护审计通过：6545客户端/服务端基线6538保持，7授权变化；3既有源表另与事前原件精确比对，新资产仅6路径，无删除越界。所有旧动画/图标/HUD/气力场景/跳跃门禁/用户注释与本机设置保持。主1010项末稿/编译/原生/HUD/专项输入SHA一致，目标diff检查通过。当前无剩余已复现问题。

证据 `_temp/f-roll-slide-20261012/`，分析 `项目分析/F_ROLL_SLIDE_DESIGN.md`。重新运行测试或登录重建11080/技能；墙体碰撞/网络为薄测试边界，未正式PlayMode、真实联机或发布Bundle，不清临时、不挂正式测试组件、不自动提交上传。
