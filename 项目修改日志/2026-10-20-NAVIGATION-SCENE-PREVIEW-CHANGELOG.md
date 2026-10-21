# 公共导航文件的场景网格预览

- 日期：2026-10-20。状态：代码、原生数据/生命周期验证及独立审查通过，正式窗口已打开并启用预览；最终合成画面未独立目检。
- 用户要求：直接在Scene视图看导出网格，核对当前是否仍依赖Unity导航插件。
- 范围：现有导出窗口加入读取公共文件的Scene预览、显隐和定位；最小新增同名Editor预览类型及meta。预览不向导航世界注册数据，不改烘焙、数据布局、服务端或移动。
- 依赖说明：当前地图烘焙仍使用Unity AI Navigation；自写部分是公共格式、导出、两端读取和本轮文件预览。纯.NET读取不依赖Unity插件；本轮没有自研烘焙器或完整路径查询。
- 分工：navigation_scene_preview负责实现与原生验证，navigation_preview_review独立审查，主协调验收、更新入口和说明。
- 使用说明项目分析/UNITY_NAVIGATION_EXPORT.md；证据_temp/navigation-preview-20261020。

## 修改文件

- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationExportWindow.cs`：显示/重新读取、隐藏、定位按钮；选择另一地图时清旧预览；窗口关闭清理；纵向滚动保证原有矮窗口可操作；说明烘焙依赖。
- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs`及`.meta`：从公共文件缓存世界顶点、32位线段端点与包围盒；Scene原生Handles线框；隐藏/关闭/域重载清数组及监听，不创建场景对象。
- `项目分析/UNITY_NAVIGATION_EXPORT.md`、`项目分析/README.md`、`项目分析/项目难点.md`：使用步骤、依赖边界、本轮渲染路径取舍及真实验证。
- `Docs/AI/PROJECT_CONTEXT.md`、`Docs/AI/CURRENT_TASK.md`、`Docs/AI/DECISIONS.md`、`Docs/AI/TEST_REPORT.md`及本日志：协作状态、验收与交接。

## 实现与取舍

文件预览只需要SceneAsset对应的已导出文件，不要求选NavMeshData或清空导航世界；原导出流程仍维持空导航世界限制。缓存每面AB/BC/CA三个边的成对索引，重绘只执行一次`Handles.DrawLines`，颜色及深度状态在finally恢复。通过蓝色穿透线框查看覆盖；不代表角色可穿墙或连通性已验证。

首稿曾采用Mesh/材质绘制透明面加GPU线框，camera.targetTexture只取得中间缓冲，不能证明最终Scene合成。为了保持最短显示流程，改为原生Handles线框，删除Mesh/Material及着色器依赖；没有为截图扩展生产工具。地图烘焙仍使用Unity AI Navigation，自写的是导出/格式/读取和查看，不声称实现独立烘焙或服务端A*。

## 验证结果

- Unity末稿实际编译/域重载通过，独立从Editor.log核Csc、ILPostProcess、CopyFiles成功；末次Console错误0。
- `native-final.json`原始响应：270735顶点XYZ、743862边端点逐项与文件一致，对应123977三角；32位int索引，重复显示Scene监听仅1个，隐藏和窗口关闭清缓存及回调通过。首稿Mesh专项不冒充末稿结果。
- 最后重新打开正式窗口并启用文件预览：MainCity/1场景/非Play/dirty=true/全局导航0，visibleAtDelivery=true。dirty=true是本轮已有用户状态，没有保存、切图、启动Play或修改正式相机。
- 开发1307保护输入与独立423输入核对，仅授权窗口源码改变；新增Preview.cs/meta。原NavMesh、场景、公共格式、Common工程、Plugins及Packages保持。
- 用户在本轮开始前自行重导了公共文件，当前SHA为`4659a2c35d5fdbbdd0dbb084321328408f560d5fd239b084b3dfbc7cd3984b84`，本轮预览保持不变。首次工具验收61D6的SHA作为历史值保留，不将两次差异归因于预览。
- 14项源码人工复核及禁用语法扫描通过，独立结论PASS；主复核两源和原始证据，正式3路径SHA与交付一致。汇总`_temp/navigation-preview-20261020/root-final-validation.json`，独立`review/final-review.json`。

## 失败尝试及边界

- 新文件尚未入AssetDatabase时scripts-only请求出现CS0246，正常完整刷新导入后编译通过；未为导入时序添加业务防御层。
- 临时窗口激活探针调用不存在API，只是探针编译失败，未进入正式代码。桌面像素图`scene-local.png`、`scene-hidden.png`受其他应用遮挡无效；`scene-overview.png`是首稿相机缓冲。这些都不作末稿视觉通过证据，保留_temp不删除。
- 未独立确认末稿最终合成截图或地形逐处对齐，用户可直接在主城Scene查看已启用的蓝色网格。没有FPS测量、烘焙范围调整、服务器寻路、联机移动或打包发布。
- 当前无待修代码阻断。后续若要求独立烘焙或完整服务器导航，需要另行实现；本轮只完成用户要求的文件可视化入口。难点记录见`项目分析/项目难点.md`导航案例补充。
