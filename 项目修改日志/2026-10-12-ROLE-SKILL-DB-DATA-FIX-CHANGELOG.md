# 2026-10-12 角色技能数据库数据补齐（仅数据，不改代码）

## 一、需求与范围

- 用户需求：项目测试模拟里有专门模拟“接收服务器角色技能回包”的测试代码，解析该回包数据，在 MySQL 中补齐角色技能相关数据，使角色真实登录后即可直接使用技能。
- 硬性范围：**只允许修改 MySQL 中角色技能相关数据**；不修改任何客户端/服务端代码、配置、协议、表结构、存储过程、触发器；测试代码只读。
- 经用户交互确认：对现有两个角色（角色 ID=1「789」、角色 ID=2「112」）**都补齐**。

## 二、测试回包文件与字段解析

- 测试文件：`LYF_MMO/Assets/GM_Game/Scripts/UI/test/test.cs`
- 模拟方法：`LoadMockLearnedSkills()`（test.cs L210-287），构造 11080 回包后经正式入口 `UIRoot.Instance.MainCtrl.LoadSkillResponse(payload)` 装载。
- 协议（`LYF_Server/ProtoBuf/proto/ResultEntity.proto` L167-180）：
  - `RoleSkillInfoRet { CmdCode cmd_code=1; repeated RoleSkillInfo roleSkillInfoList=2; int32 role_id=3; }`
  - `RoleSkillInfo { int32 skill_id=1; int32 level=2; string bind_key=3; }`
- 模拟取值：`RoleId=本地主角持久ID`、`CmdCode=Succeed`；遍历 Luban 技能主表 `SkillInfo` 全部 7 行，每行 `Level=1`；仅 6 个有正式战斗表现的技能给快捷键，其余 `bind_key=""`：

| skill_id | 名称 | Type | level | bind_key | 键位 |
| --- | --- | --- | --- | --- | --- |
| 10010 | 剑气攻击（普攻） | 1 | 1 | Q | Q |
| 10040 | 清风剑法 | 2 | 1 | E | E |
| 10050 | 千影剑决 | 2 | 1 | R | R |
| 10060 | 斩天剑 | 2 | 1 | 2 | 2 |
| 10070 | 无限剑决 | 2 | 1 | 1 | 1 |
| 10080 | 滑行 | 3 | 1 | F | F |
| 10090 | 轻功 | 4 | 1 | ""（空） | 无效果/表现，摆设行 |

- 客户端装载链：`MainCtrl.LoadSkillResponse` → `SkillCtrl.ApplyRoleSkillInfoPayload/ApplyRoleSkillInfo`：仅当 `level>0 且 bind_key 非空` 才写入快捷键字典 `_skillsByKey` 并上快捷栏；技能核心由 `MSkillFactory.CreateCore(skillId, level, JobId)` 创建，要求效果表存在 `(skillId, level)` 行且职业匹配。`bind_key` 为空的已学技能无法通过快捷键使用，且无“点击图标施法”旁路。
- 关键约束：效果表 `SkillEffectInfo` 这 6 个技能**只有 Level=1 行**，等级必须写 1（写 2 级以上反而 `SkillConfigMissing`）；角色职业必须为魔剑士 `JobID=1`。

## 三、服务端来源链路与 MySQL 映射

- 协议号：`CMD_RoleSkillInfoCode = 11080`（`Network/Socket/com/NetDefine.cs` L28）。
- 链路：进入世界 `EnterWorldHandler` → `RoleManager.HandleClientEnterWorld`（转发 CenterServer）→ `CenterServer/Ctrl/CenterRoleCtrl.cs OnEnterWroldHandle`（L63-78）→ `CenterServer/DB/Modle/CentRoleModel.cs RoleSkillInfo`（L101-143）查库组包 → 回传 GameServer `RoleSkillInfoHandler`（L32-74）→ `RoleSkillComponent.LoadLearnedSkills` 替换 `_learnedLevels` → GateServer 原样转发 Unity。
- 数据库（`CenterServer/DB/DBMgr.cs` L27）：`Server=localhost;Port=3308;DataBase=game_db;User=root`，SqlSugar/MySQL。
- 技能表：`game_db.role_skill`（模型 `CenterServer/DB/Table/RoleSkillTable.cs`，`[SugarTable("role_skill")]`），列为
  `Id`(PK,AUTO_INCREMENT)、`IsUnlocked`、`RoleID`、`SkillID`、`SkillLevel`、`SkillExp`、`Bindkey`(varchar255)、`CreateDate`、`UpdateDate`；引擎 InnoDB；`(RoleID,SkillID)` 无唯一约束。
- `bind_key` **持久化在 `Bindkey` 列**；组包时另有两处只改回包不写库的硬规范（CentRoleModel L121-133）：10080 回包键位强制 `"F"` 且 `Level<=0` 时回包强制为 1；10060 当库内键位为 `"F"`（忽略大小写）时回包改 `"2"`。
- 服务端权威施放 `RoleSkillComponent.TryCast`：`_learnedLevels[skillId]>0`（否则 `SkillNotLearned=28`）、效果表 `(skillId,level)` 精确命中（否则 `SkillConfigMissing=29`）、职业匹配且 `Type∈{1,2,3}`（Type4 轻功拒绝）、`TargetMode=0`、CD 已结束、`CurrentMp>=MpCost`。
- 故障根因：变更前两角色各 7 行 `role_skill` 全部 `SkillLevel=0`，登录回包等级为 0，服务端按 `SkillNotLearned` 拒绝（10080 仅因回包层兜底为 1 可用）。

## 四、变更前数据（只读探查）

- 账号表 `accout`：ID=1 用户名 aaaaaa；ID=2 用户名 weq。
- 角色表 `role`：

| Id | AccountID | Nickname | JobID | Level | ServerId |
| --- | --- | --- | --- | --- | --- |
| 1 | 2 | 789 | 1 | 1 | 1 |
| 2 | 1 | 112 | 1 | 1 | 1 |

- `role_skill`：两角色各 7 行，`SkillLevel` 全为 0；`Bindkey`：10010=Q、10080=F、10090=V，10040/10050/10060/10070 为空。无 `(RoleID,SkillID)` 重复行。
- `rolebasearrt`：两角色 `MaxHP/CurrHP=1000`、`MaxMP/CurrMP=500`，足够释放最贵技能 10070（耗蓝 180），无法力阻塞。

## 五、实际执行的数据变更（已提交）

仅对 `game_db.role_skill` 做 UPDATE，未 INSERT、未改其他表、未改表结构。事务内对 RoleID=1、2 执行：

```sql
-- 6 个可施放技能等级置 1（共影响 12 行）
UPDATE role_skill SET SkillLevel=1, UpdateDate=NOW()
WHERE RoleID IN (1,2) AND SkillID IN (10010,10040,10050,10060,10070,10080);

-- 补齐 4 个 Type2 技能快捷键（10010 已是 Q、10080 已是 F；共实际变更 8 行）
UPDATE role_skill SET Bindkey='E', UpdateDate=NOW() WHERE RoleID IN (1,2) AND SkillID=10040;
UPDATE role_skill SET Bindkey='R', UpdateDate=NOW() WHERE RoleID IN (1,2) AND SkillID=10050;
UPDATE role_skill SET Bindkey='2', UpdateDate=NOW() WHERE RoleID IN (1,2) AND SkillID=10060;
UPDATE role_skill SET Bindkey='1', UpdateDate=NOW() WHERE RoleID IN (1,2) AND SkillID=10070;
```

变更后两角色一致：

| SkillID | SkillLevel | Bindkey | SkillID | SkillLevel | Bindkey |
| --- | --- | --- | --- | --- | --- |
| 10010 | 1 | Q | 10060 | 1 | 2 |
| 10040 | 1 | E | 10070 | 1 | 1 |
| 10050 | 1 | R | 10080 | 1 | F |

- 10090 轻功**保持不动**（`SkillLevel=0, Bindkey='V'`）：效果表无其 Level=1 行、服务端 Type=4 直接拒绝、客户端物理按键集不含 V，属无法使用的摆设行，按“只补必需数据”原则不改。
- 未改 `IsUnlocked/SkillExp/CreateDate`，未新增/删除行。

## 六、备份与回滚

- 库内整表备份：`game_db.role_skill_bak_20261012`（`CREATE TABLE ... LIKE role_skill` 后整表复制，14 行，变更前全部 `SkillLevel=0`），确认备份行数与原表一致后才执行变更。
- 文件备份与脚本（目录 `_temp/skill-db-fix-20261012/`）：
  - `role_skill_original_20261012.csv`、`role_skill_original_20261012.sql`：变更前 14 行原始数据。
  - `rollback.sql`：依据备份表回滚（UPDATE ... JOIN 还原全部列）。
  - `01_verify_readonly.py`、`02_backup_and_fix.py`、`03_post_verify_packet.py`：只读复核、备份+事务变更、提交后回包复现脚本。
- 回滚方式（如需）：执行 `_temp/skill-db-fix-20261012/rollback.sql`，或
  `UPDATE role_skill r JOIN role_skill_bak_20261012 b ON r.Id=b.Id SET r.SkillLevel=b.SkillLevel, r.SkillExp=b.SkillExp, r.IsUnlocked=b.IsUnlocked, r.Bindkey=b.Bindkey, r.CreateDate=b.CreateDate, r.UpdateDate=b.UpdateDate;`
- 备份表 `role_skill_bak_20261012` 暂保留，确认游戏内正常后再由产品经理确认清理。

## 七、验证结果

- 事务内权威复核：两角色 6 个技能终态 `(level=1, bind_key)` 与目标完全一致、各角色仍 7 行无增删、10090 未变，方 COMMIT；等级 UPDATE 实际变更 12 行、快捷键 UPDATE 实际变更 8 行。
- 提交后新连接只读复核：备份表 14 行且全为 0 级；现表数据与预期一致。
- 回包级静态验证（`03_post_verify_packet.py` 严格复刻 `CentRoleModel.RoleSkillInfo` 组包与 `TryCast`/客户端上栏规则）：两角色登录 11080 回包均为
  10010/1/Q、10040/1/E、10050/1/R、10060/1/2、10070/1/1、10080/1/F、10090/0/V；
  6 个技能“已学习 + Level=1 效果命中 + 职业1 + Type 允许 + MP 充足 + 快捷键有效”全部判定为**可施放且上栏**；10090 如预期不可用。

## 八、未验证项与生效说明

- 未启动 Center/Login/Gate/Game 服务与 Unity 做真人联机登录（本次为仅数据修复，且按要求不改代码、不部署/重启正式服务）。回包内容已用服务端真实组包逻辑对真实库静态复现，但**最终以游戏内重新登录实测为准**。
- 生效条件：GameServer 运行期把已学等级缓存在内存 `_learnedLevels`，**角色需重新登录（重走进入世界 → 11080）**才会重新装载；若 GameServer 当前在线，需重登该角色。
- 本次未触碰代码/配置/表结构/其他业务表，无编译项；`rolebasearrt` 法力 500 满足现有 6 技能消耗，未修改任何非技能数据。
