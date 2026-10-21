# 主城低帧率：2026-10-20运行采样

## 结论

本轮已找到一个明确的编辑器负载：Play时Scene视图和Game视图同时重绘。基准段299个剔除诊断尖峰后的主线程帧中，SceneView.Paint平均7.949毫秒、GameView.Paint平均7.992毫秒，主线程总帧平均19.927毫秒。Update脚本阶段（Update.ScriptRunBehaviourUpdate）平均0.407毫秒，不含LateUpdate和FixedUpdate；Global.Update平均0.117毫秒，蒙皮更新平均0.187毫秒、Animator更新平均0.328毫秒。本轮样本不支持将这些已测更新节点判为主要瓶颈，不能将0.407毫秒称为所有业务脚本总耗时。

暂时最大化已有Game视图后，第二段300帧SceneView.Paint为0，主线程总帧平均11.288毫秒、中位9.836毫秒；GameView.Paint平均6.452毫秒。窗口随后恢复为原非最大化状态，Profiler所有开关恢复原值，主城dirty为false。

**镜头在调查期间发生移动，不能将两段差值写成严格同镜头优化收益，也不将CPU帧时间倒数当成正式Player稳定FPS。** 已确认的是Scene视图绘制开销在单Game视图时消失，编辑器重复绘制是当前值得先处理的一项成本。原用户截图36.6FPS/27.3毫秒与本轮采样不在同一时刻，不能把本轮20毫秒说成截图的完全还原。

## 实时环境与渲染归属

- Unity2023.2.20f1c1、Scene_MainCity、Play中/非暂停/非编译；Ultra、vSync=1、targetFrameRate=-1，使用RTX4070 Laptop而非Intel核显。
- 初次原生读取559 Batches、306 SetPass、941986三角形，与截图量级一致。Main Camera远裁剪5000、1920×1080；另有UICamera，不是小地图再次渲染整城。
- 当前54个活动SkinnedMeshRenderer：Role_MJS(Clone)/CALISTA28、INQUISITOR23、Demon3。后一次快照任意相机可见分别28/16/2，共46，原图47是另一时刻。不是47个角色；isVisible也不是严格的主相机遮挡后可见。
- 三组所有活动部件的网格三角形合计分别72289、71446、7428；是模型组件总量，不等于本帧提交量。Animator均CullUpdateTransforms，蒙皮updateWhenOffscreen均false；不能以屏外始终更新推断本轮成本。
- 2058个活动启用Renderer，1932已经进入静态批处理、675个LODGroup、66个共享材质实例。不能据Stats的Saved by batching=26认定项目未合批，也不应默认全场再建LOD。常用材质Instancing关闭，但静态批处理与Instancing需按实际绘制链评估，不能只勾开关就宣称收益。
- 后次快照启用灯为一烘焙方向光与一无阴影实时点光。此前关闭主城54点光为历史场景结果，当前动态场景不能照搬“活动实时灯绝对为0”。截图Shadow casters=0也不等于没有任何灯。

## 采样方法与限制

用户重新开启MCP后，标准本机HTTP端点8082可用。当前会话未直接暴露工具，通过标准MCP initialize/resources/read/tools/call连接同一已启动服务，不启动新Unity、不更改服务器配置。先读custom-tools及editor/state，反射确认ProfilerDriver/RawFrameDataView API，再录真实Profiler原始数据到_temp。

基准段原始文件live-cpu.raw；载入后有300帧。按MCP ProcessQueue耗时达到1毫秒排除一帧，剩299帧；该诊断帧包含工具执行造成的约814毫秒尖峰，不能当成游戏稳态。第二段game-only-cpu.raw为300帧，无同阈值诊断尖峰。完整按帧表及阈值在performance-summary.json；采样节点为主线程inclusive时间，不能把父子节点相加。代表帧树显示PlayerLoop约2.96毫秒，SceneView.Paint8.34、GameView.Paint7.97，重复绘制在同一主线程树上有直接证据。

Profiler开启、二进制记录和编辑器自身均有开销。单次FrameTiming可用：GPU6.094毫秒、CPU frame16.163毫秒、main5.042毫秒，是另一瞬时样本，不能与CPU原始帧直接相加或代替GPU持续采样。不是正式独立客户端性能验收。独立复核逐列重算两段全部均值和中位数、核对状态恢复与帧树，26项通过；记录见`项目修改日志/2026-10-20-FPS-CPU-INDEPENDENT-REVIEW.md`。

首次三个资源并发请求复用JSON-RPC id=1，引起返回错配与超时；诊断客户端改用每请求唯一id，后续串行访问Unity，重新读取成功。首次重定向Python stdout按系统编码导致JSON读取失败，使用UTF8后重取。只影响临时取证，未为此修改业务或资源。

## 建议顺序

1. 编辑器测试时先只显示Game视图，使用该窗口的Maximize/Play Maximized，避免Scene视图同时持续重绘；本轮测试后已恢复用户布局，未保留此设置。
2. 保持相同相机、分辨率和画质，对独立客户端采Profiler，再决定游戏本体瓶颈。当前不能以Editor截图保证正式Player达到多少帧。
3. 本轮无需为FPS先改移动、技能或日志框架。角色部件多和场景材质提交仍可作为后续渲染优化候选，须用独立Player的Frame Debugger/GPU采样排出贡献；已有静态批处理和LOD保持，逐项验证。

现场未删除/关闭模型、未改灯光/质量/视距/资源/业务、未停止Play、未保存场景、未发布。根日志：`项目修改日志/2026-10-20-FPS-INVESTIGATION-CHANGELOG.md`；历史主城结论：`项目分析/MAIN_CITY_PERFORMANCE.md`。证据目录：`_temp/fps-investigation-20261020/`，两原始Profiler约911MB保留，不擅自清理。
