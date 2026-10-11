# 技能、走跳音效及根运动接入

日期：2026-10-10。修改记录：项目修改日志/2026-10-10-SKILL-AUDIO-ROOT-MOTION-CHANGELOG.md。

## 需求与真实依据

从只读 RO 参考项目取得技能、走路和跳跃声音，放入 Unity 工程 `Assets/GM_Game/Audios/Characters/MJS`，配置到既有技能编辑器。普通攻击 Q 三段、E、F 需要动画根运动向前位移。

RO 当前没有可直接复制的技能 wav；真实声音位于 `clientproj/Assets/artres/_FMod/Mobile/Action.bank`，事件映射需结合 strings bank、FMOD 缓存和技能配置核对。Library 中 Unity Timeline 示例的 jump/jog 音频不属于 RO 游戏音源，不可代用。提取必须保留来源和解码映射，不把事件名伪装为音频文件。

当前技能正式链只调度 FxEvents，音效及位移作者数据被导出器拒绝。本项目动画含根轨迹，新实体的旧 RoleCtrlBase.OnAnimatorMove 已直接返回，因此动画位移没有应用到实体。已有 MMoveComponent 合并水平速度与垂直重力后调用 MEntity.MoveCharacter，应继续作为碰撞移动提交入口。

特别保护：当前 Q1 作者资产已有 F0–15、Animation 驱动、rootMotionScale=1 的位移段，属于既有用户稿，不能覆盖。Q 特效仍为1/1/2、第三第二刀光与用户局部参数保持。原动画不改；预览不能回退到按重绘累积导致人物跑远的旧逻辑。

## 制作与正式执行契约

1. 复用 Y Effect 内 Audio 子类型制作单次声音，通过既有帧/FPS导出到 SkillRuntimePresentation.AudioEvents。新 SkillRuntimeAudioEvent 保存原序号、帧、TimeSeconds、Clip 强引用及 Volume，当前未支持的循环、区间或随机参数仍明确拒绝。
2. 复用 Y Motion 的 Animation 驱动及 rootMotionScale，导出 SkillRuntimeRootMotionSegment 的 StartSeconds、EndSeconds、Scale。仅指定 Q/E/F 制作有效窗口，不添加固定的凭空冲锋距离。原 Q1 窗口原样保留。
3. SoundComponent 沿 RO 模型持有的类名和职责，用 Unity AudioSource 适配播放及清理，不引入 FMOD 全套运行库。正式技能沿已锁定表现行和施法时钟派音效，资源冷加载/池复用不能串技能。
4. 实际 Animator 上的 SkillRootMotionCollector 接收动画 deltaPosition，只对当前占用技能动画槽且窗口有效的实例提取水平 XZ；交 MMoveComponent 与原重力合并一次 MoveCharacter。结束、切段、打断和回收要清除归属与缓冲，旧技能列表继续粒子生命周期不能继续推动角色。
5. 走路、跑步、起跳和落地属于默认动作，使用独立 SkillConfigSO 作者动作稿及强类型 SkillRuntimeLocomotionConfig 发布；保留旧表现表动画 Location，不制造可施放技能或服务器技能 ID。声音按实际动作时钟或事件派发。
6. 根运动预览从固定基准按采样时间确定位置，倒拖、重绘、回零和循环不能累积漂移；正式游戏移动由碰撞入口负责。现有编辑器接口、九类RO制作轨道与双语保留。

## 协作与验收

- audio_motion_assets：真实音源调查/解码/复制与来源映射、宿主导出和目标配置制作、编辑器根运动预览与手册。
- audio_motion_runtime：正式音效、走跳声、根运动数据与播放/移动/生命周期。
- audio_motion_review：独立基线、契约/源码审查、编译、必要实际源码回归与资源映射检查；失败打回开发。
- 主 Agent：范围协调、连接恢复、验收与记录。

所有临时产物在 `_temp/skill-audio-rootmotion-20261010`。正常 Unity 资产接口用于创建/编辑/保存，不猜 YAML 或 GUID；不启动第二个正式工程 Unity；当前 Editor 的 MCP/桌面连接不可用时，使用 _temp 隔离资产工程通过真实 Unity API 制作，仅经审查回拷目标配置与新资源 meta。不生成测试角色或按游戏键，不保存用户无关未保存资产。原生编辑器/动画根数据/音频实测与薄边界或隔离编译须分别记录。

## 实际音源和当前制作初值

声音来自 RO 原 `clientproj/Assets/artres/_FMod/Mobile/Action.bank`（SHA256 `6fea583aff90a77c75e9f07d72d4e03913751918fb77306b6036d4aee0207498`）。使用参考项目自身 FMOD 原生接口逐个读取实际子声音，导出24kHz/16bit PCM波形；未使用 Timeline 示例音源或空 FmodBank.zip。正式目录为 `LYF_MMO/Assets/GM_Game/Audios/Characters/MJS`。来源/样本下标/时长/声道/hash 完整记录在本轮修改日志及 `_temp/skill-audio-rootmotion-20261010/assets/audio-copy-manifest.json`。

12份声音含 sword_01、sword_02、_attack_sword2、两份 Knight 技能、GhostChargedEnergyBlast1、ef_magnumbreak、四份男性脚步和 Whoosh_BigJump3。脚步003保留作为可选原音源，当前步点使用001/002、落地004。这些是本游戏新制作初值，不冒称 RO 原技能的逐帧配置完全相同。

| 本游戏动作 / 表现行 | 音效作者帧 / 秒 | 初始声音 | 根运动 |
|---|---|---|---|
| Q1 / 3001001 | F48 / 0.4 | Skill_BasicAttack_Slash01 | 保留用户F0–15；120FPS导出[0,0.13333334)秒 |
| Q2 / 3001002 | F23 / 0.19166666 | Skill_BasicAttackF_Slash02 | 原完整动画窗口，倍率1 |
| Q3 / 3001003 | F23 / 0.19166666、F54 / 0.45 | Skill_BasicAttack_Slash01（0.8）、Skill_Combo_FastSlash（0.2） | 原完整动画窗口，倍率1 |
| E / 3004001 | F9 / 0.3、F36 / 1.2 | Skill_E_Phase01_HeavySlash（0.65）、Skill_E_Phase02_ShortSlash（0.3） | 原完整动画窗口，倍率1 |
| R / 3005001 | F63 / 0.525、F136 / 1.1333333 | Skill_R_Full（0.35）、Skill_R_Followup_HeavySlash（0.45） | 保持原无位移段 |
| F / 3006001 | F37 / 0.30833334、F142 / 1.1833333 | Skill_BasicAttackF_Slash02（0.65）、Skill_Combo_FastSlash（0.25） | 原完整动画窗口，倍率1 |
| 1 / 3007001 | F4 / 0.13333334、F9 / 0.3 | Skill_SwordDrop_Charge、Skill_SwordDrop_ReleaseImpact | 保持原无位移段 |

| 普通动作稿 | 声音帧（120FPS） | 实际源剪辑 |
|---|---|---|
| Walk | F4、F42 | Anim_MJS_Walk |
| Run | F4、F32 | Anim_MJS_Run_F |
| BattleWalk | F14、F62 | Anim_MJS_Walk_ZD |
| BattleRun | F9、F48 | Anim_MJS_Run_F_ZD |
| JumpStart | F0 | Anim_JX_Jump_Start |
| JumpEnd | F0 | Anim_JX_Jump_End |

走跑步点取本项目真实动画脚部曲线低点；六稿均skillID=0，不注册网络技能。MModel.Update驱动SoundComponent读取实际ReadyClipPath/NormalizedTime。二、三段起跳沿既有动画重播，落地立即恢复移动时由MJumpComponent调用零秒落地声音，实际落地动画另走时钟，避免同次双播。

## 关键实现入口与生命周期

- `SkillEditorAudioMotionSetup.PrepareAndExport`显式向七个原稿追加声音，只给空的指定Q/E/F位移列表补Animation段，并生成六普通稿。`ExportLocomotion`单独发布六普通稿；后续修改普通稿后需执行此入口，修改技能仍用既有绑定导出。
- `SkillEditorAudioMotionTools.ReadAudio/ReadRootMotion/ValidatePublished`负责强引用、帧秒映射和作者/运行一致校验；原Y单帧复制粘贴允许end<=key，区间/随机化等未实现内容仍拒绝。九类RO制作轨道的非默认正式发布门禁继续保留。
- `MArtSkill.Update`独立派发音效与粒子；`ReleaseAnimation`只解除动作槽和水平根运动，常规Q接段后单次刀声留在旧_skills自然播完。真实取消、结束回收和Reset停止声源，核心表现句柄持到声音清理后归还。
- `SkillRootMotionCollector.Init/OnAnimatorMove/ConsumeDelta/Release/Unbind`在模型创建时一次接管applyRootMotion；读取实际Animator.deltaPosition仅XZ，按当前实例、实际剪辑和半开窗口筛选。模型解绑恢复旧开关，换段不把旧缓冲交给新技能。
- `MMoveComponent.PlayerUpdate`将一次根delta与原输入/重力合并后调用同一次MoveCharacter。不使用RootT值直接推动正式角色，不绕过CharacterController，不改变服务器协议或施法权限。
- `SkillEditorRootMotionPreview.Evaluate`从零帧曲线及Avatar尺度计算绝对水平预览位置；烘焙XZ的动作保持原地，同帧重绘、倒拖、回零不叠加。`SkillEditorWindow.AudioPreview`仅拥有隐藏编辑声音源，按baseVolume播放，不修改正式相机。

## 资产制作执行边界

正式Editor的桌面native pipe和Unity MCP会话不可用。使用 `_temp/skill-audio-rootmotion-20261010/asset-worker/LYF_MMO` 独立资产工程，复制原真实源码、程序集名称、meta、表字节及相关资源，真实Unity2023执行生产PrepareAndExport。音频使用正式Editor自动导入生成的meta，避免临时工程另生GUID覆盖正式身份。临时GM_Game只编译纯数据所需源、裁剪其依赖，Tests临时排除；这些临时配置不回拷正式工程。

仅经独立验证后的七原作者/七运行表现、六普通稿及其meta、一份普通运行稿及其meta回拷。隔离工程未编译五类无关角色脚本，部分材质包Shader依赖不完整；没有保存/回拷Prefab、动画、场景或材质，也不把资产验证扩大成完整角色PlayMode/声音听感或碰撞距离通过。

## 多段声音调整（2026-10-11，沿用10月10日发起的任务归档）

针对用户反馈多段技能声音单调，E用两次独立短挥刀，R降低原长音并增加后续短挥刀，Q3/F降低第二刀过强的声音；F声音第二事件独立移至F142，原粒子F84不变。声音时点根据原手部动作曲线与样本包络核定，不把特效帧当作每个声音必然应相同的帧。原12波形保留，另增3份真实RO动作采样，普通走跳不改。

显式制作入口只识别上一版默认clip/frame/volume并迁移上述声音；自定义音量保留，其他声音事件和所有粒子/位移字段不重置。重复制作不能重复添加第二声。验收状态见Docs/AI/TEST_REPORT.md；方案项目分析/SKILL_AUDIO_POLISH_NOTES.md，修改日志项目修改日志/2026-10-10-SKILL-AUDIO-POLISH-CHANGELOG.md。离线试听仅按配置混合，实际GameView三维衰减与主观听感须用户确认。
默认声音迁移边界：已有旧事件仅音量被用户修改时，保留该旧事件而不迁移；若用户改变了旧事件的资源或帧，原事件保留，同时缺失的新版默认声音会追加。此入口会补默认制作事件，不代表对整份自定义声音稿完全不作增量修改；自行制作时可直接在原窗口编辑并通过绑定发布。
## 2026-10-11 音频命名及目录

MJX内十五份资源现按移动、跳跃、技能（通用挥刀/E/R/1/备用）分类，以中文用途命名；具体目录、原RO采样名对照和完整帧秒表见项目分析/AUDIO_ASSET_USAGE_GUIDE.md。本文上方来源记录中的原文件名用于追溯，不表示当前平铺路径。所有音频和原meta成对迁移，GUID/波形/导入参数保持；Setup加载字符串同步新路径，作者/Runtime强引用与声音帧秒音量不需重新制作。
## 全英文音频目录（2026-10-11最新布局）

整个Audios内目录和文件名均采用英文：角色声音在Characters/MJS/Movement、Jump和Skills；Skills分Shared、E、R、SwordDrop、Legacy。文件名前缀Movement_、Jump_、Skill_直接区分类别。原三份技能特效AudioSource声音在Shared/Skills/Effects，依实际Skill04蓄力/释放、Skill02蓄力引用命名。此前MJX中文分类章节属于上一轮迁移历史，当前目录及完整18声音对照以项目分析/AUDIO_ASSET_USAGE_GUIDE.md为准。

所有原wave/meta/GUID和目录身份保留，制作脚本同步英文路径，作者/Runtime与特效预制体的强引用延续；时点、音量、特效和根位移不因改名调整。正式归档项目修改日志/2026-10-11-AUDIO-ENGLISH-REFACTOR-CHANGELOG.md。