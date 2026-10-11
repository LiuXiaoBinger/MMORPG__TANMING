# 2026-09-23 客户端输入停止与跳跃等待修复

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs`、`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`。
- 实现内容：Movement.started/performed 使用 `MMoveEventArgs` 通过 `MEvent_Move` 派发最新世界方向；Movement.canceled 发送无载荷 `MEvent_StopMove`，由 `MMoveComponent` 清理方向、预测位置和移动状态；跳跃动画装配在 `MAnimationMgr.IsPreloaded` 为 false 时等待，避免提前解析短名产生错误日志。
- 设计思路：输入事件只保留强类型移动载荷，停止事件不创建对象池参数；实体事件分发器继续作为移动参数唯一回收者，快速前后切换由后到达的方向事件覆盖 `_targetDir`。
- 验证结果：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/client-input-jump-fix-build` 成功，0 个错误、39 个项目既有警告；目标代码静态约束检查和 `git diff --check` 通过。
- 未解决问题：未执行 Unity Editor Play Mode，需在专用测试场景实测快速前后输入松开停止、跳跃动画异步加载和落地状态恢复。
- 追加修正：`DispatchMovement` 在调用移动事件桥接器前设置参数所有权已交接标记；桥接器/分发器异常时不再由外层重复回收，创建后且交接前异常仍由当前方法回收。
