# 技能后无法继续施放：后端只读现场核对

## 本轮范围

针对用户报告“所有技能出现下沉，且一次技能之后无法再施放”，后端职责只读检查正式进程的时钟、在线角色、技能动作与法力；模型高度由客户端职责调查。本轮不修改服务业务、不发送技能请求、不重启或部署、不写数据库。

## 已确认事实

- 当前正式 GameServer PID 76312，路径 `LYF_Server/GameServer/bin/Debug/GameServer.exe`；文件 SHA256 为 `99C5D7260D743FE53C406F5CFDC95FBFE4EC16049B8FABCA5EE9C9C3A551FAEB`，与前轮双账号调查中的部署文件一致。
- CLRMD 被动读取两份现场：LogicTick 从 92020 到 97767；UTC 14:53:52 至 14:58:39 约287秒对应5747帧，符合既有20Hz。不能沿用历史“逻辑时钟停滞”作为本轮原因。
- 在线角色1和2均 `RoleDataLoaded=True`、`SkillsLoaded=True`、HP1000、地图1、在线状态1；各自当前 `skill_` 都为空，无活动施法槽占用。
- 第二份快照两角色服务器权威 MP 都为0，MaxMP都为500。此为服务器状态，不能归因于客户端单独显示0或客户端响应覆盖。
- 只读数据库两角色均为 CurrMP500/MaxMP500；中心服 `LoginModle.OnStartGame` 现有代码从该字段原样装载。此结果不能重建当前在线生命周期最初收到的每一条回包，但不支持“数据库初始MP为0”。
- 正式 GameServer 源码全查，在线 CurrMp 唯一业务写入是 `ROSkillComponent.TryCast` 成功分支减去当前效果行 `MpCost`。部署程序集的 TryCast IL同样包含读取旧CurrMp、读取MpCost、sub、写CurrMp，再写结果CurrentMp。没有发现单次施法无条件清零法力的实现。
- 当前链没有法力自动恢复实现；扣费只属于本次在线生命周期。持久化仍500并不表示在线MP应自动恢复。

## 对本轮故障的结论

主Agent同步确认客户端技能动作已结束、CD归零，费用为20/80/100/120/180的攻击技能 `CanStart=False`，零费用10080可进入。结合服务器权威0/500，当前继续施放攻击被法力不足门禁拦截，已有证据不支持活动技能状态卡死。主通过正常入口请求10040时没有实际发起，不能称服务器返回拒绝；本轮没有获取当前连续扣费请求的完整序列，不能断言“第一次技能扣光500”。

暂不修改MP门禁、不擅自增加回蓝、不重置当前角色MP或数据库。若用户要求具体回蓝规则，应按明确需求另行实现；本轮模型下沉的修复不应绕过法力规则。

## 重复入口与UUID核对

- `GateSkillCtrl.OnServerCommand` 对一次入站请求只执行一次既有 `SendData`。GameServer 的技能协议仅注册一个 `SkillCastHandler`；处理器 `Handle` 内只有一次 `TryCast` 调用，部署 IL 同样只有一次调用。
- 两级网络队列是传递关系：总队列出队后只交给客户端消息队列，再出队执行一次注册处理器，并非分别执行两次技能业务。注册字典会拒绝同来源同协议重复登记；没有发现两条服务端请求入口同时扣费的实现证据。
- 现有 `CheckDoSkill` 在技能仍活动时要求新UUID与当前UUID不同，部署 IL含 `GetSkillUUID` 与不等比较。因此同UUID在当前动作期间再次到达，会在扣费前被拒绝。
- 当前没有已结束UUID的历史去重缓存；技能已结束后的旧UUID重放仍须经过学习、MP和CD等正常检查。不能把现有检查描述为全生命周期防重，也没有重复旧UUID导致本轮耗尽的现场证据，故不增加历史缓存或额外门禁。

## 证据与边界

- `_temp/skill-height-dip-20261013/server/live-state-1.json`、`live-state-2.json`：正式进程被动快照。
- `role-db-readonly.json`：只读SQL结果，无账号密码输出。
- `deployed-trycast-il.json`、`deployed-clock-il.json`：现有部署文件方法IL。
- `deployed-checkdoskill-il.json`、`deployed-casthandler-il.json`：当前UUID检查与单次处理器调用IL。
- `input-hashes.json`：部署与只读源码输入哈希。
- `game-console.txt`：传统控制台只有80×25缓冲，相关过滤结果为空；不以空结果证明没有历史异常或扣费。
- 首次运行诊断工具使用64位dotnet与32位目标不匹配，仅临时工具失败；改现有32位dotnet运行成功，未改变服务器。探针编译0错0警告，不等于服务工程本轮重编。

日志：`项目修改日志/2026-10-13-SKILL-STUCK-SERVER-READONLY-CHANGELOG.md`。四入口由主Agent统一维护。
