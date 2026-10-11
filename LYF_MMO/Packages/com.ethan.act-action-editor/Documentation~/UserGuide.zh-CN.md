# ACT 动作编辑器使用手册

本项目技能窗口支持中文与English切换。本手册介绍窗口布局、轨道配置、常用操作与预览流程。

编辑器源码集中在`Assets/GM_Game/Editor/Skill/ActionEditor`：`Window`保存窗口及各职责分文件，`Timeline`保存轨道注册和特效字段规则，`Infrastructure`保存资源操作、Undo、迁移、帮助与自动化接口。同级`Assets/GM_Game/Editor/Skill`保存本游戏绑定和导出工具；运行类型与本手册继续位于嵌入包。源码按职责划分并附中文注释，位移、碰撞、飞行物、音效、镜头和流程等制作入口保留。游戏中实际执行的轨道范围由宿主运行链和导出校验决定。

## 1. 先认识窗口

打开 Unity 的 `Tools > ACT Action Editor > Open Editor`，选择一个 Action 资源。工具栏的 **Help**，或 `Tools > ACT Action Editor > Help > User Guide (Chinese)`，可打开本手册。

整个窗口使用同一个播放头、帧率和时间轴。上方工具栏管理资源与预览；时间轴配置事件；属性面板编辑选中的事件。宽窗口会把时间轴和属性并排放置，窄窗口调整为上下结构。

时间轴明确分成两层：

| 分层 | 用途 | 轨道 |
| --- | --- | --- |
| Presentation（表现层） | 动作看起来、听起来如何 | Animation、Effect、Camera、Hit Effect、Trail |
| Mechanics（机制层） | 动作如何移动、命中、转接与响应 | Motion、Collision、Action Flow、Projectile、Armor、Interaction |

两层在同一个时间轴直接编辑。例如第 12 帧可以同时开始攻击判定、播放剑光、播放挥刀声和触发镜头。Effect 中的 Visual Effect 和 Audio 是同一种轨道上的不同事件，不需要分别建立两条轨道。

角色与怪物共用这些轨道。`Action properties > Actor profile` 区分配置身份；它不会自动创建玩家输入、怪物 AI、伤害系统或锁定目标。项目的运行时接收器决定事件在游戏里如何执行。Scene 中预览正确也不等于真实战斗已接入完成。

## 2. 工具栏按钮

| 控件 | 用法 |
| --- | --- |
| New | 创建新动作资源，再选择保存位置。新动作默认使用显式时间轴帧率。 |
| Save | 保存当前动作。修改后应保存，再重开确认。 |
| Undo / Redo | 撤销或重做资源编辑，例如添加、删除、拖动事件和修改属性。 |
| Action | 选择正在编辑的动作资源；切换前会结束当前预览，清除事件选择并重置滚动位置。界面在下一次绘制刷新，避免不同资源布局混用。 |
| Action properties | 编辑整段动作的名称、说明、Actor profile、退出帧、阶段标记和机制参数。 |
| Tools | 打开高级设置、角色设置、备份恢复与迁移等入口，见下表。 |
| Help | 打开本中文手册。 |
| Preview actor | 指定用于预览的模型或场景对象。预览系统使用隔离的模型副本。 |
| Clip source | 指定含动画的模型资源，供 Animation 的 Add 菜单选择动画片段。 |
| Play / Stop | 开始或停止编辑器预览；不会替代游戏 Play Mode 中的完整战斗测试。 |
| Previous / Next | 播放头向前或向后移动一帧。 |
| Frame | 直接输入要查看的帧。右侧显示时间轴长度、当前秒数与 FPS。 |
| Saved / Unsaved | 保存状态提示，不是按钮。 |
| Issues | 展开或收起校验结果；数字为错误和警告总数。 |
| Zoom | 调整每帧的显示宽度。只改变视图，不改变事件时间。 |
| Fit | 调整缩放，让时间轴适应当前可用宽度。 |

本项目嵌入版的 `Preview actor`（预览角色）使用预制体时，副本默认放在世界原点，保留模型原朝向和缩放；预制体保存的历史场景位置不作为预览位置。显式时间轴在 `Play / Stop`（播放 / 停止）、逐帧和拖动 `Frame`（帧）时，从固定角色根及 Animator 根姿态重新采样，再根据 Motion 的 Animation 窗口重建累计水平根位移。反复采样同一帧保持同一位置，拖回零帧返回预览原点。骨骼与挂点保留当前动画姿态。选择特效事件后，在 Scene 视图按 F 可聚焦该事件位置。

Animation 根运动预览使用真实 RootT 水平曲线（Generic 使用根 Transform 曲线）和模型人体缩放；Y 轴保持初始根姿态。它用于当前完整单段动画的前移与特效对齐，不能代替正式角色的碰撞、重力、动画混合及原生 Animator.deltaPosition 验证。Program 位移仍只绘制参考。场景对象作为源时沿用原实例化姿态，不强制归零；带变换父级的世界对齐以及旧时间模式的采样行为不属于这项修复。正式角色与原动画资产不会被此预览修改。

`Tools` 菜单：

| 选项 | 用法 |
| --- | --- |
| Show advanced properties | 显示较少使用的字段。Effect 即使展开高级属性，也只显示当前类型的字段。 |
| Actor settings | 编辑已有的角色或怪物扩展配置；实际作用取决于接入项目。 |
| Restore backup | 选择备份恢复当前资源。先核对恢复目标和备份时间。 |
| Upgrade schema with backup | 旧资源需要升级时出现。先备份，再升级；不会批量自动升级其他资源。 |
| Arrange animation clips | 至少有两段动画时出现。可按 3 / 5 / 8 帧混合重新排列，关闭空隙，或清除全部混合。会修改动画时间位置，应检查原有事件是否仍对齐。 |

`Action properties` 中的 `Migrate to explicit timing` 用于旧帧率模式迁移，会先确认并备份。迁移保留事件帧号；包含不同 FPS 动画时，应重新检查长度与重叠。

选择需要 schema 升级的旧 Action 时，三行工具栏保留在顶部；下方显示只读提示、`Action asset` 和可滚动的属性列表。它不是时间轴编辑模式，也不是文字编码损坏。清空 `Action` 后，下方显示选择/创建提示。切换资源不会自动升级或改写旧数据；确需编辑时使用 `Tools > Upgrade schema with backup`。

## 3. 时间轴基本操作

1. 点击标尺定位播放头，或在 Frame 输入目标帧。
2. 点击轨道右侧 **Add**，在播放头处添加事件。Effect、Motion、Collision、Action Flow 会继续弹出子类型菜单。
3. 点击事件，在属性面板修改内容。拖动事件主体可移动时间；有边缘手柄的范围事件可拖动起止边界。
4. 预览、检查 Issues，再 Save。对于命中、交互、转向、镜头等行为，还需在接入角色的 Play Mode 中验证。

| 控件或右键选项 | 用法 |
| --- | --- |
| Add track | 添加尚未显示的轨道；已显示的类型会标注 already added。 |
| Add | 在当前帧新增该轨道事件。 |
| Hide / Show | 折叠或展开轨道内容，不会禁用事件。 |
| Add … at playhead | 轨道右键菜单中的新增入口；Effect 同样先选择 Visual Effect 或 Audio。 |
| Copy / Copy selected … | 复制选中事件及其配置和资源引用。 |
| Paste … at frame … | 在播放头处粘贴兼容类型的事件，保留原相对长度。Visual Effect 和 Audio 均可粘贴到 Effect。 |
| Remove / Remove selected … | 删除选中事件，支持 Undo。 |
| Move track up / down | 调整轨道顺序；Presentation 与 Mechanics 的分层显示始终保留。 |
| Collapse track / Hide track | 有事件的轨道只折叠内容，保留轨道标题；空轨道可从布局移除。折叠不影响运行。 |

Animation 的右键菜单还包括 `Reset source trim`（恢复源动画裁剪）、`Blend over 5 frames`、`Blend over 8 frames`（设置过渡混合）、`Clear blend`（清除该段混合）。

快捷键仅在编辑器取得焦点且没有输入文字时生效：Space 预览/停止；左右方向键逐帧；Home / End 跳到时间轴首尾；Delete 删除选中事件；Ctrl+C / V 复制/粘贴；Ctrl+Z 撤销；Ctrl+Y 或 Ctrl+Shift+Z 重做。时间轴上 Ctrl+滚轮缩放。Mac 可用 Command 代替复制、粘贴、撤销和重做的 Ctrl。

## 4. 各轨道如何配置

### RO制作轨道

`Add track`（添加轨道）可选择下列新增类别。现有 Animation、Effect、Motion、Collision、Projectile、Camera 和 Action Flow 等轨道继续保留。

| 分组 | 英文 / 中文标题 | RO原配置 | 原时间与用途 |
| --- | --- | --- | --- |
| Presentation | Color Change / 模型变色 | `ChangeColorList` | `At` 至 `End`，填写 `colorGradientPath` 渐变资源路径。 |
| Presentation | Skill Bar / 技能进度条 | `SkillBarList` | `At` 开始，持续 `Duration` 秒。 |
| Presentation | RO Camera Shake / RO震屏 | `ShakeList` | `At` 开始，持续 `Time` 秒；配置坐标空间、视角和 XYZ 振幅、持续时间内震动次数、曲线路径及命中结果关联。 |
| Presentation | Camera Post Effect / 镜头后处理 | `CameraPostEffect` | `At` 至 `End`；单个配置，设置启用、观看者范围、后处理类型及参数。 |
| Presentation | Camera Effect / 镜头特效 | `CameraFxData` | `At` 触发镜头特效 ID，并设置显示对象范围。 |
| Mechanics | Summon Unit / 召唤单位 | `MobUnitList` | `At` 召唤模板 ID；配置原点、偏移、归属、属性、寿命及循环。 |
| Mechanics | AI Event / AI事件 | `AIEventList` | `At` 触发填写的 AI 事件键。 |
| Mechanics | Buff Event / Buff事件 | `BuffList` | `At` 触发原 RO 规则索引；此结构只有 `At` 和 `Index`，不把索引改名为 Buff ID。 |
| Mechanics | Rotation / 旋转 | `RotateList` | `At` 至 `End`，`AngularVelocity` 为角速度，单位度/秒。 |

新增事件的 `At` 取当前播放头换算出的秒数。其他字段采用 RO 数据默认值：标量为零、开关关闭、资源路径为空；范围事件的 `End` 初始等于 `At`。后处理新增时将 `UseCameraEffect` 设为启用，方便区别于空占位。召唤模板、AI事件键、渐变路径等内容需要填写，未完成时 `Issues` 会提示具体字段。

RO震屏的 `Frequency` 表示 `Time` 时长内的震动次数，原实现用 `Time / Frequency` 求震动间隔，不按每秒频率解释。`FovAmp` 和 `AmplitudeX/Y/Z` 保留有符号振幅，正负用于表示方向，允许负值。

RO 时间字段保存原始秒数，窗口按当前 FPS 显示在帧轴上；修改 FPS 只改变显示帧号。拖动事件主体及粘贴会平移 `At`，范围事件同时平移 `End`，保留原秒数跨度。`Duration`、`Time`、`LoopCycle` 保持原值。召唤循环块延伸到最后一次触发：`At + (LoopCount - 1) × LoopCycle`；`LifeTime` 是被召唤对象自己的寿命，不延长技能时间轴。事件末尾参与整段长度计算，也适用于旧时间模式。

可点击事件编辑所有原字段、右键复制/删除、在同类轨道粘贴，并使用原 Undo / Redo / Save。重新打开仍从同一 `SkillConfigSO` 的原字段读取；不会另建一份编辑数据。镜头后处理保持一个对象，已存在有效配置时 Add 选中已有项，粘贴入口关闭以保护已有内容。删除后可重新添加；Unity 内联序列化产生的全零关闭对象按空占位处理，不会自动恢复一条事件。关闭但已填写参数的后处理仍保留并可编辑。

这些新增类别当前完成制作和持久化，属性面板会提示其游戏执行与 Scene 预览需要宿主接入。游戏发布校验会阻止未接入事件，避免把“保存成功”当成游戏已生效。RO 震屏保留各轴振幅、空间、视角及结果关联，与原 Camera 的通用偏移配置各有用途；Rotation 配置角速度，与 Motion 中跟踪目标/锁定朝向的字段各有用途。Collision / Projectile 保留既有制作能力，未承诺包含 RO `ResultList` / `LongAttackData` 的全部结算与弹道字段。

### Animation：动画

Add 选择 `Clip source` 中的动画；没有可用片段时可先添加 `Empty clip (assign animation later)`，再指定 Clip。常用属性为 Clip、Start Frame、Clip Start Frame、Clip End Frame、Blend In Frames。

Start Frame 是这段动画在动作时间轴的位置。源裁剪帧属于动画本身，不能把不同 FPS 动画的源帧号直接当作同一时间轴的帧号。重叠片段与 Blend In Frames 一起控制过渡。先确定动画节奏，再配置命中和表现事件。

### Effect：特效、音效与预警

点击 **Add** 后必须先选类型：

| 子类型 | 属性面板显示 | 示例 |
| --- | --- | --- |
| Visual Effect | Start frame、Visual effect、Offset、Rotation、Scale、Follow actor、Custom Lifetime | 第 12 帧在武器位置生成剑光。 |
| Audio | Start frame、Audio clip、Base Volume | 第 12 帧播放挥刀声。 |
| Telegraph | 预警时间与 Visual / Audio 分页 | 出招前的闪光、声音或危险区域提示。 |

Visual Effect 的高级属性包含 End frame、Playback Speed、Use World Space；Audio 的高级属性包含 End frame、Audio Randomize、Pitch Variation、Volume Variation。音效事件不会显示粒子、偏移或缩放字段，特效事件不会显示音频与随机音量字段。时间轴标签分别以 **VFX:** 和 **Audio:** 开头，空引用时也保留所选类型。

需要同时播放声音和特效时，在相同帧添加两个事件，分别调整时间与属性。复制、粘贴、保存与重开会保留子类型。新事件必须指定对应资源；混入另一种资源或丢失所需资源时，校验报错 `ACT163`。

`Preview visual effect` 预览所选粒子；`Preview audio` 试听所选音频；`Stop effect preview` 清理预览粒子并停止预览音频。试听使用 Base Volume；运行时随机音高、音量等效果取决于项目接收器，不应只靠此按钮验收。

本游戏发布现已支持 **Audio** 的单帧单次声音，使用 Audio clip 和零至一的 Base Volume；End frame 不大于 Start frame 时仍是单次事件，包括复制粘贴后的同帧结束值。随机音量/音高、倍率、自定义寿命及跨帧音频区间尚未接入，发布时会明确报错。声音资源强引用随正式配置收集，实际技能按和粒子相同的动作秒时钟触发。预览使用窗口独占隐藏声源，不借用正式摄像机，关闭窗口、进入游戏和域重载时清理。

`Tools/技能/YCombat/编入RO音效与MJS根运动` 以 Unity API 向当前七份 MJS 作者稿添加真实 RO 声音并发布。原声音目录是 `Assets/GM_Game/Audios/Skill/MJX`；Q 的原粒子 1/1/2 和第三段第二刀局部参数保留。Motion 使用 Animation 驱动，只发布根位移窗口与 Root Motion Scale，角色重力和碰撞继续生效；程序方向、目标和曲线参数沿原 Y 在 Animation 模式下不执行。已有作者位移段保留，首次添加只处理无段的 Q2/Q3/E/F，全段倍率一；Q1 原 F0–15 窗口保留。

走路、跑步、战斗走路、战斗跑步、起跳及落地保存在六份 `SkillConfigSO_MJS_*` 作者稿，skillID 为零，用同一编辑器的动画和音效轨道制作。左右脚的首次步点取原脚掌高度曲线最低帧，起跳和落地声音在零帧；最终脚步贴合仍需动画试听调整。使用 `Tools/技能/YCombat/导出MJS走跑跳声音` 发布到 `SkillRuntimeLocomotionConfig_MJS.asset`；它由已有移动/跳跃状态读取，不建立新技能表行。执行制作与发布前须保存目标作者稿；入口检测目标未保存修改并停止，避免替用户覆盖编辑稿。

**旧资源兼容：**旧版事件可能同时引用粒子和音频。选中后可用 `Visual Effect / Audio` 页签分别编辑；切换页签只改变显示内容，不删除引用，也不改变运行时两者都可播放的行为。若希望拆成两个事件，分别新建并配置完整、检查时间和引用后，再手动删除旧事件。打开和保存不会自动拆分已有动作。

Effect 是按起始帧触发的表现事件。`End frame` 是编辑时间范围，不能保证在该帧自动停止粒子或声音；粒子清理时长通过 Custom Lifetime 等配置交给接收器，声音播放长度通常来自 AudioClip。值为 0 的 Custom Lifetime 表示使用粒子的默认时长。高级字段是否被执行须以项目接收器为准。

**Telegraph 预警效果**

使用 Effect → Add → Telegraph 创建预警。Visual 页指定 Visual effect，设置资源后显示偏移、旋转与缩放；Audio 页配置提示声音。时间轴标签以 Telegraph: 开头，与普通特效、音效共用轨道，重叠时自动分行。玩家与怪物均可使用。预警表达危险提示，实际命中范围和结算仍在 Collision 中设置；结束帧是否控制效果寿命取决于项目预警接收器。

已有预警事件在 Effect 中继续编辑，原有资源引用与触发方式保留。

### Camera：镜头

配置 Start frame / End frame、Position Offset、Rotation Offset、Field Of View Offset、Shake Amplitude、Shake Frequency 和 Envelope。Envelope 控制范围内的效果强度，适合做短促强调、蓄力收镜、命中震动。

需接入 Camera 事件接收器。包内 `ActionCameraDriver` 需要绑定专用镜头支点或相机；与项目已有相机控制器协调，避免多个组件同时写同一 Transform。当前 Scene 预览不模拟 Camera 轨道，请在 Play Mode 检查并测试中断后复原。

### Motion：位移与朝向

同一 Motion 轨道内分为 Displacement 和 Facing / target tracking 两排。Add 有三个选择：

| 选项 | 用法 |
| --- | --- |
| Displacement | 配置一段位移。设置帧范围、Drive Mode、Direction、目标、曲线、Distance limit 等。曲线或 Root Motion 的执行需接入位移后端。 |
| Allow target tracking | 允许在指定范围内朝目标转向。配置 Tracking target、Turn speed (deg/s)、Y Axis Only。 |
| Lock facing / planted feet | 在指定范围内锁定朝向，适合落脚、出刀等不应持续追踪的阶段。 |

`Allow target tracking` 是朝向开关，关闭时目标与转速不产生追踪作用。存在朝向窗口时，窗口之间的空隙保持朝向；重叠时锁定优先；完全没有朝向窗口则保持旧的朝向策略。转速 0 表示立即朝向目标，较大的转速会更快跟随。

Boss 蓄力可先 Allow target tracking，出招落脚时切到 Lock facing。新建敌人追踪窗口默认目标为 Player，玩家默认为 LockedTarget。锁定只限制朝向，不能自动解决动画脚位、根运动或位移曲线不匹配造成的滑步；仍须连同动画和位移一起调整。自有控制器需遵守同一朝向策略，或接入包内 `ActionFacingDriver`。

### Collision：碰撞范围与接触响应

点击 Add，选择 Box 或 Sphere。每个事件定义一个碰撞体和生效帧段，选中后按三个页面配置：

| 页面 | 配置内容 |
| --- | --- |
| Shape | 碰撞体形状、尺寸、偏移、旋转与骨骼采样。 |
| Response | 检测频率以及碰到目标后的伤害结算或接触信号。 |
| Feedback | 确认命中后播放的特效、声音和打击反馈。 |

**Shape**

Box 的 Size 为完整 X / Y / Z 尺寸，Rotation 为相对角色的旋转；Sphere 使用 Radius。尺寸单位为世界单位，Offset 为角色局部坐标。角色缩放会影响偏移位置，不额外缩放碰撞体尺寸。

勾选 Edit in Scene 后，可用 Move 移动中心、Resize 拖动边界、Rotate 调整盒体朝向。Frame collision 将 Scene 视角聚焦到碰撞体；Go to start frame 将播放头移到事件起始帧。没有 Preview actor 时，碰撞体显示在世界原点。

选中的碰撞体在时间范围外仍显示，并标注 outside window；未选中的碰撞体只在其生效帧段显示。无关资源的校验错误不会关闭碰撞体显示。处于旧资源只读状态时可查看但不能拖动修改。

Use Bone Tracking 用于按帧跟随骨骼。勾选后，将 Preview actor 层级中的骨骼拖入 Tracking bone，点击 Bind bone path；也可先在 Hierarchy 选中骨骼，再点击 Use selected bone 直接绑定。Sample bone path 采样范围内的偏移。Move 编辑当前帧的采样点，Resize 与 Rotate 修改整个事件的尺寸与朝向。在时间范围外不编辑骨骼采样位置。Clear bone offsets 清除采样并回到固定偏移。

需要复制路径时，在 Hierarchy 右键骨骼，选择 ACT Action Editor → Copy Bone Path；也可使用 Tracking bone 下方的 Copy bone path。须先打开动作编辑器并指定 Preview actor，路径相对于该角色根节点生成，不包含场景分组或角色名称。角色根节点自身的相对路径为空；其他角色中的骨骼不能绑定到当前 Preview actor。

**Response**

On contact 选择响应类型：

- Damage：配置 Damage multiplier、击退、冲击等级、韧性伤害及格挡/弹反条件。具体数值结算由项目的战斗接收器执行。
- Signal：填写 Signal 名称，供接触响应接收器处理交互或其他自定义行为。该模式不执行伤害结算。

Detection 设置检测模式；重复检测时可设置 Tick Interval。目标层级、筛选与命中去重由宿主碰撞查询负责。独立播放核心通过事件通知宿主执行查询，不会仅凭时间轴事件自动确认命中。

**Feedback**

Visual、Audio、Impact 分别配置粒子、音频和顿帧/镜头反馈。指定资源或启用对应效果后才显示其附加参数。Signal 模式提供 Visual 和 Audio；Impact 用于伤害命中反馈。动画时间点上的表现使用 Effect，碰到目标后才发生的表现配置在 Collision 的 Feedback 中。

### Action Flow：动作流程

在时间轴的目标帧点击 Add，选择 Allow Exit、Branch 或 Complete。节点竖线标明生效帧，浅色延伸线表示允许操作的时间范围；重叠节点自动分行。点击节点编辑，拖动节点移动起点，右键 Copy / Remove 或使用复制、粘贴与撤销快捷键。

| 节点 | 含义 | 常用设置 |
| --- | --- | --- |
| Allow Exit | 开放其他动作接替当前动作；没有请求时继续播放。 | Allowed category、Priority rule |
| Branch | 自动或收到指定指令时衔接目标动作。 | Next action、Automatic、Command |
| Complete | 正常完成当前技能，将后续选择交给输入或 AI。 | Frame |

Allow Exit 与 Branch 默认从 Frame 起持续到动作结束。勾选 Limit window 后，Until frame 是最后一个有效帧，包含该帧。Complete 为单帧边界，不配置时间窗口；到达该帧时先结束技能，不再执行该帧及之后的机制事件。

**Allow Exit**

Allowed category 默认为 Any，可选择 Attack、Shoot、Special、Dodge、Jump、Movement 或 Custom。类别由宿主请求提供，Custom 的 Category ID 必须与项目一致。Priority rule 默认为 Higher Than Current，要求接替动作优先级严格高于当前动作；Any 不限制优先级，At Least 使用指定的最低优先级。此节点不直接结束技能，也不自动选下一招。

**Branch**

指定 Next action。Automatic 默认关闭，使用 Command（默认 Attack）接收输入或 AI 指令；开启后在有效窗口内尝试自动衔接。接替动作仍须通过宿主激活条件；失败时继续当前动作，不提前消费资源。多个有效 Branch 按数据列表顺序尝试，成功一次即停止本轮处理；Branch 优先于普通 Allow Exit 请求。

Show conditions 可配置项目提供的 ActionFlowCondition 条件资源，全部满足才允许接替。条件只用于判断，资源扣除等操作应由成功激活的动作处理。过渡动画采用宿主的混合策略。

**Complete**

用于标记技能的逻辑终点。例如 Boss 招式在第 38 帧完成，行为树的技能任务即可成功结束，再决定移动、等待或释放另一招。动画画面的过渡由宿主处理；保持收势画面不会继续保留旧技能的碰撞和位移。

同帧存在 Complete 和其他流程规则时，Complete 优先。多个 Complete 以最早可达者为准，Issues 会提示重复边界以及完成后无法执行的节点。没有 Complete 时仍按原有动作长度结束。

**配置示例**

- 角色：第 18 帧 Allow Exit / Dodge；第 24 帧 Branch / Attack → 第二段；第 32 帧 Allow Exit / Movement；第 45 帧 Complete。
- 怪物：招式完成时放置 Complete，下一招由 AI 决定；固定的招式衔接可以使用 Automatic Branch。

旧 Transition、Cancel 与 Exit frame 在同一 Action Flow 轨道显示，保留原有执行方式。旧 Branch 的结束帧不包含在窗口内；旧 Allow Exit 使用含等号的最低优先级，结束帧不大于起点表示持续开放。旧 Exit frame 保留宿主原有的帧末结束顺序。需要统一为新的边界语义时，明确编辑或替换对应节点，再在游戏中检查。

Scene 预览只展示时间和动画，不执行分支跳转或行为树。独立播放核心执行新 Flow 规则；外部控制器应接入共享策略，接入方法见 Agent Guide。

### Projectile：投射物

时间轴会自动显示有事件但缺少布局记录的轨道，不会因此修改资产。可用轨道标题上的 Show / Hide 展开或折叠，右键 Add track to layout 将自动显示的轨道保存到布局。缺少 Prefab 的投射物显示为 `Missing Prefab F帧号`；选中后补充 Prefab，或删除不需要的事件。缺少必填引用会阻止播放前校验通过。

指定 Prefab、Fire Point、Spawn Offset、Speed、Lifetime、Damage Ratio、Auto Aim、Shot Count、Spread Angle 等。用于箭矢、飞弹、弹幕。`Preview projectile` 用于观察生成位置；真实移动、追踪和碰撞需投射物接收器及项目实现。

### Armor：霸体窗口

为指定帧段配置霸体级别等数据，适合重攻击或 Boss 强韧阶段。霸体表示如何抵抗打断，不等于自动免伤；伤害与受击控制由宿主决定。

### Interaction：闪避、格挡与弹反

设置唯一 ID、Signal、范围、Priority、Response、Source Angle、Damage Multiplier、Suppress Hit Reaction、Max Activations，以及可复用的 Conditions。窗口范围包含首尾帧。相同信号下多个条件有效的窗口以最高优先级决胜。

| 按钮 | 预设内容 |
| --- | --- |
| Evade preset | IncomingHit 信号、Evade 响应、360°来源角、伤害倍率 0、优先级 20。 |
| Block preset | IncomingHit 信号、Block 响应、120°来源角、伤害倍率 0.2、优先级 10。 |
| Parry preset | IncomingHit 信号、Parry 响应、120°来源角、伤害倍率 0、优先级 30。 |

三个预设都开启 Suppress Hit Reaction，并将 Max Activations 设为 0（不限次数）。预设会覆盖相应字段，使用后检查条件与范围。条件资源在 `Create > Combat > Interaction Conditions` 创建；可用组合条件表达 All / Any / Not。宿主必须提交交互信号并应用结果，单独添加窗口不会让角色自动具备弹反能力。

### Hit Effect：命中反馈

配置命中目标时使用的特效与声音等反馈数据。它与 Effect 的区别是执行条件：Effect 跟随动作时间，Hit Effect 供真实命中逻辑使用。此轨道仍保留其独立的命中反馈数据结构，不使用 Effect 的新增子类型菜单。

### Trail：拖尾

设置帧段内拖尾的开关与相关配置。需要宿主接到实际使用的拖尾实现，配置轨道本身不会自动创建拖尾渲染器。

## 5. 两个配置示例

**玩家普通攻击：**先放 Animation；在挥刀开始帧添加 Effect > Visual Effect 和 Effect > Audio；在刀刃经过目标的帧段添加 Collision；需要向前挪动时添加 Motion > Displacement；恢复段添加 Transition 接下一招；最后 Save，并在真实角色上验证动画、位移、命中与输入衔接。

**Boss 蓄力斩：**先放 Animation；蓄力阶段使用 Motion > Allow target tracking，目标选择 Player；双脚落地至出刀结束使用 Lock facing；危险提示使用 Telegraph；刀锋接触时配置 Collision 与 Effect；需要视觉强调时加 Camera。在 Play Mode 检查朝向锁定、退出清理和镜头复原。

## 6. 校验、保存与排查

Issues 展示配置错误与警告。`Locate` 跳到对应事件和帧；有些全局问题没有可定位事件。常见原因包括缺少动画或资源、非法范围、交互条件缺失、没有对应接收器。新建 Effect 未指定资源时出现校验错误是正常的未完成状态，填写对应资源后再检查。

若新增类型选择后属性不对，先确认选择的是新事件还是旧混合事件；旧事件通过页签切换显示。若预览按钮不可用，检查 Preview actor、资源引用以及旧资源是否尚未升级。若预览有声音但游戏没有，检查宿主音频接收器与音频输出，而不是增加第二种 Effect 字段。

推荐每次交付前执行：校验配置 → 预览关键帧 → Save → 重开资源 → 在角色/怪物上播放 → 中断动作 → 检查粒子、声音、朝向和镜头是否按预期清理。

## 7. 相关文档

- [运行时与集成指南](AgentGuide.md)
- [入门教程](index.html)
