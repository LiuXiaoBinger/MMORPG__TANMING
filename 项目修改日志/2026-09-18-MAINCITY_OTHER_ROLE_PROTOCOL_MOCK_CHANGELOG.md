# 2026-09-18 主城其他玩家正式协议模拟变更说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/MainCityOtherRoleMock.cs`
- `LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity`
- `Docs/AI/CURRENT_TASK.md`
- `Docs/AI/TEST_REPORT.md`

## 实现内容

- 复用已有 `MainCityOtherRoleMock`，不新建重复测试组件。
- 测试构造强类型 `RoleBaseInfo`，经 `SocketDispatcher.DispatchEvent` 投递正式协议 `CMD_SyncotherOnlineCode`，协议号为 `11100`。
- 组件在固定延迟后逐帧等待 `MainCityMgr.Instance`、`Global.Instance` 与 `Global.Instance.SceneWorld` 全部就绪，随后只投递一次协议。
- `New Scene` 的专用测试对象挂载组件且 `_simulateOnStart=1`；未修改正式主城场景、`MainCityMgr`、`Global` 或协议生成代码。

## 设计思路

正式链路为 `SocketDispatcher -> MainCityMgr.SyncotherOnlineHandle -> RoleBaseInfo.Parser.ParseFrom -> HandleOtherRoleOnline -> SceneWorldSystem.AddOrUpdateOtherRole -> CreateRole`。测试只投递相同协议，实体更新、角色 ID 去重与首次表现创建均由正式路径负责。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/mock-maincity-role-response-build`：成功，0 个错误、39 个既有警告。
- 静态检查：目标测试脚本未发现 `namespace`、`foreach`、三目运算符、`object` 或 `dynamic`。

## 未解决问题

未执行 Unity Play Mode，原因是当前没有可用 Unity MCP 编辑器会话。恢复会话后需运行 `New Scene`，确认角色 ID `900001` 的同步日志和 `Role/Role_MJS_Other` 表现对象。

## Play Mode 自动化测试

- 新增专用 Unity Test Framework 测试文件 `LYF_MMO/Assets/GM_Game/Scripts/UI/test/Editor/MainCityOtherRolePlayModeTests.cs`；该文件打开 `New Scene`、进入 Play Mode，并轮询 `Global.Instance.SceneWorld.TryGetRole(900001)`。
- 已尝试执行 Unity batchmode `-runTests -testPlatform PlayMode`，指定结果为 `_temp/maincity-other-role-playmode-results.xml`，日志为 `_temp/maincity-other-role-playmode.log`。
- 未执行到测试：Unity 日志准确报告项目已被另一实例占用，错误为 `Multiple Unity instances cannot open the same project`。日志已生成，结果 XML 未生成；进程退出码 `0` 不表示测试通过。
- 占用实例关闭后需使用同一命令重新执行并以 XML 测试结果为准。

## PlayMode 测试程序集修复

- 根因：旧测试类位于 `UI/test/Editor` 默认 Editor 程序集，未通过 asmdef 获取 `TestAssemblies` 引用，因此 `UnitySetUp` 与 `UnityTest` 无法解析。
- 修改：测试类迁移到 `UI/test/PlayMode/MainCityOtherRolePlayModeTests.cs`，并新增同目录 `MainCityOtherRolePlayModeTests.asmdef`。该程序集引用 `GM_Game` 并声明 `optionalUnityReferences: [TestAssemblies]`。
- 测试实现只依赖运行时 `UnityEngine.SceneManagement`、NUnit 与 `UnityEngine.TestTools`，不再引用 UnityEditor；场景加载和角色 `900001` 断言保持在专用测试代码内。
- 验证：`dotnet build LYF_MMO/GM_Game.MainCityOtherRole.PlayModeTests.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/maincity-other-role-playmode-test-build` 成功，0 个错误、40 个既有工程警告，`UnitySetUp` 和 `UnityTest` 引用错误已消除。
- 运行前置：`New Scene` 尚未加入 Build Settings，测试会明确断言该前置条件而不改写用户配置；Unity batchmode 仍受项目占用限制，未重跑。

## New Scene 主城管理器启用

- 仅修改 `LYF_MMO/Assets/GM_Game/Scenes/New Scene.unity`：已有 `MainCityMgr` GameObject 的 `m_IsActive` 由 `0` 改为 `1`；组件本身原已启用。
- 该改动使 `MainCityMgr.Awake` 在专用测试场景启动时注册 `CMD_SyncotherOnlineCode`，既有 `MainCityOtherRoleMock` 因而可以自动投递正式协议。
- 用户运行态已验证：手工启用 MainCityMgr 后，`SceneWorld.TryGetRole(900001)` 成功，且角色表现为激活的 `Role_MJS_Other(Clone)`。
- 等待 Unity 刷新 15 秒时，现有 Editor.log 无新增内容，无法取得本次外部 YAML 更新后的 Console 日志；未关闭或重启用户的 Unity 实例。独立 PlayMode 测试程序集的最近构建保持 0 错误。
