# RO 技能角色快照隔离专项测试修改日志

## 修改文件

- `LYF_Server/Tests/SkillRoleSnapshotSmoke.cs`：新增不进入正式服务工程的角色技能列表隔离测试。
- `Docs/AI/TEST_REPORT.md`：追加本轮独立构建、协议验证、A/B 迟到序列和客户端静态复审记录。

## 修改内容与设计思路

测试构造同一 Gate/Unity 复合会话当前绑定角色 B 的场景，先提交角色 A 的迟到 11080，再验证角色 B 的有效列表。它使用生产处理器的角色归属判断，并直接检查 A 回包不会装载 B 的权威技能组件。有效 B 回包的组件装载单独验证，避免测试依赖真实 Gate 会话或数据库。额外检查 Protobuf 描述符中的 `role_id = 3`。

## 验证结果

- `protoc 3.21.9` 成功解析 `ResultEntity.proto`，描述文件位于 `_temp/skill-role-snapshot/ResultEntity.desc`。
- `MMoRpgCommon`、`Network`、`GameServer` 隔离构建 0 错误；`SkillRoleSnapshotSmoke.exe` 退出码 0，A 回包被拒、B 回包可装载。
- 客户端早到 11080 缓存、角色切换清理和无表现施法占用路径已静态复核；具体命令、输出及边界见 `Docs/AI/TEST_REPORT.md`。

## 未解决问题与下一步

真实四服务联网、同连接换角迟到回包和 Unity Play Mode 的技能表现/UI 时序尚未联调。正式场景验证时按角色 A 切换 B、A 回包延迟、B 回包提前、本地实体延迟创建的顺序复测。
