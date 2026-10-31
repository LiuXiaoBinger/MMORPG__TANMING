# 附近邀请与主城退队可见性修复

- 任务编号：`TEAM-NEARBY-LEAVE-20261031`；业务执行与验证于 2026-10-31 完成，随后收到开发者明确“写入”授权补录本记录。
- 分支：`codex/feature-development`；业务基线 `f5ea272`；代码提交 `70bf5e8` 已成功推送，范围仅下表四个业务/测试文件。
- 状态：代码与隔离验收通过；真实双客户端登录后联调、截图及 Player 打包未验证。

## 需求与根因

用户双客户端现场：A、B 在主城互见，邀请附近的人页面却没有候选；A 建队再退队，B 看不到 A。

主城退队沿 `LeaveTeam → LeaveTeamInternal → ResetRoleInstance` 无条件广播 `RoleLeave`，随后改地图、实例和坐标并发回城通知。B 删除 A，但 A 本来就在主城，未真正重载场景，不再通过 EnterWorld 向 B 广播出现。附近邀请行原本隐藏，按钮仍依赖手输 ID，尚未接入候选数据。

普通场景主动离队现在仅清队伍关系并下发空队伍状态，保留地图、实例、位置和可见性；副本离队继续先向旧实例广播离开，再回主城并广播出现；掉线清理仍广播离开。候选数据复用服务器已经同步到 SceneWorld 的真实角色，不新增查询协议。RO 邀请页的行模板、固定控件路径和按角色 ID 邀请方式保留，业务在 Ctrl、展示在 Window。

## 逐项文件清单

| 操作 | 文件 | 具体变化 |
|---|---|---|
| 修改 | `LYF_Server/GameServer/manger/TeamManager.cs` | 按普通场景主动离队、副本离队和掉线处理可见性；抽取空队伍通知；副本回城补发主城出现。 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/controller/UIController/ThemeDungeonTeamCtrl.cs` | 打开页面或队伍状态改变时，从真实 SceneWorld 收集附近候选，过滤本人、同队、不同地图/实例、缺资料及超 100 米实体。 |
| 修改 | `LYF_MMO/Assets/GM_Game/Scripts/UI/Main/Window/ThemeDungeonTeamCreateWindow.cs` | 复用原邀请行、按需复制多行、写入昵称等级，每行绑定自身真实 RoleId；保留手输 ID 入口。 |
| 修改 | `LYF_Server/Tests/TeamAuthoritySmoke.cs` | 新增主城队长/成员/单人退队、副本退队、掉线五组帧级回归；负向断言先确认正向收件。 |
| 新增 | `项目修改日志/2026-10-31-附近邀请与主城退队可见性修复_CHANGELOG.md` | 本任务完整文件清单、执行身份、验证边界、Git 和用量证据。 |
| 追加 | `Docs/AI/CURRENT_TASK.en.md` | 英文续作事实、真实轮次、代码提交和待验范围。 |
| 追加 | `Docs/AI/CURRENT_TASK.md` | 同编号中文同步，由子 Agent 执行与增量检查。 |
| 追加 | `Docs/AI/TEST_REPORT.en.md` | 英文验证、失败修正、证据路径与覆盖边界。 |
| 追加 | `Docs/AI/TEST_REPORT.md` | 同编号中文同步，由子 Agent 执行与增量检查。 |
| 追加 | `项目分析/项目难点.md` | 队伍关系与场景可见性联动问题的独立复盘条目。 |
| 维护 | `Docs/AI/RECORD_INDEX.md` | 根据源记录机械刷新标题、行段与摘要。 |

无新增业务类型、协议、资源、场景或 `.meta`，无移动、删除。`RoleManager.cs` 与 `ThemeDungeonTeamCreate.prefab` 未修改：已有广播接口、序列化邀请行和 ScrollRect 足够。工作区其他历史改动保留，未混入本任务提交。

## 真实执行与返修

执行者为国内版 WorkBuddy 5.9.2.0，原生实际模型 `hy4-preview`，原生 session `1e4790ff-fc7c-4469-8482-a786b5b1d71e`，直连任务 `TEAM-NEARBY-LEAVE-20261031-HY4`。

1. 第 1 轮 `run-4a826143-0bee-4750-8a84-ac3714e1b4a2`：完成四文件业务实现与自检，交付真实文件、构建、帧级断言及编辑器隔离证据。
2. 第 2 轮 `run-384376be-c9cb-45a6-ab9a-065d68155e51`：仅为新增局部变量补中文用途注释；两文件词法 token 一致，主 Agent 确认四文件非注释内容一致，未重复运行行为测试。WorkBuddy 本轮明确未使用 Skill。

此前国际版 `deepseek-v4.1-flash` 返回 429/code6004 额度限制，原 session `df323eca-beff-41fc-a54a-840e40ff96d5`、attempt `run-3a5f998c-b47c-4842-8e19-78d7506fbd6c` 已取消，未领取执行业务、未修改业务文件，不计开发轮次。用户随后明确选择国内版 Hy4；“夜间免费”为用户偏好，未核验账户账单或余额。连接恢复和原生权限处理不计开发轮次，未改桥接器或任务库伪造回执。

## 验证、失败修正与证据

证据根目录：`_temp/team-nearby-leave-20261031/`。

| 检查 | 真实结果与边界 |
|---|---|
| 服务端、客户端构建 | WorkBuddy GameServer 与 GM_Game 定向构建均 0 错误；存在既有警告。Codex 后续正常 GameServer 输出构建 0 错误。 |
| 同套协议冒烟 | 基线 `checks=95 failures=6`，修复 `checks=95 failures=0`；Codex 独立重跑修复版 95/95 通过。 |
| 覆盖主城离队 | 队长、非队长、单人末人；地图/实例/坐标不变；观察者不收到 RoleLeave，仍接收出现与移动；队长转移和解散保留。 |
| 覆盖副本与掉线 | 副本离队向旧实例发离开、本人回城、主城观察者收到出现；真正掉线仍清理角色。 |
| 邀请行隔离 | WorkBuddy 验证 0/1/3 行与 EventSystem pointerClick，各行返回对应 RoleId；不是双客户端实际邀请成功证明。 |
| 候选收集补验 | Codex 实际调用 `CollectNearbyCandidates`，以真实类型的隔离 SceneWorld/PlayerEntity/RoleEntity 验证本人、同队、跨地图/实例、缺资料、100.01 米、离场过滤；附近及恰好 100 米保留。 |
| 临时夹具纠正 | Codex 最初的动态编译遇到同名 Global；改反射后纠正 Instance 字段/属性假设及 Int32 InstanceId 赋值类型，最终通过，未改业务代码。 |
| 构建夹具纠正 | MSBuild 曾选择旧 GameServer.exe；最终显式放入新构建，并确认修复运行目录 EXE SHA256 与新构建相同后执行断言。服务间 Session 使用非空 GameServer NetClient，避免未绑定玩家 KCP 的实时丢弃被误判。 |
| 注释与编辑器 | 第二轮两文件 token proof 一致；主 Agent 四文件非注释内容一致。Unity 最终错误 0，临时对象已移除，团队缓存恢复，未保存测试组件到正式资源。 |
| 服务启动 | 2026-10-31 02:07 UTC+8 观察：Center PID109956、Game PID106584、Gate PID88996、Login PID60296；10110/10120/10130/10140 监听，Gate→Game TCP Established。仅记录当时状态，不保证后续存活。 |

关键证据：`developer-report.md`、`file-manifest.json`、`smoke/smoke-baseline-utf8.log`、`smoke/smoke-fixed-utf8.log`、`root-smoke.log`、`root-filter-test.cs`、`root-filter-result.json`、`round2/tokenproof.py`、`root-deploy-build.log`、`started-servers.json`、`git-receipt.json`。完整原始输出仅保存在 `_temp`，不复制私密会话或凭据到正式记录。

未验证：真实双客户端重新登录后的邀请接受、主城退队互见与后续移动、实际截图、Player 构建/热更新包。客户端编辑器使用已编译业务程序集；本轮未声称打包客户端已更新。下一步为两端重登后现场复测。

## RO 对照与复盘

RO `TeamInviteCtrl.lua:SetPanelByInfo/ExportItem` 使用 `AchieveTpl`、`NameInfo/Name`、`Lv`、`BtnSmall_B`；`TeamMgr.GetInvitationIdListByType(3)` 来自原项目推荐协议。本项目只沿用行结构、按 RoleId 邀请与控制器职责，候选改用已有场景真实角色。完整困难复盘见 `项目分析/项目难点.md` 的 `TEAM-NEARBY-LEAVE-20261031`，不重复原项目框架。

## Git 与文档集成

- 业务代码：`70bf5e8`，已成功推送 `origin/codex/feature-development`，远端范围 `f5ea272..70bf5e8`。
- 本次批准的文档：只提交本任务新增段、新日志和相应索引；既有未提交文档差异保留，不合并 main，不强推。文档提交/推送结果由收尾回执补充。

## 用量与技能证据

业务阶段本地实验日志窗口：2026-10-31 00:23:39–02:16:48 UTC+8；输入 27,797,579（其中缓存读取 27,627,648），输出 60,244（含 reasoning_output_tokens 22,370）。来源 `task-usage.json` 的当前 Codex 会话 token_count 差值；不是账单或订阅剩余额度。分类的上下文与规则、需求、工具输入输出、外部协作、重复读取/返工/验证均不能精确拆分，WorkBuddy 用量与账户剩余额度不可测。

已执行 token-monitor collect/report，原始汇总见 `collect-delivery.txt`、`report-delivery.txt`；24 小时跨会话汇总不冒称本任务用量。当前对话累计压缩 3 次，截至 2026-10-31 02:17:02 UTC+8，见 `compactions-delivery.json`。本次文档补录另保留统计，不覆盖业务窗口。

主 Agent 业务阶段实际技能：project-subagent-coordination 派工与验收两轮；workbuddy-executor 国内版连接与真实执行；karpathy-guidelines 最小差异审查；unity-mcp-orchestrator 过滤补验与编辑器检查；task-log-policy 弹窗确认与索引维护；token-monitor 本地用量和对话压缩计数。当前补录使用 task-log-policy、project-subagent-coordination、token-monitor；同步子 Agent 的真实身份、技能与检查证据在回执中记录。


## 中文同步与范围检查回执

OpenAI Codex 原生子 Agent /root/sync_team_repair 完成两组中英增量同步和一致性检查，运行时未披露具体模型，本子任务未使用技能。两份英文精确等于原字节前缀加授权 suffix，两份中文保留原字节前缀；仅检查本任务新增段，未检查或翻译旧历史，结果通过且无差异。证据：_temp/team-nearby-leave-20261031/approved-records/chinese-sections.json、sync-check.json。暂存仅取本任务追加段和新日志；索引按暂存源记录生成，工作区其他历史改动保留。


## 文档提交与推送回执

批准的 7 个记录文件已提交为 `f895abf`，并成功推送 `origin/codex/feature-development`（`70bf5e8..f895abf`）。Git 暂存范围检查和空白检查通过，工作区索引 `--check` 通过；未提交其他既有文档改动。回执补记只追加本日志，提交号以 Git 历史定位，避免自引用。

文档补录轮本地实验日志窗口：2026-10-31 08:47:48–08:51:22 UTC+8；输入 1404319（缓存读取 1232512），输出 10666（含推理 2671）。仅当前主会话；规则、需求、工具、协作与重复读取/返工/验证的分类用量不能精确拆分，原生子 Agent 用量和账户余额不可测。已 collect/report，证据 `_temp/team-nearby-leave-20261031/approved-records/collect.txt`、`report.txt`、`usage.json`；本对话累计压缩 3 次，截至 2026-10-31 08:51:34 UTC+8，见同目录 `compactions.json`。本轮仅文档验证，无业务改动或游戏测试重跑。
