# 2026-09-18 测试场景模拟主角角色包变更说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`
- `Docs/AI/CURRENT_TASK.md`
- `Docs/AI/TEST_REPORT.md`

## 实现内容

- 在专用测试组件 `test` 中增加账号 ID、角色 ID、昵称等可序列化的强类型模拟角色数据。
- YooAsset 默认包初始化成功、预置 Global 启用且 `SceneWorld` 就绪后，自动调用公开测试入口 `SimulateMainRolePacket`。
- 测试入口构造 `MainRoleInfo` 和 `RoleBaseInfo`，统一调用正式 `Global.SubmitLocalRoleLoad`。角色实体仍由正式 `SceneWorld.LoadLocalRole` 创建。
- 当当前连接代次无效时，测试脚本复用 `NetSocketMgr.ConnectServer` 建立正式连接代次，以满足 Global 的真实前置校验；不修改 Global 或协议生成代码。
- 通过测试提交标记、当前主角账号/角色 ID 比对和 Global 内置去重，避免同一主角重复创建。

## 设计思路

测试逻辑只存在于 `UI/test`。不复制玩家实体构建、入世界生命周期或角色组件初始化逻辑，避免测试路径与正式开始游戏路径产生行为分叉。连接代次由既有网络管理器维护，测试不以反射、直接赋值或其他旁路绕过正式校验。

## 验证结果

- 已执行：`dotnet build LYF_MMO/GM_Game.csproj -o D:/unitypro/LYFMMORGP/_temp/test-mock-role-packet-build`。
- 结果：0 个错误，39 个项目既有警告；构建产物位于 `_temp/test-mock-role-packet-build`。
- 静态检查：目标脚本未新增 `namespace`、`foreach`、三目运算符、`object` 或 `dynamic`。

## 未解决问题

- 未执行 Unity Play Mode，尚未在运行态核验 YooAsset EditorSimulate、连接代次和 PlayerEntity 模型创建。
- Unity MCP 恢复后，应运行专用 `New Scene` 并确认 `Global.Instance.SceneWorld.LocalRole` 的账号 ID 和角色 ID 均为 10001。

## 补充修正

- 修正 `test` 类 XML 摘要，使其准确说明该测试组件会在场景实体世界就绪后初始化测试 NPC 并模拟主角角色包；未修改任何业务逻辑。
