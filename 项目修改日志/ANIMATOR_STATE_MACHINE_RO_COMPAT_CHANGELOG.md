# RO 状态许可与 Animator 兼容返工

## 修改内容

- 按参考项目 `state_machine_define.cpp` 落地完整 15×15 `StatePermitted` 矩阵。
- `controllerLocation` 通过 YooAsset Location 异步加载 Controller，使用会话版本和资源租约保护池化对象。
- 对现有 `Movement`（Float）与 `Action`（Int）控制器增加 Idle/Move 及动作状态兼容映射。
- 旧 `RoleFSM` 保留，新状态机仍是唯一写入 `MEntity.Model.Ator` 的入口。

## 验证

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/animator-state-fix-build`：0 错误，31 个既有警告。
- 目标文件扫描：无 namespace、foreach、三目运算符和 object 业务载荷。

## 未解决

- 尚未在 Unity Play Mode 验证真实 YooAsset Controller 地址及各预制体动作数值。
