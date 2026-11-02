# 状态互斥基类与虚函数约束

- 任务编号：`STATE-INTERFACE-VIRTUAL-20261102`。
- 日期：2026-11-02。分支：`codex/feature-development`。任务基线：`3e9a4c40c6ae865892df590e34ac04ae1e90ab6f`。
- 用户要求：`IStateInterface` 不使用 C# `interface`；以后业务代码默认通过基类和虚函数扩展，特殊情况才用接口；核对 `Check(OnlineRole role)` 的子类与 RO 宏定义。
- 本任务正式记录清单已通过交互弹窗明确获准。授权不覆盖此前未批准的服务器移植日志或已拒绝的分区整理日志。

## 实际文件清单

| 状态 | 项目根相对路径 | 具体变化 |
| --- | --- | --- |
| 修改 | `LYF_Server/GameServer/StateMutex/State/IStateInterface.cs` | 保留 RO 类名与文件路径，改为抽象基类；构造保存只读状态编号；GetType 非虚返回编号；Check/Leave 虚函数默认 false；GetErrorCode 抽象函数对应 RO 纯虚。 |
| 修改 | `LYF_Server/Tests/StateMutexSmoke.cs` | ProbeState 通过构造传递编号，override 三个业务函数；增加只重写错误码的测试子类和基类契约用例，保留原状态切换、异步与释放用例。 |
| 修改 | `AGENTS.md` | 增加基类与 virtual/override 优先约束；接口例外须说明具体必要性，不批量替换既有接口。仅暂存本次新增规则，保留既有用户改动。 |
| 新增 | `项目修改日志/2026-11-02-状态互斥基类与虚函数约束_CHANGELOG.md` | 本任务唯一完整修改记录、文件清单、失败与验收边界及 Git 回执。 |
| 追加 | `Docs/AI/CURRENT_TASK.en.md` | 仅新增本编号英文变更与续作边界。 |
| 追加 | `Docs/AI/CURRENT_TASK.md` | 子 Agent 仅同步本编号中文变更。 |
| 追加 | `Docs/AI/TEST_REPORT.en.md` | 仅新增本编号真实编译、测试与未验边界。 |
| 追加 | `Docs/AI/TEST_REPORT.md` | 子 Agent 仅同步本编号中文验证事实。 |
| 机械更新 | `Docs/AI/RECORD_INDEX.md` | 根据本次实际暂存的英文记录生成对应定位目录；工作区目录保留其他尚未提交记录。 |

未新增生产子类、资源、.meta、协议、配置、项目 Compile 项；未移动或删除文件。工作区原有大量其他任务修改保留，不能宣称工作区干净。临时报告与构建仅放 `_temp/state-interface-virtual-20261102/`，不提交。

## RO 对应与真实边界

RO 根为只读 `F:/UnityPro/ro/Ro_cs422nb`。

1. `rogamelibs/state_mutex/state/i_state_interface.h:10-25` 本身是 C++ 抽象基类，不是 C# 接口。构造保存 type_，GetType 非虚；Check/Leave 有默认 false；GetErrorCode 是纯虚。
2. `rogamelibs/state_mutex/state/climb_collect_fish_state.h:13/33` 声明 ClimbActionState、CollectionState 等真实子类；同名 .cpp:11/48/67/109 实现其 Check。`state_mutex_manager.cpp:29` 的 Init 逐项 RegisterState(NEW_OBJECT(...))。`state_mutex_def.h:7` 的 STATE_MUTEX_MGR 宏只取得管理器，不生成 Check 子类；头文件保护宏也不生成业务实现。
3. 本项目管理器生产注册表仍为空，修改前只有 ProbeState 测试实现；本次新 DefaultBehaviourState 也仅用于测试。不能把“虚函数基类已改好”表述为“生产采集、攀爬、载具等状态已经移植”。没有为不存在的玩法注册永远 false 的空壳，没有把 RequestSinging 伪造为 RO 不注册的源状态。
4. Check 的职责是判断角色是否处于某个源状态；管理器再结合目标列和互斥表判断能否切换。现有动作状态/吟唱业务未改动。
5. C# 必要适配：GetType 用 new 隐藏 object.GetType，保留 RO 函数名与返回状态编号语义；抽象基类 protected 构造由子类调用。未引入额外管理层。

## 真实执行身份、失败与修正

- Codex 主 Agent：限定范围、核对 RO 源码、审查实际差异、机械落盘经核对的 Qwen 补丁、最终编译测试、正式记录与 Git 集成。
- 国内版 WorkBuddy 原生 `deepseek-v4.1-flash`，任务 `state-interface-virtual-domestic-20261102`，会话 `46168831-5453-4dcb-aacc-3d540babbcb5`，第 1 轮 `run-c94f33ce-4ce9-4ffc-95f9-51a5bb0b7bcb`，`runtimeSource=desktop_queue`。真实模型通过本机 native session 数据确认。
- WorkBuddy 完成了基类、测试和规则大部分修改，先编译与烟测通过，随后为了测试把 GetType 改成返回无效 kStateMaxCount，尚未恢复便于 18:00 收到原生 429，提示 2026-11-03 01:02:21 UTC+8 恢复。此时间仅是当次错误提示，不是账户余额或当前可用承诺。
- 原生 inspect 确认 idle 后，通过官方取消接口保留回执。回执 state=cancelled、cancellationRequested=true、remoteStopped=false；停止证据是原生 incomplete 429 与 idle，不把取消请求当作停止证明。未冒称 WorkBuddy 完成交付或验收。
- 第二顺位豆包 CDP9225 不可用；Computer Use 两次窗口读取均返回 `window id 67484 no longer belongs to Doubao.ChatApp; current owner is Doubao.ChatApp`。未能确认原有工作状态，因此没有强杀，也没有派发豆包业务轮次。
- 本地 Ollama 原服务未运行，启动既有安装并确认 `qwen3:8b-light` 可用。该模型一次限定文本修改返回 `GetType -> type_` 补丁；Codex核对唯一文件和唯一语义变化后机械落盘。Qwen没有自主文件工具，也未编译或运行；不能将主 Agent 的验证算作其执行。
- 原始证据：`_temp/state-interface-virtual-20261102/native-failure.json`、`cancel-round1.json`、`qwen-request.json`、`qwen-response.json`、`qwen-patch-applied.json`、`scope-review.json`。WorkBuddy 1 轮被限流中断；Qwen代码补丁1次。文档同步是独立轻量任务，单独记录其实际结果。

## 验证

最终当前源码的实际命令：

```powershell
dotnet build LYF_Server/GameServer/GameServer.csproj -t:Rebuild -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/state-interface-virtual-20261102/gameserver-out/ -v:minimal
dotnet build _temp/state-interface-virtual-20261102/tests/StateMutexSmoke/StateMutexSmoke.csproj -t:Rebuild -v:minimal
& _temp/state-interface-virtual-20261102/tests/bin/StateMutexSmoke.exe
```

- GameServer：退出码 0，14 警告、0 错误。测试工程：退出码 0，0 警告、0 错误。测试执行：退出码 0，`StateMutexSmoke: all cases passed`。
- 测试直接编译 `LYF_Server/Tests/StateMutexSmoke.cs` 并引用本次新构建的生产程序集；覆盖构造固定编号、基类默认 false、override 派发及原有表格、空生产注册、禁止/直接/先退旧态、退出失败、取消、销毁与回调抛异常后的事件释放。
- 测试输出含 SkillCastingRules 找不到时的现有默认策略提示；本次只验证互斥基类与现有烟测，不能据此声称完整施法配置接入已验。
- WorkBuddy 最初直接写旧生产 bin 输出遇到既有服务器占用，4 错误、38 警告；改为本任务 _temp 输出后成功。其早期烟测通过发生在后续临时错误之前，不作为最终验证。最终验收在 Qwen 修正之后重新编译与运行。
- 本任务未启动四服，未停止用户原已运行的服务器；未做实网、Unity执行或干净检出验收。现有工作区状态表与管理器的其他用户改动作为测试环境保留，未混入本次提交。
- 日志：`_temp/state-interface-virtual-20261102/codex-build-GameServer.log`、`codex-build-StateMutexSmoke.log`、`codex-StateMutexSmoke.log`。变更范围检查证明正式源代码/规则仅本任务三个文件产生新增差异。

## 双语同步、技能与用量

英文入口仅追加本编号。独立的 Qwen 文档同步调用（输入1259、输出1203 token）未按约定返回 records 结构，且 differences 与 passed 自相矛盾；主 Agent 拒绝落盘，没有将它自报通过当作验收。随后将获批的两段文档同步交给 Codex 文档子 Agent state_virtual_records（实际模型未披露），仅追加中文并做增量一致性检查。该子 Agent 发现英文没有明写第1轮，主 Agent 按其反馈补齐英文 round 1，再交其核对。未全文检查或翻译历史。最终检查证据在收尾追加。

实际使用技能：project-subagent-coordination（限定派工、失败转交与验收）、workbuddy-executor（原生任务与取消真实回执）、karpathy-guidelines（保留最小继承改动及未移植边界）、computer-use（确认豆包窗口失败，未操作业务界面）、task-log-policy（获准记录与索引）、token-monitor（collect/report及当前对话压缩统计）。WorkBuddy因中断未回报技能使用情况；Qwen代码补丁回报未使用技能。

本任务 Qwen代码补丁可观测输入1282、输出533 token；外部 WorkBuddy token未披露。Codex分类token/账户剩余额度的不可测边界及最终统计见收尾附录；原始工具报告留_temp，不提交源码或私密会话内容。

## Git 回执

待本任务增量记录完成后，主 Agent 仅暂存本任务三个源/规则文件的变化和上述获准记录，提交并正常推送到 `codex/feature-development`。不合并 main、不强推、不包含已有用户修改。实际提交与推送回执在完成后追加。

## 最终记录检查与用量快照

- Codex 文档子 Agent `/root/state_virtual_records` 已完成两个中文增量，9类字段检查通过、剩余差异0；原有字节/换行保持，英文轮数与工作区测试措辞两处问题已按其反馈修正。主 Agent仅检查实际范围和报告输入哈希，未重复逐项中英审计。证据：`_temp/state-interface-virtual-20261102/codex-record-sync-check.json`。子 Agent 回报未使用技能。
- token-monitor 已执行 collect/report；任务窗口 2026-11-02T09:43:55.996Z 至 2026-11-02T10:21:52.910Z（UTC）。Codex原始计数差：输入~8876773、其中缓存~8728320、新输入~148453、输出~33079（已含推理~8281）。这是重复请求累计，不是唯一上下文长度或账单；Codex适配器为实验性，保留~。
- 分类：上下文与项目规则、用户需求、工具调用输入输出、重复读取/返工/压缩/验证，各自token数均未记录/不可测。协作：Qwen代码调用1282输入/533输出，文档调用1259输入/1203输出（未通过，未落盘）；WorkBuddy和独立Codex文档子对话用量未记录/不可测。豆包未派发业务任务，其token未记录/不可测。账户剩余额度本地日志不可测。
- 上下文压缩：当前对话累计 3 次，截止 2026-11-02T10:22:37.929Z（UTC），来源同一对话两份原始Codex日志顶层compacted事件，按时间与载荷哈希去重，损坏行0。不合并子Agent或WorkBuddy对话。统计不含该截止时间之后的Git回执和最终回复。
- 用量与压缩来源明细：`_temp/state-interface-virtual-20261102/token-and-context-final.json`、`token-collect.txt`、`token-report.json`、`compactions-current-part.json`。日志仅保留汇总数字，不复制私密会话正文。
