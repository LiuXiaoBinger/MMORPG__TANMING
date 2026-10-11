# 本地主角模型修复记录

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`

## 根因

测试主角包没有设置职业编号，默认值不是 MJS；同时 `MainCityMgr.Start` 只执行一次，可能早于测试场景延迟启用 Global 和本地主角创建。`Role_MJS_Other` 是其他玩家同步路径的固定模型，不应作为本地主角模型。

## 实现

- 测试角色包写入 `RoleJobtype.MJS`。
- `MainCityMgr` 订阅 `Global.LocalRoleLoadCompleted`，并在 Global 延迟启用时补订阅、补处理已加载角色。
- 本地 MJS 角色映射到 `Role/Role_MJS`。
- 其他玩家仍由 `HandleOtherRoleOnlineCore` 使用 `Role/Role_MJS_Other`。
- 增加本地角色 UID 请求标记，避免异步加载期间重复请求预制体。

## 验证

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/main-role-mjs-build`：0 个错误，39 个既有警告。
- 目标文件静态检查通过：无命名空间、`foreach`、三目运算符及 `object`/`dynamic` 业务载荷。
- Unity Play Mode 尚未执行；待编辑器空闲后确认本地主角和其他玩家的实例资源名。

## Unity 实际验证补充

- 在 `New Scene` 进入 Play Mode，确认日志出现：`Global 状态：Loaded`、`开始加载本地主角模型：Role/Role_MJS`、`角色表现绑定成功，角色 UID：2，资源路径：Role/Role_MJS`。
- Game View 截图确认主城中显示角色模型，验证后已停止 Play Mode。
- 运行期间出现的 `CameraMgr.Update` 空引用和 Global 资源包重试提示属于独立问题，本次未修改。
