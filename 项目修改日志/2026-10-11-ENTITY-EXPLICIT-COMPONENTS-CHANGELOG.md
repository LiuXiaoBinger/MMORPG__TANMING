# 实体显式组件初始化精简

- 状态：已完成；独立审查及测试均通过，主已读取两报告并核对末稿输入。
- 用户指出位移组件已经显式挂接，当前没有附加组件登记，AppendExtraComponents(this) 不起实际作用。
- 业务范围仅 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/MEntity.cs`：删除 OnBeforeInitialize 内该调用，更新对应中文函数说明；保留显式 CreateComponent 与 base.OnBeforeInitialize。
- 设计：当前需要的位移组件直接在实体声明和创建，普通容器继续负责初始化与后续生命周期；移除未使用的附加装配调用。MComponentMgr 的既有登记接口不在本轮修改范围。
- 分工：displacement_entity_develop 修改业务；displacement_entity_review 独立核依赖、编译与已有实体生命周期回归；主负责范围、验收与正式记录。
- 保护：保用户所有前置修改、当前作者F13/end111/F49、Runtime、场景、资源、meta及DLL；不重新导出或扩大实现。
- 验收计划：确认客户端 Assets/Packages 没有附加组件登记调用；核初始化仍挂接和初始化显式位移；271源客户端编译及已有实体运行回归，验证同一末稿输入。结果待独立报告，不以旧189充数。
- 临时证据统一 `_temp/entity-explicit-components-20261011/`，保留上轮证据，不清理。交接与结果见 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。

## 最终验收与交接

- 实际仅MEntity一份业务源码变化，精确删除调用及替换summary两项差分；位移CreateComponent、base.OnBeforeInitialize和原Builder显式组件装配保持。全客户端Assets/Packages只有RegisterExtraComponents/AppendExtraComponents定义，无实际调用。真实MObject.Initialize仍先虚调用装配、后初始化普通容器并进入Active。
- 独立本轮271真实GM_Game源码Rebuild零错误、37既有警告；直接复制已有夹具重新执行189断言全部通过（63运动、92技能生命周期、34真实实体组件），未新增镜像测试。45完整生产输入含真实实体/管理器/容器/基类与技能运动链，Unity原生、状态机和表资源仍薄边界。
- 独立2796保护路径2795不变，唯一MEntity授权变化，无新增、删除或越界。共享枚举、两DLL、其它运动源码和用户作者F13/end111/F49、Runtime/meta/场景/资源保持。主重核开发1末稿、独立1末稿、271编译及45运行输入共318项SHA零差异，root-manifest-check.json=PASS。
- 证据在review下independent-review.md、independent-test-report.md、final-audit.json、final-diff.txt、registration-final-search.txt、runtime-final.log/45输入和GM_Game-build.log/271输入；develop保before与末稿manifest/差分。审计夹具首轮误假设CRLF，仅改_temp为真实LF，精确字节检查通过，非生产返工；原历史证据保留。
- 无已复现代码阻断。本轮未重新构建共享DLL或运行原生导出，未验真实Unity画面、墙体碰撞、Animator末帧/跨窗、听感、服务器及Bundle。下一步原正式入口体验，不继续扩大测试或自动发布资产；临时证据待产品经理确认后清理。
