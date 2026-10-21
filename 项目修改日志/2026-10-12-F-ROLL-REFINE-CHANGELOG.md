# 翻滚最左、位移5、原特效及连续释放

日期：2026-10-12。状态：实施与独立验收完成。

用户要求翻滚放技能栏最左、根运动位移5、加回之前特效、CD降低到可连续翻滚。沿刚完成10080/3008001技能接入，不新增协议/组件/自动连发/缓冲/气力或无敌玩法，F及原攻击2绑定保持，跳跃禁止施放保持。

主只协调与记录，roll_client_develop修改客户端资源，roll_binding_backend修改源表/正常生成bytes，roll_slide_review独立审查。当前完成稿冻结before，保所有其他既有修改。

实际正式范围共11路径，无新增业务类型或正式资产：

- `LYF_MMO/Assets/GM_Game/Prefabs/UIPrefabs/UIRoot.prefab`：仅F/Q/E三锚点X值形成F/Q/E/1/2。
- `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Editor/Authoring/SkillConfigSO_3008001.asset`、`LYF_MMO/Assets/GM_Game/Scripts/Data/Skill/Runtime/SkillRuntimePresentation_3008001.asset`：根倍率1→1.458485，作者/运行保持一致，原绑定/meta不改。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MArtSkill.cs`、`MSkillTimerArgs.cs`：为10080登记/交付原网格残影计时采样，追加无载荷强类型回调构造。
- `LYF_MMO/Assets/GM_Game/Scripts/Common/Utils/GhostEffect.cs`：已复现模型复用空网格的两缓存修复及修改范围中文注释。
- `LYF_MMO/ProjectSettings/GraphicsSettings.asset`：AlwaysIncluded仅追加原Ghost Shader引用。
- `Luaban/MiniTemplate/Datas/#SkillInfo.xlsx`、`#SkillEffectInfo.xlsx`：10080主CD0.5秒/效果500ms及描述。
- `LYF_Server/Network/LubanManage/Tb/tbskillinfo.bytes`、`tbskilleffectinfo.bytes`：正常Luban生成同步。

主表float CD支持0.5秒，无schema改动；667ms动作结束可再次按F。用户确认完整滑步约5米，绑定/meta/其他表/服务器源码保持。

实施事实更新：原特效是MainRoleCtrl旧Slider状态调用GhostEffect的白色网格残影，四参数均0.2，并非粒子。批准最小接入MArtSkill.FireEvents→MTimerMgr→InnerProcessTimer约30Hz快照及MSkillTimerArgs无载荷强类型回调；模型复用原GhostEffect，未就绪/离槽不出影，旧影0.2秒自然淡出。不增加残影编辑轨道或新组件类型。若原生复现失活再启缓存问题，才最小修既有组件。

布局按三锚点最小移动形成F/Q/E/1/2，保Q/E原相对顺序；根5可选问题超过60秒未答后按完整约5米实施，由原生水平3.428米校准倍率约1.4585，后续用户答复优先。不将原位移曲线改成程序Charge。

用户已明确确认每次滑步约5米。原生显式执行GhostEffect.OnDisable/OnEnable复现缓存顶点43749→0，批准最小CleanUp两缓存列表改为null重新收集，不改绘制算法；Shader目录虽收集但没有材质引用或加载，GraphicsSettings只追加原Character_Ghost到Always Included以供Shader.Find，未重写其他设置。实际客户端范围增加`Scripts/Common/Utils/GhostEffect.cs`与`ProjectSettings/GraphicsSettings.asset`。

后端四路径已完成，真实服务端连续三次动作结束再放、500ms与零MP/不重叠共17项通过；两表全行/真实读取及正常Luban产物一致，只有三个单元值改变，其他ZIP成员字节保持。本轮配置不变源码，254编译输入一致后复用原程序集，未把旧断言当新测试。客户端/独立终验待补。

客户端末稿272源Rebuild零错误/37既有警告，真实Unity作者Validate→Export→ValidatePublished七项通过、Runtime与真实导出字节一致。原生单Skill Controller80步120Hz水平(-0.038126,0,4.999854)米；真实原Shader白残影和F最左HUD两类原生图均主目检通过。原生模型停启只证缓存重收集，EditMode Destroy提示如实保留，不冒称正式释放/自然淡出或现场网络通过。

独立63生产源＋真实五bytes/权限JSON，34本轮专项通过：三次完整动作续按F、500ms时CD完但动作未完拒叠放、每次21个30Hz残影点/原白四0.2参数、冷配置400ms到达不补历史、中断杀未来Timer、未就绪/自然离槽不出新影、跳跃禁F/2。首夹具沿旧允许互断/默认未接地造成假失败，仅改测试用真实Catalog/实际接地，不返改业务迎合测试。保护审计与主最终SHA记录待追加。

终验保护审计PASS：6554基线6544保持/10授权变化，基线外GraphicsSettings与before独立精确比对；11正式既有路径，零新增资产/删除越界。主748项末稿/编译/原生/HUD/专项输入SHA一致。目标源码/设置/文档diff检查通过，HEAD中UIRoot既有序列化尾空格保持，本轮before三锚点差异无新空白问题。原动画/图标/旧技能/满资源/场景/跳跃门禁和所有其余用户修改保持。

无已复现剩余问题。重新运行或登录加载新配置，反复按F即可在每次完整动作结束接下一次，2仍原攻击；未做按住自动循环。正式PlayMode、原生墙体与真实TCP联机/Bundle发布未验，临时不清理、不自动提交上传。

验证计划：真实作者导出/发布反校验、必要原生HUD/动画FX，源表精确样式/旧行保护及正常Luban回读，连续三次施放/CD内拒绝/跳跃门禁，独立最小改动审查与末稿SHA。结果、实际路径、未决问题及交接待补。

证据 `_temp/f-roll-refine-20261012/`，分析 `项目分析/F_ROLL_SLIDE_DESIGN.md`，前轮日志 `项目修改日志/2026-10-12-F-ROLL-SLIDE-CHANGELOG.md`。不写活DB、不保存正式场景、不清临时、不自动提交上传。
