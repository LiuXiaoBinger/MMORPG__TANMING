# 默认动作脚步点短路失效导致运行空引用

日期：2026-10-13。检查范围仅为客户端 `MDefaultActionComponent.OnUpdate`；技能特效收集链另行调查。

## 实际故障与原因

真实 Unity 编辑器日志重复出现 `MDefaultActionComponent.OnUpdate:296` 的 `NullReferenceException`，调用链为 `MActionComponent.StateUpdate → MStateMachine.UpdateSeconds → MComponentMgr.UpdateComponent`。用户确认编辑器与打包客户端均报错；本项已证实的堆栈来自编辑器，不把它冒称为打包客户端日志。

当前脚步点字段 `_footStepsTimes` 没有赋值入口，未接脚步配置的正常角色该字段为 null。布尔条件重构前，脚步点检查依次判断表非空、下标非负、下标不越界，再读取对应脚步点。重构后，`footTimesValid` 与 `crossedStepTime` 均提前求值；即使表为空，第二个局部变量也直接索引数组，因此移动更新持续抛异常。

原表达式证据保存在 `_temp/boolcond-refactor/review-orig-violations.txt`。真实编辑器错误摘录及本轮 before 保存于 `_temp/skill-fx-package-runtime-20261013/client/`。

## 最小修复

仅修改 `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/Action/MDefaultActionComponent.cs` 的 `OnUpdate` 与职责注释。脚步表和下标验证成功后才检查脚步时间，并保持 `_lastStepTimeIdx` 的正常更新；没有新增脚步配置、音效或空值门禁。

同一方法另两处从原条件抽出的具名变量也改变了求值顺序，现恢复原短路：移动且跑步才读取跑速；静止不读取移动动画信息。表达式均不超过三个判断项，原动画与脚步正常流程保持。

## 验证边界

本轮开发将全部 280 份真实 GM_Game 源编译到 `_temp`，零错误、38 条警告。`compiled-source-hashes.json` 冻结实际编译输入；`source-manifest.json` 证实 `OnUpdate` 修改范围之外内容完整一致。

独立行为测试与 Unity 现场复核由测试职责执行并交主 Agent 收口。本开发记录不宣布正式安装包已更新，不发布、重启客户端或保存正式场景。

修改日志：`项目修改日志/2026-10-13-DEFAULT-ACTION-RUNTIME-FIX-CHANGELOG.md`。
