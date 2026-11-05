# Unity 全量打包与 Shader 兼容修复（2026-11-05）

- 任务编号：`UNITY-FULL-BUILD-20261105`。
- 需求：用户反馈“怎么还报错啊”，要求继续解决 Unity 打包错误；执行者为用户明确指定的 Codex，未派发子 Agent、WorkBuddy 或其他模型。
- 分支：`codex/feature-development`；基线：`8822196b09fb7bbbb34c3b9942873a261507c812`。
- 验收范围：Unity 2023.2.20f1c1、Windows64、DefaultPackage、BuiltinBuildPipeline、ForceRebuild、LZ4、EncryptionNone，开启构建结果检查。

## 问题、修复与失败尝试

1. 用户本轮失败日志同时包含磁盘写入失败与 Shader 错误。D 盘当时仅剩 84,205,568 字节，原生构建报 `Copying data failed when creating archive file: 'Temp/unitystream.unity3d'` 和 `Failed to finalize AssetBundle!`。用户同意删除旧临时包，但自动工具审批两次拒绝，返回 `blocked by policy`。Codex 未删除该目录，也未改用其他通道绕过；随后外部操作释放了空间，构建得以继续。
2. 十个 Shader Graph 在 PBRDeferredFragment 的声明、调用和 ShadowMask 输出条件上不一致；Shader Graph 16.0.6 本地包只统一两处条件为 `OUTPUT_SHADOWMASK`，保留所有原有资源与版本。参考 [Unity 官方源码](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRDeferredPass.hlsl)。
3. 三份 Toon include 的旧坐标宏与 UNITY_LIGHT_ATTENUATION 不配套，导致 `_ShadowCoord` 不存在。修复宏并传 UV1 后，第一次全量构建暴露了天使环关闭变体未声明 `texcoord1`，再将 UV1 声明移出该条件。没有关闭阴影、删除 Shader Pass 或停用材质。
4. 本地包路径变化后，旧 Bee 编译图一度引用已移除的 PackageCache 路径，出现 CS2001。通过 Unity `CompilationPipeline.RequestScriptCompilation(CleanBuildCache)` 重新生成脚本编译缓存，最后两次清理后的编译均无 C# 错误；没有删除 Library 或用户资源。
5. 第二轮资源构建返回成功，但 Console 仍有 `m_InstanceIDToAssetBundleIndex.count(id) > 0` 断言及四个编辑器对象不能打包的警告，不能据此报告验收通过。
6. 第三轮在主资源过滤器排除 HumanTemplate、两个子图和变体收集配置后，仍出现断言与两个子图警告。源码核对证明自定义 IFilterRule 只用于主资源；GetAllDependencies 走 DefaultFilterRule，子图会再次进入共享包。在 YooAsset 1.5.7 默认过滤规则中补充 `.shadersubgraph`，第四轮子图警告消失，但原生断言仍在，证明断言另有独立来源。
7. 对齐原生日志的包大小和文件写入顺序，定位到 UGUI 的四张 TMP 编辑器图标。单独构建这四张 PSD 即复现相同断言（`isolate-icons.log`）。第四轮报告中 47 个主资源（含 6 个场景）携带这些图标依赖，包含 ChatItemWidget.prefab；证据见 `editor-icon-reference-evidence.json`。在共享的 IsValidateAsset 中排除 `Packages/com.unity.ugui/Editor Resources/`，同时覆盖主资源与依赖；这是最终消除断言的针对性修复。

## 逐项文件清单

| 文件 | 操作 | 用途与具体变化 |
|---|---|---|
| `LYF_MMO/Assets/GM_Game/Models/Role_JX/Shaders/UCTS_DoubleShadeWithFeather.cginc` | 修改 | 补充 UV1；阴影坐标声明和传递改用与 UNITY_LIGHT_ATTENUATION 配套的 Unity 宏。 |
| `LYF_MMO/Assets/GM_Game/Models/Role_JX/Shaders/UCTS_ShadingGradeMap.cginc` | 修改 | 所有天使环变体统一保留 UV1，修复 ShadowMask 阴影坐标宏的组合。 |
| `LYF_MMO/Assets/GM_Game/Models/NPC/QuQu/Shader/UCTS_ShadingGradeMap.cginc` | 修改 | 同步修复 NPC Toon Shader 的 UV1 与 ShadowMask 宏。 |
| `LYF_MMO/Assets/Editor/CollectWithoutAsmref.cs` | 修改 | 排除 .asmref、HumanTemplate、Shader 子图及 ShaderVariantCollectorSetting 编辑器配置；保留运行时资源。IFilterRule 是 YooAsset 强制要求的扩展接口。 |
| `LYF_MMO/Assets/AssetBundleCollectorSetting.asset` | 修改 | 仅将 Models、Shaders 两个收集器接入现有过滤器；该文件其他任务的 StreamingAssets 分组不进入本次提交。 |
| `LYF_MMO/Packages/com.unity.shadergraph-16.0.6-shadowmask.tgz` | 新增 | 固定 Shader Graph 16.0.6 修复包；2,497 个文件中只有 PBRDeferredPass.hlsl 改变，统一两处 OUTPUT_SHADOWMASK 条件。 |
| `LYF_MMO/Packages/com.unity.shadergraph-16.0.6-shadowmask.patch` | 新增 | 提供上述两处条件修复的可审阅零上下文差异；手动应用需 git apply --unidiff-zero。 |
| `LYF_MMO/Packages/com.tuyoogame.yooasset-1.5.7-build-filter.tgz` | 新增 | 固定 YooAsset 1.5.7 修复包；1,424 个文件中改变 DefaultFilterRule.cs 与 AssetBundleCollector.cs，主资源与依赖统一排除 Shader 子图及 UGUI 编辑器图标。 |
| `LYF_MMO/Packages/com.tuyoogame.yooasset-1.5.7-build-filter.patch` | 新增 | 提供 YooAsset 两文件补丁，便于后续合并或升级时核对。 |
| `LYF_MMO/Packages/manifest.json` | 修改 | 将 Shader Graph、YooAsset 指向项目内的本地 tarball，保留各自版本。 |
| `LYF_MMO/Packages/packages-lock.json` | 修改 | 固定两个本地包的来源与原有依赖关系。 |
| `Docs/AI/CURRENT_TASK.en.md` | 追加 | 英文续作事实、实际修复范围及完整日志入口。 |
| `Docs/AI/CURRENT_TASK.md` | 追加 | 对应中文事实；由用户指定的 Codex 直接同步。 |
| `Docs/AI/TEST_REPORT.en.md` | 追加 | 英文验收摘要、失败链及未验证边界。 |
| `Docs/AI/TEST_REPORT.md` | 追加 | 对应中文验收摘要。 |
| `Docs/AI/RECORD_INDEX.md` | 生成 | 刷新本次新增记录的标题、行段及源摘要；保留已有工作区记录。 |
| `项目分析/项目难点.md` | 追加 | 记录全量构建中磁盘、Shader、包缓存和依赖收集问题的分层排查。 |
| `项目修改日志/Unity全量打包与Shader兼容修复-2026-11-05.md` | 新增 | 集中保存逐文件变化、失败与验收证据、Git 回执及边界。 |

本次提交没有已跟踪文件的移动或删除；旧的未提交依赖草案移至本任务 _temp 保存；Assets 中没有新增文件，已有 CollectWithoutAsmref.cs.meta 未变。两组 tarball/patch 位于 Packages，无 Unity .meta。其他用户改动保留在工作区，使用独立 Git 暂存索引分离本次内容。

## 实际验证

- 最终全量版本：`2026-11-05-1144-icons`；耗时 `741` 秒；结果 `Success=true`，日志包含 `ForceRebuild pipeline build succeed !`。
- `2820` 个主资源、`1181` 个包；逐包核对文件存在与 BuildReport 文件大小，缺失和大小不符均为 0。
- 最终构建日志没有 Shader/C# 编译错误、磁盘归档错误、资产索引断言及编辑器对象不能打包警告；构建后 Console Error/Assert 条目为 0。
- 修复前报错的十个 Shader Graph 与两个 Toon Shader 名称均有最终序列化输出；UIRoot.prefab、Chat.prefab 仍在清单中；.asmref、.ht、.shadersubgraph 和 ShaderVariantCollectorSetting.asset 不在主资源清单。
- 构建前核对了 2,820 个主资源、20,411 条依赖引用：编辑器对象匹配 0，UIRoot/Chat 保留。YooAsset 正确加载本地修复包，IsIgnoreFile(".shadersubgraph") 返回 true。
- 两个依赖包均逐文件对比原包，Shader Graph 2,496 个文件和 YooAsset 1,422 个文件原样保留；修改文件数分别为 1 和 2。
- 实际输出目录：`D:/unitypro/LYFMMORGP/LYF_MMO/Bundles/StandaloneWindows64/DefaultPackage/2026-11-05-1144-icons`。
- 逐项证据：`_temp/unity-force-build-20261105-01a1a67e/verification.json`、`force-accepted-result.json`、`force-accepted.log`、`console-final.json`、`dependency-map-final.txt`、`compile-refresh.log`、`yoo-compile.log`、`icons-filter-compile.log`、`package-verification.json`、`yoo-package-verification.json`。
- 失败证据：同目录 `editor-before.log`、`force-first.log`、`force-final.log`、`verification-with-assert.json`、`force-clean.log`、`force-clean-errors.json`、`force-verified.log`、`force-verified-errors.json`、`isolate-icons.log`、`cleanup.json`；保留失败事实，不将中间成功回执作为最终验收。

## 未验证与后续注意

- 本次在当前工作区验证 Windows64 YooAsset 资源包，包含已有本地资源配置；未在干净检出重复全量构建，未执行 EXE/移动端打包、两设备真人联机或所有场景逐项视觉回归。
- 两个本地补丁包后续升级时需对照 .patch 核对是否已被上游修复；本次未升级 Unity 或包版本。
- 旧临时包清理没有完成；项目当前空间足够完成本次全量构建，该清理已不是构建阻断项。
- 难点复盘：`项目分析/项目难点.md` 中同任务编号条目。

## 技能与额度证据

- karpathy-guidelines：依据真实失败限定最小修复范围，并分离未关联的用户改动。
- computer-use:computer-use：查看构建界面；后台窗口无法可靠定位后转用编辑器 MCP。
- unity-mcp-orchestrator：解析本地依赖、请求干净脚本编译、核对依赖清单、执行真实全量构建并读取 Console。
- task-log-policy：按索引定向查证，收到本清单的明确授权后才发布本日志和双语记录。
- token-monitor：运行 collect/report；本轮原生计数和当前对话压缩次数见 `_temp/unity-force-build-20261105-01a1a67e/task-usage.json`、`context-compactions.json`。原始工具结果只留本机 _temp；账户剩余额度本地日志不可测。

## Git 集成状态

- 实现提交：`947257b9d9b9b95d3910cf36da7d0047c949a7c5`，11 个代码/配置/依赖文件，已推送 `origin/codex/feature-development`；远端分支头已核对一致。
- 开发者已通过原日志清单弹窗明确回复“写入”；本次正式日志、四份双语记录、索引与难点复盘按该授权单独提交。实现与记录分开提交。
- Git 证据：`_temp/unity-force-build-20261105-01a1a67e/git-receipt.json`、`git-push-output.txt`。

## 本轮本地额度快照

- 统计窗口：`2026-11-05T09:12:13.163Z` 至 `2026-11-05T10:59:43.906Z`（UTC）；原生输入 26941349，其中缓存 26740992，输出 140406。不含随后收尾输出。
- 上下文/项目规则、用户需求、工具输入输出、重复读取/返工/验证均无独立 token 计量；子 Agent/WorkBuddy 未调用，协作用量未记录。
- 当前对话累计压缩 3 次，源为当前会话原始 JSONL 的 compacted 事件，观测无缺口。token-monitor Codex 适配器为实验性来源；账户剩余额度本地日志不可测。

## 日志收尾验证

- Codex 按用户明确要求直接同步本次中英文段落；核对同一任务编号、版本、2,820 主资源、1,181 包、741 秒、错误/断言 0 及未验证边界。只检查本次增量，不重验其他历史。
- 本次仅发布已批准记录，不修改构建输入或重复运行 Unity；沿用最终构建证据。私有 Git 索引只纳入本次七份文档的新增内容，保留无关工作区变动。
- 本次收尾额度与压缩计数证据保存在 `_temp/unity-force-build-20261105-01a1a67e/record-close-usage.json` 和 `record-close-compactions.json`，完整工具输出保留在同目录。

## 日志发布回执与本次收尾用量

- 日志发布提交：`a8c5b61c1015e16c85aa0c1fe8d44868442784d3`；范围为完整日志、CURRENT_TASK/TEST_REPORT 各自中英文版、RECORD_INDEX 和项目难点，共七份文件。已推送 `origin/codex/feature-development`，远端分支头核对一致。
- 文档增量核验通过：与批准草稿一致、中英关键字段一致、工作区和提交内容各自索引有效、git diff --check 通过；证据 `_temp/unity-force-build-20261105-01a1a67e/record-close-verification.json`。未改动构建输入，未重跑 Unity。
- Git 原始回执：`_temp/unity-force-build-20261105-01a1a67e/record-close-git-receipt.json`、`_temp/unity-force-build-20261105-01a1a67e/record-close-push-output.txt`。
- 本次日志收尾统计窗口：`2026-11-05T11:01:40.709Z` 至 `2026-11-05T11:05:12.090Z`（UTC）；原生输入 955412，其中缓存 944384，输出 6461。该快照不含随后回执补记和最终回复。
- 上下文与项目规则、用户需求、工具输入输出、重复读取/返工/验证均无独立 token 计量；子 Agent/WorkBuddy 未调用，用量未记录。token-monitor 已完成 collect/report，Codex 适配器为实验性来源，账户剩余额度本地不可测。
- 当前对话累计压缩 3 次，依据本对话原始 JSONL 的 compacted 事件，观测截至 `2026-11-05T11:05:12.090000+00:00`，无覆盖缺口。额度与压缩证据见本日志前述 record-close-usage.json、record-close-compactions.json。
