# 客户端脏标记改造记录

> 本文件由每次代码修改后更新，记录实现内容、设计依据、验证结果和未解决问题。

## 2026-09-12

### 追加验证：CenterServer 依赖版本

- 本次后端启动问题与脏标记功能无关，已单独记录在 `Docs/AI/TEST_REPORT.md`。
- `CenterServer` 的 Unsafe 依赖已统一为 6.0.0.0，验证输出放在 `_temp/center-unsafe-build`。

### 需求

- 物品添加、删除、使用、交换以及服务器同步到达时，先更新角色组件数据。
- 不立即刷新背包 UI，将对应背包页标记为脏。
- 背包窗口的帧循环检查当前页是否脏，打开或显示时再刷新。
- 脏标记不能只支持背包，还要支持次数、商城、副本、任务等模块。
- 暂不增加 `DataVersion`。
- 为后续拍卖行等跨模块物品转移保留来源模块、目标模块和格位变化信息。

### 设计思路

1. 角色组件保存权威客户端运行时数据。
2. 组件收到服务器快照或增量后立即修改数据，不等待 UI。
3. 组件向角色级脏状态对象报告领域变化。
4. 脏状态按领域保存：背包、次数、商城、副本、任务等；背包领域再按 `KnapsackType` 分开保存。
5. 背包页面只在打开、切换页签或帧循环检查到当前页脏时刷新。
6. 背包变化记录具体 UID、旧容器/旧格位、新容器/新格位和变化类型，以支持普通背包与拍卖行等模块之间的转移。
7. 立即生效的业务数据和延迟执行的 UI 刷新分离，脏标记不参与服务器结算或持久化。

### 代码修改

- 新增 `FunlModule/Component/ClientDirtyModule.cs`、`ClientDirtyTracker.cs`、`ClientDirtySnapshot.cs`、`ClientChangeContext.cs`，建立角色级模块脏状态及背包分页脏状态。
- `ClientRole` 持有并在释放时清理脏标记；`RoleComponentBase` 提供统一标记入口。
- `RoItemComponent` 在模型数据更新后记录背包类型、格位和跨背包变化上下文；`RoleCountComponent` 记录次数模块脏状态。
- `KnapsackCtrl`、`ShopCtrl` 改为在窗口激活帧循环消费脏状态，购买回包只提示，不直接重建商城列表。
- `KnapsackWindow`、`NpcShopWindow` 增加激活状态下的帧通知；四个新增类型已加入 `GM_Game.csproj`。

### 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug`：0 个错误，仅既有警告。
- 代码审核：通过；确认无 `foreach`、无三目运算符，注释和角色切换清理符合约束。
- 测试：静态及构建验证通过；未执行 Unity Play Mode 和真实四端联调。

### 未解决问题

- 历史模型事件仍保留作兼容用途，禁止新 UI 直接订阅并绕过脏标记。
- 拍卖行等跨模块操作目前保存变化上下文，但统一事务 ID 传递仍需在具体交易协议中接入。
