# RO设置界面融合：独立测试

日期：2026-10-20。需求仅为移植RO设置界面，并按现有View、WindowBase、WindowType及MainCtrl接入；不实现设置项效果。

## 范围与方法

- 本角色只读正式客户端源码、资源和配置，不改正式源码、资源，不操作Unity MCP；开发临时夹具和输出仅在`_temp/ro-settings-integration-20261020/test/`。
- `compile_client.py`保留前轮已核插件与程序集引用，动态枚举实际业务源码，排除独立测试asmdef和Editor目录，避免新增窗口漏编；全部中间产物写入本轮test子目录。
- `audit_references.py`独立解析Unity YAML，检查五个设置预制体和UIRoot新增对象的脚本、精灵、纹理、字体GUID及本地对象引用，比较既有UIRoot对象、核主Agent冻结保护输入SHA。该检查不代替Unity原生资源加载及屏幕画面验收。

## 验证流水

- 环境准备第一次构建因临时项目XML声明`utf8`不被MSBuild识别失败；修夹具为`utf-8`。第二次动态枚举误包含独立PlayMode测试asmdef，缺UnityTest属性失败；修夹具按实际程序集边界排除。两次失败原日志分别保留在`test/baseline/`、`test/baseline-encoding-fixed/`，没有因此改正式项目。
- 当前源码独立Rebuild通过：281源、0错误、38警告，证据`test/baseline-production/`。此时已含SettingPanel新增源，但正式UIRoot组装尚在进行，不作为最终交互验收。
- 首次引用核查发现来源SettingAuto两处、SettingPushNotification八处`m_TargetGraphic`引用指向已删除组件，已反馈客户端最小清理；当时UIRoot新增2313对象无悬空引用。首份失败证据保留`test/reference-audit-initial.json`，不因后续通过删除。
- 最终281源Rebuild通过，0错误、38既有警告，证据`_temp/ro-settings-integration-20261020/test/final`；末稿源码SHA由主复核。最终引用审计通过，新增2313对象无缺失，旧3159对象保留，10冻结输入不变。前述失败证据保留。

## 验收边界与交接

本角色不构建或发布正式Player，不写仿制业务实现测试。原生入口打开、关闭、分页、滚动、视觉与画质/垂直同步/目标帧率/音量不变，交由主Agent协调现场验收。`Docs/AI/CURRENT_TASK.md`及`TEST_REPORT.md`由主Agent统一汇总本日志，避免多角色覆盖。

## 主Agent补充最终原生验收

- 工具能力丢失后主接管Unity调用与归档，测试角色没有代报原生通过。正式设置子树克隆在PreviewScene中沿原持久事件验证四页、系统页滚动到底、关闭active=False、重开默认系统页，0缺脚本；画质5/vSync1/目标帧率-1/音量1保持。五末图目检通过布局/中文/页签图标，静态勾图及挂机槽数字不是业务数据。
- PreviewScene缺Play生命周期，最终只对克隆调整监听EditorAndRuntime并显式OnEnable；首轮未触发与非Play运行场景失败响应保留，不为测试切换Play或改业务。入口只核静态接线及编译，未验正式Play主城点击、Player包或设置效果。
- 5转换中间prefab/meta归档`_temp/ro-settings-integration-20261020/client/converted-prefabs`，正式Root无依赖，初稿10悬空引用留作失败证据。末态Boot非Play、dirty=false，预览枚举仅余编辑器CustomLightsScene，无本轮克隆；Console0错误/警告。未发布。
- 汇总`_temp/ro-settings-integration-20261020/root-final-validation.json`，专题`项目分析/RO_SETTINGS_UI_INTEGRATION.md`，根日志`项目修改日志/2026-10-20-RO-SETTINGS-UI-CHANGELOG.md`。后续正常加载UIRoot后进行正式端点击验收。
