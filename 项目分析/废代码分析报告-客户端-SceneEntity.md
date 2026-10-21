# 废代码分析报告 —— 客户端 SceneEntity 分片

日期：2026-10-12。范围：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity`（128 个 .cs）。
本报告**只记录不删除**任何代码；所有结论均经过全局引用核查。

## 一、分析范围与方法

- 分片根目录：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity`，含 3DModel、Builder、Component（Action/base/State）、Entity、event/eventclassArg、Skill 及根目录实体基类，共 128 个 .cs。
- 扫描方法：
  1. 用 Python 静态脚本（`_temp/scan_sceneentity.py`、`_temp/scan_annot_gap.py`、`_temp/scan_unused_using.py`）粗扫类/成员注释缺口、疑似空方法、连续注释代码块、TODO、Obsolete、疑似未使用 using。
  2. 对每一个可疑成员，使用 Grep 在整个 `LYF_MMO/Assets`（含 Editor、UI、Prefab、artres）全局搜索引用，跨文件确认，不以单文件内直觉下结论。
  3. 逐文件人工阅读 Builder、Entity、event、eventclassArg、Component/Action、Component/State、Component 各业务组件、根基类与 Skill 数据类。
- 严禁误判项（按任务要求已排除）：`[SerializeField]` 私有字段、MonoBehaviour 生命周期方法、public 类/方法、接口实现与 abstract/virtual/override 成员、反射/序列化/ProtoBuf/ScriptableObject 类型、UI 消息方法、`#if UNITY_EDITOR`/`#if LIBS_EDITOR`/`#if WINDOWS && DEBUG` 内代码。

## 二、统计汇总

| 废代码类别 | 数量 | 说明 |
| --- | --- | --- |
| 零引用的空类（可删除） | 1 | `MAttrComponent` |
| 零外部引用的遗留类型（可删除） | 1 | `MSkillPrediction`（重构后运行链已不创建） |
| 空壳死方法（调用点与方法体均被注释） | 1 | `MActionComponent.TakeBattleVehilce` |
| 被注释掉的大段代码块 | 7 处文件 | 详见第三节 |
| 空方法/空 override（模板钩子，保留） | 多 | 基类 virtual 扩展点，非废代码 |
| TODO/FIXME（确认仍有意义，保留） | 2 | `MActionComponent.SyncPredicted`、`MMoveComponent` 男女区分 |
| Obsolete | 0 | 无 |
| 重复定义 | 0 | 无 |
| 不可达代码（return/throw 后） | 0 | 未发现 |
| 恒真/恒假死分支 | 0 | 未发现 |
| 正式脚本残留测试/Mock | 0 | 未发现（测试入口均在 `Scripts/UI/test`） |
| 疑似未使用 using（低置信，编译器为准） | 若干 | 见第四节，多为误报 |

## 三、确凿废代码条目（建议删除，仅记录）

### 1. MAttrComponent —— 空类，全工程零引用
- 文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MAttrComponent.cs`
- 行号：1–4（整个文件仅 `public class MAttrComponent { }`，无任何成员）。
- 判定依据：Grep `MAttrComponent` 在整个 `LYF_MMO/Assets` 仅命中本类声明 1 处；无任何 `CreateComponent<MAttrComponent>`、字段、变量或配置引用。
- 处理建议：**可删除**（连同 .meta）。该类无成员、无引用，属历史占位残留。

### 2. MSkillPrediction —— 重构后运行链已不创建，全工程仅自引用
- 文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillPrediction.cs`
- 行号：1–34（`public sealed class MSkillPrediction`）。
- 判定依据：Grep `MSkillPrediction` 在整个 `LYF_MMO/Assets` 仅命中本文件 2 处（类声明 + 构造函数），无 `new MSkillPrediction`、无 `GetSkillPrediction` 工厂借还、无字段持有。与 `项目分析/RO_SKILL_CD_LIFETIME.md` 末节一致：用户已明确"取消额外确认等待"，Core 不再维护 `_predictions`，该类型已无运行链创建点。
- 处理建议：**可删除**（连同 .meta 及 .csproj 编译项；删除前需同步从 GM_Game.asmdef 所在工程编译清单移除）。

### 3. TakeBattleVehilce —— 空壳方法，唯一调用点与方法体均被注释
- 文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MActionComponent.cs`
- 行号：方法定义 436–441（方法体 438–440 全部位于 `/* */` 块注释内）；唯一调用点 119–122 同样位于块注释内。
- 判定依据：Grep `TakeBattleVehilce`、`BattleVehicleID`、`MBattleVehicleTemporaryHiding` 在整个 `LYF_MMO/Assets` 仅命中本文件这几处；无子类 override，无其他调用。
- 处理建议：**可删除**该空壳方法连同其注释块；当前它不产生任何行为。

## 四、被注释掉的代码块（记录，建议清理时一并移除）

| 文件（相对项目根） | 行号 | 内容摘要 | 判定依据 | 建议 |
| --- | --- | --- | --- | --- |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs` | 235–242 | 7 行 `//RegisterEvent(...MEvent_EnterWall/CarryItem/RideVehicle/...)` | 所引用的 `_onEnterWall`、`_onCarryItemEvent`、`_onPrepairAnimEvent`、`_onReloadBattleRideAnimation` 在本类内**无字段声明**，仅存在于注释中 | 可删除 |
| 同上 | 250–257 | 对应 7 行 `//RemoveEventListener(...)` | 同上，反注册侧同样失效 | 可删除 |
| 同上 | 294–303 | `/*if (((MRole)Entity).AttrRole.IsMale){...}else{...}*/` 男女分动画 | 已被 305 行起按职业表现表的新逻辑取代 | 可删除 |
| 同上 | 331 | `/* 声音组件尚未接入，脚步音效后续按角色表现配置绑定。 */` | 说明性块注释，非可执行代码 | 保留（说明意图） |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs` | 334 | `// _skillAfterToken = MTimerMgr.singleton.SetTimer(tt, _onSkillAfterEnd);` | Grep `_skillAfterToken`/`_onSkillAfterEnd` 仅命中此行注释，字段/回调均不存在 | 可删除 |
| 同上 | 249–250 | `/*!Entity.IsRidePubVehicle &&*/` 块注释片段 | 旧条件残留 | 可删除 |
| 同上 | 371 | `//throw new System.NotImplementedException();` 在空方法 `killTimer()` 内 | `killTimer()` 被 303 行调用但方法体为空，仅一行注释 | 记录：空占位方法，见第五节 |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MObject.cs` | 176–181 | `/*if (LifecycleState != Active){return;} _components.Update(...);*/` | 已被 182–185 新逻辑取代的旧 Update 守卫 | 可删除 |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/EntityAttributesComponent.cs` | 37–43 | `/*if(Entity.IsRideVehicle){return VehicleSpeed;}*/` 载具分支 | `VehicleOrRunSpeed` 现直接返回 `RunSpeed` | 可删除 |
| 同上 | 59–62 | `/*if(Entity.IsEagle){return 1;}*/` 鹰分支 | `RunAtorSpeed` 现固定返回 1 | 可删除 |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MActionComponent.cs` | 119–122 | `/*if (Entity.BattleVehicleID > 0){ TakeBattleVehilce(); }*/` | 调用点本身被注释，见第三节第 3 条 | 随方法一并删除 |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/3DModel/MModel.cs` | 178 | `//Move();` 单行注释 | `Update` 末尾旧调用残留 | 可删除 |
| `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/IEventArg.cs` | 87 | `// MDebug.singleton.AddErrorLogF("Duplicate Recycle args...")` | 重复回收告警日志被注释 | 保留（调试日志关闭态） |

## 五、空方法 / 空 override（甄别后保留，非废代码）

以下方法体为空或仅占位，经核查均为**基类 virtual 钩子或有意空实现**，不是死代码：

- `EntityComponentBase` 的 `Initialize/Update/Shutdown/LateUpdate/Attached/EventSubscribe/EventUnsubscribe`：组件生命周期虚方法，子类按需 override；空基类实现是模板方法扩展点。
- 各业务组件（MBeHit/MDead/MStun/MBorn/MClimb/MCollect/MDance/MFearRun/MFishing/MSinging/MSpecial/MDefaultAction 等）的空 `EventSubscribe/EventUnsubscribe`：这些组件改用 `RegisterActionEvents` 注册动作事件，空 override 显式关闭基类事件订阅，属正常设计。
- `MActionComponent` 的 `OnStart/OnUpdate/OnFinished/OnStop/AfterFinish` 空 virtual：动作生命周期钩子，子类按需覆写。
- `MDefaultActionComponent.killTimer()`（369–372）：方法体为空（仅一行注释掉的 throw），被 303 行 `killTimer()` 调用。**当前为无操作占位**，后摇定时器逻辑已移除；建议后续随后摇机制清理，本轮按规则不删。

## 六、TODO / 待办（仍有意义，保留并记录）

| 文件 | 行号 | 内容 |
| --- | --- | --- |
| `Component/Action/MActionComponent.cs` | 36 | `SyncPredicted` 摘要内 TODO：后续接入 syncMode 配置。 |
| `Component/MMoveComponent.cs` | 304 | `//todo 这里应该区分男女 暂时不区分`：按职业表现表选移动动画，男女动画区分待做。 |

## 七、疑似未使用 using（低置信，仅记录，未改动）

脚本粗检命中若干 using，经人工核对多数为误报（`MMoRpgCommon.SceneEntity` 被 `EntityType`/`EntityComponentType` 等类型使用）。以下为**需编译器进一步确认**的低置信候选，本轮未删除任何 using：

- `3DModel/SoundComponent.cs` 的 `YooAsset`
- `3DModel/AsyncObj/MAnimationMgr.cs` 的 `System.Globalization`
- `Component/State/ActionStateKey.cs` 的 `System`
- `Skill/` 下若干数据类（SkillCastingRulesConfig/RuntimeAudioEvent/RuntimeFxEvent 等）的 `UnityEngine`/`System`

说明：C# 未使用 using 需以 Roslyn/Unity 编译警告为准；本报告不据此下删除结论。

## 八、结论

- 确凿可删除的废代码共 **3 处**：`MAttrComponent`（空类零引用）、`MSkillPrediction`（重构遗留零引用）、`TakeBattleVehilce`（空壳死方法）。
- 被注释掉的代码块共 **12 处文件片段**，多为历史骑乘/载具/男女动画分支残留，建议后续清理。
- 无不可达代码、无恒真/恒假死分支、无 Obsolete、无重复定义、无正式脚本残留测试/Mock。
- 大量空方法为组件生命周期模板钩子，**不得误删**。
- 本报告为只读分析，未删除任何代码；删除需另行安排并跑编译回归。
