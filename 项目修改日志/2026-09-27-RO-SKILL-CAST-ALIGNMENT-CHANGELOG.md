# 2026-09-27 RO 技能施放链收敛

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/SkillCtrl.cs`：删除技能包来源标记、本地/联网双路径、单请求重试和迟到回包状态；快捷键按已学技能和动作状态校验后生成客户端序号，尝试发送 11170 并立即启动同序号预表现。11170 按序号应用权威 MP/CD 和服务器施法 ID；11171 对待确认的本地技能不按技能 ID 猜配，已确认实例按服务器 ID 去重，未预表现的施法由广播启动。
- `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/MainCtrl.cs`：移除没有调用方的强类型技能快照包装入口，仅保留测试与正式回包共用的 ByteString 解析入口。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MSkillComponent.cs`：移除正式业务中的离线施法模式和本地预测组 CD；保存未确认技能 ID 并由常驻组件更新回滚单技能预测。动画启动失败但请求已经发出时仍保留序号，以接纳权威结果；已确认实例和无动画施法增加结束通知缺失时的兜底释放。组 CD 只由 11170 设置，显示时按 RO 规则在单技能 CD 结束后读取组 CD。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillCore.cs`：未确认预表现按动画时长加 2 秒回滚预测 CD；无动画配置使用施法时长加 2 秒。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkill.cs`：已确认的动画实例按表现时长加 10 秒兜底结束，与 RO 技能生命周期一致。
- `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/Widget/SkillSlotWidget.cs`：复用原有冷却逐帧订阅，按槽位绑定技能 ID 从当前角色技能核心读取剩余 CD；角色切换或解绑时清遮罩，预测超时回滚后下一帧同步显示。

## 设计依据

- RO `MNavigationComponent` 在请求发送后立即触发本地预表现；`MSkillComponent.OnSkillStart` 用技能 UUID 匹配并确认现有实例，未命中时才由服务器开始通知触发表现。
- 本项目现有 11170 回传 `client_cast_seq` 和 `server_cast_id`，11171 带 `server_cast_id`；不改协议，由 11170 先精确绑定，再用 11171 去重。GameServer 当前先发送 11170 再广播 11171。
- `test.cs` 仍仅构造并通过正式 11080 解析入口装载全学技能包；按键统一由正式 `SkillCtrl` 处理，不保留 Mock 来源判断或第二套输入订阅。原技能表及已有配置未改。

## 验证结果

- `dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/ro-cast-client-build-final6 --verbosity quiet -clp:ErrorsOnly`：0 错误、38 条既有警告。
- 目标文件禁用语法扫描无 `namespace`、`foreach`、三目运算符、`dynamic` 或 `object[]` 命中；`git diff --check` 通过。
- 尚未在 Unity Game View 手动按键和真实四服联机复验，不能把独立构建当作动画实测。

## 未解决问题及交接

- 传输 API `SendData` 返回 `void` 且可能内部捕获发送异常，客户端无法取得 RO `Send` 的成功布尔值。为满足当前模拟角色按键体验，施法表现不以连接状态为前置；真实请求失败时由预表现时长加 2 秒回滚。
- `SendData` 的内部异常不可由技能控制器识别；出现这种底层失败时按 RO 的表现期限回滚，快捷栏从技能核心读取实时 CD，避免旧倒计时残留。
- Game View 需手动确认 Q/E/R/F/1 每次仅播放一次、移动阻断保持有效、动画时长后回 Idle、无回包时可再次施放；联机需检查 11170/11171/11173 顺序和 MP/CD 更新。

## 独立审查与最终交接

- 测试 Agent 对最终文件独立构建 `GM_Game`，产物位于 `_temp/ro-cast-review/final6`，结果 0 错误、38 条既有警告；目标静态规范和差异空白检查通过。审查 Agent 对 RO 施放时序、回包去重、无动画生命周期、预测过期及 UI 单技能/组 CD 显示复审通过。
- 手动 Game View 和真实四服联调尚未执行。11171 协议没有 RO 的请求 UUID，当前由服务端先送 11170 再送 11171 实现精确绑定；若极端乱序先到 11171，本地待预测实例会忽略该次开始通知，后续仍可由 11170 确认。
- 协作记录：`Docs/AI/CURRENT_TASK.md` 和 `Docs/AI/TEST_REPORT.md` 已登记文件范围、验收结果及下一步手动验证。
