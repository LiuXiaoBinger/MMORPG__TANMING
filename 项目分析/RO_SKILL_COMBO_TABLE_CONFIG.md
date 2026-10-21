# RO MSkillComboTableConfig 用途与调用关系

## 核心职责

`MSkillComboTableConfig` 是客户端连招配置查询单例。读取 `SkillComboTable` 和 `FirstComboSkillTable`，判断前一个技能与当前技能能否形成连招，取得实际技能效果，并供技能组件、导航发包、技能栏与AI查询。此类不执行动画、伤害或自动连续施放。

参考文件：`F:/UnityPro/ro/Ro_cs422nb/clientcode/CSProject/MoonClient/Table/Config/MSkillComboTableConfig.cs`。

## 匹配过程

- Init以Combo三个整数建立索引：前技能pre_skill_id、当前skill_id、配置level；同技能对的level排序，查询从高到低尝试满足条件的条目。这里level用于选择配置档位，实际技能等级在取效果时另行查询。
- PlayerGetSkillComboTableID以MPlayerInfo.ComboPreSkillID和本次技能匹配，开启时间检查；UtcMillis减ComboTime不能超过EffectiveTime乘1000，表单位秒、同步时间为毫秒。
- ActiveCondition由Lua SkillComboActiveCondition脚本判断，失败继续尝试下一档；匹配成功返回SkillComboTable行ID，失败返回0。
- ComboPreSkillID和ComboTime由process_ptcg2c_skillcombo更新，重连同步也恢复这两项；该类读取连招状态，不自行推进状态。

## 替换与允许释放

- GetComboReplaceSkillID读取ReplaceSkillId（0时采用当前原技能），按原技能对应的技能等级与Buff等级最大值，或变身技能等级，取SkillTable.EffectIDs[level-1]。返回的是SkillEffectTable效果ID，不是主表技能ID；方法命名容易误解。
- TryGetSkillComboID有匹配时返回替换效果ID，无匹配时返回skillInfo.currentSkillEffectId。
- CanSkillRelease有匹配配置则允许；普通非连招技能也允许。属于连招技能但未匹配时，必须在FirstComboSkillTable配置可起手条件；没有条目则拒绝，有条目且无条件函数则允许，配置函数时由FirstComboSkillCondition Lua判断。
- TryUseSkillCombo/Only名字中的Use是资格查询，不在这里发包；Only还要求本身属于连招技能集合。

## 实际消费者

- MNavigationComponent.OnAfterAction查连招、替换实际skillCore，用其执行CanSkill和本地预表现；请求SkillId仍取originSkillCore经Buff还原的原始技能ID。
- MSkillComponent.CanSkill也查替换核心以作当前施法条件判断。
- MSkillClassicsHandler和MSkillDoubleDiskHandler按实际效果查询图标/技能信息、按钮遮罩与连招提示；IsShowComboEffect还检查IsUseEffect。
- MAISkillControl用TryUseSkillComboOnly判断连招队列技能当前是否可用。

## 示例与边界

假设配置前技能A、当前B、有效时间2秒、替换技能C：服务器同步前技能A及时间后，玩家2秒内按B且条件满足，就查到连招行，使用C在原B等级对应的效果做表现；请求仍沿原技能身份。超时或条件失败则无连招匹配，B能否作为起手还看FirstComboSkillTable。此为逻辑示例，不代表实际表内已有这些配置。

本轮只读源码核对，没有修改参考项目或LYF客户端/服务端业务，没有实施连招移植。修改记录：项目修改日志/2026-10-13-RO-SKILL-COMBO-ANALYSIS-CHANGELOG.md。
