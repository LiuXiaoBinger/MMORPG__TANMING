# 音频按用途改名并分类

2026-10-11。用户要求音频名字能识别用途或使用时间，移动、跳跃、技能分目录。

## 范围及设计

MJX目录保留作角色声音根目录；十五份原音频以中文用途命名，分别移动、跳跃、技能的通用挥刀/E/R/1/备用目录。共享声音有多个技能/帧使用，文件名描述用途；准确时点由项目分析/AUDIO_ASSET_USAGE_GUIDE.md映射作者与运行配置说明。原RO采样名在映射表保留用于追溯。

开发audio_organize_develop迁移wave及原meta、更新Setup硬编码相对路径及中文注释；独立audio_organize_review检查身份引用与范围、必要编译。所有十五wave/meta字节与GUID保留，作者/Runtime/音量/帧/特效/根位移/动画/表/场景/监听修复不改。目录meta使用Unity合法导入；不添加新的播放业务、不重写整套制作稿。

## 验收和未决

先冻结旧新路径与SHA/GUID、相关资产引用。候选迁移后检查新AudioClip身份、编辑与运行强引用、Setup新位置可加载、旧路径无残留、collector覆盖和宿主编译；审查失败开发返工。当前尚未最终验收。不生成角色、自动游戏按键或保存未保存场景，临时文件统一_temp/audio-organize-20261011并不清理。
## 2026-10-11 音频中文命名及分类验收完成

- 已迁移十五wav及原meta到MJX/移动、跳跃、技能（通用挥刀、E、R、1、备用），例如E_第一段重挥刀.wav、E_第二段短挥刀.wav、跳跃_起跳破风.wav。原波形/导入参数/meta/GUID均保持，共享声音不重复复制。新目录8份meta由Unity创建。
- 源码仅SkillEditorAudioMotionSetup.cs音频相对路径与对应说明同步；制作脚本旧默认迁移也引用新备用资源位置，无正式旧音频文件名路径残留。collector原SO依赖链覆盖，未修改。所有作者/Runtime资产保持原字节，不重新导出；事件帧秒/音量/FX/root、表/动画/场景/角色与监听修复保护。
- 开发audio_organize_develop，独立audio_organize_review审查：Unity原生MoveAsset迁移十五身份通过，独立原生67项资源检查包含44个作者/Runtime声音引用；必要宿主8源码隔离Rebuild0错/2旧警告。2614前置文件2583原位保持，30wave/meta按映射迁移，1计划源码修改，8新目录meta；无未计划改动。最终39路径冻结一致，主重算39SHA0差异。
- 文档项目分析/AUDIO_ASSET_USAGE_GUIDE.md包括十五原名/新名、目录及七技能12声音事件和六普通稿10声音事件的帧/FPS秒/音量。项目分析/SKILL_AUDIO_ROOT_MOTION_INTEGRATION.md当前声音表、YCOMBAT_EDITOR_INTEGRATION_GUIDE.md和USAGE.md入口同步，原来源名用于追溯。
- 开发临时入口反斜线转义与独立编译临时工程编码夹具失败只修_temp，原失败日志保留，未为测试改播放逻辑。本轮不重导全配置、不生成角色/按游戏键、不保存用户未保存场景、不调整系统音量或打开第二正式Unity。只资源与必要宿主验证，正式Game听感/发布Bundle未实测。
- 正式日志项目修改日志/2026-10-11-AUDIO-ORGANIZE-CHANGELOG.md；证据_temp/audio-organize-20261011/develop/migration-manifest.json、final-manifest.json、native-move-result.json、develop-result.md和review/independent-protection.json、native-reference-result.json、host-build.log及独立报告。临时资料不清理。无代码/引用阻断，用户可从新目录选声音并按原编辑器发布。