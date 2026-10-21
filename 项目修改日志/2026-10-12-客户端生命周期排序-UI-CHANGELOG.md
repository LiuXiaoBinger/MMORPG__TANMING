# 客户端 UI 模块生命周期函数顺序重排 修改日志

- 日期：2026-10-12
- 执行人：客户端开发子 Agent（UI 模块）
- 范围：`LYF_MMO/Assets/GM_Game/Scripts/UI/` 下 18 个含 Unity 生命周期函数的 C# 脚本；明确不含 `UI/test` 测试目录下任何脚本，也未触碰清单以外的任何文件。
- 原则：纯排版重排，零逻辑改动。仅把 Unity 生命周期函数整体上移到“成员变量区之后、其他业务方法之前”，代码 token、注释、特性、条件编译块均不增删改；写盘前原文已镜像备份。

## 一、重排后统一的类内结构（从上到下）

1. `using` 引用（保持不动）；
2. 类型声明及类级注释；
3. 成员变量区：字段（含 `[SerializeField]`）、`const`、只读字段、`event`、委托、属性，保持原相对顺序；
4. 构造函数（本批 UI 文件未涉及）；
5. 生命周期函数区，固定时序：`Awake → OnEnable → Start → FixedUpdate → Update → LateUpdate → OnDisable → OnDestroy`（出现哪些排哪些；同名重载按方法名聚合、重载间保持原序）；
6. 其他业务方法，保持原相对顺序；
7. 嵌套类型移到类末尾（本批文件未涉及嵌套类型）。

识别规则：按“方法名”识别生命周期，同名但带参数/不同返回值的也算；接口中同名方法同样处理。每个成员连同其上方的特性、XML 文档注释、普通注释及 `#if...#endif` 条件编译块整体移动，不拆散。

## 二、文件清单与实际移动情况

路径均相对 `LYF_MMO/Assets/GM_Game/Scripts/UI/`。“实际移动”取自工具 `report.json` 的 `Moved` 字段；`NOCHANGE` 表示本就符合约定顺序、未写盘。

| 序号 | 相对路径 | 所含生命周期方法 | 实际移动（Moved） |
|---|---|---|---|
| 1 | CreateRole/Window/CreateRoleWindow.cs | Awake | 无（本就符合） |
| 2 | Loading/LoadingView.cs | OnEnable, Start | OnEnable, Start |
| 3 | Main/Window/ChatWindow.cs | Start, OnDestroy | 无（本就符合） |
| 4 | Main/Window/KnapsackWindow.cs | Awake, OnEnable, Update, OnDestroy | Awake, OnEnable, Update, OnDestroy |
| 5 | Main/Window/MinMapWindow.cs | Start, Update | 无（本就符合） |
| 6 | Main/Window/NpcShopWindow.cs | Update, OnDestroy | Update, OnDestroy |
| 7 | Main/Window/RoleAttriibuteWindow.cs | 1 个生命周期方法 | 无（NOCHANGE，本就符合，未写盘） |
| 8 | Main/Window/SkillInfoWindow.cs | Update | Update |
| 9 | Main/Window/Widget/BuyItemWidget.cs | Awake, OnDestroy | 无（本就符合） |
| 10 | Main/Window/Widget/EquipSlotWidget.cs | Awake | 无（本就符合） |
| 11 | Main/Window/Widget/GoodsItemWidget.cs | OnDestroy | OnDestroy |
| 12 | Main/Window/Widget/SkillSlotWidget.cs | OnDestroy | OnDestroy |
| 13 | Style/InputStyle01.cs | Start | 无（本就符合） |
| 14 | Style/UIDragWidget.cs | Start | 无（本就符合） |
| 15 | Style/UISmoothTransitionMatValue.cs | Awake, OnEnable, Update, OnDisable, OnDestroy | OnDestroy |
| 16 | Style/WindowTitleDragHandle.cs | Awake | 无（本就符合） |
| 17 | Tips/BuyDialog.cs | Awake, OnDestroy | Awake, OnDestroy |
| 18 | UIRoot.cs | Awake, Start, Update, OnDestroy | OnDestroy |

## 三、验证结果

### 1. 工具内置 5 项校验（全部通过才写盘）

- ① 代码 token 多重集合前后一致：通过（代码本体零增删改）；
- ② 注释 / 预处理指令 / 禁用文本多重集合一致：通过（注释与条件编译块零丢失）；
- ③ 语法错误诊断不新增：通过；
- ④ 各类型成员定义数量守恒：通过；
- ⑤ 重排后顺序符合约定：通过。

BOM 有无严格保持。原文已在写盘前镜像备份到 `_temp/lifecycle_run/B_ui/backup/`（共 17 个文件，对应 17 个 OK 文件；NOCHANGE 文件未写盘故无备份）。

### 2. 逐文件人工回读结论

对全部 17 个 `Status=OK` 文件逐一用 Read 回读，确认：

- 成员变量（字段/事件/属性/常量）均位于类最前；
- 生命周期函数紧随成员变量区，且按 `Awake→OnEnable→Start→Update→OnDisable→OnDestroy` 标准时序排列（出现哪些排哪些）；
- 特性（`[SerializeField]`、`[Header]`、`[DisallowMultipleComponent]` 等）与 XML/普通注释均跟随所属方法，未出现注释脱离方法或悬空；
- 无方法被截断，方法体完整闭合；
- 本批文件无嵌套类型需迁移。

### 3. 大括号 / 条件编译配对结论

对全部 18 个文件做了独立统计：每个文件大括号 `{` 与 `}` 数量差均为 0（配平）；本批文件不含 `#if/#endif` 条件编译块（统计均为 0），故不存在条件编译配对被破坏的风险。

### 4. 待用户确认

本次为纯排版改动，工具校验与人工回读均通过，但仍**需用户在 Unity Editor 中做一次编译确认**，确保工程无编译错误。

## 四、SKIPPED / FAILED / 需人工确认文件

- SKIPPED：0 个。
- FAILED：0 个。
- 需人工确认：无。`UISmoothTransitionMatValue.cs` 为第三方导入资源，自带 `namespace IndieImpulseAssets` 命名空间，本次仅按约定重排其方法顺序，未改动命名空间、逻辑与任何代码 token，保持原样语义。

## 五、本模块汇总

- 清单文件总数：18
- OK：17
- NOCHANGE：1（RoleAttriibuteWindow.cs，本就符合约定，未写盘）
- SKIPPED：0
- FAILED：0
