# 2026-09-27 Q 普攻接段节奏调整

参考结论见 `项目分析/RO_Q_COMBO_TEMPO_REFERENCE.md`。本日志记录本轮具体修改与验证。

## 服务端

- 修改文件：`LYF_Server/GameServer/Components/RoleSkillComponent.cs`。
- 修改内容：多动作技能使用选中表现段的正值 `SkillStateTimeMs` 作为服务器活动施法占用及 11170/11171 的 `CastDurationMs`。状态时间为零时沿用该段 `DurationMs`；单动作技能继续使用 `CastMs + RecoveryMs`。Q 的段数轮换、MP 20、CD 500 毫秒、协议与完整动画时长均未调整。
- 设计依据：RO 的 `SkillStateTime` 控制技能状态退出，独立于动画总时长。本项目 Q 三段各配置 700 毫秒，使第二次施法在该状态窗到期后可被服务器接受。
- 验证：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/combo-tempo-server-build/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/combo-tempo-server-build/obj/ --verbosity quiet -clp:ErrorsOnly`，首次 0 错误、11 条既有警告；注释补齐后增量复编 0 错误、7 条既有警告。目标文件禁用语法扫描无命中，`git diff --check` 通过。隔离产物保留于 `_temp/combo-tempo-server-build/`。
- 未解决问题：Unity Game View 手动连击画面与四服联调尚未执行；650/700 毫秒边界已在独立复验中通过。

## 客户端与表现配置

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillData.cs`、`MSkillCore.cs`、`MSkillFactory.cs`、`MSkill.cs`、`SceneEntity/Component/Action/MSkillComponent.cs`；`Luaban/MiniTemplate/Datas/#SkillPresentationInfo.xlsx`；生成物 `LYF_Server/Network/LubanManage/Codes/SkillPresentationInfo.cs`、`Tb/tbskillpresentationinfo.bytes`、`LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`。
- 修改内容：表现表增加 `SkillStateTimeMs`，Q 三段均按 RO 剑士普攻参考值配置为 700 毫秒，原 `DurationMs` 2167/1967/2500 毫秒保留；其他单段技能状态时间沿用原完整时长。核心选段时同时选状态时间和完整动画时长；单次表现分别保存两个时钟。多段技能以状态时钟退出动作槽，旧段实例仍按请求序号等待服务器确认和结束；单段技能继续按完整动画时长退出。
- 设计依据：RO 的 `MArtSkill.IsEnd` 使用 `SkillStateTime/TimeScale` 结束技能状态，与完整剪辑时长分开。未加入连击输入缓存、加速倍率或新协议。
- 验证：Luban 正式生成字段和 bytes；Network 构建 0 错误、0 警告；GM_Game 构建 0 错误、42 条既有警告。客户端插件 Network.dll 与本轮构建产物 SHA-256 同为 `33F5B3359A26C56910BDD5D8C8A00A43AA298DEEB67452F9D1BB5A3AA2617E3C`。目标代码无 `namespace`、`foreach`、三目或弱类型载荷命中，`git diff --check` 通过；临时生成与编译输出位于 `_temp/combo-tempo-luban/` 和 `_temp/combo-tempo-client-build/`。
- 未解决问题：MJS 原剪辑比 RO 剑士普攻剪辑长，700 毫秒状态退出会切回待机或接入下一段；实际挥击关键姿态是否在状态窗口内、视觉连贯和手感须在 Unity Game View 手动确认。

## 独立终审与交接

- 审查 Agent 对真实 `MSkill` 源码作 0.699/0.701 秒状态边界断言，并对正式 `RoleSkillComponent` 作 650/700 毫秒接段边界断言；三段轮换、失败/重传不推进、单动作技能不变和旧 11173 隔离通过。两端独立构建均 0 错误，无剩余阻断项。
- 实际 Game View 键盘与四服联机未运行，不将逻辑断言等同于挥击画面验收。用户需逐次按 Q 检查 Atk01→Atk02→Atk03 的关键姿态是否在 700ms 内出现、切段是否流畅、技能期间是否不能移动。需要调整节奏时仅修改 Q 的 `SkillStateTimeMs` 并让两端读取相同生成表，不改完整剪辑长度或倍速。
- 协作记录见 `Docs/AI/CURRENT_TASK.md` 和 `Docs/AI/TEST_REPORT.md`；临时构建与验证文件保留在根目录 `_temp`。
