# 公共地图导航网格导出：独立测试与审查

## 当前结论

最终审查通过，无剩余阻断问题。共享格式、真实主城文件的纯.NET读取、客户端位姿和双地图专项证据均已交叉核对。本轮不实现路径查询、邻接图、协议或角色移动。

## 审查范围与设计

- 正式生产范围：`LYF_MMO/Assets/GM_Game/Scripts/Navigation/`三份公共类型，`LYF_MMO/Assets/GM_Game/Editor/Navigation/`两份编辑器源文件及程序集定义，`LYF_Server/MMoRpgCommon/MMoRpgCommon.csproj`同源链接，`NavigationData/Scene_MainCity.navmesh.bytes`公共地图文件。
- 实现只提取已有Unity烘焙结果的世界坐标XYZ、三角形索引及区域；没有重新烘焙、二次按角色半径缩边、扁平化Y或声称保留Unity全部内部连接。服务器不需要Unity程序集即可读取公共文件。
- 因`NavMesh.CalculateTriangulation`读取全局导航世界，当前最短正常流程在Boot或空导航场景注册单份指定资产，完成后`finally`移除自己创建的实例，不卸载用户地图。该限制来自真实Unity API，未扩展隔离框架或自动场景切换。
- 正文与使用说明由主Agent归档到`项目分析/`，本文件只记录独立测试事实，不重复完整设计。

## 已执行验证

1. 冻结1108份正式业务源、场景、Prefab、程序集、工程、导航资源和包配置；完成共享与导出代码后重核，仅授权`MMoRpgCommon.csproj`变化，既有主城、NavMesh资产、包配置、正式DLL和业务源保持SHA。
2. 使用VS2022 MSBuild直接Rebuild真实`MMoRpgCommon.csproj`，中间和输出均指向`_temp/navigation-export-20261020/review/`。结果0错误，4条警告来自原有`ItemDesc.ToString`和`IdipParam`未用字段。
3. 使用.NET Framework编译器编译专项程序，直接引用本轮真实Common程序集；末稿执行107项断言，覆盖两张地图/不同Agent独立、中文场景名、负值/小数坐标、同XZ不同Y上下层保真、索引绕序、逐面区域、独立文件头解析、重复读写字节一致及真实主城和两份Unity原生隔离地图文件读取。早期95/99项是阶段结果，不累加数量。
4. 实际主城公共文件为5,232,487字节、270,735顶点、123,977三角形、AgentTypeID=0。纯.NET加载后重新保存，SHA逐字节相同：`61d6ab8fe38ab80dd59fcbc425e652ac637fb7de30d45cd6bc4987853f3cbb4c`。
5. 独立Python解析真实二进制：LYFN/版本1、场景名和载荷长度正确；坐标均有限，所有索引在范围内，123,977面区域均为0；XYZ包围盒约`(-452.400,-15.369,-507.113)`至`(546.940,243.795,492.130)`。
6. 5份新增源与meta静态核对通过：没有命名空间、foreach、三目或动态/无类型载荷；手工复核并排布尔项未超过3，类型、字段、参数、函数与局部变量具有具体中文说明。编辑器对Unity未公开AgentTypeID使用序列化边界读取实际烘焙设置。
7. 客户端原生10项专项证据通过：两份专用小地图均32顶点/16面，位置分别`(50,3,-20)`和`(-30,8,60)`，Y轴旋转37度和-21度；导出顶点世界坐标集合与原生参考完全一致，Y高度和上下层分离保留。已有全局网格时拒绝导出且保留原网格，真实写入失败后仅移除自己的临时实例。末态Boot非Play、dirty=false、全局导航三角形0。独立.NET又读取两份真实小地图，逐字节重写一致，场景名分别`Isolated_Map_0/1`，没有串图。

专项首次使用旧csc时，正斜杠相对路径被编译器错误解析，改绝对Windows路径后编译运行通过；没有据此修改生产源码。客户端首次Unity编译发现`NavMeshData.agentTypeID`未公开，开发已改最小序列化读取；该失败属于API适配，不宣称首稿编译通过。客户端隔离专项首稿误要求两次原生注册后的索引数组顺序相同，修正测试为比较世界几何集合，生产代码没有为测试改动；单次导出到反读的数组逐项等价仍通过。

## 证据路径

- `_temp/navigation-export-20261020/review/frozen-inputs.json`、`input-audit.json`：输入保护与五源末稿SHA。
- `_temp/navigation-export-20261020/review/common-build.log`：真实Common编译结果。
- `_temp/navigation-export-20261020/review/NavigationDataReview.cs`、`net48-actual-review-final.log`：107项真实Common专项与三份Unity文件字节往返。
- `_temp/navigation-export-20261020/review/actual-map-inspection.json`：独立真实文件统计。
- `_temp/navigation-export-20261020/client/maincity-export-result.txt`：开发执行真实Unity导出与逐元素读回，结束全局三角形0、Boot非Play/dirty=false。审查只读此证据，未重复占用MCP。
- `_temp/navigation-export-20261020/client/isolated-probe-result.txt`、`final-unity-state.txt`：原生位姿/双地图/清理10项与最终Unity状态；两份小地图二进制保留在同目录。

## 未执行与后续边界

- 本轮没有服务器A*、邻接/跨tile拼接、跳跃链接、动态障碍、客户端路线执行或联机协议验收；导出文件不能直接当作Unity NavMeshData重新注册。
- Unity主城逐项等价来自客户端开发原生执行证据，独立审查另以真实.NET读取及独立布局解析交叉验证，不能说独立重复执行了Unity导出。
- 换副本使用同一工具选择其场景与已烘焙资产；本轮不要求自动批量切换场景。公共源文件尚未自动复制到客户端Bundle或服务器运行目录，不改变正式部署。
- Unity客户端公共类型当前属于`GM_Game`，服务端Common链接同三份源码；Unity内两份旧Common插件未更新。未来若把本轮新Common复制到Unity同时保留源码，会出现类型重复，发布时需明确程序集归属；本轮没有替换插件或扩展程序集重构。
- 客户端只读核场景时误用`LoadAllAssetsAtPath(.unity)`留下一条`Do not use ReadObjectThreaded on scene objects`诊断，未保存/修改场景；正式导出和隔离专项没有该错误，不清控制台，也不把末态说成全控制台零错误。
- 主Agent统一更新`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`及项目难点/使用说明，避免多方覆盖协作入口。独立审查未修改生产业务代码，新增仅本报告和`_temp`专项/证据。
