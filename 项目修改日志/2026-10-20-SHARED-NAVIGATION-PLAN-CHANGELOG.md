# 两端共用导航数据的接入步骤

- 日期：2026-10-20。需求：用户在询问Unity烘焙后，进一步问服务器也要使用，应怎样做。
- 范围与文件：仅`项目分析/RO_PATHFINDING_ANALYSIS.md`第5.3节、`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`及本日志4份文档。未修改客户端/服务端业务、用户新导航资源、场景或包清单。
- 当前事实：用户已安装AI Navigation 2.0.6，主城NavMeshSurface已引用新NavMesh-MainCityNavigation.asset；服务器.NET Framework 4.8，当前移动保存转发终点，尚无导航查询实现/依赖。补充说明早期未生成导航资源与当前已生成是不同检查时点。
- 设计思路：保持RO职责与接口方向，先验证两端共享查询后端兼容，再做原几何/角色参数导出、公共文件烘焙、服务端加载查询、客户端沿路点移动。当前Unity资产保留；不将其改后缀当服务器文件，不将已缩边的三角网再次按同半径无差别烘焙。
- 验证：静态源码/包/框架/Surface及asset GUID核对、本轮文档段编码、输入SHA，证据`_temp/shared-navigation-plan-20261020/validation.json`。纯方案未运行或构建业务，没有实测路径/性能。
- 调查边界：DotRecast上游csproj读取请求20秒超时，没有取得版本兼容依据；.NET4.8可用版本/底层实现、文件格式及正式资源加载位置待后续实现阶段核定，未安装或升级。猜测服务端Scene目录后已定位manger/SceneManager，未修改文件。
- 交接：正文`项目分析/RO_PATHFINDING_ANALYSIS.md`5.3节为候选方案及分阶段验收。业务实施必须委派客户端/服务端开发；不自动增加怪物AI、移动回拉、跨图及速度校验，不把普通地面网格直接用于拒绝跳跃/特殊位移。无本轮已复现待修故障，不新增虚构项目难点案例。
