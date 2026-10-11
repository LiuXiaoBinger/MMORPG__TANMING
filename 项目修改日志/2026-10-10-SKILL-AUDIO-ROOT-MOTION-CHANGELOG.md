# 技能音效、走跳声音及根运动

日期：2026-10-10。需求：真实RO声音进入 `LYF_MMO/Assets/GM_Game/Audios/Skill/MJX` 并编入既有技能编辑器，包含走路/跳跃；Q三段、E、F按动画根运动向前位移。

方案及制作映射：项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md。操作：项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md。源码阅读：项目分析/SKILL_EDITOR_CODE_READING_GUIDE.md。

## 分工和文件范围

- audio_motion_assets负责真实音源解码复制、宿主音频/Animation位移导出、目标配置、编辑预览和使用说明；audio_motion_runtime负责正式声音、走跳和碰撞根移动；audio_motion_review独立审查与编译回归，问题打回开发。主负责协调资产执行、验收和记录。
- 正式运行11源：Scripts/SceneEntity/Skill下MArtSkill、SkillRuntimePresentation、SkillRuntimeAudioEvent、SkillRuntimeRootMotionSegment、SkillRuntimeLocomotionAction、SkillRuntimeLocomotionConfig；SceneEntity/3DModel下MModel、SoundComponent、SkillRootMotionCollector；SceneEntity/Component下MMoveComponent、MJumpComponent。六个新类型及meta，实际manifest在本轮runtime/manifest.json。
- 宿主Editor/Skill下SkillEditorAssetTools以及新增SkillEditorAudioMotionTools、SkillEditorAudioMotionSetup；集中ActionEditor下Window/SkillEditorWindow.cs、.Preview.cs、.SerializedInspector.cs、新.AudioPreview.cs，新增Infrastructure/SkillEditorRootMotionPreview.cs及对应meta。
- 音频12wav和真实导入meta；七原SkillConfigSO和七Runtime表现定点更新，六SkillConfigSO_MJS普通动作与一SkillRuntimeLocomotionConfig_MJS新资产及其meta。Data/Skill只配置不源码。
- 包中文手册/CHANGELOG及项目GUIDE/USAGE/阅读索引同步。原角色Prefab、正式场景、原动画、旧表格与二进制、原绑定GUID、Q特效1/1/2和3-2局部参数保护。

## 实现内容和设计思路

复用原Y Effect的Audio单次事件和Motion的Animation模式，新增强类型运行数据；声音保持资源强引用、秒时钟、固定音量。七技能声音按挥刀/蓄力/落剑原特效帧制作；四走跑按真实脚部曲线低点，起跳与落地零帧，另覆盖现有战斗Walk_ZD/Run_F_ZD动画。六普通稿skillID=0，不增服务器技能/协议。

根运动读取真正Animator.deltaPosition水平XZ，由MMove与原重力合并一次MoveCharacter。当前动作/模型/clip归属匹配、窗口有效才消费，切段/退出/池重置清旧buffer；不直用RootT推正式角色或绕碰撞。模型Init一次接管applyRootMotion，Unbind恢复旧开关，避免每技能setter重新初始化破坏Q接段。原Q1 F0–15/倍率1保持，120FPS含末帧转[0,.13333334)秒；空Q2/Q3/E/F初始全动画倍率1，R/1没有加位移段。

编辑根预览按固定零帧曲线+Avatar尺度绝对计算，烘焙XZ时保持原地；重绘/倒拖/回零不叠加。窗口专用隐藏预览声音对象取baseVolume，不改正式相机。

原九类RO轨道、程序位移、多段/裁剪动画、区间或随机声音等未接入内容仍按非默认门禁拒绝正式发布，保留编辑保存能力。原统一动画接口、skill_uuid、对象池、旧_skills列表、700ms接段窗保持。

## 音源来源和身份

真实RO源 `F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/artres/_FMod/Mobile/Action.bank`，SHA256 `6fea583aff90a77c75e9f07d72d4e03913751918fb77306b6036d4aee0207498`。通过参考项目自己的FMOD低层API读取真实子声，逐个SeekData(0)解码24kHz/16bit PCM；不复制源bank meta，不使用Library示例或空ZIP。原FMOD事件名/样本映射结合strings bank、FMODStudioCache查证。下面的波形hash与实际目标一致；制作帧映射详见方案。

| 文件 | bank样本下标 | 声道 | 时长（秒） | SHA256 |
|---|---|---|---|---|
| sword_02.wav | 28 | 2 | 0.780625 | `89070f1a196a541b471c084a0e5f3308caba6389866816ecc4919217000c19a1` |
| sword_01.wav | 364 | 2 | 0.780625 | `49eb0e2bff9ea36946475d1c1319b934f9c27bab2b8782ebb27c073d0a7dce4f` |
| _attack_sword2.wav | 205 | 1 | 0.280542 | `21ed45e38a247d0a33929c022d4fd2c3152f5fc60da11e30c24f6acaa0b7b9d7` |
| Player_Knight_ShiZiFenXun.wav | 365 | 2 | 1.801958 | `01c00b71e32acbc7615767f3c04c4aeed5f5d9192f0e9715368451238ffe68f4` |
| Player_Knight_JianDingDaJi.wav | 666 | 2 | 1.306417 | `5534e37d377bb1b7d103c48095c31012afb9fcf9def2a49755c383097e8694dc` |
| player_male_footstep_base-001.wav | 140 | 2 | 0.405583 | `514787bb56f67231d30d3e80cdb38217fc5dbda5636e44f728df495cc150ba74` |
| player_male_footstep_base-002.wav | 439 | 2 | 0.359167 | `122181cdbf5279bf7dfdb4d0df8dd9536d3856202dc2903ab3438e7dec314992` |
| player_male_footstep_base-003.wav | 487 | 2 | 0.317083 | `266ed6ac093cb87d538934883352e45c5aa1548a5e0917a4fe2a1405c4316e07` |
| player_male_footstep_base-004.wav | 585 | 2 | 0.555542 | `14416b2a9ceeb6ca6a295a0e8b9f655166d384a491aba10948692ecc41820292` |
| Whoosh_BigJump3.wav | 257 | 1 | 1.182458 | `eb084f70e0cf1efe7da4fe7b01608bc69c68144e0614470f3ce63e2d45889eaa` |
| ef_magnumbreak.wav | 407 | 1 | 1.113083 | `0c12923a06360db5eb992135559630b4f52eb525071b3af0226777c1d984d6f4` |
| GhostChargedEnergyBlast1.wav | 228 | 1 | 1.171833 | `dd03fd1d4f6ac1533b9a2d5560e4a1a4db82eca7c3fe6bacec6c2374c8757a5c` |

## 资产执行和验证

正式桌面native pipe及MCP会话不可连接；已有本地MCP8081启动但instances仍空，未用其他UI替代。使用_temp隔离资产工程复制真实源/程序集身份/原meta/相关依赖/原表，实际Unity2023生产API制作与保存。正式Editor自动刷新生成音频meta后同步至隔离工程，保持正式音频GUID。仅目标配置经保护检查回拷，原Prefab/场景/动画、临时asmdef不回拷。

六程序集真实源隔离Rebuild均0错，运行265源；既有警告37/2/4/2/2/2。真实运行薄边界36断言、编辑导出/预览薄边界24断言通过。Unity原生AssetDatabase/导出/资源强引用/作者运行映射/预览方向和回零162断言通过，二次PrepareAndExport全Skill目录70文件SHA保持。原生报告 `review/native-asset-result.json`，HumanScale实际1.2893449；曲线预览全窗口正向位移Q1约.315、Q2约1.446、Q3约2.098、E约3.581、F约3.791米，仅为预览数据，实际动作槽窗口和碰撞会影响游戏位移。

## 审查返工和边界

- 首审发现ReleaseAnimation截断Q刀音：动作槽退出只解除根/动画，已开始声音和后续表现留旧_skills自然收尾；真正取消/回收Reset停止并归还依赖，已复验。
- Y音频粘贴单帧可能end=key，导出按原<=key单帧语义允许；区间仍拒绝。原预览借用Camera音源可能为空，改专用隐藏对象并应用音量，已复验。
- 隔离首导入缺真实Network.dll与测试NUnit，第二轮缺SkillFxSocketResolver，环境补入真依赖、Tests只在临时工程排除后成功，正式代码未为测试stub改写。
- 原生首验仍期望1技能一音，与最终蓄力+落剑两音候选不符，保留失败证据；测试期望核实际生产源和两FX后更新，复验通过。不是修改生产迎合测试。
- 原生资产工程省略五种无关角色业务脚本且部分Shader依赖不全，警告留存；没有生成游戏角色、自动按游戏键、挂正式测试组件、保存未保存用户资产或另开同一个正式工程。
- 尚无正式PlayMode按键、音频听感、原生Animator.deltaPosition+CharacterController现场距离或YooAsset完整Bundle发布验收；不得把薄边界/曲线预览当这些已通过。

证据全部 `_temp/skill-audio-rootmotion-20261010`；临时文件未经产品经理确认不清理。最终配置回拷与独立保护结论在验收完成后追加。

## 正式配置回拷与冻结复核

- 28文件已由资产开发Agent完成保护回拷：14原asset、6新普通作者及meta、1LocomotionRuntime及meta；14原asset写前SHA逐项等于生产输入，全部新路径写前不存在。正式12声音GUID由实际Editor导入产生，worker同步正式身份。
- 独立920项基线895保持、25允许变化、丢失0、未manifest变化0；七原作者全既有粒子字段、Q1原位移段保护，Unity重新序列化的九空轨道/全零后处理为默认表示，没有新增执行含义。主重核22运行+41资产源/音源/文档+28配置的91路径SHA无差异。
- 本轮恢复尝试启动的MCP服务会话59145已结束，不留下本轮后台服务；原正式Editor保持运行。

## 最终验收

独立总审PASS，无代码或资产阻断。920基线895保持、25计划修改，58新增全部在manifest，无删除或未计划修改；正式28配置与Unity原生验收产物SHA完全一致。16运行源和7编辑边界方法冻结稿无差异；末稿六程序集0错、36运行/24编辑/162Unity原生断言通过。新源/修改范围中文语义注释及命名空间、foreach、三目限制已审查通过。开发与测试报告并存于本轮_temp/runtime、assets、review。现场手动测试及完整Bundle发布边界如前，不扩大为已测；本任务开发与配置集成完成。
