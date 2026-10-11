# Audios全目录英文用途重构

2026-10-11。用户要求整个Assets/GM_Game/Audios不用中文，按角色、技能、移动和跳跃分目录，文件名直接识别声音类别。本需求替代此前MJX中文分类，历史日志保留。

## 实际范围与事实

全量18wav及原meta；原MJS十五声归Characters/MJS，子目录Movement、Jump、Skills/Shared、E、R、SwordDrop、Legacy，文件名以Movement_、Jump_、Skill_用途前缀区分。三原特效音源不是根据名字推断用途：DragonAttack引用于Effect_Skill04_Juqi，Clip_Skill5于Effect_Skill04，Shield_buff于Effect_Skill02_Juqi。归Shared/Skills/Effects，名字分别Skill_04_ChargeFx、Skill_04_ReleaseFx、Skill_02_ChargeFx。

开发迁移所有wave/meta与既有目录meta，保持波形、导入参数、GUID；新父目录meta正规Unity创建。Setup同步根目录及相对路径/对应中文注释，其它播放实现不改。原Skill目录和MJX目录分别迁移到新Effects/MJS位置保身份，不删除旧资产或未确认临时资料、不留中文路径。预制体/作者/运行配置以GUID继续引用，原文件字节保持。

## 流程和验收

audio_organize_develop冻结旧新映射并开发，audio_organize_review独立基线、18clip/目录GUID与44作者Runtime及3FX引用、Setup路径、全目录无中文/用途前缀、必要宿主编译及保护验证；失败交开发返工。主同步项目分析/AUDIO_ASSET_USAGE_GUIDE.md等最新说明和四协作入口。声音帧/音量/特效/根运动/原表/动画/场景均不修改。

当前开发中，未宣称最终通过。临时统一_temp/audio-english-refactor-20261011，不再读中文任务PASS代替本轮验证，不重导全制作稿/生成角色/模拟游戏按键/保存用户未保存场景/打开第二正式工程。最终记录继续追加此日志。
## 2026-10-11 全Audios英文目录与用途名（最终验收）

- 已完成全量十八wav：Characters/MJS/Movement、Jump、Skills（Shared/E/R/SwordDrop/Legacy），以及Shared/Skills/Effects三既有特效音源。所有资源与目录名全ASCII，文件名前缀Movement_、Jump_、Skill_识别类别；例如Movement_Footstep01、Jump_TakeoffWhoosh、Skill_E_Phase02_ShortSlash。旧Skill/MJX及中文路径不再存在。
- 三原特效声音按实际Prefab AudioSource引用命名：DragonAttack到Skill_04_ChargeFx，Clip_Skill5到Skill_04_ReleaseFx，Shield_buff到Skill_02_ChargeFx；Prefab不改，未凭文件名添加龙角色或绑定新技能。
- 开发audio_organize_develop，独立audio_organize_review最终范围验证PASS。十八wave和原meta、十个旧目录meta/GUID全保；三新父目录meta由Unity实际生成。源码仅Setup英文根与相对路径/相关中文注释。所有70配置、3FX预制体、collector/表/动画/场景/播放实现/声音时点音量/根位移保持。
- 验证：Unity原生迁移与独立79项检查PASS，含18clip/13目录/44作者Runtime音频强引用及依赖/3FX AudioSource强引用及依赖/collector；宿主8真实源隔离Rebuild0错/2旧警告。2629前置2582保持，36wave/meta和10目录meta按映射迁移，1计划源码更新、3新目录meta；无未计划变化。31clip/目录GUID各唯一、无旧源filename/root字符串。50正式冻结与原生输入一致，主重核50SHA0差异、全18wave路径无非ASCII。
- 使用说明项目分析/AUDIO_ASSET_USAGE_GUIDE.md现是全目录英文结构、18旧源/新用途映射、22声音事件帧秒音量；三个现用技能说明路径及素材表同步。旧中文方案历史记录保留但当前决策采用英文。
- 失败留痕：迁移临时MoveAsset已到目标目录no-op被拒绝，只修_temp工具跳过no-op并用原GUID恢复隔离输入重跑；独立GUID扫描夹具加CRLF容忍以读取旧meta，未修改生产meta迎合夹具。不重导制作配置，不生成角色/游戏按键/保存用户未保存场景或启动第二正式Unity。本轮仅资源/引用/必要编译，实际Game声音和完整Bundle发布未测。
- 正式日志项目修改日志/2026-10-11-AUDIO-ENGLISH-REFACTOR-CHANGELOG.md；证据_temp/audio-english-refactor-20261011/develop/migration-manifest.json、final-manifest.json、native-move-result.json、develop-result.md；review/native-reference-result.json、independent-protection.json、host-build.log及独立报告，主root-manifest-check.json。临时不清理。无代码/引用阻断，后续编辑从新英文目录选择同GUID声音。