# 2026-10-12 场景管理恢复简单登录流程

## 追加：严格执行用户要求范围

用户进一步要求“让做什么就做什么，不写过多扩展”。在AGENTS.md、AIconfig/workflow.md、AIconfig/config.toml加入适用于主Agent及全部子Agent的通用范围约束：只完成用户本次要求与必需的最小适配，不自行扩功能、顺带重构无关模块、设计额外框架或未来需求。同步CURRENT_TASK.md与TEST_REPORT.md。三处规则静态核对通过，本次无业务修改、不重复编译；无未解决问题，后续直接遵守。

## 用户要求与设计

恢复原项目登录、加载场景、进入游戏的简单流程，删除无需求依据的校验和管理机制，并将该要求写入持久约束。真实主城加载被“业务根必须默认禁用”的人为门禁拒绝并卸载，不修改主城资源来迎合该门禁。

沿提交 51ca995 的 Init / LoadSceneMode / EveryUpdate 结构，只适配实际加载完成结果、可空加载界面和现有创角禁用业务根。分析见 `项目分析/SCENE_SIMPLE_LOGIN_FLOW.md`。

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/Manger/SceneMgr.cs`：由客户端开发子 Agent 恢复 YooAsset Single 流程，删除代次、请求队列、默认根状态门禁及手动卸载激活机制。
- `AGENTS.md`：新增简单业务保持简单、无事实依据不得添加多余校验或防御包装的规则。
- `AIconfig/config.toml`、`AIconfig/workflow.md`、`AIconfig/agent/unity_client.md`、`AIconfig/agent/code_reviewer.md`：同步开发和审查约束。
- `Docs/AI/PROJECT_CONTEXT.md`、`CURRENT_TASK.md`、`DECISIONS.md`、`TEST_REPORT.md`：更新当前交接与结果，覆盖历史复杂场景管理决策。
- `项目分析/SCENE_SIMPLE_LOGIN_FLOW.md`：记录实际故障、原参考与最小流程。

## 验证与交接

- 独立审查最终结论PASS，主审查最终报告及原生成功日志后通过验收。
- SceneMgr从本轮759行恢复为80行，只有原三个字段及Init/LoadSceneMode两方法；去掉代次、请求对象、队列、重试、手工卸载激活、默认根状态门禁、启动异常包装和额外加载状态。
- 末稿272真实GM_Game源Rebuild零错误、37既有警告；主复核272源码和268编译依赖SHA全部一致。五个调用方与冻结before保持一致，末稿SHA为`7a2215d41625c7865137b3a85e2fec640c58d776da88fbc3347f295eaf7a89e1`。
- 独立Unity2023.2原生执行28项通过：创角禁根启用、已启用主城正常进入、旧场景卸载/目标活动、provider提前捕获的SceneObject正确、回调和场景生命周期一次、加载进度/关闭、无Loading继续成功及失败结果不调成功回调。
- 测试运行真实SceneMgr与UniRx；Yoo入口薄映射Unity原生Single，场景为正式原生层级/启用值副本，外围脚本/外部Prefab依赖移除。DDOL和内置禁用UI使用探针，失败结果为薄边界模拟，未执行正式Global/UIRoot/服务器完整业务或真实热更包。首隔离夹具缺UnityEngine.UI依赖未执行，补真实DLL后通过；未因此给业务加包装。
- 项目规则差异静态检查通过。证据统一存放 `_temp/scene-simple-20261012/`，保留首失败和最终成功日志。不清理临时文件，不发布热更，不修改用户正在运行的安装包。
- 未发现本轮已复现问题尚未修复；正式安装包验收仍需按原流程生成并更新GM_Game热更DLL与配套资源，再验证真实登录进入主城。
