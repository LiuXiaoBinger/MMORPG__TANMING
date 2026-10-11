# 2026-10-08 技能代码与配置目录分离

## 需求与范围

用户要求 `LYF_MMO/Assets/GM_Game/Scripts/Data/Skill` 保存生成的技能配置，移出此前混入的运行、编辑和验证源码。只整理路径与编译引用，类名、字段、功能、程序集身份及已有配置保持。

## 目录方案与分工

- 顶层9个运行与数据结构源码及 `.meta` 迁到 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill`。
- Editor直接6个编辑工具源码、对应 `.meta` 与 `LYF.SkillAuthoring.Editor.asmdef` 迁到 `LYF_MMO/Assets/GM_Game/Editor/Skill`。
- Editor/Tests的5个验证源码及独立程序集，连同原目录 `.meta` 迁到 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/SkillVerification`，继续与现有合同测试程序集隔离。
- 配置继续位于 Data/Skill 下的 Editor/Authoring、Runtime、Resources 和根规则 `.asset`，原路径与内容不修改。
- 更新3份Unity编译索引及 GameServer.csproj 的3条客户端共享源路径，不改协议或服务器逻辑。
- 开发子 Agent `skill_directory_develop` 执行迁移，`skill_directory_review` 独立只读审查，`skill_directory_test` 独立编译和引用验证；主 Agent 负责验收与归档。

## 当前状态与证据

目录迁移、独立审查与对应隔离编译完成。迁移前 Data/Skill 共99文件，含20个C#与2个asmdef；SHA256基线位于 `_temp/skill-code-relocation-20261008/data-skill-before.json`。45项源码/asmdef/meta迁移原字节不变，54项配置和元数据原位保留，Data/Skill内.cs/.asmdef均0。正式Unity已有一个GUI实例与两个导入worker，不启动第二同工程Editor。

详细状态 `Docs/AI/CURRENT_TASK.md`，测试结果 `Docs/AI/TEST_REPORT.md`，技能用法 `项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`。所有中间产物留在项目根 `_temp/skill-code-relocation-20261008`，未经确认不清理。

## 实际修改文件

- 运行目录迁入：`SkillCastingRules.cs`、`SkillCastingRulesCatalog.cs`、`SkillCastingRulesCollection.cs`、`SkillCastingRulesConfig.cs`、`SkillCastingRulesValidation.cs`、`SkillFxPlacementKind.cs`、`SkillFxSocketResolver.cs`、`SkillRuntimeFxEvent.cs`、`SkillRuntimePresentation.cs`及各自meta。
- 宿主Editor目录迁入：`SkillCastingRulesConfigInspector.cs`、`SkillCastingRulesExportTool.cs`、`SkillEditorAssetTools.cs`、`SkillEditorBinding.cs`、`SkillEditorBindingInspector.cs`、`SkillEditorFxSource.cs`、`LYF.SkillAuthoring.Editor.asmdef`及各自meta。
- 验证目录迁入：`SkillAuthoringAssetDiagnostics.cs`、`SkillManifestAssetSnapshot.cs`、`SkillManifestBundleSnapshot.cs`、`SkillManifestSnapshot.cs`、`SkillPresentationBuildVerification.cs`、`LYF.SkillAuthoring.Verification.Editor.asmdef`及各自meta，目录meta由原Tests沿用。
- 编译路径修改：`LYF_MMO/GM_Game.csproj`、`LYF_MMO/LYF.SkillAuthoring.Editor.csproj`、`LYF_MMO/LYF.SkillAuthoring.Verification.Editor.csproj`、`LYF_Server/GameServer/GameServer.csproj`，准确变更25个路径区间，保留原编译顺序/Link语义与所有其他内容。
- 文档修改：本日志、`Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md`、`项目分析/YCOMBAT_EDITOR_INTEGRATION_GUIDE.md`、`项目分析/YCOMBAT_EDITOR_INTEGRATION_USAGE.md`，同步当前源码路径说明。

## 设计与验证结果

迁移仅调整职责目录，不改类名/字段/实现/程序集身份。保留原源码与meta使24份配置的m_Script仍唯一指向原脚本；所有配置路径和字节保持。原两份Editor asmdef随源码迁移形成正确边界，验证程序集不会被现有合同测试程序集吸收。服务器原3个共享规则编译路径同步更新，没有复制第二份规则或改协议。

- 开发45项迁移hash、99项全量保护通过；独立审查与独立测试再次按自身推导映射验证均PASS，Data/Skill仅余24份asset、1份JSON、29份meta。报告 `independent-final-review.md`、`independent-test-report.md`、`migration-content-hashes.json`。
- 冻结后GM_Game、宿主Editor、验证Editor、GameServer完整引用隔离重建均0错误，警告37/2/2/11，原代码/引用与隔离Unity源码生成器适配提示保留，不因源码搬迁变更行为。所有bin/obj均在本轮_temp，正式程序集未覆盖。
- 两端及隔离部署SkillCastingRules.json哈希一致；无旧源码Compile路径、缺失或重复项目条目；独立7外围保护不变。四工程仅准确路径区间变更，源码原字节一致，git diff --check通过。
- 早期路径脚本预检因全局逆转前缀碰到既有SceneEntity/Skill而安全停止，未写工程；修复为精确区间验证后完成，记录保留。无需业务源码返工。

## 未解决与后续交接

正式Unity当前MCP无法连接，Editor.log没有本次迁移导入/域重载证据；隔离C#编译不冒充正式Unity实编。两个新增Editor文件夹的目录meta待当前Editor下次资产刷新自动生成，不手写GUID；已迁脚本/asmdef/meta全部保持原身份。下次正常回到Unity时继续其资产刷新，若出现具体错误按开发子Agent/独立复审流程修复。

未改技能功能，不运行角色/按键/四服/Bundle重复验收，也没有测试组件挂正式场景。所有临时证据保留项目根_temp，不清理用户文件。
