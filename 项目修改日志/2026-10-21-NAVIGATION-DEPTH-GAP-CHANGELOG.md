# 自绘导航贴地深度漏显修正

- 日期：2026-10-21；状态：正式草地/障碍视觉通过，末次运行专项与用户追加原生恢复因MCP断开待完成。
- 用户问题：d332截图中大草地三角形在Unity原生导航中覆盖，自绘却裸露。前轮障碍可读性通过未覆盖此地形深度情况。
- 范围：必要Editor预览/Shader，不改公共文件、地图、烘焙、协议或角色移动；不退回无界透画、不擅加连通域。
- 分工：navigation_walkable_preview独占Unity诊断及最小开发，navigation_preview_review只读冻结/审查，主负责验收及记录。
- 计划验收：缺口位置同镜头成功原生/旧新自绘，确认数据与深度原因；草地补齐和柱树石遮挡同时通过，另核编译/Shader/资源清理/数据保护。
- 证据：_temp/navigation-preview-depth-gap-20261021。分析：项目分析/UNITY_NAVIGATION_EXPORT.md；连续排查复盘：项目分析/项目难点.md。修改文件、方案、实际验证与剩余边界完成后补齐。
- 已确认原因：depth-samples-result.json中缺口公共三角52401存在。Terrain点(101.34,18.66,86.89)下导航面Y17.88766、低0.771米；另一点低0.324米；相邻52400高地表0.047米。按场景深度直接LessEqual会裁掉这段低于地形的简化导航面，并非文件缺三角形。
- 原生参考：当前MainCityNavigation整个对象inactive、全局原生网格0，直接开显示开关无法参考。仅临时注册同NavMeshData，真实CaptureSceneView取得grass-native.png（123977面、草地完整覆盖），finally移除注册并恢复显示开关；不激活/保存场景。grass-before/grass-hidden同位置保留，当前自绘修正实验待补。
- 实验方案：三角三个顶点均高地表约0.047/0.047/0.094米，单改顶点不足；进一步861个面内采样最大地表高差1.590706米，采用有限1.65米显示容差。仅遮挡场景点与Terrain高度图相符（0.08米内）、导航面在其下且差值在范围内，才修预览片元深度；仍LessEqual并写深度，其他障碍按原深度遮挡。grass-experiment2与obstacle-experiment双景已主目检，缺口补齐且柱树石未回归穿透。当前仅实验通过，正式源/编译/清理待补。
- 失败实验：初版使用Shader内置相机矩阵的约定未重建出实际Scene点，grass-experiment仍漏显；换实际Scene相机矩阵后grass-experiment2成功。首失败图保留，不冒称通过。
- 用户追加要求“MainCityNavigation这个原先的网格不显示啊”：据本次授权恢复该对象/必要组件激活并实际显示原生蓝网格，自绘先Hide，最终留原生可见；不保存场景。先前不改inactive的范围按用户最新要求更新，不重复询问许可。原草地修复正式验证继续，显示模式分别记录。
- 正式修改文件：LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs、MNavigationPreviewLine.shader（沿用原文件/meta，无新增正式文件）。高度快照RFloat线性保存Terrain世界Y，面/线共用有界片元修正；UNITY_MATRIX_I_V实时更新、无新相机回调。新增Height Texture销毁及DepthTextureMode恢复，保面/线几何生成流程。后补两行高度图z/x局部变量用途注释。
- 已完成：正式grass-final、grass-rotated-final、obstacle-final主/独立目检通过，草地完整、转镜头继续生效、柱树石仍遮挡。原生参考grass-native真实捕获但仅属临时资产注册，finally移除，未激活正式组件。
- 未完成及原因：后续Unity MCP工具从会话ALL_TOOLS消失，实际localhost8082/mcp连接失败。用户最新授权的原对象激活/原生实际显示未执行；新增资源与相机深度模式的最后运行清理专项/最终Console未取，最后两行注释未再编译。不得写为原生已恢复或全量测试PASS。已向用户说明Hierarchy启用对象、Gizmos/ShowNavMesh和隐藏自绘的最短操作。
- 交接：连接恢复先按用户最新请求Hide自绘、激活MainCityNavigation/必要组件、实际原生Capture并留原生显示，不保存场景；再补本轮新增资源清理及数据保持的必要验证。项目分析/UNITY_NAVIGATION_EXPORT.md与项目分析/项目难点.md记录实际边界。
- 末稿保护与独立结论：429输入427保持，仅Preview/Shader两源改变；SHA分别bf7be1944e13299ef556a66bd58ebcbbb2c2a7b9aa81db2f90eace150cd3b99b、ecfa5b1e94e4d71520bdd434c5e21710058b3ae5d2fe306831fb56a20a58ec0f。review/final-review.json为“部分通过，运行验收未完成”，非总PASS；静态diff检查通过。
- 证据完整性：861点max1.590706米原采样源码/输出未落盘，只在开发交接中标注为当时会话观测转录，不能当可重放原始JSON。正式图采集前当时Shader无错/Console0，不代表连接断开后的当前Console；最新磁盘日志已有用户导出时导航非空异常及openupm连接错误，未将其扩为本轮无关修复。
## 原生网格显示续接检查

- 本次仅追加Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md及本日志，记录连接复核结果；没有修改客户端、场景或导航数据。
- Unity进程仍在，配置的8082无监听且当前没有Unity MCP工具，无法代为激活MainCityNavigation。此前inactive证据仍是已有定位，未取得新的实时对象状态或恢复后实图。
- 后续先恢复连接，再隐藏自绘、激活指定对象及必要组件、开启原生显示并核图；不保存场景。原生恢复与新增资源清理验收仍未完成，详见Docs/AI/CURRENT_TASK.md。

