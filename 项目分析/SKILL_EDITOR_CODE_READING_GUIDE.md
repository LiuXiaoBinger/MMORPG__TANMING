# 技能编辑器代码阅读索引

编辑器源码统一位于`LYF_MMO/Assets/GM_Game/Editor/Skill/ActionEditor`，本游戏绑定与导出工具在同级`LYF_MMO/Assets/GM_Game/Editor/Skill`。按下面顺序阅读即可了解主流程，无需先遍历整个嵌入包。

## 窗口与编辑流程

| 文件（相对于ActionEditor目录） | 主要职责与阅读入口 |
| --- | --- |
| `Window/SkillEditorWindow.cs` | 窗口入口、编辑状态和整体绘制；各partial共同组成原SkillEditorWindow |
| `Window/SkillEditorWindow.Toolbar.cs` | 顶部资源、语言和预览操作 |
| `Window/SkillEditorWindow.Timeline.cs` | BuildTrackDefs注册轨道、时间轴绘制及事件创建 |
| `Window/SkillEditorWindow.RoTracks.cs` | RO新增九类轨道的注册、秒区间绘制、属性编辑、创建/删除、拖动和强类型剪贴板 |
| `Window/SkillEditorWindow.Interaction.cs` | 选择、拖动、删除、复制粘贴和键盘操作 |
| `Window/SkillEditorWindow.Inspector.cs` | DrawInspectorPanel分派属性页，InspBasic编辑整段技能 |
| `Window/SkillEditorWindow.SerializedInspector.cs` | SerializedObject/SerializedProperty字段绘制和提交 |
| `Window/SkillEditorWindow.Persistence.cs` | SelectConfig/LoadConfig载入，BindListsToConfig/SaveConfig保存，UndoLatest/RedoLatest撤销重做 |
| `Window/SkillEditorWindow.Preview.cs` | SampleAnim/SampleExplicitPreview采样，FxUpdate/ProjUpdate预览生命周期，EnsurePreviewInstance创建隔离副本 |
| `Window/SkillEditorWindow.AudioPreview.cs` | 窗口独占隐藏声源的创建、停止与销毁，不借用正式摄像机 |
| `Window/SkillEditorWindow.Collision.cs`与`Window/SkillEditorWindow.CollisionScene.cs` | 碰撞属性页、Scene形状编辑及可视化 |
| `Window/SkillEditorWindow.Flow.cs` | 流程节点与分支编辑 |
| `Window/SkillEditorWindow.Structure.cs` | 统一特效轨道布局、高级工具与属性入口 |
| `Window/SkillEditorWindow.InteractionWindows.cs` | 交互窗口事件编辑 |
| `Window/SkillEditorWindow.HostPresentation.cs` | 本游戏预览与绑定适配 |
| `Window/SkillEditorLocalization.cs`、`SkillEditorPropertyLabels.cs`与`SkillEditorLanguage.cs` | 中英文界面、字段显示名与语言身份 |

## 编辑支持

| 目录/文件 | 用途 |
| --- | --- |
| `Timeline/BuiltinTrackRegistry.cs` | 轨道类型和序列化列表对应关系 |
| `Timeline/EffectAuthoring.cs` | 粒子/音效分类及字段可见性，隐藏字段仍留在原数据中 |
| `Infrastructure/ActionEditorTransaction.cs` | Undo手势分组、标脏与只保存目标资产 |
| `Infrastructure/ActionAssetCreation.cs` | 新配置创建菜单 |
| `Infrastructure/ActionConfigMigrationService.cs`与`ActionTimingMigration.cs` | 旧配置及显式帧率迁移 |
| `Infrastructure/ActionEditorSettings.cs`与`ActionEditorUserSettings.cs` | 项目和个人编辑偏好 |
| `Infrastructure/ActionEditorAgent.cs`与`ActionEditorAgent.Editing.cs` | 原自动化请求、序列化编辑和结果报告 |
| `Infrastructure/ActionEditorHelp.cs` | 帮助菜单，使用Runtime程序集定位仍在包中的手册 |
| `Infrastructure/ActionPlayerInspector.cs` | 原Y播放器Inspector |
| `Infrastructure/SkillEditorRootMotionPreview.cs` | 从真实根曲线和固定起点重建水平位移，重复采样同帧不累加位置 |
| `Ethan.ActionEditor.Editor.asmdef`与`AssemblyInfo.cs` | 独立编辑程序集和原测试的内部可见性 |

## 本游戏发布与播放

同级`SkillEditorBindingInspector.cs`打开窗口并触发发布；`SkillEditorAssetTools.cs`校验完整身份、原表和宿主支持字段，导出运行表现。编辑器保存作者配置与发布正式表现是两个操作。

同级`SkillEditorAudioMotionTools.cs`读取Audio和Animation位移段、核对已发布依赖与逐字段一致性；`SkillEditorAudioMotionSetup.cs`显式将RO声音编入七技能与六普通动作稿，并以Unity API发布，保护用户现有位移段与未保存修改。

编辑器的位移、碰撞、飞行物、音效、镜头和流程等功能全部保留。正式角色仍通过`MSkillComponent.PlayAnim → MSkill.TriggerAnim → MAnimator`播放动画，`MArtSkill`按技能时钟派粒子与声音；Animation位移由模型读取原Animator水平增量并经实体移动处理。六普通动作声音由`SoundComponent`读取动作路径和时钟。新增九类RO轨道仍只有制作保存支持；完整范围见`项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`。

原配置位于`LYF_MMO/Assets/GM_Game/Scripts/Data/Skill`，运行类型位于`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill`和嵌入包Runtime。配置引用、类名、程序集及meta身份保留。目录整理设计见`项目分析/SKILL_EDITOR_SIMPLIFICATION_DESIGN.md`，变更记录见`项目修改日志/2026-10-10-SKILL-EDITOR-SIMPLIFY-CHANGELOG.md`。

## RO 新增制作数据

包内 `Runtime/Data/MSkill*Data.cs`、`MCamera*Data.cs` 及各同名枚举保存 RO 原字段；`Runtime/Core/MSkillTrackTiming.cs` 负责秒与帧投影、末端及制作校验。原 `ActionTiming` 与配置校验入口调用该辅助类，宿主导出通过 `SkillEditorAssetTools.DefaultOnlyFields` 拒绝未接入执行的非默认数据。

新增轨道与运行接入的边界见 `项目分析/RO_SKILL_TRACK_EXTENSION_DESIGN.md`，使用见 `项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`。本轮源码及数据均不写入 `Data/Skill`，现有技能配置继续使用原目录。
