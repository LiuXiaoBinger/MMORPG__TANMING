# RO寻路源码分析整理

- 用户需求：查看RO参考项目的寻路实现并整理成文档。
- 范围：只读`F:/UnityPro/ro/Ro_cs422nb`中活动C#、原生C++及服务器相关入口，编写`项目分析/RO_PATHFINDING_ANALYSIS.md`和必要协作记录；不移植或改业务。
- 设计：按玩家操作与实际调用链说明点地、技能逼近、导航查询、沿路移动、到达/中断、追踪、跨图、同步及本项目当前差异；每项给源码定位，区分静态证据与运行结果。
- 职责：主整理业务链与正文，main_menu_review只读独立核原生查询/算法与失败语义。
- 修改文件：新增`项目分析/RO_PATHFINDING_ANALYSIS.md`；更新`项目分析/README.md`、`Docs/AI/PROJECT_CONTEXT.md`、`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`及本日志，共6份正式文档。未修改客户端、服务端业务或RO源码。
- 内容：14节正文含玩家流程图、职责/成员表、Detour A*实际调用、数据加载、逐帧距离消费、技能追近示例、目标重算、到达/中断、空气墙、跨图编排、位置上报与服务器校验、远端平滑及当前项目缺失链。
- 重要核对：纠正只凭扩展名判断.nav/.bytes用途的风险；两个PathFind重载分别说明；部分路径非空不等于完整可达；原生TrySetHeight未检查最近多边形状态，不能将true当有效点保证；发包成功不等于服务器确认。本项目现有同名组件与真正寻路明确区分。
- 验证：主24份源码定位及SHA清单、独立12份原生链源码节选/SHA核查及正文复核通过；最终复核输入SHA、正文14节/关键引用及6份文档存在。初次临时定位将本项目MoveModel标记误用void，按实际bool校正，无业务返工。仅文档不做业务编译，不调用Unity或启停服务。
- 证据：`_temp/ro-pathfinding-analysis-20261020/main-source-manifest.json`、`review/source-manifest.json`、`review/source-excerpts.txt`、`review/native-findings.md`及`root-final-validation.json`。
- 核查修正：首次全文编码检查遇协作记录旧历史段乱码，改为核本轮新增首段，其余历史内容保持；首次报告另存`_temp/ro-pathfinding-analysis-20261020/root-final-validation-first.json`，不将旧记录问题记成本轮正文失败。
- 未解决事项/边界：未实测寻路耗时、GC、成功率、特殊地图/动态障碍、客户端DLL与当前原生源码版本对应；这些是未执行的运行核验，不是本轮已复现缺陷。无当前整理阻塞，不自动移植或增加功能。后续实现须先明确导航数据/后端及既有移动接入范围。
