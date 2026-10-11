# YCombatEditor 技能制作与发布

编辑器沿用 `SkillEditorWindow`、`SkillConfigSO`，本地包为 `LYF_MMO/Packages/com.ethan.act-action-editor`。菜单 `Tools/ACT Action Editor/Open Editor` 保留；游戏角色继续使用 `MAnimator` 和 `MSkillComponent.PlayAnim`，正式预制体没有另挂 Y 播放组件。

编辑器源码集中到`LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor`，原程序集和meta保留。`Window/SkillEditorWindow.cs`为入口，Timeline、Inspector、Preview、Persistence等分文件分别负责轨道、属性、预览和保存；`Infrastructure`为资源/撤销/迁移/帮助工具，`Timeline`为轨道注册与特效分类。各文件补中文用途和数据语义注释。位移、碰撞、飞行物、音效、镜头和流程等现有制作入口全部保留；正式游戏导出支持范围仍以“已接入范围”为准。

## 界面语言

技能窗口顶部提供“语言 / Language”下拉框，可选“中文”和“English”，默认中文。切换后工具栏、窗口标题、轨道名称、属性标签、下拉选项及提示即时更新；个人选择保存到 Unity EditorPrefs，重开窗口或编辑器后继续使用。

语言只控制显示，技能名称、用户自定义轨道名、资源/挂点路径与配置字段值保持原输入。右键菜单按当前语言显示，原 Unity 菜单入口仍是 `Tools/ACT Action Editor/Open Editor`。宿主技能绑定与施法规则 Inspector 共用当前语言。验证结果与未决事项见 `Docs/AI/TEST_REPORT.md` 及 `项目修改日志/2026-10-07-YCOMBAT-EDITOR-LANGUAGE-CHANGELOG.md`。

## 配置位置

全部新增技能配置位于用户指定目录 `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill`：

- `Editor/Authoring/SkillConfigSO_<表现ID>.asset`：七条 MJS 表现的可编辑 Y 资产。
- `Editor/Authoring/SkillEditorBinding_<表现ID>.asset`：表身份、预览模型与原事件秒数参考。
- `Editor/Authoring/JX_Attack01_Reference.asset` 与对应 ReferenceBinding：原 JX FBX/特效的独立参考，不替换 MJS Q。
- `Runtime/SkillRuntimePresentation_<表现ID>.asset`：游戏读取的已校验表现，粒子使用真实强引用。
- `Editor/Authoring/SkillConfigSO_MJS_*`：走/跑、战斗走/跑、起跳/落地六份声音动作稿，没有技能表ID。
- `Runtime/SkillRuntimeLocomotionConfig_MJS.asset`：上述六普通动作的声音运行配置。
- 特殊施法规则由独立规则配置共享主技能 ID，不随表现异步加载；规则入口见该资产 Inspector。

技能代码另按用途保存：运行逻辑和配置类型位于 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill`，宿主生成/导出工具位于 `LYF_MMO/Assets/GM_Game/Editor/Skill`，专用资源验证脚本位于 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/SkillVerification`。`Data/Skill` 只保存配置资产、JSON与对应元数据；编辑配置继续放在其 `Editor/Authoring` 子目录。

旧主表、效果表、表现表、动画、`RoleAtkConfig` 和原角色预制体保持原资产身份。生成器只创建缺失的编辑资产；已经编辑的资产不会再次从旧事件导入覆盖。

## 操作

1. 首次选择 `Tools/技能/YCombat/生成缺失技能资产并导出`。`SkillEditorAssetTools.GenerateAndExport` 通过 Unity AssetDatabase 建立资产和 meta，同时维护专属 YooAsset 运行表现收集组。
2. 选择某份 `SkillEditorBinding`，点击“用 SkillEditorWindow 编辑”。窗口载入其 Y 资产和对应现有角色预制体，由窗口创建自己的隔离预览副本。
3. 在“动画 / Animation”和“特效 / Effect”轨道编辑触发帧、粒子和位置。动画目前要求原剪辑的完整单段，起点/裁剪/混合均为零。时间轴采用原剪辑真实 `frameRate`；原事件浮点秒按最近整数帧导入，初始误差不超过半帧，绑定保留 `OriginalTimeSeconds`。
4. 在原窗口点击“保存 / Save”。随后在绑定 Inspector 点击“校验并导出当前表现 / Validate and export this presentation”，或选择 `Tools/技能/YCombat/校验并导出全部表现`。保存编辑资产本身不会偷偷发布运行配置。
5. 使用 `Tools/技能/YCombat/验证已保存资产与依赖`，重新核对旧表身份、已发布动画/时长、每条事件全部字段与粒子依赖。该入口验证资产，不代替角色实际画面验收。

## 已接入范围

预览角色绑定预制体时，窗口副本放到世界原点，保留原朝向与缩放；显式动画从固定角色根和 Animator 根初始姿态采样，再重建 Animation 位移窗的水平根位置。重复采样同帧不累积跑远，拖回零帧回原点，骨骼与挂点保持当前姿态。选择特效事件后，在 Scene 视图按 F 聚焦该事件位置，使用“播放 / Play”、“上一帧 / Previous”、“下一帧 / Next”或“帧 / Frame”检查。根曲线预览不替代正式碰撞、重力或动画混合；Program 位移仍只绘制参考。场景源沿用原实例化姿态；带变换父级的世界对齐和旧时间模式未扩展。原修复记录见 `项目修改日志/2026-10-08-SKILL-EDITOR-PREVIEW-MOTION-CHANGELOG.md`。

七条正式表现为 Q 三段 `3001001/3001002/3001003` 与 E/R/F/1 的 `3004001/3005001/3006001/3007001`。完整时长及 Q 的 700 毫秒状态窗来自旧表。2026-10-08按用户要求修正Q刀光：根据用户后续纠正，首两段各一条，第三段两阶段。第一段复用现有JX_Attack01参考刀光F48/120FPS（0.4秒），第二段F23/120FPS（约0.191667秒），第三段F23/F54均为普攻Effect_Attack01刀光（约0.191667/0.45秒），第二次沿EffectPos挂点使用局部位置(0.92,0.36,0.07)米与局部欧拉角(298.69,254.45,98.72)度。作者轨道与运行配置同步，正式MJS动画保持。

普通粒子支持 `ActorRoot`、`NamedSocket`、`TargetTop`、`CasterForwardWorld`、`WorldAbsolute` 五种定位。2026-10-08移除运行时旧事件后，`10070` 的蓄力和落剑也从编辑器轨道发布并运行，使用统一粒子校验。默认30FPS下第4帧蓄力、第9帧落剑；可编辑触发帧与已支持参数，不再因它是10070而禁止修改。

`NamedSocket` 使用完整 Transform 路径。`CasterForwardWorld` 对照旧 skill03_2：角色前方六米，原场景坐标不叠加到落点，Y/Z 朝向来自角色。`TargetTop` 预览目标来自工具栏“预览目标 / Preview target”，未设目标明确提示前方兜底，不能据此声称目标头顶已验证；游戏使用实体模型高度，预览用渲染边界高度。

导出器检查所有序列化顶层字段，不论轨道显示或隐藏；支持固定音量 Audio 单次事件、Animation 水平根运动窗口，以及下述 Program 前向程序位移子集。未实现轨道、区间或随机音频、区间重复 FX、自定义寿命/粒子倍率、超出已支持子集的 Program 参数、忽略重力/碰撞、动画裁剪或多段拼接均明确失败。新增九类 RO 轨道继续只制作保存，未接入事件非默认内容阻止发布。Y 的取消优先级不转写为 RO 互断；主技能特殊规则使用独立配置。

## 音效与根运动操作

RO WAV 保存在 `LYF_MMO/Assets/GM_Game/Audios/Characters/MJS`。在 Effect → Add → Audio 创建声音事件，选择声音、Start frame 和零至一的 Base Volume。End frame 不大于起点时是单帧单次，粘贴后End等于Start也能导出；Preview audio 试听使用窗口专用隐藏声源。技能声音修改后和粒子一样，在绑定 Inspector 发布。

Motion → Animation 使用原动画水平根位移和 Root Motion Scale，Y轴仍由正式移动/跳跃处理。当前Q1原F0–15窗口保留，120FPS含末帧结束为约0.133333秒；Q2/Q3/E/F无段时初始使用全动画倍率一。不要把 Animation 模式下不执行的 Program Max distance 当作角色前移距离；前移来自动画根曲线，倍率控制幅度。

Motion → Program 使用 Forward 方向、Self 起点、固定施法朝向与水平累计曲线，发布为运行资产 ChargeList；开始帧与结束帧直接除以作者 FPS 换秒，结束帧不加一帧，结束不晚于开始时为瞬时段。maxDistance 是前向距离硬上限（米），curve 的值是累计距离比例。Continue 允许曲线下降时回拉，Clamp 不回拉，Stop 在回拉或到达上限时结束。Program 与 Animation 窗口不得重叠；相邻区间可以接续，瞬时点不得落在 Animation 半开窗口内。

Program 的目标移动、输入或定制方向、动态方向、纵向、忽略重力或碰撞、alignFacing、目标偏移及到达限制尚未接入，非默认值会阻止发布。Forward/Self 下默认 target 不参与运动计算，不能用它表示追目标。保存制作稿后，仍需通过原绑定入口显式导出，已有正式资产不会自动改写。本轮实现与独立验收见 `项目分析/RO_SKILL_MOTION_IMPLEMENTATION.md`，修改归档 `项目修改日志/2026-10-11-RO-SKILL-MOTION-CHANGELOG.md`。

`Tools/技能/YCombat/编入RO音效与MJS根运动` 是显式制作发布入口，保留已有粒子和位移段，并创建缺失六份普通动作稿。Q3/E/R/F的声音只对上一版默认资源、帧和音量完全匹配的事件执行定点迁移；用户自定义音量和其它声音保留。普通动作可在原窗口选择对应 `SkillConfigSO_MJS_*` 和 Role_MJS 预览模型编辑，然后保存，执行 `Tools/技能/YCombat/导出MJS走跑跳声音` 单独发布。走/跑与战斗走/跑步点来自原左右脚最低帧；起跳和落地声音为零帧。稿件有未保存修改时制作或发布停止，不替用户覆盖。

## 资源与运行边界

YooAsset 专属组只收集 `Data/Skill/Runtime`，编辑资产和绑定不会混入该组；粒子强引用构成真实依赖。运行缓存按表现ID异步预取，单次 `Fire` 锁定本段ID、动画和原表时长；冷配置就绪后按已累计时钟补发本段事件，不使用旧动画事件。已生成粒子独立持有源表现租约，根/子粒子为有限单轮，真实尾粒结束后释放。

此说明不把编译或 AssetDatabase 依赖核对当成实际 GPU 预览、Game View 按键或服务器联调通过。实测证据以 `Docs/AI/TEST_REPORT.md` 与实施修改日志为准；设计参考 `项目分析/YCOMBAT_EDITOR_INTEGRATION_DESIGN.md`。

## 位置字段速查

本地包的“特效 / Effect”属性页使用“定位方式 / Placement kind”、“挂点路径 / Socket path”与“前方距离（米）/ Forward distance (m)”，工具栏使用“预览目标 / Preview target”。输入单位分别为定位枚举、完整层级路径、Unity 米；“旋转 / Rotation”为度。以下表格用原枚举值说明数据语义；切换语言时下拉会显示对应中文名称。

| Placement kind | Offset/Rotation 语义 | 跟随标志 |
| --- | --- | --- |
| ActorRoot | 角色根局部坐标与相对欧拉角。 | Follow actor=true，Use World Space=false。 |
| NamedSocket | Socket path 所指节点局部坐标与相对欧拉角。 | Follow actor=true，Use World Space=false。 |
| TargetTop | 目标头顶或角色前方兜底，加世界 Offset，世界 Rotation。 | Follow actor=false，Use World Space=false。 |
| CasterForwardWorld | 角色前方米数加世界 Offset，Rotation.x 是倾角，Y/Z 来自角色。 | Follow actor=false，Use World Space=false。 |
| WorldAbsolute | Offset 是绝对世界坐标，Rotation 是世界欧拉角。 | Follow actor=false，Use World Space=true。 |

普通粒子 Custom lifetime=0，预览与游戏的根/子粒子克隆均为有限单轮，通过真实 IsAlive 保留延迟发射及尾粒；原预制体不修改。窗口 Stop 或循环重置会清理其隔离预览实例。显式自定义寿命继续使用 Y 原预览截断方式，本游戏导出拒绝该设置。

本轮实际导入的 JX_Attack01 为 120 FPS，PlayEffect=0.40227696 秒，导入到 Y 的第 48 帧（0.4 秒），初始量化误差约 -2.277 毫秒。MJS 的 R/Q2/Q3 也为 120 FPS，E 与 1 为 30 FPS；生成器统一读取各自实际 clip.frameRate，不假定所有动画为 30 FPS。旧 JX 右手挂点经实际 Avatar RightHand 语义映射到 MJS 的唯一 hand_r 路径，未知或歧义路径拒绝导出。

详细实现说明：`项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md`。本地兼容改动记录：`项目修改日志/2026-10-07-YCOMBAT-EDITOR-INTEGRATION-CHANGELOG.md`。

## RO 扩展制作轨道

时间轴新增九类制作入口，添加方式与现有轨道相同；窗口顶部语言选项同时控制新轨道与属性显示。

| 中文 / English | 保存字段 | 主要编辑内容 |
| --- | --- | --- |
| 旋转 / Rotation | RotateList | At、End、AngularVelocity（度/秒） |
| RO震屏 / RO Camera Shake | ShakeList | At、Time、坐标系、时长内震动次数、三轴/FOV振幅与曲线路径 |
| 镜头特效 / Camera Effect | CameraFxData | At、CameraFxId、FxShowTo |
| 镜头后处理 / Camera Post Effect | CameraPostEffect | UseCameraEffect、At、End、类型、距离与Args0/Args1 |
| 模型变色 / Color Change | ChangeColorList | At、End、colorGradientPath |
| 技能进度条 / Skill Bar | SkillBarList | At、Duration、Index |
| 召唤单位 / Summon Unit | MobUnitList | At、模板、循环、定位、寿命及归属/属性规则 |
| AI事件 / AI Event | AIEventList | At、Event、Index |
| Buff事件 / Buff Event | BuffList | At、Index，关联规则需后续宿主接入 |

这些原 RO 数据保存秒数，时间轴按配置 FPS 显示帧。拖动起点会写回秒；End 区间平移保持长度，Time/Duration 和召唤的 LoopCycle 保持原值。召唤区间显示到最后一次触发时刻，LifeTime 是召唤对象寿命，不延长施法时间轴。后处理为单对象，已有条目时不能再次添加或粘贴覆盖；UseCameraEffect 关闭后仍保留已编辑参数。

RO 震屏的 Frequency 表示 Time 秒内的震动次数，原运行代码用 Time/Frequency 求间隔。三轴及 FOV 振幅允许正负，负值可以表示相反方向，不能限制为非负。

新轨道可编辑保存，正式角色的执行接入需后续实现。当前发布工具拒绝这些字段中的非默认内容，避免保存的规则被忽略；空字段兼容已有七份技能。原碰撞/飞行物轨道仍可制作，但其字段并不等同 RO ResultList 全结构。

本次设计与实际验收边界见 `项目分析/RO_SKILL_TRACK_EXTENSION_DESIGN.md`、`Docs/AI/TEST_REPORT.md`；修改归档为 `项目修改日志/2026-10-10-RO-SKILL-TRACK-EXTENSION-CHANGELOG.md`。

当前多段声音制作：E为F9/F36（30FPS），R为F63/F136（120FPS），F为F37/F142（120FPS），Q3为F23/F54（120FPS）。每次挥刀可以选择独立Audio事件及不同音源，声音帧依据动画动作和样本起音，不要求与粒子同时。详细帧秒/音量见项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md，调整理由见项目分析/SKILL_AUDIO_POLISH_NOTES.md。
默认声音迁移边界：已有旧事件仅音量被用户修改时，保留该旧事件而不迁移；若用户改变了旧事件的资源或帧，原事件保留，同时缺失的新版默认声音会追加。此入口会补默认制作事件，不代表对整份自定义声音稿完全不作增量修改；自行制作时可直接在原窗口编辑并通过绑定发布。
## 2026-10-11 音频用途分类

角色声音位于MJX内的移动、跳跃和技能子目录；技能分通用挥刀、E、R、1、备用。资源使用中文用途名，例如Skill_E_Phase01_HeavySlash、Skill_E_Phase02_ShortSlash、Jump_TakeoffWhoosh、Movement_Footstep01。共享素材只保留一个文件；每个动作的具体声音帧、秒和音量查项目分析/AUDIO_ASSET_USAGE_GUIDE.md。原meta/GUID随文件迁移，编辑器引用继续使用原身份。
## 全英文音频目录（2026-10-11最新布局）

整个Audios内目录和文件名均采用英文：角色声音在Characters/MJS/Movement、Jump和Skills；Skills分Shared、E、R、SwordDrop、Legacy。文件名前缀Movement_、Jump_、Skill_直接区分类别。原三份技能特效AudioSource声音在Shared/Skills/Effects，依实际Skill04蓄力/释放、Skill02蓄力引用命名。此前MJX中文分类章节属于上一轮迁移历史，当前目录及完整18声音对照以项目分析/AUDIO_ASSET_USAGE_GUIDE.md为准。

所有原wave/meta/GUID和目录身份保留，制作脚本同步英文路径，作者/Runtime与特效预制体的强引用延续；时点、音量、特效和根位移不因改名调整。正式归档项目修改日志/2026-10-11-AUDIO-ENGLISH-REFACTOR-CHANGELOG.md。
## 音效轨道与事件拖动（2026-10-11）

音效与粒子分别显示在“音效 / Sound”和“特效 / Effect”行，仍使用原fxList资源引用，不复制声音或改变技能时点。旧稿未保存Sound布局但已有音频时，窗口恢复一条可操作的音效行；折叠只隐藏显示，添加到布局需显式操作。

整体拖动点事件时，正数结束帧随开始帧一起移动；结束帧为0或负数的旧单次哨兵保留。区间整体拖动保持原跨度。此规则修复了声音及判定、粒子、预警、取消、飞行物、霸体、朝向、刀光、位移的单次事件左拖后意外形成区间问题；动画及RO秒字段沿各自原单位处理。

正式单次音效要求结束帧不大于开始帧。若发布提示音效区间未接入，请按提示定位音频，选中该音效，开启“工具 / 显示高级属性”，将结束帧设为0或等于开始帧，再发布。需要制作区间音效时可以保留作者数据，但当前正式播放尚不支持区间，不会默默把区间发布成单次。

本轮修复不会自动改写已保存的自定义帧。开发与独立验收结果见Docs/AI/TEST_REPORT.md；归档项目修改日志/2026-10-11-SKILL-TRACK-DRAG-FIX-CHANGELOG.md。

## 特效暂停、逐帧和声音波形对齐

点击“播放 / Play”开始预览，再点击“暂停 / Pause”保留当前动画、粒子和声音位置。空格同样切换播放与暂停；暂停后点击播放继续。“停止 / Stop”清理本次预览副本和声音。通过“上一帧 / Previous”“下一帧 / Next”、帧输入框或拖动标尺暂停定位，检查挥刀动作和特效在该帧的姿态。倒拖会从头重建到目标帧，同帧重绘不会累积根位移或继续推进粒子。

音效轨道显示音频完整波形，多段重叠声音各占独立显示行。拖动整个声音块会平移其触发帧；从块中间抓取不会把开始帧跳到鼠标位置。也可以选中声音，在属性面板精确编辑开始帧。

两种对齐方式：

1. **对齐开始发声时刻**：选中音效，把播放头定位到动作目标帧，点击“触发点对齐播放头 / Align trigger to playhead”。
2. **对齐声音内部的挥刀或撞击点**：先按住 Alt 点击波形中的目标声音点，再把播放头定位到动作目标帧，点击“选点／主波峰对齐播放头 / Align selected peak to playhead”。未选点时使用真实 PCM 的绝对主峰。黄色线表示所选声音点，金色线表示主峰。“刷新波形／清除选点”可恢复使用主峰，音频替换后旧选点自动失效。

第二种方式平移整个声音事件，使选点落在目标动作帧，不裁剪音频。声音内部时点保留到源采样精度，触发帧按技能配置 FPS 取最近整数帧。若计算需要负触发帧或超出制作范围，窗口提示拒绝；应重新选择目标播放头位置。随机音高会改变实际声音点出现时间，当前对齐以原音频时间为依据。

波形完整长度只用于显示和滚动，不延长技能 END、不把单次事件改成区间。对齐支持原撤销／重做；保存后仍需通过绑定的“校验并导出当前表现”发布。既有区间声音的正式发布限制沿上节处理，不自动改写用户稿。

读取失败时窗口明确提示波形不可读；可在属性面板刷新重试，不修改音频导入设置。特效副本的内嵌音源仅在节点活跃、组件启用且原来 playOnAwake=true 时参与暂停、继续和定位；原来不自动发声的音源保持原行为。

本轮编译、原生验证和现场界面／设备听感的验证边界见Docs/AI/TEST_REPORT.md；修改归档项目修改日志/2026-10-11-SKILL-PREVIEW-PAUSE-ALIGN-CHANGELOG.md。

## 预览延迟特效和尾段

预览不会在正式技能END立即回零清理。END之前已触发的粒子、声音以及手动预览实例会继续到尾段结束，随后开始下一轮；角色保持正式结束帧，已配置的根运动不在尾段继续增加。正式END之后的作者特效／声音事件不会因此补发。

工具栏同时显示正式结束帧、预览尾帧、当前时间和FPS。上一帧／下一帧、帧输入、标尺定位及音频Alt选点可进入预览尾段；尾段用于检查延迟发射粒子和调整声音对应点。声音事件拖动和对齐仍按原制作范围操作，尾帧显示不会改写正式END或扩大事件制作区间。

当前1技能（SkillConfigSO_3007001）蓄力F4、落剑F13、正式END34／30FPS。落剑Prefab内部的闪电和撞击粒子有0.8～1.05秒延迟，原预览在F34循环时尚未到发射点。可在F38、F45、F58逐帧检查后续粒子。落剑当前为TargetTop，未指定预览目标时前方10米兜底；若未在视野内，选中该事件后在Scene按F聚焦，或指定原工具栏预览目标。是否在实际用户视野可见仍需现场观察。

本轮其它轨道核查范围：

| 轨道／入口 | 预览行为与尾段 |
| --- | --- |
| 特效、音效 | END前各事件分别触发，保留已触发实例的延迟发射与尾迹／尾音。 |
| 动画、Animation根运动 | 尾段保持正式结束帧，不继续额外移动。 |
| 判定、位移和轨迹Scene参考 | 继续按各自原帧区间显示参考，不自动延长区间或结算伤害。 |
| 手动飞行物、跳转提示 | 沿原按钮生成与生命周期模拟；已生成实例播完再循环，不改成时间轴自动发射。 |
| 镜头、九类RO扩展、Buff／AI等 | 保持已有制作保存和参考范围，Scene执行仍需要宿主接入。 |

该尾段修复只修改集中编辑器预览，作者／Runtime资产和正式游戏播放不重导。实际编译及原生验收结果见Docs/AI/TEST_REPORT.md，变更归档项目修改日志/2026-10-11-SKILL-PREVIEW-MULTISTAGE-CHANGELOG.md。

## 2026-10-11 当前表现导出与动画之后的事件

完整原动画支持结束帧为零哨兵，或按动画源采样率填写自然末帧。例如 Anim_MJS_Skill04 为34源帧；时间轴显示为完整动画时可直接导出，不必手动把末帧清零。真正裁剪、偏移、混合和替换原动画仍按现有宿主能力校验。

导出的 DurationSeconds 保留旧表原动画秒数；TimelineDurationSeconds 保存制作内容的最后触发范围，至少覆盖原动画。字段为零的旧运行资产继续使用原动画长度。制作时间轴可以包含动画之后的新粒子和声音事件，例如当前1技能的特效45帧、声音46帧；动画、动作解锁与根运动窗口仍使用各自原配置，预览粒子尾迹/完整波形的显示长度不作为制作时间轴终点。

声音保持现有预览行为：从开始帧按固定音量播放一次完整音频。声音结束帧作为制作标记保存，不截断、循环或重复播放音频；例如开始0、结束2仍在0帧触发完整声音。随机音高/随机音量、变速、混合粒子等未接入内容仍会给出校验失败原因。

服务器正常结束退出角色动作后，原施法在旧列表保留后续计时器、缺模型待派发指令和声音尾音；完成后回池。协议 Interrupted 为真、拒绝或清场仍立即清理。此规则沿用本项目原 RO 计时器、UUID及池生命周期，不改变服务端扣费/伤害/冷却。

先退出 Unity 运行模式，再在对应 SkillEditorBinding 检查器点击“校验并导出当前表现”。运行期间导出按钮会禁用并提示原因；其他失败原因显示在检查器。保存作者稿仍须通过该按钮显式发布正式表现。

修改及验收记录：项目修改日志/2026-10-11-SKILL-EXPORT-FIX-CHANGELOG.md；协作测试摘要：Docs/AI/TEST_REPORT.md。

### 粒子事件的制作区间与导出

粒子开始帧决定一次生成特效的时刻；结束帧允许保留为作者制作标记，既有预览和正式导出都不会据此每帧重复生成或在末帧截断粒子。例如当前1技能落剑制作为13～111帧，导出一条13/30秒触发的特效，111仍留在作者稿；粒子自然结束继续由原资源决定。结束帧为0、等于开始帧或非负且早于开始帧时，沿原Y单帧哨兵处理；负帧仍由原Y校验拒绝。

原ActionTiming.ContentEnd对特效和声音读取开始帧，结束标记不额外延长Runtime时间轴；原动画时长下限仍保留。若设置了明确退出帧，原Y的范围校验继续约束事件。

此前ReadFx强制结束帧为0，导致正常制作区间被笼统提示为“混合音频、倍率、区间或自定义寿命”。此次仅修正粒子导出门禁；实际未接入的倍率、自定义寿命或混合声音等设置继续拒绝，错误列出绑定、fxList下标、实际字段及值，便于定位。修改与验证记录：项目修改日志/2026-10-11-SKILL-FX-EXPORT-CHANGELOG.md。

## 特效触发时间与持续时间

特效轨道每条事件显示四行：触发帧/秒（真实作者区间则显示起末帧/秒）、预计发射到估算结束的绝对帧/秒、持续时间说明、资源名。每行下沿另画播放参数范围：白线是触发点，灰段是预计延迟，淡绿段是预计发射和尾粒，橙线是估算结束或自定义截断。重叠播放范围分到不同显示行，可直接比较各自时段；拖动仍以原事件标记为入口。

悬停事件文字可查看完整资源与时间说明。选中特效后，属性面板显示相同摘要，并提供“暂停定位到触发”“暂停定位到预计发射”“暂停定位到估算结束／截断”。点击后通过原预览入口暂停采样该帧，之后可逐帧确认画面并对齐声音；开始帧继续通过原字段编辑。帧秒使用当前时间轴FPS，预计导航点向上量化到帧。

预计发射点仅根据可定位的定时发射参数计算，表示参数起点，不保证此帧已经能看到像素。随机、曲线、距离等无法可靠定位的条件显示“首发起点不确定”，整段画灰色并禁用预计发射定位；无活跃粒子不伪造播放范围，仍可定位作者触发。

持续时间是读取粒子延迟、发射、尾粒和子发射器参数得到的单轮保守估算，系统simulationSpeed与事件playbackSpeed分别换算一次。它不是实际逐粒子测出的停播时间，不应当成随机寿命、曲线或外部脚本的精确结果。资源循环设置单独提示；本项目自动寿命技能按单轮播放。设置customLifetime时显示自定义截断秒数，不含预览收尾宽限。空引用或无活跃粒子给出状态说明。

这些文字、播放估算条及纯显示滚动范围不会改写事件结束帧、技能END、Runtime、原粒子资源或正式播放方式。显示行跨度取作者区间、文字与估算尾端的最大值。资源重导后会沿既有项目变化入口清除持续参数缓存；速度、截断值和FPS变更读取当前事件值。

每个特效按自身触发帧独立生成，后一个不等待前一个结束。1技能蓄力F4、落剑F45的实例分别计时；落剑预制体各层另带0.25至1.1秒延迟，会使实际视觉阶段晚于事件触发。判断实际首末画面请暂停逐帧查看，不能把实例存在、参数尾端或粒子计数直接当作渲染像素。

修改与验收：项目修改日志/2026-10-11-SKILL-FX-TIME-DISPLAY-CHANGELOG.md、项目修改日志/2026-10-11-SKILL-FX-RANGE-CHANGELOG.md，Docs/AI/TEST_REPORT.md。
