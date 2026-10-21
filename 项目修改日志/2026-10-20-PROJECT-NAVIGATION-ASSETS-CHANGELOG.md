# 本项目导航资源现状核查

- 日期：2026-10-20。用户询问.nav是什么、从何处来、本项目是否有。
- 修改文件：`项目分析/RO_PATHFINDING_ANALYSIS.md`补5.2节、`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`及本日志，共4文档；无业务/配置/正式资源改动。
- 内容与思路：先按RO解析器说明.nav是地图几何文本输入，具体原始导出工具未定位；再检查本项目真实磁盘文件、场景引用、配置、代码与可选插件，避免把Unity基础模块/区域配置/同名导航类等同于已生成地图或已接入寻路。
- 验证结果：包含忽略与隐藏文件并排除缓存/中间目录后.nav扫描为空；两主城场景导航数据引用0；Assets文本未命中NavMeshData类型块/NavMeshSurface；正式业务未查到NavMesh查询/烘焙接入，现有导航为空壳、移动用CharacterController。证据`_temp/project-navigation-assets-20261020/validation.json`，正文`项目分析/RO_PATHFINDING_ANALYSIS.md`。
- 核查过程：一次rg模式被误认参数，改-e后成功重跑，仅为只读命令错误，无业务变更；参数配置与动作编辑器位移后端已分别说明。
- 未解决/交接：未核Unity运行时内存或外部资源下载；RO原始导出器仍未定位。后续选Unity导航或RO工具链时应使用本项目地图自己的几何与生成数据，本轮不选择、不实施。
