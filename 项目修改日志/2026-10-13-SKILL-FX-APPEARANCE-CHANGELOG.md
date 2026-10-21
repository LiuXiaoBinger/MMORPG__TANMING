# 2026-10-13 技能特效颜色与形状资源修正

- 需求：用户明确特效颜色或形状不对，重新核查资源/Shader链，不能仅证明Particle非空。
- 修改文件仅三正式配置资源：`LYF_MMO/Assets/GM_Game/Shaders/ShaderVariantCollectorSetting.asset`、`LYF_MMO/Assets/GM_Game/Shaders/MyShaderVariants.shadervariants`、`LYF_MMO/Assets/GM_Game/Shaders/MyShaderVariants.json`。新增分析`项目分析/SKILL_FX_APPEARANCE_SHADER_VARIANTS.md`及本记录。
- 已复现缺项：Unity原生读取实际新1071 Shader包只含旧17 Shader/20变体，用户根新集合为77/165未被Collector收集；SavePath指根而正式收集目录指GM_Game/Shaders。构建已结束后才执行资源修改，没有打断用户打包。
- 修改思路：SavePath定向原正式收集地址，用户新165变体直接同步原collection/JSON；真实YAML逐身份验证全部旧20已在新165中，无旧变体损失，不新增合并框架。
- 用户稿保护：两meta/GUID、根新collection和JSON完全不动；Runtime、Prefab、材质、Shader源码、Collector和正式1071输出未改。当前基线/末稿SHA、before备份位于`_temp/skill-fx-appearance-20261013/package/resource-before-hashes.json`、`resource-final-hashes.json`和`before-*`。
- 末稿SHA256：setting=`F75675A236CDB2A74320C863804E8044DB1C8419F7DF050D583F1CC5D076F89D`，collection=`0D13D4B9A5A30738F6CCE82DD19DBFA01AE6D9A6DC2F2453037B27E36F721CE5`，JSON=`FFF097DA3D8B5FB0165B3C77BA72DD6BE6AF6426A45326FD3EAA456A9A047F43`。
- 验证准备：主AssetDatabase刷新后以原正式规则临时实际构建Shader/特效依赖，独立测试对比源、原1071和临时修复包画面；待结果后追加。原1071基础引用和isSupported均有效仍出现差画面，故不以这些结果充当颜色修复验收。
- 失败留存：完整序列化脚本因MCP连接超时初次未返结果，后台稍后完成并写入526687字段/18489原始差异；引用ID、未用曲线及材质序列化重排产生噪声，已准备有效Shader属性名短版对比，不把rawdiff当实际故障。
- 原生有效材质属性末稿对比：主Agent执行`native-material-effective.cs.txt`成功，66材质、1527实际Shader公开定义字段逐名读取，差异0（包括有效颜色、向量、float、纹理引用格式/尺寸/UV、关键词）。完整rawdiff不代表有效材质属性缺失，纹理像素内容和实际Shader变体仍由独立渲染检查。
- 独立因果实验：43对纹理GPU回读RGBA全部逐像素一致，48次单实例重复截图一致；Skill3仅临时换GroundCrack/Crack11bcg的包HS_Blend_CG Shader为源Shader，白矩形消失，像素平均差1.95485→0.07428；Skill04 Smoke同Shader替换差0.58593→0.21459。测试未改正式材质/Shader，证明主要故障在实包Shader执行结果，临时重新构建当前165变体后继续验收。
- 未解决边界：Shader集合漏收是实证流程错误；它是否解释全部颜色/形状差异须实际修复包渲染及纹理内容/同源控制实验，当前没有宣称最终画面恢复或正式资源已发布。

## 截图与 shared 续查、临时末稿视觉阶段验证

- 续查不增加正式业务修改；新临时测试源、工程、程序集及静态清单脚本全部位于`_temp/skill-fx-appearance-20261013/{review,package}`。正式3资源修正范围保持。
- 正式1071静态依赖核对：8Runtime/36依赖包，实际大小及MD5零差异；唯一share名包为技能音效，HS_Blend_CG源GUID定义唯一。不能误称所有共享资源都必须采用share文件名前缀；统一Shader实际在unityshaders包。
- 真实临时43包Shader hash现为093936c2d20a6ef9518b6e51cc914694，原1071仍26c970663cecd022fc412170c2186735，二者分开保留，不发布正式输出。
- after首探针Particle为空原因是测试误将SimulateBuild空DependIDs当原生依赖闭包，随后又要求真实share包名映射进模拟清单而失败。两个失败保留；只修改_temp，改按原生AssetBundleManifest载全部43，并采用实际包名字典。AllActualCollectionBoundary2独立编译0错、主原生执行成功。
- 主初检末稿图24对中19逐像素一致，Skill3白矩形及Skill04黑轮廓已消失；5对残差、71元字段差异及Shader Compiler IPC记录交独立复核，不以引用非空或编译支持性代替最终画面验收。
- 未解决边界：仍须最终集合原生计数、残图/IPC审查；正式1071未更新或发布。主负责四协作入口最终汇总，完整分析`项目分析/SKILL_FX_APPEARANCE_SHADER_VARIANTS.md`。
- 最终集合计数确认：主Unity原生读正式1071=17 Shader/20变体、临时真实43包=77/165、当前源=77/165，证明新集合实际进入临时Shader包。响应`_temp/skill-fx-appearance-20261013/package/collection-after-unity-result.json`及`collection-native-after.txt`保留。正式1071未发布，残5图与IPC边界继续保留，不称全部现场通过。

## 主验收与协作记录更新

- 主更新文件：Docs/AI/PROJECT_CONTEXT.md、CURRENT_TASK.md、DECISIONS.md、TEST_REPORT.md，记录三资源修改范围、shared实包证据、修后画面、夹具失败、正式1071仍旧及下一步。测试响应/哈希/IPC证据全部放_temp，正式场景不保存，无业务代码新增。
- 主已查看用户两截图及修后源/包总览、两1.2秒原图；独立核Skill3/Skill04八对全部逐像素相同，且源图与before相同。白色矩形和黑色轮廓在修后临时实包消失。24对19完全一致，其余5轻微差异保留，48重复控全部一致。不能将空帧当可见技能完整验收，不能称所有特效全部逐像素相同。
- 原生IPC影响诊断已执行并保存review/ipc-affected-root-native-result.txt及原始响应：当前默认根材质Shader实际为Particles/Standard Unlit，而非此前猜测的Standard Surface；异常技能根Renderer关闭。HS_Blend_CG实包Shader消息0，源有3宏重复Warning。Standard Surface没有在此包LoadAllAssets命中，不声称该Shader或全构建IPC已经修好；捕图阶段无新IPC。历史真实通信错误完整保留。
- 资源末稿7路径SHA重核通过，root-resource-final-recheck.json；源与包collection计数已原生验证。真实43临时构建与已存在1071输出分开，未发布、替换正式版本或运行安装包。下一步沿原资源构建流程生成正式新版本后现场验证两技能；若再次出现IPC，保存具体构建日志按该Shader处理，不盲改shared或全库收集。
- 主已读取独立independent-shared-review-final.json与根日志 项目修改日志/2026-10-13-SKILL-FX-SHARED-INDEPENDENT-REVIEW.md：343冻结源342保持/唯一授权collection变化/缺0，24源图与before一致，71噪声只涉Color模式colorMin/colorMax，过滤后2203有效记录差0。统一裁切三列近图主目检通过，图review/native-20261013102847/named-skills-before-after-source.png。
- 四入口diff空白检查退出0；三资源一起检查仅报Unity生成collection空keywords尾空格，保持用户集合原始内容、不为样式重写资源。无新增客户端/服务端业务代码。
