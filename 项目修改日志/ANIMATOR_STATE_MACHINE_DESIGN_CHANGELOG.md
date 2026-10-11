# MAnimator 与状态机设计文档变更说明

## 修改文件

- `项目分析/ANIMATOR_STATE_MACHINE_DESIGN.md`
- `Docs/AI/CURRENT_TASK.md`
- `Docs/AI/TEST_REPORT.md`

## 实现内容

- 解析参考项目 `Ro_cs422nb` 的 `MAnimator`、`MAnimationMgr`、`MStateMachine`、`MActionComponent` 及调用时序。
- 设计本项目客户端 `MAnimator`、Clip 资源适配器、AnimatorOverrideController、BlendTree、参数缓存、归一化时间、对象池和异步回调保护。
- 设计 `EntityViewComponent` 与 Animator 的绑定生命周期。
- 设计 PlayerEntity、RoleEntity、MonsterEntity 的统一状态机骨架、优先级/打断规则、输入/网络/AI 事件来源，以及旧 `RoleFSM` 迁移方案。

## 设计思路

保持 `MObject → MSceneObject → MEntity` 实体层级；状态机只处理逻辑状态和动作生命周期，动画封装只驱动 Unity Animator。YooAsset 调用集中到独立适配器，使用会话版本和引用计数避免对象池复用及异步加载造成串台。

## 验证结果

- 已检查参考源码和当前项目实体、状态机、表现组件、资源管理器实现。
- 本轮仅修改文档和协作记录，未改业务代码，未执行编译或 PlayMode。
- 文档待独立子 Agent 审核。

## 未解决问题

- 动画配置表的最终字段和 YooAsset location 需要实现前确认。
- 状态同步协议的序列号、动作起始时间和预测确认字段需要与后端对齐。
- 各实体类型的完整动作组件清单需要客户端与策划共同补齐。

## 第一轮审核返工

- 初审结论：不通过。问题集中在动作状态实例装配、时间单位、控制器资源句柄、异步 Clip 租约、状态仲裁和旧 RoleFSM 映射不完整。
- 已修订：加入动作状态注册表/工厂及初始化回收时序；规定毫秒到秒的唯一转换点和 MAnimator 单次更新；补充 RuntimeAnimatorController 租约；定义 ClipLease 过期状态和 Retain/Release 配对；补充 MActionComponent 兼容契约、事件解绑、优先级队列仲裁与服务器 token 覆盖；补齐旧 RoleFSM 全状态映射和相机迁移。
- 当前状态：等待独立子 Agent 复审，未进入代码实现。

## 第二轮审核返工

- 复审新增问题：当前世界层没有 LateUpdate 调度，动作状态 Attached/事件订阅时机不明确，BlendTree 共享 normalizedTime、远程状态字段降级和仲裁伪流程不够具体。
- 已修订：补充 Global/SceneEntityWorld/MEntity LateUpdate 唯一链路；明确动作状态 Attached、事件注册、解绑和回收顺序；补充 RO animInfo 元数据预加载与 LRU(64/32/60)；定义 BlendTree 全组时间和失败回退；加入事件队列排序/权限/服务器序列覆盖伪代码；增加远程 `RemoteActionSyncState` 及字段缺失时 Idle/Move 降级。
- 当前状态：等待独立子 Agent 再次复审，未进入代码实现。

## 第三轮审核返工

- 复审新增问题：LateUpdate 尚未明确接入 `ROObject`/世界接口，动作状态容器可能与普通组件键冲突。
- 已修订：规定新增 `ROObject.LateUpdate(int)`，由 `MObject` 空实现、`MEntity` 转发秒值并由 `SceneEntityWorld`/`Global` 统一调用；定义独立 `MActionStateContainer` 保存和逆序回收动作状态，避免与普通组件及四个专用组件重复注册。
- 当前状态：等待独立子 Agent 再次复审，未进入代码实现。

## 第四轮审核返工

- 复审新增阻塞：LateUpdate 与 PendingDestroy 时机矛盾、毫秒/秒签名不精确、状态容器键冲突、动作组件与当前基类契约不一致、注册表初始化顺序未固定。
- 已修订：规定 Update 末完成 PendingDestroy 销毁、LateUpdate 仅处理 Active/Sleeping；补齐所有更新接口签名及唯一换算点；使用 `ActionStateKey` 唯一索引和 `StateDefine` 二级索引；明确 `Global.Awake` 注册、实体 Attach/Attached/SetDefault 时序；统一 `MEventArgs`、`AnimatorFacade` 和 `ResetForPool` 回收契约；补充旧 `RoleState.Attck` 和表现入口迁移。
- 当前状态：等待独立子 Agent 最终复审，未进入代码实现。

## 最终复审（2026-09-14）

- 独立审核 Agent 已复审最新 `项目分析/ANIMATOR_STATE_MACHINE_DESIGN.md`，结论为通过。
- 审核确认：LateUpdate 与 PendingDestroy 的唯一时序、毫秒/秒接口及单次换算、Global.Awake 注册先后、`ActionStateKey → MActionComponent` 独立索引、`MActionComponent(ROObject owner)` 与 `MEventArgs`/`Attached`/`ResetForPool` 契约、以及 `EntityViewComponent.AnimatorFacade` 安全访问均已闭环。
- 本轮仍未写入客户端或服务端业务代码，未执行编译和 PlayMode；下一步按设计文档分阶段进入客户端实现。

## 用户要求重设计（2026-09-14）

- 用户明确要求严格采用 RO 的 `MModel` 封装，并指出 `MModel` 必须由 `MEntity` 直接持有、内部持有 `MAnimator`。
- 已整体重写 `项目分析/ANIMATOR_STATE_MACHINE_DESIGN.md`，删除 `EntityViewComponent`、`AnimatorFacade` 及其绑定、更新和回收路径。
- 新设计直接使用 `Entity.Model` 和 `Entity.Model.Ator`，并列出全部实体、模型表现组件、动画资源类、状态机、动作状态和普通实体组件。
- 新设计保留 RO 的 `MObject → MSceneObject → MEntity` 职责边界，同时按本项目要求将 `_model` 放置在 `MEntity`。
- 本轮仅修改设计文档和协作记录，未修改客户端、服务端、协议或资源，未执行编译和 PlayMode。

## 独立审核返工（2026-09-14）

- 审核指出文档中旧表现中间层名称仍有残留，且动作状态容器的索引层级表述不准确。
- 已清理设计正文中的旧类型名称，统一改为“独立表现中间层”；动作状态容器改为 `ActionStateKey → MActionComponent` 唯一主索引，并补充 `StateDefine → ActionStateKey` 辅助索引。
- 已补充 RO 中 `MPubVehicle`、`MSoul`、`MSceneTriggerObject`、`MDrop` 等本轮不纳入对象的范围声明。
- 审核结论：类结构关系、职责边界和用户要求已通过；本轮仍仅修改文档，未进入代码实现。
