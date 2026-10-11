# 变更说明：SceneEntity Component 目录中文注释补充（2026-09-15）

## 修改文件清单（共 16 个）

### Component 根目录（11 个）
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MComponentMgr.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MBuffComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityTransformComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityRoleComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityProjectileComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityMonsterComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityComponentMarker.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityAttributesComponent.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/base/EntityComponentBase.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/base/EntityComponentContainer.cs`

### Component/State/（5 个）
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/MStateMachine.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/MActionStateContainer.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/IMStateTransform.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/CommonStateTypeTool.cs`
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/State/ActionStateKey.cs`

## 实现内容

仅新增中文 XML 注释和行内注释，业务逻辑零变更：

1. **类级 `<summary>`**：说明每个组件/状态机/容器的职责、在实体组件体系中的位置。
2. **字段/属性注释**：说明每个私有字段、公共属性的用途和语义。
3. **方法 XML 注释**：所有公开/私有方法补充 `<summary>`、`<param>`、`<returns>`，说明副作用（事件派发、对象池复用、实体快捷引用变更等）。
4. **关键逻辑行内注释**：
   - 组件生命周期：Initialize/Update/Shutdown/RebindOwner/ResetForPool 时序。
   - 对象池复用：TakeComponent 优先复用、ReleaseComponent 不允许回池判断、Marker 不复用。
   - 事件分发：RegisterEvent 记录订阅、ClearRegisteredEvents 快照遍历解绑、属性变化事件派发条件。
   - 状态机：按实体类型注册动作状态分支、CanTransfer 权限校验、TransferToState 切换流程、LateUpdate 延迟动画播放、Shutdown 容器统一回收 Skill。
   - 状态许可矩阵：CommonStateTypeTool 越界枚举保护。
5. **接口契约注释**：IMStateTransform 每个方法和属性补充契约说明。

## 设计思路

- 严格遵守"仅新增注释、业务逻辑零变更"原则，所有 Edit 均以原始代码为 old_string，new_string 仅在原代码行之间插入注释行。
- 保留已有注释，不覆盖、不回退用户已有修改。
- 注释风格与前几批注释任务保持一致：XML 文档注释 + 关键逻辑行内中文注释。

## 验证结果

- 人工抽查 3 个代表性文件：
  - `MStateMachine.cs`：类级 summary、7 个私有字段注释、全部公开方法 XML 注释、RegisterDefaultStates 分支行内注释完整。
  - `EntityAttributesComponent.cs`：Apply 方法范围钳制和事件派发分支行内注释齐全，if/赋值/派发代码行未改动。
  - `EntityComponentContainer.cs`：Update/Shutdown 的 for 循环和委托调用原样保留，异常隔离逻辑未变。
- 未执行 Unity 编译（按任务要求，仅注释变更，无需编译验证）。

## 未解决问题

无。Component/Action/ 目录下各动作状态组件（MStunComponent、MSkillComponent 等 16 个文件）属于下一批次，不在本次范围。
