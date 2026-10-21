# 2026-10-13 技能特效与运行报错独立验收

- 职责：独立测试与只读审查，不修改客户端/服务器业务及四协作入口。
- 审查末稿：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`、`LYF_MMO/Assets/GM_Game/Editor/Skill/SkillEditorAssetTools.cs`、`LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/SkillVerification/SkillPresentationBuildVerification.cs`，由开发Agent修改。
- 证据文件：新建 `_temp/skill-fx-package-runtime-20261013/review/` 下前后精确方法运行夹具、编译工程/日志/280源码SHA、Manifest/BuildReport与39实际包大小/MD5审计、Unity内存动作4项和995实际加载脚本、final-audit.json和review-report.md；证据保留，未清理历史临时目录。
- 设计思路：用已复现空表移动和真实八份配置验证最小短路/计数修复；仅核实际引用与现包，不靠扩大目录打包解释特效缺失。
- 结果：before空表移动NullReference、final同方法18项通过，独立280源0错39既有警告，主Unity真实动作4项/编辑器编译/ValidateAll通过；主专用43包真实构建加载8Runtime/12FX通过；995实际39包加载8Runtime/12FX/8动画与182Renderer/182材质Shader检查通过，控制台0错误。主573条291去重文件SHA一致。
- 保护：独立冻结Collector/ResourceMgr/FxEvent三个事前稿完全不变，Runtime八份记录SHA未改；未触碰正式场景、资源、服务器、发布或安装包。
- 未解决边界：现Player.log未出现用户所称安装包技能报错，无真实包侧堆栈；未启动安装包登录或验正式施法画面、Shader变体，不能据原生引用加载宣称全游戏无错误。动作修复需正常热更重编发布后游戏回归。
- 工具返工：临时工程XML/OutDir、native缺return/动画加载依赖、Roslyn不可用仅_temp修复，保留失败证据；没有为测试修改业务。
- 报告 `_temp/skill-fx-package-runtime-20261013/review/review-report.md`；分析 `项目分析/SKILL_FX_PACKAGE_RUNTIME_DIAGNOSIS.md`；主修改日志 `项目修改日志/2026-10-13-SKILL-FX-PACKAGE-CHANGELOG.md`。

## 移动报错的热更版本补充取证

- 用户明确报错发生在移动；不再要求重复提供已知编辑器错误。主委派只读版本补查，不涉及业务修改、重编、打包或发布。
- 新证据位于 `_temp/skill-fx-package-runtime-20261013/review/code-version/`：Mono.Cecil核四来源真实OnUpdate IL/哈希/MVID、主995真实代码Bundle提取副本及原生读取结果、版本报告；仅该_temp目录及本日志/独立报告新增记录。
- 结果：正式热更TextAsset和995实际包代码DLL同为567808字节、SHA9D399E048A3527D7B988103AE6DA22D748E44E68CD3F70C8743B1B67B6C0F320，139条IL仍含null脚步表后无条件索引。现Library和独立验收DLL174条IL已修；旧类型脚步表赋值次数0。以真实IL确认，不单靠修改时间。
- Boot非Editor加载YooTextAsset程序集，说明995资源完整而代码仍旧；当前安装包移动故障尚需正常热更同步修复源，不宣称已消除。没有正式DLL/资源包写入或启动安装包回归。
- 报告 `_temp/skill-fx-package-runtime-20261013/review/code-version/code-version-report.md`；此补查覆盖此前“包端异常待具体证据”的版本调查缺口，四入口由主更新。
