# 导航预览改为可行走面

- 日期：2026-10-20。状态：代码及原生专项完成，独立审查通过。
- 背景：用户指出前轮穿透线框与Unity导航可行走区域显示差别明显，无法直观判断角色能走的位置。
- 目标：公共文件的蓝色可走面填充，按场景深度遮挡；核对数据来自哪份烘焙/Agent。沿现有Editor窗口显隐/定位，不自行扩展连通查询、角色移动约束或独立烘焙器。
- 分工：navigation_walkable_preview开发与原生验证，navigation_preview_review独立核查，主协调及文档。
- 保护：不修改原场景/NavMesh/公共文件/包/服务端；仅Editor/Navigation必要预览代码及显示资源。中间证据_temp/navigation-walkable-preview-20261020。
- 使用说明：`项目分析/UNITY_NAVIGATION_EXPORT.md`；真实渲染排查及取舍补入`项目分析/项目难点.md`。

## 修改文件与设计

- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs`：反读同一公共文件，缓存世界XYZ及UInt32 Mesh；使用现成Internal-Colored临时材质透明填充，在Scene相机onPostRender沿用深度遮挡，仅Scene绘制；重新显示、隐藏、关闭及重载释放资源与回调。不再用穿透线框表示可走面。
- `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationExportWindow.cs`：显示文件AgentTypeID，说明烘焙可走面与当前位置可达范围的区别；保留显隐、定位和滚动，不增新导航功能。
- `项目分析/UNITY_NAVIGATION_EXPORT.md`、`项目分析/README.md`：替换过时线框使用说明，补深度填充、依赖和规格边界。
- `项目分析/项目难点.md`：追加用户反馈、数据一致性证据、深度验证及角色规格差异，保留前轮历史取舍。
- `Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md`：更新当前协作事实与验收交接；本日志归档本轮修改。

## 验证结果

- Unity原生编译/域重载通过，开发报告Console0错误警告。主城公共文件与原NavMeshData三数组逐项一致：270735顶点/123977三角形；预览Mesh世界坐标/索引及UInt32通过。
- 生产相机回调在专用PreviewScene经Camera.Render真实执行；前方遮挡、后方可见和Game排除三项通过。独立PNG像素核到64×64实心填充，确认透明混合有效。重复Show旧资源释放且回调1个，Hide回调0，Close清理通过。
- 独立冻结426路径，424保持，仅两授权源变化；开发13保护输入保持。原公共文件SHA256=`4659a2c35d5fdbbdd0dbb084321328408f560d5fd239b084b3dfbc7cd3984b84`保持。主最终证据`_temp/navigation-walkable-preview-20261020/root-final-validation.json`，独立审查`review/final-review.json`为PASS。
- 原资产实际Agent0规格：半径0.29米、高2.1米、坡度45°、爬高0.75米。MJS半径/高相符但台阶0.3米不同；没有活动玩家现场确认，不能声称完全匹配当前角色。公共文件只存AgentTypeID，参数来自原资产调查。

## 边界与交接

- 正式窗口已经打开主城填充预览；非Play、dirty=true保持本轮原值、全局导航0。未保存场景、启停Play、重烘焙/覆盖公共文件，未改服务端、协议、移动或插件。
- 主城最终合成截图未独立目检：截图工具拒绝向Unity工程外的项目根_temp输出，未绕到其他目录创建文件。隔离深度图不冒充主城最终效果。
- 本轮无代码阻断；用户直接切Scene查看。当前显示指定Agent烘焙面，不是从角色当前位置查询的连通范围，未接入实际移动、动态障碍或完整服务器寻路，制作仍使用Unity烘焙。没有测得或宣称FPS收益。
