# 代码注释完整性约束变更说明

## 变更日期

2026-09-15

## 修改文件

- `AGENTS.md`
- `AIconfig/config.toml`
- `AIconfig/workflow.md`
- `AIconfig/agent/unity_client.md`
- `AIconfig/agent/code_reviewer.md`
- `Docs/AI/PROJECT_CONTEXT.md`
- `Docs/AI/DECISIONS.md`
- `Docs/AI/CURRENT_TASK.md`
- `Docs/AI/TEST_REPORT.md`

## 实现内容

- 新增并统一“新增或修改业务代码必须写具体中文注释”的项目约束。
- 注释覆盖类型、接口、函数（含构造函数）、字段、属性、常量、事件、参数和局部变量。
- 注释必须说明实际用途、数据语义、单位/范围或生命周期，禁止只重复标识符名称。
- 将注释完整性加入客户端交付标准、代码审查质量门禁和工作流规则。

## 验证结果

- 已通过文本扫描确认入口约定、AI 配置、角色模板和持久协作记录均已登记该规则。
- `git diff --check` 未发现本次文档修改引入的空白错误；未修改业务代码，因此未执行编译。

## 未解决问题与交接

- 后续所有客户端、服务端和公共库代码修改都必须按此规则补齐注释。
- 代码审查和测试静态检查需要将注释完整性作为必检项。
