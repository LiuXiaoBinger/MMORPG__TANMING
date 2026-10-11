# 2026-10-10 技能编辑器图文解析 HTML 文档新增

- 用户要求：检查本项目的技能编辑器，总结其使用方法，生成一份图文解析文档，要求包含详细的代码接口解析。
- 任务性质：只读代码分析 + 仅新增交付文档/协作记录/变更日志；禁止修改任何业务代码、配置资产、场景、预制体与包源码。

## 新增文件

- `项目分析/SKILL_EDITOR_GRAPHIC_GUIDE.html`：自包含单文件图文解析文档（约 260KB，全中文，11 章，9 张内联 SVG），可直接双击在浏览器打开，断网可用。
- 协作记录：`Docs/AI/CURRENT_TASK.md`（顶部条目更新为已完成）、`Docs/AI/TEST_REPORT.md`（追加本次测试记录）。
- 本变更日志：`项目修改日志/2026-10-10-SKILL-EDITOR-GRAPHIC-GUIDE-CHANGELOG.md`。

## 文档内容（11 章）

1. 编辑器定位与整体架构：嵌入包 Runtime / 包编辑器镜像 / 宿主桥接 / 运行时技能与配置 / 独立验证五部分职责与程序集引用关系。
2. 窗口界面解析：6 个菜单入口、SkillEditorWindow 工具栏三行控件、Timeline 区、Inspector 区、预览隔离副本机制、交互快捷键与持久化事务。
3. Timeline 轨道与 Clip 体系：BuiltinTrackRegistry 全部 16 条轨道逐一列出字段类型、中文标签、默认值、单位/取值范围与语义（动画、特效/音效、位移、朝向、碰撞判定三页签、技能衔接、取消、流程节点、飞行物、命中特效、镜头、预警、霸体、刀光、交互窗）。
4. 核心数据资产：SkillConfigSO 顶层 30+ 字段与 16 类嵌套列表元素字段、全部枚举速查；Authoring 资产与 Runtime 资产的区别及 SkillEditorBinding 绑定关系。
5. 校验与导出发布链路：身份五元组核对、剪辑时长核对、Y 配置白名单/默认值校验、DefaultOnlyFields 未支持数据阻止发布清单、Runtime SO 写入与 YooAsset 收集、独立验证程序集检查项。
6. 运行时接口详解：施法入口 18 跳链路；MSkillComponent/Core/MSkill/MArtSkill/MSkillData/MSkillFactory/MSkillPrediction 公开方法签名；FxEvents 缓存引用计数、FxLifetime 独立租约、挂点解析、五种定位。
7. 施法规则配置接口：SkillCastingRulesConfig/Collection/Catalog/Validation 数据结构与装载时机；InteruptSkill、MoveCasting 语义与边界；服务端 RoleSkillComponent.TryCast 十步校验顺序；两端 JSON 同源双写。
8. 完整使用教程：打开编辑器 → 绑定动画/参考 → 逐轨道编辑 → 校验导出 → 运行时接入 → 打包发布的端到端步骤，含常见返工场景与工作习惯建议。
9. 详细代码接口手册：按四个程序集分组的核心类/接口/枚举/委托清单（Ethan.ActionEditor.Editor、Ethan.ActionEditor.Runtime、LYF.SkillAuthoring.Editor、LYF.SkillAuthoring.Verification.Editor、GM_Game 运行程序集）。
10. 附录与索引：名词约定、FAQ、限制清单、文档索引。

## 图（9 张，全部内联 SVG，无外部依赖）

- 图1-1 五部分架构与程序集引用关系图；图2-1 SkillEditorWindow 界面布局标注图（明确标注"界面示意图，非 Unity 实机截图"）；图3-1 轨道→Clip→字段分组层级图；图5-1 校验·导出·双端 JSON 发布时序图；图6-1 运行时技能播放流程图；图7-1 施法规则双端数据流图；第 8 章教程流水线图；另含导读与附录辅助图。

## 设计思路

- 由三个并行只读研读子 Agent 分模块逐文件读真实源码（编辑器窗口与轨道 / 嵌入包 Runtime 与宿主导出链路 / 运行时技能与施法规则），接口签名、字段、默认值、单位与语义一律以源码为准，每个接口解析标注源码相对项目根目录的路径；已有分析文档仅作索引与交叉引用，不做二手转述。
- 文档为单文件：CSS/JS/SVG 全部内联，不依赖外网 CDN；内置离线轻量语法高亮。

## 验证结果

- 使用 html skill 的 shot.py 在桌面（1440 宽）与移动（390 宽）两视口实际渲染：桌面 consoleErrors、resourceErrors、horizontalOverflow 均为空，无挤压列；目录锚点跳转正常；全部 SVG 图渲染无文字重叠；代码块高亮正确；表格列宽完整。移动端卡片堆叠、表格与代码块横向滚动、图表占满视口宽度。
- 修复过程：① 高亮脚本多趟 replace 打烂自身插入的 span 标签 → 改为单趟分词；② 网格卡片横向溢出 → 补 min-width:0；③ 架构图箭头穿越条件编译胶囊、界面图"时间轴尺"标注压"新建"按钮、时序图失败文字碰菱形 → 分别改路由/删冗余标注/移位并重新内联；④ 窄屏表格改为横向滚动。

## 未修改代码声明与未决问题

- 全程只读，未启动 Unity，未改动任何业务代码、配置资产、场景、预制体与包源码。
- 界面图为示意图而非实机截图；源码层面疑点（Jump.endKey 与 ActionPlayer jumpList 差一、NavMeshAgentBackend.SetActorCollisionIgnored 空实现、Global.Attack.causePlayerStagger Obsolete 仍序列化、SkillTypeCombined/SkillTypeChannelling 已定义未接入、MoveCasting 服务端不读取且无水平移动权威结算、MMoveComponent `#if false` 死代码等）均已在文档正文以提示框如实标注。
