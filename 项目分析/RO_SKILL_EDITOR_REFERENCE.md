# RO 技能编辑器可见实现调查

## 范围与结论

参考工程为 `F:/UnityPro/ro/Ro_cs422nb`。检查了 `clientproj/Assets/Editor`、`config/Assets/Editor`、`clientcode/CSProject`、`rogamelibs` 的技能编辑相关源码和产物。本次仅分析，不修改 RO 工程或本游戏业务代码。

仓库保留了大量技能编辑配置、导出的运行配置，以及 C#/C++ 预览接入接口；**没有找到绘制技能编辑器窗口、时间轴交互、保存/导出操作的源码**。因此可以还原数据组织和预览接入链，不能断言窗口长相、菜单路径、拖拽方式、自动保存规则或具体导出函数。

## 三层数据

| 层 | 路径及例子 | 可确认的作用 |
| --- | --- | --- |
| 编辑辅助数据 | `config/Assets/Editor/EditorResources/SkillData/Affix/Affix_HuoYanZhiDi_RongYanPenShe.config` | XML 根 `MConfigData`。保存 `SkillName`、`SkillId`、`EffectId`、`Directory`、预览角色 `Player`、`isRole`、`IsMale`，以及 `ResultList`、`FxList`、`animListEx` 等编辑辅助字段。`Foldout` 是列表折叠状态；`Ratio`/`EndRatio` 保存相对时间。其具体 UI 控件没有源码可核对。 |
| 可读运行数据 | `config/Assets/Resources/SkillData/Affix/Affix_HuoYanZhiDi_RongYanPenShe.txt` | XML 根 `MSkillData`。保存 `Time`、`AnimationLength`、`SkillStateTime`、`ResultList`、`FxList`、`AnimList` 等运行字段；例如此技能总时长 10 秒，第二个特效 `At=2.33333349` 秒。 |
| 实际加载数据 | 同目录同名 `.bytes`；结构见 `config/AutoGen/protobuf/local/common/pbl_skill.proto` | `PbLocal.MSkillData` protobuf。`MSkillFactory.BuildSkillData` 从 `SkillData/{path}/{scriptName}.bytes` 解析并缓存，客户端运行时不读取上述 `.config`。 |

同名样本中，编辑配置第二个 `MFxDataExtra.Ratio=0.233333349`，运行数据第二个 `MSkillFxData.At=2.33333349`；与 10 秒总时长相乘吻合。这证明该样本的相对时间与运行秒数对应，但**不能仅凭产物确定编辑器完整的转换算法**。`timeLineDrawData` 在所查样本里为空节点，不能据此推断时间轴绘制算法。

另一个分段动画样本：`config/Assets/Editor/EditorResources/SkillData/Dungeon_JiFenDiXia/Monster_FengMoWuShi_LongJuanShu.config` 的 `animListEx` 有两个 `MSkillAnimationExtra.time` 值 2、1；同名运行 `.txt` 的 `AnimList` 有两个动画，`begin_time` 为 0、2 秒。这显示编辑辅助数据与运行时分段动画分别存放，不能把 `time` 直接当作每段的 `begin_time`。

## 表格和运行入口

`SkillTable.csv` 的 `SkillDataPath` 给出脚本目录，`SkillEffectTable.csv` 的 `SkillScriptPrefix`/`SkillScriptPostfix` 选择脚本及武器变体。`clientcode/CSProject/MoonClient/Skill/MSkillCore.cs` 的 `Init`/`InitWeaponDatas` 拼出路径，并调用 `MSkillFactory.BuildSkillData`。后者位于 `clientcode/CSProject/MoonClient/Skill/MSkillFactory.cs:67`，解析 `.bytes` 为 `PbLocal.MSkillData`。`pbl_skill.proto` 中 `MSkillData` 的重复字段包括 `ResultList`、`ChargeList`、`FxList`、`AudioList`、`AnimList`；各项的 `At` 或 `begin_time` 是运行时秒数。

客户端 `clientcode/CSProject/MoonClient/Skill/SkillLogic/MArtSkill.cs:259` 的 `FireEvents` 按 `At` 注册特效、位移、音效等计时回调，和编辑配置里的比率字段不是同一消费层。运行时表现细节参见 `项目分析/RO_SKILL_FX_ANIMATION_ROOT_MOTION_REFERENCE.md`。

## 编辑器预览接入链

可见接口指向“将编辑中的 protobuf 数据注入预览角色”这一流程：

1. `clientcode/CSProject/MoonClient/SwigTools/CppInterface/rogamelibs/ROGameProxy.cs:197` 暴露 `SkillEditorAttachSkill`；`rogamelibs/csproxy/ro_game_proxy.cpp:177` 转到实体的 `ROSkillComponent`。另有 `SetSkillEditor` 开关。
2. `rogamelibs/entity/component/skill_component.cpp:654` 的 `AttachSkillEditorSkill` 校验技能表与效果表，再用 `SkillReader::LoadSkill(script_name, data, data_len)` 读入传来的 protobuf 字节，随后交给 `ROSkillManager`。WIN32 编辑器模式下，已有技能或吟唱未完成时会拒绝重新附着。
3. `rogamelibs/table_manager/skill_data_reader.cpp:88` 按数据内 `uuid` 缓存 `ROSkillData`；`rogamelibs/entity/skill/skill_manager.cpp:312` 创建核心并调用 `ROSkillCore.SetSkillEditorSkillData`，后者位于 `rogamelibs/entity/skill/skill_core.cpp:174`，将该 UUID 作为预览核心的技能数据。组合技配置还会附着子技能。
4. C# 的 `MSkillCore.SetSkillEditorData`（`MSkillCore.cs:737`）也能用传入的 `PbLocal.MSkillData` 替换当前武器类型数据，但本仓库未找到调用它的编辑器窗口代码。

这些是**接入接口**，不是完整预览调用链：编辑器 UI 如何构造 `data`、何时调用接口、如何控制预览播放，目前无可见源码证据。

## 缺失边界与本项目参考点

`clientproj/Assets/Editor` 中有 28 个 C# 文件，但技能编辑窗口、`MConfigData`/`MResultDataExtra` 定义及其保存/导出代码均未找到；`config/Assets/Editor` 没有 C# 文件。`config/Assets/Editor/EditorResources/SkillEditorData/CopyConfig.txt` 为 0 字节，不能作为功能依据。原项目是否依赖未随仓库提供的独立工具或程序集，现有证据无法确认。

若后续为本项目建设同类工具，可以借鉴“编辑辅助数据与运行时 `MSkillData` 分离、以统一时间轴导出秒数、预览数据直接注入技能核心”的**已见数据边界**。具体窗口、导出器和编辑交互须依据本项目需求设计，不能称为照搬 RO 的已验证实现；现阶段不应据此改动现有表格和业务代码。

## 可确认的时间轴内容类别

根据pbl_skill.proto的MSkillData和MArtSkill.FireEvents，除FxList外，可确认的数据类别包括AnimList（分段动画）、AudioList（音效）、ChargeList（角色位移）、RotateList（转向）、WarningList（攻击预警）、ShakeList（震屏）、CameraFxData/CameraPostEffect（镜头效果）、ChangeColorList（模型变色）、SkillBarList（技能进度条），以及ResultList（命中/范围/击退/弹道数据）、MobUnitList（召唤）、AIEventList（AI事件）、BuffList（Buff生效时点）。后四项为协议数据类别，不能据此声称均由客户端MArtSkill.FireEvents直接派发。

弹道位于ResultList元素的LongAttackData，不能称为已确认的独立Projectile轨道。CombineSkillList是组合子技能配置；SingingFxList及吟唱音效、channellingData、EagleData、BarrowData等属于独立或特殊技能配置，不凭字段存在断言编辑器均显示独立轨道。编辑辅助.config中的ResultList/ChargeList/CameraShake/Audio/Mob/FxList/animListEx等提供类别旁证，但本仓库仍缺窗口源码，具体UI名称与轨道数量未知。
