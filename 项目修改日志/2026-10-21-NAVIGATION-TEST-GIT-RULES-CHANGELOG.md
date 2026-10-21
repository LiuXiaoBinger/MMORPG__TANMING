# 2026-10-21 点地测试恢复、NPC初始化与功能分支交付

## 用户要求与实际实现

确认当前为三角网格A*加漏斗/门户路点；检查主城没有NPC，并恢复可用点地测试。新增长期规则：每次代码交付列文件、用途和具体变化并写日志，验证后提交推送功能分支；仅用户明确要求才合主分支。

本次仅MainCityMgr.Start补原NpcManager.Initialize；新增NavigationClickTest.cs/meta、NavigationClickTestMenu.cs/meta、Test.meta共5路径。NPC真实配置只有4001 QuQu，原manager已挂启用但正式入口未初始化；原唯一调用在停用测试对象。正式TouchObject、查询算法、地图、场景和Prefab本次不修改。测试菜单进入Play后手动启用，使用DontSave临时宿主；退出Play明确移除。没有新增Mock、重试/队列或另一套移动框架。

开发navigation_click_test_dev、NPC检查与修正navigation_npc_inspect、独立navigation_test_review完成；主Agent协调、验收、文档和Git，不修改业务源码。

## 规则、说明和文件范围

AGENTS.md、AIconfig/config.toml、AIconfig/workflow.md及本目录README加入规则。完整累计导航文件用途、算法说明、测试步骤见`项目分析/导航代码文件清单与测试说明.md`。协作四入口和导航实现专题/索引同步本轮状态。

用户要求上传时发现运行导航及制作工具此前尚未提交，所以本次Git范围包含累计导航格式/查询/工具/运行接入、主城公共地图及随包副本；并非声称这些历史文件全是本轮新开发。上传AI Navigation包依赖用于制作工具，不上传当前场景/烘焙设置或17MB烘焙资产，避免混入用户场景状态。主城实际运行直接使用公共数据。

以下为本次Git交付的完整逐文件清单（含此前未提交的导航，A/M对应相对HEAD状态）。其他UI/跳跃同步/协议/场景/正式DLL/服务端目录整理的已有改动均留工作区；MMove及MainCity只暂存导航相关hunk，没有覆盖用户内容。

| 操作 | 文件 | 用途与具体变化 |
| --- | --- | --- |
| 修改 | `AGENTS.md` | 持久交付文件清单、功能分支、中文提交与推送约束 |
| 修改 | `AIconfig/config.toml` | 同步项目级Git/逐文件交付策略 |
| 修改 | `AIconfig/workflow.md` | 主Agent提交推送职责与验收规则 |
| 修改 | `Docs/AI/CURRENT_TASK.md` | 本轮分工、范围、验收和交接 |
| 修改 | `Docs/AI/DECISIONS.md` | 正式/测试入口与Git规则取舍 |
| 修改 | `Docs/AI/PROJECT_CONTEXT.md` | 当前能力、分支约束和验证边界 |
| 修改 | `Docs/AI/TEST_REPORT.md` | 双侧工作区编译、独立审查与暂存快照结果 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/LYF.Navigation.Editor.asmdef` | 累计Editor导航工具程序集 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/LYF.Navigation.Editor.asmdef.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationExportWindow.cs` | 累计导出/自绘预览工具窗口 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationExportWindow.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapExporter.cs` | 累计已烘焙资产导出 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapExporter.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs` | 累计公共数据Scene自绘及资源恢复 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationPreviewLine.shader` | 累计预览面线与有限地形深度处理 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationPreviewLine.shader.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/Test.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/Test/NavigationClickTestMenu.cs` | 本次新增Play启停菜单、DontSave宿主、退出清理 |
| 新增 | `LYF_MMO/Assets/GM_Game/Editor/Navigation/Test/NavigationClickTestMenu.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs` | 本次补正式NPC初始化；仅暂存Start导航相关hunk，保留工作区跳跃同步改动 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/Manger/SceneMgr.cs` | 场景成功加载后读取同名公共导航地图 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigation.cs` | 累计邻接/接缝/A*/门户计价/漏斗路点 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigation.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationEdge.cs` | 累计边几何及所属面 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationEdge.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationMapData.cs` | 累计公共三角网结构 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationMapData.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationMapSerializer.cs` | 累计公共格式二进制读写 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationMapSerializer.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationMgr.cs` | 累计Unity/公共查询适配与Geometry射线 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationMgr.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationPoint.cs` | 累计纯C#世界XYZ数据 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationPoint.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationPointComparer.cs` | 累计相同XYZ比较 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationPointComparer.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationPortal.cs` | 累计相邻面门户 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationPortal.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationTriangle.cs` | 累计面几何/邻接；返工只补Center注释 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationTriangle.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs` | 累计导航走跑动画接入 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs` | 累计导航移动提交/方向中断/SetMoving访问；仅暂存导航hunk，其他跳跃同步留工作区 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MNavigationComponent.cs` | 累计目标/路线/实际路点消费/转角/到达NPC对话与中断 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/MNavigationEventArgs.cs` | 累计位置/目标/范围/动作的强类型事件 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/UI/test/NavigationClickTest.cs` | 本次新增仅Editor点地测试，UI/实体/拖动排除，复用正式事件 |
| 新增 | `LYF_MMO/Assets/GM_Game/Scripts/UI/test/NavigationClickTest.cs.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/TouchObject.cs` | 累计NPC点击导航、保选择规则，正式点地不派发 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/controller/RoleController/NpcCtrl.cs` | 累计共用原三维10米对话距离 |
| 新增 | `LYF_MMO/Assets/StreamingAssets/NavigationData.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 新增 | `LYF_MMO/Assets/StreamingAssets/NavigationData/Scene_MainCity.navmesh.bytes` | 主城正式公共地图随包同份副本 |
| 新增 | `LYF_MMO/Assets/StreamingAssets/NavigationData/Scene_MainCity.navmesh.bytes.meta` | Unity脚本/资源或目录GUID标识，与同名文件用途对应 |
| 修改 | `LYF_MMO/Packages/manifest.json` | 导航制作依赖AI Navigation 2.0.6 |
| 修改 | `LYF_MMO/Packages/packages-lock.json` | 对应导航包依赖锁定 |
| 修改 | `LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj` | 累计链接8个同源纯C#格式/查询文件 |
| 新增 | `NavigationData/Scene_MainCity.navmesh.bytes` | 主城正式公共地图源数据 |
| 新增 | `项目修改日志/2026-10-13-RO-REF-MNAVIGATION-COMMENT-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-NAVIGATION-EXPORT-INDEPENDENT-REVIEW.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-NAVIGATION-EXPORT-TOOL-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-NAVIGATION-PATHMESH-VIEW-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-NAVIGATION-PREVIEW-SCENE-FIX-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-NAVIGATION-SCENE-PREVIEW-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-NAVIGATION-WALKABLE-PREVIEW-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-PROJECT-NAVIGATION-ASSETS-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-RO-NAVIGATION-BAKE-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-20-SHARED-NAVIGATION-PLAN-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-21-NAVIGATION-BUSINESS-FIX-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-21-NAVIGATION-DEPTH-GAP-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 新增 | `项目修改日志/2026-10-21-RO-NAVIGATION-CHANGELOG.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 修改 | `项目修改日志/README.md` | 导航相关历史修改/验收记录；README补提交日志要求 |
| 修改 | `项目分析/README.md` | 导航参考/实现/导出/难点专题及索引；保留已存在内容 |
| 新增 | `项目分析/RO_NAVIGATION_IMPLEMENTATION.md` | 导航参考/实现/导出/难点专题及索引；保留已存在内容 |
| 新增 | `项目分析/RO_PATHFINDING_ANALYSIS.md` | 导航参考/实现/导出/难点专题及索引；保留已存在内容 |
| 新增 | `项目分析/UNITY_NAVIGATION_EXPORT.md` | 导航参考/实现/导出/难点专题及索引；保留已存在内容 |
| 新增 | `项目分析/导航代码文件清单与测试说明.md` | 新增逐项清单与测试步骤 |
| 新增 | `项目分析/项目难点.md` | 导航参考/实现/导出/难点专题及索引；保留已存在内容 |
| 新增 | `项目修改日志/2026-10-21-NAVIGATION-TEST-GIT-RULES-CHANGELOG.md` | 本轮代码/Git规则与提交范围逐项记录 |

## 验证、失败记录和边界

- 开发与独立实际工作区客户端291源编译0错误38既有警告，导航Editor4源0错误2构建环境警告。
- 独立14审计/26薄边界通过，3038冻结仅批准MainCityMgr变化，5新增路径完整；无NPC点地、UI/实体/10像素拖动、事件池、重复启用/DontSave及退出清理覆盖。NPC另9静态项通过，配置和表/实体世界时序已核。
- 主从Git暂存区导出源码及受控程序集引用至_temp/index/snapshot验证提交快照：客户端290源0错误37警告，Editor4源0错误1环境警告。不依赖未提交跳跃协议/设置UI；计数与整个工作区不同，不累加。产物全部_temp，未替正式DLL。
- Git起初被08:00遗留0字节index.lock阻挡；只读确认无git/ssh进程后备份_temp/stale-index.lock，仅移除该锁后暂存成功，不修改工作区。独立源码PASS已完成，补充暂存验证委派后遇代理403余额不足，主依既有编译流程完成只读暂存验证；不将该附加编译称独立Agent结果。
- 默认git diff --cached --check提示旧Unity meta空值尾空格及历史日志末尾空行，未为排版扩大改动；仅本次命令忽略上述空白类型后静态检查通过，没有修改仓库全局配置。新增业务C#无此提示。
- 当前没有Unity MCP，没有主动启停Play/服务、保存场景、替换正式DLL。真实Game点击、NPC资源显示、走跑动画、碰撞和原生退出清理仍需正常重载后现场验收，隔离边界不当正式Player结果。
- 证据`_temp/navigation-test-entry-20261021/{client,npc,review,index}`；累计算法与早期回归见`项目分析/RO_NAVIGATION_IMPLEMENTATION.md`及历史导航日志。

## 测试操作

退出旧Play完成编译，正常进入主城Play后，菜单“工具/导航/测试/启用点地导航（仅本次运行）”，Game点击空地；UI/实体/拖动不发测试地面导航，WASD接管。NPC QuQu加载后点击验证原对话。关闭菜单或退出Play移除临时输入，不保存测试组件到正式场景/预制体。

## Git记录

- 分支：`codex/feature-development`，从本地main的`a8c982225cdeb697149423ee18395dcfe1fad604`创建，远端main只读核对相同；未执行合入main或推送main。
- 主提交内容：以上导航累计实现、本次NPC/测试修正、约束与逐项日志。中文提交正文说明修改/验证并引用本日志。
- 当前状态：暂存快照验证通过，准备提交并推送当前功能分支；最终结果由后续Git交付记录补充，未成功前不声明已上传。
