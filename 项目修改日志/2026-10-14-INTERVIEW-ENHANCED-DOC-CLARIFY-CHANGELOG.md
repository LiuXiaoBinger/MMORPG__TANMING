# 2026-10-14 面试文档第二轮“澄清整理”：修改记录

## 一、任务背景

用户在增强版基础上反馈“有些东西还是不太明确”，要求继续整理同一个文件 `项目分析/项目难点与亮点-面试版-2026-10-14.md`，不新建文档。重点：四类 ID 是否混用、协议职责、预表现与权威校正顺序、位移生产消费与叠加、Mock 与正式数据修复关系、布尔重构数字边界、验证状态标注、重复大段与排版。

本轮仍为**纯文档轮**：不改业务代码/资源/配置/数据库，不启动 Unity、不构建、不启动服务、不连库。

## 二、先取证、后修改

先派两个只读核对子 Agent 重新对照源码（均带路径行号），再据实修改：

1. **技能链**：`SkillInfo.Id`（主表 10010…）、`SkillEffectInfo.Id`（效果行 2001001…，外键 SkillId+Level）、`SkillPresentationInfo.Id`（表现 3001001…，外键 EffectId+ProfessionId）、`SkillUuid`（`PlayerEntity.NextSkillUUID`，1~9999 循环 uint64）；`SkillCastReq/Ret/StartNtf/EndNtf` 全部 proto 字段；`ApplyAuthoritativeMp`（MP 权威校正）、`OnSkillCoreSync`（CD 校正）、`Cease(true)`（拒绝回滚）确认存在；`IsWaitingForServer` 标识符在本项目**不存在**。
2. **移动链**：本地输入/根运动/程序位移/重力合入一个 `frameMovement` 单次 `CharacterController.Move`；远端影子追赶直写 `transform.position` 不过碰撞、与碰撞移动在远端 Update 互斥；11180/11181 字段确认（均无速度字段），`MoveOperationHandler` 不做速度/路径/穿墙校验，11180 事件驱动收一包广播一次。

## 三、修改文件

| 文件 | 操作 | 说明 |
| --- | --- | --- |
| `项目分析/项目难点与亮点-面试版-2026-10-14.md` | 修改（同文件迭代） | 1493 → 1602 行 |
| `项目分析/README.md` | 更新描述 | 补充本轮新增的字段表/泳道表 |
| `Docs/AI/CURRENT_TASK.md` | 追加本轮条目 | 协作记录 |
| `Docs/AI/TEST_REPORT.md` | 追加本轮验收 | 文档验收记录 |
| `项目修改日志/2026-10-14-INTERVIEW-ENHANCED-DOC-CLARIFY-CHANGELOG.md` | 新建 | 本日志 |
| `_temp/interview-doc-parts/part*.md` | 同步修改 | 分片与主文件保持一致 |

## 四、本轮具体改动

### 1. 事实修正
- 删除不存在的标识符 `IsWaitingForServer`（术语表与参考工程表两处），改为“项目没有独立等待确认布尔，迟到结果按 SkillUuid 匹配后校正”。
- 协议表：11070 改为 `EnterWroldReq{role_id}` 且说明出生位置另由 11180 上报；11170 改为“同号双向，C→S 请求、S→C 仅回本人 SkillCastRet”；11171/11173 用途明确；**新增 11090**（主角进世界同步给其他玩家）。11090/11100 消息体字段标注“待确认”。
- 讲透一第七步按源码重写：CD 用 `cooldown_end-server_time` 差转秒、MP 调 `ApplyAuthoritativeMp(current_mp)`、拒绝执行 `Cease(true)` 并按快照校正，无单独“返还 MP”逻辑、错误码经 `GetCastErrorText`。
- 第三步补 `SkillUuid` 为 1~9999 循环、非永久全局唯一、不做长期防重。
- 第六步把“效果 ID/段号”落到 11171 的 `effect_id`、`using_times` 字段。
- 讲透四补边界：影子追赶直写 Transform 不碰撞、服务端不校验速度/穿墙、无速度字段、速度由 `距离/(time×1.1+0.03)` 自估。

### 2. 新增表（全部字段已对照 proto / 源码）
- 四类 ID 不要混表（主表/效果行/表现行/施法 UUID 的主键、例子、关联、生命周期）。
- 施法协议字段表：`SkillCastReq`/`SkillCastRet`/`SkillCastStartNtf`/`SkillCastEndNtf` 逐字段。
- 按一次 Q 的时序泳道表（7 步，客户端/网络包/服务器三列）+ 关键文件。
- 位移生产/消费表（本地输入/根运动/程序位移/重力/远端影子，生产者、消费入口、是否过碰撞、叠加规则）。
- 移动协议字段表：`MoveOperationReq`(11180)/`SyncMoveNtf`(11181) 逐字段。

### 3. 结构与可读性
- 每个大章节（二~十六，共 15 处）导航下新增 `> 💡 本章一句话：…`，说明本章解决什么问题。
- 高频追问合并 2 条重复的 Send 问答，新增“UUID 会不会重复”“服务器是否校验移动速度/穿墙”2 条。
- 排版修复：13 个被折成多行的快速导航恢复单行；142 处连续空行清零；emoji 样式块统一紧凑；亮点七被拆行的边界句合并。

### 4. 验证状态（未改动原有边界，仅确保标注一致）
- 待机滑步问题七保持“公共动画链两处修复、代码专项通过，正式双端停走画面待验”；目标协议/伤害/刷怪、role_skill 真人联机、布尔重构 Unity 运行时冒烟、正式资源包发布、Shader IPC/Burst 首次内部起因等仍明确标注未完成/待验证。

## 五、验证结果

主 Agent 独立复跑（PowerShell）：
- 页内链接 79 个，锚点失配 **0**；代码围栏 10 个成对闭合；连续空行 **0**；`##` 17 个；UTF-8 无 BOM；1602 行。
- `IsWaitingForServer` 出现 **0**；`本章一句话` 15 处；11090、UUID 循环、MP/CD 校正等新增内容已落位。
- 本轮新增引用的 7 个源文件相对路径（EntityInputEventBridge / MDisplacementComponent / MainCityMgr / MoveOperationHandler / ROMoveComponent / MModel / PlayerEntity）全部存在。
- 纯文档轮，未做编译/联机/打包；业务结论均来自源码只读核对或归档记录。

## 六、仍需用户确认 / 待确认事项

1. **11090/11100 消息体字段**：本轮只核实了方向与注册点，未逐字段展开 proto，文档已标注“待确认”；如需要可再补。
2. `NetSocketMgr.Client.SendData` 本项目封装内部是否返回 bool 未展开，文档以“调用处未判断返回值、靠 try/catch 异常路径”表述。
3. 影子追赶为 `距离/(time×1.1+0.03)` 自估速度的线性追赶，不是严格按 time 回放，也无 RTT 抖动缓冲（文档已如实写明）。
4. 业务侧未完成项（目标伤害/刷怪、移动物理权威、role_skill 真人联机、布尔重构运行时冒烟、正式包发布等）维持现状，需用户安排对应环境验证。

## 七、回滚方式

本轮在同一文件迭代；如需回退到上一版，删除本轮新增表格与句子即可，分片在 `_temp/interview-doc-parts/` 保留；无业务代码/资源/数据库改动。
