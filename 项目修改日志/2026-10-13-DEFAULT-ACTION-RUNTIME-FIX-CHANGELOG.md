# 默认动作运行空引用修复

日期：2026-10-13。需求：排查编辑器和打包客户端运行报错。职责：客户端开发执行业务修复，测试独立验收，主 Agent 协调并更新协作入口。

## 修改文件

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs`：仅 `OnUpdate` 及职责注释，恢复脚步点、动画就绪、跑速检查的原短路求值。
- `项目分析/DEFAULT_ACTION_SHORT_CIRCUIT_RUNTIME_FIX.md`：记录实际故障证据、原因、最小修复和测试边界。
- 本修改日志；`Docs/AI/CURRENT_TASK.md` 与 `Docs/AI/TEST_REPORT.md` 由主 Agent 统一收口。

## 修改内容与设计思路

真实编辑器 `OnUpdate:296` 堆栈对应 `_footStepsTimes` 为 null 时直接读取数组。此前布尔重构将脚步表有效性与脚步时间判断抽成两个均提前计算的 bool，破坏原短路。现用嵌套分支，验证表与下标之后才读取数组，同时保留动画进度更新；不添加音效功能、配置或防御框架。

同方法移动/跑速、移动/动画检查按重构前真实条件恢复短路，静止不求值后续属性。每个布尔表达式最多三个判断项，没有新增状态、接口、字段、重试、异常吞掉或包装。用户既有其他修改全部保留；本輪前后内容比对确认 `OnUpdate` 范围外一致。

## 验证结果

- 全部 280 份真实 GM_Game 源 Rebuild：零错误、38 条警告。输出目录仅 `_temp/skill-fx-package-runtime-20261013/client/compile-GM_Game`。
- 本轮源文件前后 SHA、差异和编译输入 SHA 保存在 `_temp/skill-fx-package-runtime-20261013/client/`；末稿 SHA256 为 `a8e19fbc854a5a612ef93362d1c5d9a0f73ec7be7648da0bff242773a9679971`。
- `source-manifest.json` 的 `outside_onupdate_body_same=true`；本轮未新增正式源码或修改服务器、技能资源、协议、表、场景。
- 独立行为回归与 Unity 现场结果待测试 Agent 提供，最终以主 Agent 收口记录为准。

## 未解决与交接

用户表示打包客户端也报错；本项真实堆栈来自编辑器，不能据此宣称安装包错误已经消除。需要将修复编译到当前热更代码/安装包，再按正式入口回归。技能特效打包链另由资源开发职责调查。本轮没有自动部署或重启，没有清理 `_temp`。

原因分析：`项目分析/DEFAULT_ACTION_SHORT_CIRCUIT_RUNTIME_FIX.md`。
