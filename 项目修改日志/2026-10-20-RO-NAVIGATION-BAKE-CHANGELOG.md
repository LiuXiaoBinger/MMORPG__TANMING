# RO导航数据制作流程补充

- 日期：2026-10-20。用户追问RO怎样把地图可走区域做成导航数据。
- 修改范围：`项目分析/RO_PATHFINDING_ANALYSIS.md`新增5.1节，`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`追加本轮记录，以及本日志。只改4份文档，不改业务或参考资源。
- 设计思路：把“烘焙”解释为开发阶段根据几何与角色规则生成导航网格，沿`.nav → 工具解析 → Recast构建 → Detour分块 → .bytes`实际源码说明；默认值和地图覆盖值分开，原始几何导出器未找到时明确标注，不臆造Unity菜单。
- 源码事实：独立工具RONavigation存在；BatchToolMenu普通地图调用HandleBuild/CreateServerNav；工具实现有坡度标记、栅格化、净空/落差过滤、半径收缩、分区、轮廓/多边形及Detour数据生成，保存RONV/version1分块.bytes。详见`项目分析/RO_PATHFINDING_ANALYSIS.md`5.1节。
- 验证：只读定位源文件、关键函数与SHA核验；新增正文及本轮协作记录段编码/引用检查，证据`_temp/ro-navigation-bake-20261020/validation.json`。纯文档未运行Unity、工具或编译构建。
- 核查修正：最初只读工程编译列表后半段，误判include下的实现未列入；最终全文检查确认ronavigation.vcxproj第264行已有ClCompile输入，正文与协作记录已纠正，不将目录命名当编译依据。首次证据保留为validation-first.json。
- 未解决事项：首份.nav从Unity/美术工程导出的具体工具、对象筛选规则未定位；现有exe未验证与源码版本对应；工具虽有编译输入，本轮没有实际重新编译。均为调查边界，无本轮运行故障依据，不扩展业务修复。
