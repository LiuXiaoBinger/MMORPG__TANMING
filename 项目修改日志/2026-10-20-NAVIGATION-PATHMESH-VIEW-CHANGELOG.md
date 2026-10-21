# 寻路网格观察需求纠正

## 行走网格可读性再次返工（2026-10-20，已完成）

- 用户提供be57df原生参考图，要求道路可走蓝面和树石柱障碍边界清楚，明确上版自绘杂乱不可读。前轮只确认面线可见，验收场景偏空地，不能据此判符合目标。
- 当前范围为Editor自绘必要修复，先同镜头对照遮挡/深度/边线；不改公共文件、地图、烘焙或移动，不以原生显示替代。证据_temp/navigation-pathmesh-view-20261020/walkable-readable，最终修改、设计和验收待补。
- 分析与复盘：项目分析/UNITY_NAVIGATION_EXPORT.md、项目分析/项目难点.md；协作入口CURRENT_TASK/TEST_REPORT记录当前进展。
- 排查实证：native-reference.png、self-before.png、self-visible-depth.png为同镜头含柱/树/石三态。上版Greater透线穿过柱身，透明面_ZWrite0又使多层导航互相叠加；删除Greater并让蓝面写深度后，后方杂线消失，道路蓝面连续，树根石边不可走洞口仍清楚。此前漏面不能据为永久透画理由，正常Scene CommandBuffer阶段实测可正确处理深度。
- 当前方案：只绘制通过场景与导航面深度测试的蓝面/细线；精确XYZ规范化仅用于绘制边统计，不做容差焊接或改变楼层高度；共享边一次绘制、内部淡显，单侧外轮廓深显。新增Editor专用线Shader只在视空间向相机偏移0.02米消除共面遮线，仍ZTest LEqual。末稿结果待补。
- 本轮新增的性能故障：初版使用排序端点拼接的UInt64作Dictionary边键，首次大图Show长时间占用Editor。离线只读复现250892唯一边只有14001种UInt64哈希，最大同hash链36200；相邻索引高低位异或聚集。已将绘制边键最小替换为Vector2Int排序端点，等待旧同步调用自然结束后实际验证，不强杀未保存Editor。位置规范化另核130227唯一XYZ/129332哈希/最大碰撞4，未为假设扩大位置实现。统计与脚本在walkable-readable/edge-hash-analysis.json、check-edge-hash.py，不能将离线分布当作正式Unity耗时结果。
- 最终修改：MNavigationMapPreview.cs和MNavigationExportWindow.cs两既有Editor源，新增MNavigationPreviewLine.shader及meta共四路径，asmdef保持。去Greater/蓝面写深度、内部边/轮廓边去重及轻重分色、共面细线显示偏移，窗口同步真实显示说明；烘焙/公共格式/原导航数据/移动/服务器未改。
- 实际验证：旧调用自然恢复后Vector2Int源码已编译加载，正式末次Show406.1055毫秒、四次共1765.0351毫秒；不是运行FPS收益。XYZ及原面索引逐项一致、UInt32，内部121039/轮廓129853边与原始几何无向边集完全相符、无重复/跨子网格重复。重复Show单指令、Hide/Close资源销毁与原生false/true初值恢复通过，ShaderHasError false，Console错误警告0。
- 实图与边界：首轮含柱树石末稿已主/独立实际目检，之后用户移动镜头造成self-final覆盖，如实记录不伪造旧图；首轮native-reference/self-before/self-visible-depth仍保留完整问题链。最终self-current/hidden-current冻结，当前柱/树不被后层线穿透，道路青蓝和障碍轮廓可辨；native-current实际无原生蓝面，明确不作成功对照，不称最终逐像素一致。
- 最终保护及交接：427冻结425保持，仅两授权源改变及新shader/meta，公共文件SHA4659…3984b84保持。主城非Play/dirty原true，自绘true/原生false，窗口场景/烘焙资产恢复，保用户新镜头(79.11,0.65,31.44)/size24.117。未保存/重烘焙/启停Play或发布。仍不计算角色可达连通域，原文件树冠石顶面未删除；此为后续导航采集质量边界。
- 项目分析/UNITY_NAVIGATION_EXPORT.md、项目分析/README.md、项目分析/项目难点.md及四协作入口同步本轮结论；正式证据在walkable-readable/证据说明.md、final-check-result.json、line-set-verification.json、final-console.json、final-state.json、root-final-validation.json及review。

## 本轮纠正：必须自绘（2026-10-20，已完成）

- 用户指出未显示后进一步明确：“为什么显示NavMeshSurface，我让你自己照着画”。直接切换原生显示是需求理解偏差；此前技术通过不表示交付符合需求。
- 修改范围：MNavigationMapPreview.cs、MNavigationExportWindow.cs及移除多余引用所需asmdef；反读公共文件自绘透明可走面和清晰边线，参照原生外观，不改数据或烘焙。
- 开发独占MCP验证实际Scene，主与独立目检通过。证据：_temp/navigation-pathmesh-view-20261020/self-draw。专题：项目分析/UNITY_NAVIGATION_EXPORT.md；多轮问题复盘：项目分析/项目难点.md。
- 修改文件：LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs、MNavigationExportWindow.cs、LYF.Navigation.Editor.asmdef。前两者从原生Surface选择/开关入口改为Serializer.Load反读文件、UInt32面/线Mesh及当前Scene相机CommandBuffer自绘；asmdef移除已不需要的Unity.AI.Navigation引用。无新增Shader或正式场景对象。
- 设计：透明浅蓝可见面、淡遮挡面、深蓝可见线及淡遮挡线分层，兼顾地形可读性与原文件范围；材质深度偏移缓解共面闪烁，不改XYZ。Show保持当前近景，仅暂停原生防叠加；Hide/Close/重载解绑并释放指令、两Mesh及四材质，再恢复原生。窗口预览只需地图场景，导出仍需烘焙资产，保原导出流程。
- 验证：270735顶点XYZ/123977面索引/743862边端点逐项true，两个Mesh实际UInt32；重复Show仅一自绘buffer、旧资源销毁，原生false/true恢复、Hide/Close清理、无NavMeshData预览通过。真实脚本编译及域重载后旧Mesh0/旧指令0，原生恢复true；最终Console错误警告0。
- 实际画面：CaptureSceneView抓真实Scene GUI，scene-visible.png/scene-hidden.png同近景显隐，主与独立目检浅蓝透明面/深线及地表纹理。首m_SceneTargetTexture纯黑轴图是Gizmo中间层，明确无效；较早大采集图多余黑边保留，最终按真实899×377窗口直接抓取，未强制原生补画。
- 保护：427冻结424保持，仅上述三授权文件变化，公共文件SHA4659a2c35d5fdbbdd0dbb084321328408f560d5fd239b084b3dfbc7cd3984b84保持；地图/导航资产/插件/既有移动与协议未改。CURRENT_TASK、TEST_REPORT、PROJECT_CONTEXT、DECISIONS及专题/README/项目难点同步纠正本轮结论。
- 交付：主城非Play/dirty原true，pivot(54.21,14.53,77.21)/size10保持，原窗口场景与烘焙资产字段恢复，自绘已开、原生暂false。未解决边界：原烘焙含树冠/石顶面仍如实显示，未新增角色连通域或完整服务器寻路；未打包发布。

- 日期：2026-10-20；状态：已完成原生Surface显示接入与同镜头验收。
- 用户要求：能观察寻路网格，当前全图蓝面妨碍查看。前轮数据/覆盖一致性不等于完成用户观察目的。
- 首先确认：只显示角色所在可达连通区域，或保留全图但清晰显示网格线与边界；答复前不实施依赖该选择的代码。
- 分工：navigation_walkable_preview负责客户端只读现场，navigation_preview_review冻结与独立审查，主协调需求和最终验收。
- 修改文件与最终验证见下文；确认阶段仅Docs/AI入口、本日志及项目难点记录，用户明确参考后才实施。
- 保护：不重烘焙、不覆盖公共地图文件、不保存用户场景或启停Play、不扩展服务器寻路或移动业务。
- 参考分析：项目分析/UNITY_NAVIGATION_EXPORT.md；多轮误判与真实难点同步项目分析/项目难点.md。证据_temp/navigation-pathmesh-view-20261020。

## 实现前只读结果与交接（历史阶段）

- 当前主城非Play/dirty=true，用户已隐藏文件预览，原生showNavMesh=true、123977面；没有实际运行LocalRole，当前选择Surface原点，不能据它自动计算角色连通范围。失活MJS仅是可供用户后续明确选择的观察起点，活动怪物不冒充玩家。
- 当前实现全图所有三角形Always填充，确实无网格线、边界或连通筛选。显示全文件不等于能直观观察寻路；应先确定需要的语义，避免再次做偏。
- 独立427路径和两源字节快照已冻结，开发无源码/场景/镜头/开关修改。正式仅四份文档（CURRENT_TASK、TEST_REPORT、项目难点及本日志）记录本轮现状；纯文档限定差分检查，不重复业务编译。
- 用户问题已异步发送，当前等待明确选择；未以超时当作用户同意，也未实施任一依赖分支。本轮无显示修复交付，后续必须按选择补真实Scene视觉验收。

## 用户明确参考及实施范围

- 用户答复“你参考NavMeshSurface这个组件”，无需继续二选一。开发与审查均已核AI Navigation2.0.6源码：SurfaceEditor标记所选实例，真正网格由原生导航可视化绘制；Overlay支持仅选中Surface显示。不是组件自动做角色当前位置连通查询。
- 最短方案为直接复用原生：窗口选择与原_navMeshData匹配的已加载Surface并打开原生显示，Preview只管理showNavMesh/showOnlySelectedSurfaces及恢复，移除自绘网格和蓝面。显示来源明确是Surface烘焙资产，不冒充公共文件图像；导出和格式不动。
- 必要范围为两Editor源及LYF.Navigation.Editor.asmdef追加现有Unity.AI.Navigation引用；不激活/注册额外Surface、不重烘焙、不做BFS/A*。验收参考Surface原生和通过窗口打开同镜头图、网格边界/障碍缺口可辨认及两个原生开关重复显示/结束/关闭/重载恢复。
- 结束按钮使用“结束预览 / 恢复原设置”，因为用户原本开着原生时结束后仍应保持原生开启；不能把恢复误写成强制隐藏所有网格。修改来源时清除旧Surface查看状态。

## 最终修改文件与设计

- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs`：移除自绘Mesh/Material/onPostRender；复用选中Surface的原生显示，保存/恢复两项显示设置。FrameSelected对空Surface只定位原点已实测，按包SurfaceEditor同TRS无缩放变换sourceBounds再定位，不改导航顶点或地图相机。
- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationExportWindow.cs`：场景名+导航资产匹配活动Surface并选择；任一来源变化结束旧预览；按钮和说明清晰标明Surface原生来源，结束恢复原设置。导出方法/路径/格式保持，不拿原资产画面冒充bytes读回。
- `LYF_MMO/Assets/GM_Game/Editor/Navigation/LYF.Navigation.Editor.asmdef`：引用现有Unity.AI.Navigation，为直接强类型使用组件提供必要编译接入；未安装或升级包。
- `项目分析/UNITY_NAVIGATION_EXPORT.md`、`项目分析/README.md`：当前使用方式、画面/文件来源、依赖及边界更新。
- `项目分析/项目难点.md`：追加语义误判、直接参考组件、复用取舍及真实验证，不把像素一致或字段相等写成角色可达性正确。
- `Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md`及本日志：交付、验证和交接记录。

## 验证及交接

- 真实Scene相机同镜头对比直接选Surface与窗口正式ShowSelectedSurface入口，两图1350×525全部通道差≤2、平均差0.1652，主与独立审查已目检网格线和障碍边界一致。源码与原始JSON见surface-window-compare.cs/result、两图surface-compare-0/1.png和surface-image-comparison.json。
- 两原生开关四组合的Show/重复Show/Hide12项全true，关闭恢复true，旧相机回调0；定位中心与sourceBounds一致，测试后还原当时镜头。编译及末Console0条。开发另记录真实域重载恢复后重新Show，独立报告将其与已核源码/专项分开说明。
- 独立427冻结路径424保持，只有三授权文件变化；开发13原资产/场景/bytes/包保护输入保持。独立review/final-review.json为PASS，主最终SHA与保护核对见root-final-validation.json，均在本轮_temp目录。
- 交付主城非Play/dirty原true/123977面、选择MainCityNavigation、原生与仅所选true、旧相机回调0。用户期间移动镜头，末pivot(-19.11,-20.02,111.21)/size60；没有把它强行恢复成之前参考图镜头。没有保存场景、重烘焙或覆盖导航文件。
- 当前采集RenderMeshes/layer7包含部分Elven_Tree和Rock渲染面，样例里树和部分石头自身无Collider；当前原生参考仍含树冠/石顶。未改采集与烘焙、不增加BFS或完整A*，不宣称实际角色寻路正确。若下一步要调整实际可走范围，应核采集来源和玩法，再烘焙/导出；显示层不隐去这些真实数据。
- 用户现在可从菜单窗口点击“显示NavMeshSurface寻路网格”，当前入口已开启。本轮无显示接入代码阻断，公共文件导出及服务端读取功能保持原范围。
