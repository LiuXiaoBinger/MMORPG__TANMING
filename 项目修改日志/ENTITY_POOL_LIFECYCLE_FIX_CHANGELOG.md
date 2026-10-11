# 客户端实体池与异步表现生命周期返工说明

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/SceneEntityWorld.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MComponentMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MObject.cs`
- `Docs/AI/CURRENT_TASK.md`
- `Docs/AI/TEST_REPORT.md`

## 实现内容与设计思路

- 主城角色预制体请求发起前固定保存本次世界、实体 ID、原实体引用和 `RoleBaseInfo` 快照。异步完成后必须同时通过世界、ID、引用和 Active 生命周期校验，防止旧请求读取池复用对象的新身份。
- 对加载成功但实体已经失效、组件缺失或控制器无法绑定的对象统一执行 `UnityEngine.Object.Destroy`；只有全部校验与数据初始化完成后，才把对象所有权交给 `MModel`。
- 世界创建入口不再忽略 `Add` 结果。Builder 返回空时直接返回空；加入世界失败时销毁实体、回收普通组件/专用组件/模型并归还实体池，然后返回空。
- 组件池取出普通组件时不再提前绑定。普通组件统一在 `MObject.AddComponent` 绑定；Move、Buff、Machine 在专用入口绑定；动作状态继续在动作池入口绑定，避免同一实例第二次 `RebindOwner` 清空事件登记。
- 实体销毁时先关闭普通组件，再由派生实体释放四个专用组件和模型，确保普通组件 `Shutdown` 仍能访问状态机和表现对象。

## 验证结果

- 客户端 `GM_Game.csproj` Debug 构建通过：0 个错误、31 个既有警告。
- 目标文件规则扫描通过：未发现命名空间、`foreach`、三目运算符或弱类型业务载荷。
- `git diff --check` 通过；目标文件无行尾空白。
- 构建与日志位于 `_temp/pool-lifecycle-fix-20260914/`。

## 未解决问题

- 尚未执行 Unity Play Mode 下的真实异步失效、实体池多轮复用、世界释放后创建和组件事件自动解绑专项测试。
- 本轮没有处理状态许可矩阵、Animator Controller、NPC 世界纳管、事件类型拆分和角色组件强类型接口，这些由其他客户端 Agent 分工完成。
