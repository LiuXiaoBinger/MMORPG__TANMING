# 客户端生命周期方法顺序重排 —— 其余模块修改日志

## 基本信息

- **日期**：2026-10-12
- **执行人**：客户端开发子 Agent（C_rest 批次）
- **范围**：Common、Common/Utils、controller（camerctrl、InputController、RoleController、UIController）、FunlModule（Bag、Component、Role）、根目录 Global.cs、Manger、Role，共 17 个 C# 脚本
- **原则**：纯排版重排，零逻辑改动；用 Roslyn 语法树精确定位成员边界，连同上方特性、XML 注释、普通注释及条件编译块整体移动，不拆散；BOM 有无严格保持；写盘前原文镜像备份。

## 文件清单与移动详情

### Common 目录

| 相对路径 | 所含生命周期方法 | 实际移动内容 |
|---------|----------------|-------------|
| `LYF_MMO/Assets/GM_Game/Scripts/Common/MTimerMgr.cs` | `Update(float)` | `Update(float)` 从业务方法区上移到构造函数之后、其他业务方法之前 |

### Common/Utils 目录

| 相对路径 | 所含生命周期方法 | 实际移动内容 |
|---------|----------------|-------------|
| `LYF_MMO/Assets/GM_Game/Scripts/Common/Utils/GhostEffect.cs` | `OnEnable`、`Update`、`OnDisable` | 三个方法整体上移到成员变量区之后，按时序排列为 OnEnable → Update → OnDisable |
| `LYF_MMO/Assets/GM_Game/Scripts/Common/Utils/MinMapHelp.cs` | `Awake` | 本就符合顺序（NOCHANGE），未做任何改动 |

### controller 目录

| 相对路径 | 所含生命周期方法 | 实际移动内容 |
|---------|----------------|-------------|
| `LYF_MMO/Assets/GM_Game/Scripts/controller/camerctrl/S_CameraController.cs` | `Start`、`Update` | 本就符合顺序（Moved 为空，工具校验通过），未做任何改动 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/S_CameraController.cs` | `Start`、`Update` | 本就符合顺序（Moved 为空，工具校验通过），未做任何改动 |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/InputController/PlayerInputCtr.cs` | `Awake`、`OnEnable`、`Update`、`OnDisable`、`OnDestroy` | 五个方法整体上移到成员变量区之后，按时序排列为 Awake → OnEnable → Update → OnDisable → OnDestroy |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/RoleController/RoleCtrlBase.cs` | `Awake`、`Start`、`Update` | 三个方法整体上移到成员变量区之后，按时序排列为 Awake → Start → Update |
| `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/KnapsackCtrl.cs` | `Update(int)` | `Update(int)` 从业务方法区上移到两个构造函数之后、其他业务方法之前 |

### FunlModule 目录

| 相对路径 | 所含生命周期方法 | 实际移动内容 |
|---------|----------------|-------------|
| `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Bag/RoItemComponent.cs` | `Update(int)` override | `Update(int)` 从业务方法区上移到构造函数之后、Initialize 等其他方法之前 |
| `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Component/RoleComponentBase.cs` | `Update(int)` virtual | `Update(int)` 从业务方法区上移到构造函数之后、Initialize 等其他方法之前 |
| `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Component/RoleComponentContainer.cs` | `Update(int)` | `Update(int)` 从业务方法区上移到构造函数之后、SetComponent 等其他方法之前 |
| `LYF_MMO/Assets/GM_Game/Scripts/FunlModule/Role/SceneWorldSystem.cs` | `Update(int)`、`LateUpdate(int)` | 两个手动 Tick 方法整体上移到构造函数之后，按时序排列为 Update(int) → LateUpdate(int) |

### 根目录

| 相对路径 | 所含生命周期方法 | 实际移动内容 |
|---------|----------------|-------------|
| `LYF_MMO/Assets/GM_Game/Scripts/Global.cs` | `Awake`、`Update`、`LateUpdate`、`OnDestroy` | 四个方法整体上移到全部成员变量、属性、事件之后，按时序排列为 Awake → Update → LateUpdate → OnDestroy；嵌套枚举 `LocalRoleLoadSubmitResult` 保留在类末尾 |

### Manger 目录

| 相对路径 | 所含生命周期方法 | 实际移动内容 |
|---------|----------------|-------------|
| `LYF_MMO/Assets/GM_Game/Scripts/Manger/CameraMgr.cs` | `Awake`、`OnEnable`、`Update`、`OnDisable`、`OnDestroy` | 五个方法整体上移到全部成员变量（含 `[SerializeField]`、`[HideInInspector]`、`#region` 内字段）之后，按时序排列为 Awake → OnEnable → Update → OnDisable → OnDestroy |
| `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs` | `Awake`、`Start`、`Update`、`OnDestroy` | `OnDestroy` 从业务方法区上移到 Awake → Start → Update 之后，补全生命周期时序 |
| `LYF_MMO/Assets/GM_Game/Scripts/Manger/NpcManager.cs` | `Awake`、`OnDestroy` | `OnDestroy` 从业务方法区上移到 Awake 之后，补全生命周期时序 |

### Role 目录

| 相对路径 | 所含生命周期方法 | 实际移动内容 |
|---------|----------------|-------------|
| `LYF_MMO/Assets/GM_Game/Scripts/Role/RoleAnimaBehaviour.cs` | `Awake` | 本就符合顺序（Moved 为空，工具校验通过），未做任何改动 |

## 重排后统一类内结构

每个类内部从上到下统一为以下结构：

1. `using` 引用（不动）
2. 类型声明及类级 XML 注释
3. **成员变量区**：字段（含 `[SerializeField]`、`[Header]`、`[HideInInspector]`）、`const`、`readonly` 字段、`event`、委托、属性，保持原相对顺序
4. **构造函数**（如有）
5. **生命周期函数区**，固定时序：Awake → OnEnable → Start → FixedUpdate → Update → LateUpdate → OnDisable → OnDestroy（出现哪些排哪些；同名重载按方法名聚合、重载间保持原序；手动 Tick 方法如 `Update(int)`、`LateUpdate(float)` 同样按方法名归入此区）
6. **其他业务方法**，保持原相对顺序
7. **嵌套类型**（嵌套类、结构体、枚举）移到类末尾

## 验证结果

### 工具内置五项校验结论

| 校验项 | 结论 |
|-------|------|
| 代码 token 多重集合前后一致 | 全部 17 个文件通过 |
| 注释 / 预处理指令 / 禁用文本多重集合一致 | 全部 17 个文件通过 |
| 语法错误诊断不新增 | 全部 17 个文件通过 |
| 各类型成员定义数量守恒 | 全部 17 个文件通过 |
| 重排后顺序符合约定 | 全部 17 个文件通过 |

### 逐文件人工回读结论

已逐个 Read 回读全部 17 个文件（Global.cs 分段通读），确认：

- 成员变量区均位于类内最前（含字段、属性、事件、`const`、`[SerializeField]` 标注字段）
- 生命周期函数紧随成员变量区与构造函数之后，时序符合 Awake → OnEnable → Start → FixedUpdate → Update → LateUpdate → OnDisable → OnDestroy
- 方法上方的 XML 文档注释、普通注释、`[SerializeField]`、`[HideInInspector]`、`[Header]` 等特性均随方法整体移动，未脱离
- 无方法截断、无大括号失配
- `#region` 块保持完整（如 GhostEffect 的 `#region UnityMethods`）
- 嵌套类型均位于类末尾（MTimerMgr.MTimer、Global.LocalRoleLoadSubmitResult、GhostEffect.MeshInfo / GhostObject）

### 大括号与条件编译配对结论

全部 17 个文件大括号配对正常；本批次文件未涉及 `#if...#endif` 条件编译块的移动，无配对风险。

### 需用户确认事项

本次改动为纯排版重排，**需用户在 Unity Editor 中做一次编译确认**，确保无编译错误。

## SKIPPED / FAILED / 需人工确认文件

本批次无 SKIPPED 文件、无 FAILED 文件、无 Warnings。所有文件均已正常处理并写盘（NOCHANGE 文件未写盘）。

## 本模块汇总

| 指标 | 数量 |
|-----|------|
| 清单文件总数 | 17 |
| OK（重排成功并写盘） | 16 |
| NOCHANGE（本就符合顺序，未写盘） | 1 |
| SKIPPED | 0 |
| FAILED | 0 |

备份目录：`D:\unitypro\LYFMMORGP\_temp\lifecycle_run\C_rest\backup`
结果报告：`D:\unitypro\LYFMMORGP\_temp\lifecycle_run\C_rest\report.json`
