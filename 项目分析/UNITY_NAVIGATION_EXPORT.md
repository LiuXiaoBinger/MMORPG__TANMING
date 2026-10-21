# 自有地图导航网格导出工具

2026-10-21运行接入更新：基于本格式的纯C#共享查询及客户端点地导航源码已完成，详见`项目分析/RO_NAVIGATION_IMPLEMENTATION.md`。历史“后续实现A*”描述指本文导出阶段，现查询库已具备；服务器运行加载/移动权威仍未接入。主城运行副本在`LYF_MMO/Assets/StreamingAssets/NavigationData/Scene_MainCity.navmesh.bytes`，重导后须同步，原导出/预览工具保持。

日期：2026-10-20。工具、真实主城导出及两端读回已完成。NavMeshSurface仅作外观参考，当前反读公共文件自行绘制行走蓝面，内部线淡显、障碍外边界深显，按场景和导航面深度遮挡后方线。前轮自绘可见但遮挡线杂乱，现按含树石柱场景返工；实际结果见当前测试报告。主城和后续副本地图复用同一工具，不选择RO外部工具链。

## 目标与边界

把Unity已经烘焙好的可走三角网导出成一份公共文件，Unity客户端和纯.NET Framework4.8服务端使用同样的C#类型和读写函数读取。数据保留世界坐标XYZ、三角形索引、区域、角色Agent类型与场景名，供后续服务端导航使用。

```text
地图已有NavMeshSurface及烘焙数据
  → Unity编辑器导出工具
  → NavigationData/<场景名>.navmesh.bytes
  → 同一份MNavigationMapSerializer
       ↙                         ↘
   Unity客户端读取           .NET4.8服务端读取
```

本轮做导出和读取，不直接重建完整导航查询。三角网是公共地图的几何表示；Unity内部多边形走廊、离网连接、动态障碍状态和寻路查询器不会因为保存了顶点/三角形便自动具备。后续服务端A*及相邻区域/接缝处理须在接入导航查询时实施，不能把本轮工具宣称为服务器寻路已完成。

## 范围和地图复用

一份NavMeshData导出一个文件，同一工具换地图资产后重复使用，文件按场景名区分。不自动新建副本管理、加载队列、跨图协议或批量场景切换框架。同名场景再次导出会覆盖同名文件；不同目录的同名场景需采用不同场景名，避免覆盖。本轮不做多资产合并或多Agent管理。

NavMesh已按角色参数产生可走边界，直接导出，不再做一次半径收缩，也不要求把Unity.asset转换成RO.bytes。原场景、现有NavMesh资产和用户配置保留。

## 使用步骤

1. 在地图的NavMeshSurface完成烘焙。主城直接使用现有`LYF_MMO/Assets/GM_Game/Scenes/Scene_MainCity/NavMesh-MainCityNavigation.asset`。
2. 正常结束运行后打开Boot或空场景，确保当前没有已加载的导航网格。工具不会替用户切场景、停止游戏或保存场景。
3. 打开Unity菜单：**工具 → 导航 → 导出公共地图网格**。
4. “地图场景”选择`Scene_MainCity`，“已烘焙导航数据”选择上面的主城NavMeshData资产。场景选择仅用于命名与标识，使用者应选择配套导航资产。
5. 点击“导出公共导航网格”。窗口与Console显示文件路径、顶点数及三角形数。

首次正式输出`NavigationData/Scene_MainCity.navmesh.bytes`，5,232,487字节、270,735顶点、123,977三角形，AgentTypeID为0。首次验收SHA256：`61D6AB8FE38AB80DD59FCBC425E652AC637FB7DE30D45CD6BC4987853F3CBB4C`。用户之后可自行重新导出；原生网格排列可能变化，当前文件的SHA以对应轮次验收为准，历史SHA不代表当前文件必须相同。

后续副本先烘焙自身NavMeshData，再从同一窗口选择副本场景及其导航资产。例如`Scene_Dungeon01`生成`NavigationData/Scene_Dungeon01.navmesh.bytes`，无需改导出代码。地图变动后重新烘焙并重导对应文件。

文件在Unity工程外的项目根目录，作为两端公共源文件。客户端资源包与服务器部署应使用同一地图的同一版本；自动复制、运行加载及发布流程尚未接入。

## 在Scene视图查看公共文件自绘寻路网格

用户要求自己照着NavMeshSurface的效果画；该组件只作外观参考。当前反读`NavigationData/<场景名>.navmesh.bytes`，自己绘制青蓝行走面、淡内部线和深障碍/外轮廓。前轮在空地上只验“出现面线”遗漏实际障碍处杂线，不能作最终可读性依据；本轮以含柱树石的原生参考/旧自绘/新自绘同镜头实图对照。

1. 打开对应地图，例如主城，切到**Scene**标签。
2. 打开“工具 → 导航 → 导出公共地图网格”，选择对应地图场景。已有公共文件的预览不需要选中场景Surface；烘焙资产仍是导出所需输入。
3. 点击**显示 / 重新读取网格**，窗口显示实际读取路径和网格数量。开启时暂停原生蓝面，避免两套显示重叠。
4. 默认保留当前Scene镜头；需要全图观察时再使用定位。定位只影响Scene编辑镜头，不改变游戏相机或网格顶点。
5. 点击**隐藏网格**、切换来源、关闭窗口或脚本重载时结束自绘并释放临时渲染资源，恢复原有原生显示设置。

面和边线均来自文件三角网，缓存两份UInt32 Mesh。蓝面写入深度，后方导航层不再穿过地面/柱/树叠色；三种材质分别画蓝面、淡内部边、深轮廓，不再使用Greater透画层。完全相同XYZ端点仅用于边统计规范化，无向边只画一次，单侧边归轮廓；不同高度不合并，原面索引与XYZ保持。专用Editor线Shader沿视空间向相机偏移0.02米以解决共面蓝面遮线，仍使用LEqual深度测试。隐藏时解绑指令并释放两Mesh和三材质。不临时注册NavMesh、不改场景或重烘焙。

2026-10-21补充贴地显示修正：导航简化大三角形可能顶点在地表上、内部却低于起伏Terrain，直接场景深度会产生大块裸地缺口。当前面和线共用预览Shader，读取当前Terrain世界高度快照及真实场景深度；仅遮挡点与Terrain高度相差不超过0.08米、导航面在其下且差值不超过1.65米时修正该片元显示深度。上限依据当前缺口面861点采样最大1.590706米，仍保留场景/导航自身深度遮挡；不是对任意后层透画。当前只接现有单Terrain，不改变地图真实几何、拓扑或文件；没有Terrain时沿普通深度显示。

Shader使用实时逆视矩阵，Scene镜头旋转后仍能重建遮挡点。预览暂时请求Scene相机Depth纹理，隐藏/关闭/重载时恢复原DepthTextureMode并销毁高度Texture。正式草地、转镜头和柱树石双景已目检；数据与生命周期结果见本轮协作测试报告。修正仅限显示，不作为服务端碰撞/高度修复。详细记录：项目修改日志/2026-10-21-NAVIGATION-DEPTH-GAP-CHANGELOG.md，证据_temp/navigation-preview-depth-gap-20261021。

初稿边键采用UInt64拼接端点，在主城实际触发严重哈希聚集和首次预览卡顿，现改Vector2Int排序端点键。本机末次生成约406毫秒，属于单次编辑器预览测量，不代表运行游戏FPS收益。排查与复盘见本轮日志及项目难点。

公共文件仍含部分树冠和石顶面，原生也显示这些面；这属于原采集和烘焙内容，不能在显示层删面后宣称烘焙质量已修复。原资产Agent0规格为半径0.29米、高2.1米、坡度45°、爬高0.75米；MJS台阶0.3米，未声称完全匹配当前玩家。

自绘显示与完整寻路不同：当前不计算角色起点连通区域，不接入角色移动或服务器A*。制作阶段仍用Unity烘焙，自写的是公共格式、导出、读取与显示。多轮取舍见`项目修改日志/2026-10-20-NAVIGATION-PATHMESH-VIEW-CHANGELOG.md`，当前可读性证据放`_temp/navigation-pathmesh-view-20261020/walkable-readable`；self-draw目录只作前轮显示/生命周期历史结果。

## 目前到底依赖什么

| 环节 | 当前实现及依赖 |
| --- | --- |
| 把场景地形烘焙成可走区域 | 仍由Unity AI Navigation的NavMeshSurface及Unity导航引擎完成，没有自研烘焙器 |
| 导出公共文件 | 自写Editor工具，使用UnityEngine.AI读取已烘焙网格 |
| 在Scene查看寻路网格 | 反读公共bytes，由自己的Editor预览绘制透明可走面和网格线；NavMeshSurface仅作外观参考 |
| 服务端读取公共文件 | 同源纯C#读取器，仅.NET基础库，不需要Unity或AI Navigation插件 |
| 服务端路径查询 | 2026-10-21已完成同源纯C#查询库，游戏服运行加载/调度/移动权威仍未接入 |

因此“自写导出工具”指我们维护公共格式和导出/读取代码，制作及Scene导航查看继续使用Unity。没有替换Unity烘焙算法或扩展成独立烘焙系统。

## 为什么要求空导航世界

Unity的`NavMesh.CalculateTriangulation()`读取全局已加载导航网格，不能按选择的场景资产准确筛选。已加载主城时再注册同一资产会混入两份，其他地图或Agent也可能一起导出。

工具先确认全局三角形为空，再临时注册所选资产，最后在`finally`中仅移除本次实例。不卸载用户已有网格、不打开或保存地图。提示已有导航网格时，正常切到Boot/空场景再导出。

世界坐标使用`NavMeshData.position/rotation`保存的烘焙位姿。若地图将NavMeshSurface移动或旋转到与资产保存位姿不同的位置，应按地图最终位姿重新烘焙；工具不读取未打开场景的额外Transform覆盖。

## 源码与读取入口

| 文件 | 职责 |
| --- | --- |
| `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationExportWindow.cs` | 选择来源、确定输出路径、显示结果 |
| `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapExporter.cs` | 临时注册单资产、提取世界三角网、保存、清理 |
| `LYF_MMO/Assets/GM_Game/Editor/Navigation/MNavigationMapPreview.cs` | 读取公共文件，自绘透明可走面及线网格、定位并释放临时渲染资源 |
| `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationPoint.cs` | 无Unity依赖的世界XYZ |
| `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationMapData.cs` | 单地图的顶点、索引、区域与来源信息 |
| `LYF_MMO/Assets/GM_Game/Scripts/Navigation/MNavigationMapSerializer.cs` | 两端同一格式的Save/Load |
| `LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj` | 直接链接上述三份共享源码，避免复制后格式漂移 |

两端入口均为`MNavigationMapSerializer.Load(filePath)`，返回`MNavigationMapData`。`filePath`由调用方提供该地图文件路径，读取不依赖UnityEngine，服务端保持.NET Framework4.8。

共享源码在Unity编入GM_Game，在服务器编入MMoRpgCommon。当前Unity两个旧Common插件均不含新增三类型，本轮未替换插件。未来若把含导航类型的新服务端Common.dll复制进Unity，须先确定这些类型的唯一编译归属，避免GM_Game源码与新Common同时定义同名类型。此为后续程序集更新边界，并非本轮已发生的运行故障。

## 文件格式：版本1

整数及float均按BinaryWriter小端保存，不包含Unity对象引用。

| 顺序 | 内容 | 长度/语义 |
| --- | --- | --- |
| 1 | 文件标记 | Int32，0x4E46594C，字节为LYFN |
| 2 | 版本 | Int32，当前1 |
| 3 | SceneName | BinaryWriter UTF-8字符串，前缀为7位编码的字节长度 |
| 4 | AgentTypeID | Int32，烘焙角色类型，不是地图ID |
| 5 | 顶点数 | Int32 |
| 6 | 三角形数 | Int32 |
| 7 | 顶点数组 | 每点X/Y/Z三个float，共顶点数×12字节 |
| 8 | 索引数组 | 每面三个Int32，共三角形数×12字节 |
| 9 | 区域数组 | 每面一个Int32，共三角形数×4字节 |

读取器检查标记、版本、数量、载荷长度和索引范围。区域编号保留Unity值，不等同于行走代价；主城本次区域均为0。修改布局须同步版本及两端源码。

## 实际验证与限制

- 正式主城由生产导出函数生成，Unity共享Load逐XYZ、索引和区域比对一致。真实Common.dll由.NET4.8读取同份文件；独立读取后原样重写，字节完全一致。
- Common工程编译0错误、4条既有警告；共享专项220断言通过，独立末稿107断言通过。覆盖中文、两地图、负值/小数、多高度、重复点、绕序、区域、往返及截断拒绝。
- 原生两张临时地图各32顶点/16三角，非零平移与37/-21度旋转、多高度层、已有网格拒绝及写入失败清理验证通过。临时夹具和产物都在`_temp/navigation-export-20261020`，无测试组件挂入正式场景。
- 主城坐标范围约X[-452.400,546.940]、Y[-15.369,243.795]、Z[-507.113,492.130]。保真导出不等于可走区域符合策划预期，地图烘焙范围仍应人工检查。
- 不含Unity内部多边形连接、离网跳跃链接、动态障碍或实时门状态。后续寻路须处理三角邻接与分块接缝，不能直接假定相邻三角形共用顶点索引。
- 未改原场景、原NavMesh资产、包清单、既有移动/技能代码；没有正式Player打包、资源发布、服务器进程加载或真实联机算路验收。
- 一次隔离探针误用Unity的场景资产读取接口，Console留下`Do not use ReadObjectThreaded on scene objects`；不是生产导出异常，未通过清Console掩盖。最终Boot非Play、场景未脏、全局导航三角形为0。

修改日志：项目修改日志/2026-10-20-NAVIGATION-EXPORT-TOOL-CHANGELOG.md。证据：_temp/navigation-export-20261020。
独立审查：项目修改日志/2026-10-20-NAVIGATION-EXPORT-INDEPENDENT-REVIEW.md。面试复盘：项目分析/项目难点.md本轮导航导出条目。
