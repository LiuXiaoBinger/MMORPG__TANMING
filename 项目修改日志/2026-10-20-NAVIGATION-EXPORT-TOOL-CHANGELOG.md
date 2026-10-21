# 自有地图导航网格导出工具

- 日期：2026-10-20。用户要求选择简单可控制的方案，自写地图数据导出工具，主城及后续多个副本复用，不必照搬RO。
- 设计：直接导出Unity已烘焙NavMesh的世界坐标三角网，保存场景名、AgentTypeID、顶点XYZ、三角形索引和区域；纯C#序列化格式让Unity和.NET Framework4.8服务端使用同份正式数据。无需RONavigation和外部运行库，不重复烘焙。
- 范围：当前地图导出/换地图复用、共用类型和读写、实际主城产物及双端读回验证；不扩大到完整寻路、邻接重建、AI、玩家移动、协议或构建发布。
- 分工：navigation_export_client负责Editor工具及原生导出；navigation_shared_data负责公共类型/格式及服务端源码链接；navigation_export_review独立审查、编译、保真/读取验收。主负责协调、验收、协作记录和专题说明。
- 状态：实现与验收完成。菜单“工具/导航/导出公共地图网格”，Boot或空导航世界选择SceneAsset及配套NavMeshData；每张地图一个文件，同名重新导出覆盖。场景仅负责标识，世界坐标使用资产保存位姿。

## 修改文件

代码、配置和正式产物共16路径：

- `LYF_MMO/Assets/GM_Game/Editor/Navigation.meta`。
- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationExportWindow.cs`及`.meta`。
- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapExporter.cs`及`.meta`。
- `LYF_MMO/Assets/GM_Game/Editor/Navigation/LYF.Navigation.Editor.asmdef`及`.meta`。
- `LYF_MMO/Assets/GM_Game/Scripts/Navigation.meta`。
- `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationPoint.cs`、`MNavigationMapData.cs`、`MNavigationMapSerializer.cs`及各自`.meta`。
- `LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj`，链接同份纯C#源码。
- `NavigationData/Scene_MainCity.navmesh.bytes`，真实主城导出，不是测试地图。

文档：`项目分析/UNITY_NAVIGATION_EXPORT.md`、`项目分析/RO_PATHFINDING_ANALYSIS.md`当前方向说明、`项目分析/README.md`、`项目分析/项目难点.md`、`Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md`（后3均同Docs/AI目录），本日志及`项目修改日志/2026-10-20-NAVIGATION-EXPORT-INDEPENDENT-REVIEW.md`。完整16生产路径及SHA见主验收清单。

## 实现与设计取舍

- Unity三角化接口是全局查询，因此在无现存网格时临时注册所选资产，生产函数`finally`只移除自己创建的实例。避免混图，不自动卸载/恢复用户网格，不切换或保存场景。
- 文件标记LYFN/版本1，存场景名、AgentTypeID、顶点XYZ、三角形索引、逐面区域。保留高度、重复顶点和绕序；Unity已烘焙可走边界，不二次缩边。
- `NavMeshData.agentTypeID`首轮原生编译不可用，最小改为Editor的SerializedObject读取实际烘焙设置；不添加手工Agent选择造成来源误配。
- 两端直接链接同源三类型，服务端不引用UnityEngine，目标仍为.NET Framework4.8；没有替换客户端已有Common插件或正式运行DLL。

## 实际验收

- 真实Unity编译/域重载完成，Editor程序集可加载，菜单窗口创建关闭通过。主城导出后共享Load逐XYZ/索引/区域完全一致，5232487字节、270735顶点/123977面、Agent0。
- 主城SHA256为`61D6AB8FE38AB80DD59FCBC425E652AC637FB7DE30D45CD6BC4987853F3CBB4C`；独立纯.NET4.8读取并重写逐字节一致。两方Common实际Rebuild0错误、4既有警告；共享专项220断言、独立末稿107断言通过。
- 两张Unity临时地图各32点16面，非零平移/yaw37和-21、双层高度、已有网格拒绝且保留、真实写入失败清理共10项通过。独立.NET再读两临时文件逐字节一致。测试文件不进入正式NavigationData或场景。
- 1108冻结输入仅授权Common.csproj改变；主城场景/NavMesh资产/meta、Boot、包清单、导航参数六项SHA保持。主复核生产16路径与证据一致，证据统一`_temp/navigation-export-20261020`，主汇总`root-final-validation.json`。
- 最终Boot非Play/dirty=false/全局导航0三角。主城Surface及父根与资产位姿相符，不主动启停Play/保存用户场景。

## 失败尝试、边界与交接

- 首隔离夹具误要求不同注册实例的索引排序稳定，改为世界几何集合/高度/数量核对；生产代码没有迎合测试。主城单次导出与读回仍逐元素完全一致。
- 一次只读场景探针误用`LoadAllAssetsAtPath(.unity)`，Console遗留`Do not use ReadObjectThreaded on scene objects`；无场景保存/修改，不是生产导出故障，未清Console，不能写最终全Console0。
- 本轮无阻断待修问题。未实现三角邻接/分块接缝、A*、离网连接、动态障碍、客户端路径执行或服务器运行加载/发布。后续服务器导航在本公共数据上接查询，再衔接移动；不能宣称本轮完成服务端权威寻路。
- 未来把含新类型的服务端Common.dll更新到Unity前，须确定共享类型唯一来源；当前Unity两个旧Common插件没有新增类型，本轮未出现重复定义。
- 使用说明及文件布局：`项目分析/UNITY_NAVIGATION_EXPORT.md`；跨模块难点：`项目分析/项目难点.md`本轮条目。独立审查报告引用见上。
