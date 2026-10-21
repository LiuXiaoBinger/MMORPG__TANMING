# 2026-10-21 服务端SceneEntity目录分类

## 要求与范围

用户要求将`LYF_Server/GameServer/SceneEntity`下的文件创建目录分类。开发子Agent sceneentity_organize执行，主Agent独立核迁移与范围、维护协作记录。仅移动49个既有源文件，同步`LYF_Server/GameServer/GameServer.csproj`编译路径；不修改代码内容、命名空间、类名或业务行为。

## 分类与修改文件

下表目录均位于`LYF_Server/GameServer/SceneEntity/`，表内类型名对应同名`.cs`文件：

| 目录 | 数量 | 迁移文件 |
| --- | --- | --- |
| Base | 5 | ROObject、EntityComponentBase、EntityComponent、RuntimeEntityComponent、EntityComponentContainer |
| Entity | 15 | BossEntity、CollectionEntity、DummyEntity、EagleEntity、EliteEntity、MercenaryEntity、MirrorRoleEntity、MonsterEntity、NpcEntity、PetEntity、PlayerEntity、ProjectileEntity、Role、RoleEntity、TrapEntity |
| Component | 24 | AiComponent、AttributesComponent、BeHitComponent、BuffComponent、CollectionComponent、CombatComponent、DeadComponent、FightGroupComponent、HatredComponent、LifetimeComponent、MercenaryComponent、MirrorRoleComponent、MonsterComponent、MoveComponent、NavigationComponent、NpcComponent、ProjectileComponent、RoleComponent、SceneComponent、SkillComponent、SummonComponent、SyncComponent、TrapComponent、ViewComponent |
| Skill | 2 | ROSkill、ROSkillCore |
| World | 3 | EntityWorld、EntityFactory、SceneUidAllocator |

设计按既有职责区分基础设施、实体、业务组件、技能实例与世界管理，不另建框架或改变类型归属。业务组件统一放Component，ROSkill/ROSkillCore放Skill，便于区别技能组件与技能运行实例。

工程文件仅49处Compile Include替换为新目录，源次序及其余内容逐字保持。实际项目中未发现其他需要更新的文件路径引用；历史分析文件不批量改写。当前目录说明同步`Docs/AI/PROJECT_CONTEXT.md`，本任务记录同步`Docs/AI/CURRENT_TASK.md`和`Docs/AI/TEST_REPORT.md`。

文档修改清单为以上三份协作记录、本日志及`项目分析/项目难点.md`（本次实际构建隔离与恢复复盘）。验证脚本、构建配置和全部中间证据存入`_temp/sceneentity-organize-20261021`，不作为正式业务源码。

## 验证结果

- 主独立冻结274份服务端.cs/.csproj：49源迁移时SHA与迁移前全部相同；末次48份仍相同，NavigationComponent随后仅构造函数冒号后增加空格及换行，将base调用移到下一行。内存合回该处即精确匹配原SHA，来源未确定，保留当前文件。49旧路径消失，其余224输入保持。
- 目录实际文件与49条迁移映射一致，GameServer工程49条对应Compile无遗漏或重复；工程字节与仅执行49路径替换的预期完全一致。最终主审计八项通过，严格字节差异单独记录，无隐去排版差异。
- 真实GameServer及Common/Network依赖最终隔离编译通过，0错误、11条既有警告。最终输出位于`_temp/sceneentity-organize-20261021/dev-build/final`，构建日志为其上级`dev-build/msbuild.log`及binlog。源码仅后续排版变化，不重复构建；未启动服务。

## 构建误写与恢复

首次隔离构建因MSB3540未完成；第二次将同一临时文件用于早晚导入，晚导入被忽略，工程内OutputPath覆盖隔离设置，误写GameServer、Common、Network三个bin/Debug目录内共12份既有exe/dll/pdb。该操作不符合本项目中间产物约束，不能表述为全程仅写_temp。随后使用独立early.props和late.targets，将中间及最终输出按工程名隔离，最终构建成功且不再改变这12个目标。

误写前仅保存了SHA，未直接备份字节。后续搜索找到6份原SHA精确匹配的obj/Debug原文件，对应12个目标；先备份至original-output-backup，逐个确认目标仍为本次误写SHA后恢复。主Agent再次独立读取12目标，均与迁移前SHA完全一致；更宽489份基线排除49迁移源和1工程后，439份文件全部保持。误写副本、早期失败、恢复前后SHA及来源均保留于本轮_temp，未删除临时文件。后续排版差异不在这439份范围外文件中。

事故详情与复盘见`项目分析/项目难点.md`中“2026-10-21 旧式多工程构建隔离失效与原产物精确恢复”条目。

证据统一位于`_temp/sceneentity-organize-20261021`：root-baseline.json、root-GameServer.before.csproj、root-final-validation.json、dev-mapping-after.json、dev-final-report.json、dev-output-incident.json、dev-output-restore-results.json及dev-navigation-external-change-evidence.json。dev-output-incident保留恢复前现场，其FinalBuildSHA256不代表恢复后状态，恢复后以restore-results与主末次审计为准。目录移动只做内容保真和编译核验，不新增镜像业务测试。

## 未解决与交接

目录分类与编译验收完成，无剩余阻断。保留迁移后观察到的单处排版变更，未覆盖当前源码；12份误写二进制均已恢复原字节。本轮不改变上轮导航实际Unity/Player运行待验事项。临时证据不自动清理。
