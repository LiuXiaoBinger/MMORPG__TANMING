# 实现决策

## [DECISION][协作][QWEN-MEM-20261023] 2026-10-23 本地轻量任务与双语成对版本（当前决策）

- 依据用户 2026-10-23 指令：Qwen3 8B 可承担轻量查找、注释、限定小改与面向开发人员的中文同步；仅一次尝试，连接/输出不完整/验收失败时查找交豆包、修改交 WorkBuddy（必要时豆包）；不反复返工，其尝试不消耗接收模型的各 7 轮额度。
- Qwen 是 Ollama 提供的外部模型，不是 Codex 原生 Agent 或已安装的自主文件执行器；调用方提供限定输入、执行审批工具动作并核对实际结果；复杂业务/协议/并发任务保留既定外部开发流程。
- 保留五份中文 `.md` 给开发人员，新增五份对应 `.en.md` 作为 Agent 默认读取；Codex 维护英文验收事实，其他子 Agent 只同步变更的中文记录；两版标签/ID、路径、会话/run 标识、真实轮数、失败与未验边界一致；记录未同步或失败时不得收尾。
- 英文为精简工作记忆，不预先全量翻译历史；中文历史完整保留；需要旧编号而英文缺段时，先委派限定翻译、记录英文事实再供 Codex 读取；不把旧“运行中/已完成”静默当作当前，不猜测乱码原文。
- 根 `AGENTS.md` 与 `AIconfig` 承载以上例外，优先于旧的默认豆包查找规则和旧全中文要求；用户回复/业务注释仍中文；语言本身不保证固定 token/费用节省。
- 不做玩法、资源、协议或网关重设计；分支 `codex/qwen-bilingual-memory`，基线 `446ff741867f5579b532b5079ecf1ef84ef99743`，main 不动；范围/证据/实际集成见 `项目修改日志/2026-10-23-QWEN-BILINGUAL-MEMORY-CHANGELOG.md`。

## [DECISION][协作][QWEN-LIGHT-20261023] Qwen 轻量模型精确标签（当前决策）

- 根据用户后续指令，轻量任务唯一使用 `qwen3:8b-light`；请求必须传完整标签，不得静默替换 `qwen3:8b` 或 `qwen3:8b-cpu`。
- 指定标签不可用时沿既有一次尝试规则转豆包或 WorkBuddy；不重复重试 Qwen，不无记录替换模型。

## [DECISION][任务][DS-012] 2026-10-23 RO 任务系统首阶段有界设计与 Task.prefab 损坏处置

> 中文对应同步；英文对应 `Docs/AI/DECISIONS.en.md` `[DS-012]`。仅同步协作记录，不改任何 Unity 资源、代码、配置、场景或 Git。

- 参考 RO 拥有广泛的任务缓存、目标工厂、场景/实体触发器与特殊任务变体层级；在出现已证实需求前，LYFMMORGP 不复制该层级。
- 首阶段限于剧情/支线任务，目标类型仅 `KillMonster`、`CollectItem`、`TalkNpc`；任务状态/进度/奖励由服务端权威；快照/upsert 与接取/放弃/领取请求；任务导航复用现有导航入口。每日/每周/活动/组队/虚拟任务及通用触发器脚本不在范围。
- UI 集成必须走现有 `MainView` 子窗口注册、`WindowType`、`WindowBase`、`ShowMainWindow` 流程；归档 RO prefab 仅作视觉参考，不引入 RO Lua UI 管理器、`MoonClient.dll` 或并行 UI 生命周期。
- 损坏处置：RO 原始 `Task.prefab` 因缺失 MoonClient GUID `0a06b724a7ecc1948b8fd5525e4102e6` 与 RO 脚本 GUID `f70555f144d8491a825f0804e09c671c` 损坏；仅重建 `LYF_MMO/Assets/artres/Resources/UI/Task/Prefabs/Task.prefab` 并保留 `.meta`，不改 `QuickTaskPanel`、`TaskNavigation`。
- 新预制体为 6 节点原生 UGUI 视觉壳，仅引用项目既有 Image/Text/Common.TTF，无 Lua、MoonClient、任务业务或按钮逻辑；静态检查 6 GameObject/RectTransform、5 CanvasRenderer/MonoBehaviour、两 RO GUID 零引用；Unity AssetImportWorker 导入成功且对应导入块无错误；场景/Canvas 实例化尚未验证。
- 证据与实施顺序：`项目分析/RO任务系统设计-2026-10-23.md`。本记录仅为设计/协作决策，不批准或实现代码/协议/schema 变更。分支 `codex/qwen-bilingual-memory` 不符合 `codex/feature-development`，存在无关脏改动，未提交/推送。
