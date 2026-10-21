# RO 设置界面融合：独立审查

## 范围与验收依据

- 用户最终范围：功能可以先不写，只将 RO 设置界面集成本项目，并遵循本项目 UI 设计模式。
- 已读取协作四入口、`AGENTS.md`、`AIconfig/agent/code_reviewer.md`，只读核对 RO `SettingCtrl`、五份设置 Panel 声明与本项目 `MainView`、`UIBase`、`WindowBase`、`MainCtrl`、`WindowType`。
- RO 主面板通过四个 Handler 页签组织系统、个人、挂机、推送；推送在 RO 中受系统开放配置控制。本轮只参考页面结构与界面内容，不要求移植该系统开放业务。
- 本项目沿 `MainView` 注册 `WindowType`、`WindowBase` 负责展示交互、既有 `MainCtrl` 管理主城窗口；正式控件与引用保存在预制体，复用现有字体与视觉资源。

## 审查点

1. 正式主城入口可打开、关闭窗口，页签和滚动为界面必要交互。
2. 不另建 UI 框架、Lua 管理层或不必要控制器；不实现画质、帧率、音频、FPS 显示、持久化或协议业务。
3. 正式预制体脚本/字体/图像/子预制体引用有效，无新增 Missing Script；资源符合现有收集目录。
4. 新增源码符合中文具体注释、全局命名空间、强类型、无三目/foreach、布尔条件不超过三个的项目规则。
5. 既有用户修改、无关业务与场景保持；原生截图与编译结果区分当前正式界面和隔离样例边界。

## 当前证据

- 审查基线：`_temp/ro-settings-integration-20261020/review/before-sha.json`，4278 条文件 SHA，涵盖客户端业务源码、正式预制体/场景、项目设置、服务端及资源收集设置。
- 本轮审查员不操作 Unity MCP，不修改业务源码、正式资源或四份协作入口；开发独占 MCP，主 Agent 负责入口记录汇总。
- 初审时未冻结末稿，以下终审段为最终结论，初稿失败证据保持。

## 修改文件与未解决事项

- 审查员目前仅新增本日志；临时脚本与证据统一保留在 `_temp/ro-settings-integration-20261020/review/`。
- 初审阶段未取得末稿；终审交接已完成，见下文。

## 终审结论（审查员确认，主Agent记录）

- 通过，无剩余阻断问题。MainView/UIBase.windowDic/WindowBase/WindowType/MainCtrl沿原职责接入，SettingPanel只切页，未新增业务效果、PlayerPrefs、协议或分叉框架。
- 独立rootdiff确认旧3159对象保留，仅3授权字段变化；末次引用审计正式新增2313对象无悬空引用。转换10处TargetGraphic残指针未进入正式Root，5中间prefab/meta已归档，Root无其GUID依赖。
- 独立完整281源0错38既有警告。主补充PreviewScene末稿四页/滚动/关闭/重开及参数保持、0缺脚本、五图目检；首图白块、假账号/推送行处理完成。
- 验收仅限正式资源临时克隆，克隆调整原事件运行状态并显式生命周期，不能表述为正式Play/包点击通过。模板选框/挂机槽不是业务数据，设置效果按用户要求未接入。
- 修改清单、设计及证据统一见`项目分析/RO_SETTINGS_UI_INTEGRATION.md`和`项目修改日志/2026-10-20-RO-SETTINGS-UI-CHANGELOG.md`。审查员末期工具不可用后口头交接，主完成归档/原生验收/日志，未代改业务。
