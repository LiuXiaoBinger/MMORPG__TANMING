# 真实主城导航预览不一致返工

- 日期：2026-10-20；状态：修复及真实主城范围验收完成。
- 背景：用户新截图中蓝面大块覆盖地形，反馈与Unity导航显示仍不同。前轮数据/隔离深度验证不等于真实Scene视觉通过，本轮以同镜头对照定位。
- 用户澄清：实际关注“可走范围不一致”，重点验收当前数据来源和覆盖范围；颜色/线框不作逐像素复刻需求，避免排查继续扩大。
- 范围：必要Editor导航预览修复，不重烘焙/重导覆盖、保存场景或改服务器与角色移动。
- 分工：navigation_walkable_preview开发及Unity操作；navigation_preview_review独立审查；主协调验收及协作记录。
- Docs/AI/CURRENT_TASK.md、TEST_REPORT.md已记录本轮诊断与最终交付；完整文件、根因、验证与边界见下文。
- 分析入口：项目分析/UNITY_NAVIGATION_EXPORT.md；真实多轮排查同步项目分析/项目难点.md。中间证据_temp/navigation-preview-scene-fix-20261020。

## 已确认的真实主城证据

- 当前主城非Play/dirty=true，已经加载123977面，原生showNavMesh=true，与前轮全局0面基线不同。
- 同Scene相机的完整编辑器绘制（Internal_DrawCamera后Internal_FinishDrawingCamera）取得全隐藏/原生单开/文件单开/双开四态，strict-mode-0～3.png；主已目检1～3，双开精确复现用户截图。
- 自写单开仅LessEqual裁掉被地表遮挡的导航面，中央大三角直接露草；Unity原生仍显示蓝色覆盖，岩石上原生也有蓝面和黑线。实际差异是深度显示方式与同时叠加，不能据截图改地图或认定索引损坏。
- 普通Camera.Render/Handles.DrawGizmos不足以绘原生NavMesh，早期native-only空图未当验收；最终采用现有编辑器绘制API，没有引入截图工具或正式测试入口。修改仍限两Editor文件，末稿验收见下文。
- 方案核正：曾拟Greater淡色/LEqual亮色两次绘制；主与审查随后核中央与可见区域像素，原生均可用统一青蓝(0,194,256)与alpha0.5解释，误差在量化范围内。没有原生使用两种透明度的证据，撤回双色设计，按真实样本统一透明叠加，避免引入另一套不同外观。这不等于已经反编译原生内部实现。

## 最终修改文件

- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs`：原网格单次透明叠加，避免地表深度裁切漏面；显示时暂停原生NavMesh，隐藏/关闭/重载恢复之前状态。Unity2023.2的内部显示属性通过缓存PropertyInfo在Editor边界访问，首次直接访问CS0117已修，不加兼容框架。
- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationExportWindow.cs`：说明完整烘焙面含遮挡区域、原生暂时互斥以及当前角色/连通范围边界。
- `项目分析/UNITY_NAVIGATION_EXPORT.md`、`项目分析/README.md`：更新当前预览说明、真实主城覆盖验收及已知颜色差异。
- `项目分析/项目难点.md`：补多轮失败、真实主城四态、双色方案被证据否定、最终范围核验和面试复盘。
- `Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md`及本日志：同步当前交付、验证与交接。

## 验收结果与未解决边界

- 本轮当前全局网格与公共文件的顶点/索引/区域逐项一致、Surface/asset位姿0/identity；文件270735顶点/123977面，没有修改其数据。修后主城同镜头single-pass-mode-2.png已主与独立审查目检，中央旧三角漏显补齐。
- 独立覆盖核验：同镜头底图差分青蓝mask、2px线边容差，原生626252像素/文件625132像素，缺81/多146像素。此项仅证明当前镜头显示覆盖，不是导航正确率；几何一致性由三数组逐项比较独立证明。
- Unity末稿编译通过、Console0条；Game排除和遮挡/可见都叠加三项通过。原生原值false/true重复Show/Hide六项、Close及实际重载恢复通过；开发13保护输入保持。独立及主复核427冻结路径425保持，仅两授权源变化，末稿SHA与交付一致；独立报告review/final-review.json，主汇总root-final-validation.json，均位于本轮_temp证据目录。限定git diff --check通过。
- 交付主城非Play/dirty原true/123977面，镜头与原现场一致；预览已显示，原生暂false、恢复值true、监听1。未保存/切换场景、启停Play、重烘焙、重导或修改插件、服务端和角色移动。
- 当前颜色仍略深，未复制原生黑线；按用户明确的范围诉求停止颜色排查，不称逐像素颜色一致。未验证当前位置连通范围、当前玩家参数完全匹配或实际移动导航；这些不能由烘焙区域预览推导。无本轮代码阻断，用户可直接在Scene查看。
