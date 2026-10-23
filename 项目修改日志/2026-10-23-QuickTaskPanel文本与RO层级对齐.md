# QuickTaskPanel 文本与 RO 层级对齐（2026-10-23）

## 需求与范围

用户要求按截图补齐 QuickTaskPanel 五类文本：任务标题、任务目标及当前/目标进度、任务或目标描述、截止时间或日期、任务扩展文本；节点命名与层级须与 RO 一致。只处理显示资源，不编写任务业务或恢复已清除的 RO 自定义脚本。

分支 `codex/ui-script-cleanup`，基线 `e9f9a1b`。目标 `LYF_MMO/Assets/artres/Resources/UI/Prefabs/QuickTaskPanel.prefab` 已有用户修改，清理前实际磁盘备份在 `_temp/quicktask-text-20261023/before/`；Unity 中该 prefab 正打开，首查 `dirty=false`，不覆盖未保存现场。

## 定位与执行身份

- 豆包 `38448885557691394` / `59493309331198466`：本地项目/目录参数核对正确，但只接收消息，无工具、交付或权限待办。等待后沿原会话确认停止，回执 `cancelled`、`stopped:true`，不计有效查找轮次。
- WorkBuddy Hub `hub-bc16cc63-ee30-4867-9ef6-8e7c04db3e69`，真实会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`。第 1 轮只读定位 `run-c9b20c06-fa94-4d66-8aca-9778e93f8aed`，Codex 核对 RO 组件原文和 Lua612–624行后成立；第 2 轮资源实现 `run-4aef2446-cf2a-4445-a493-a2f68eca3369`，已按独立验证证据 accepted。两轮分别是定位、实施，不是两轮失败。报告在 `_temp/quicktask-text-20261023/lookup.md`、`result.md`、`result.json`。

## 文件清单与验证交接

- 修改 `LYF_MMO/Assets/artres/Resources/UI/Prefabs/QuickTaskPanel.prefab`：只给原 `TaskTpl/Title/TitleText`、`Des/DesText`、`Deadline` 自身、`Limit/LimitText` 增加四个原生 Text 组件和四条组件列表引用；分别恢复标题、描述、截止时间、扩展文本。现有黄色目标 `Target/TargetText1..5` 全部不变。
- 对应 `.meta`：仅核对保留，不改 GUID。
- 修改 `Docs/AI/CURRENT_TASK.en.md`、`Docs/AI/CURRENT_TASK.md`：新增同编号 `[QUICKTASK-TEXT-20261023]` 的任务事实与交接，保留历史和其他任务改动。
- 修改 `Docs/AI/TEST_REPORT.en.md`、`Docs/AI/TEST_REPORT.md`：新增同编号的资源、Unity 与视觉验证证据及边界。
- 新增本日志 `项目修改日志/2026-10-23-QuickTaskPanel文本与RO层级对齐.md`：逐项变化、外部身份、验证与 Git 回执。无正式文件移动或删除；临时脚本、报告、备份与截图只存于 `_temp/quicktask-text-20261023/`，不提交或清理。

## 最小适配与验收

五类节点原本都存在，缺少的是此前清理掉的四个扩展 Text。直接保留原节点命名、父子、顺序、active 和 RectTransform，只迁移 RO 原生 Text 字段，不恢复 MoonClient、自定义超链接或省略号字段。Title 字号16、BestFit1/Min14/Max16、MiddleLeft；其他三个字号15、BestFit0、UpperLeft；沿用现有 Common.TTF 和白色，目标黄色保持。用户启用的 TargetText2 不关闭。

Codex 独立 `verify.mjs` 通过：文档243→247，Text12→16；去除四个新增文档和四行引用后，与本轮实际备份逐字节相同；meta相同、无悬挂组件、原生字段与 RO 实文档逐项一致。SHA256 `d46af5362a807248f3535fcab1713ea72d0881872a4b1d338d1ab898b8ddf555` → `cf1669113840623f18d8156bf758fc0c3e0cdee19f192f35885771ac8de03789`。

真实 Unity 同步导入与读取通过：57个节点、Missing Script=0、16个 Text，五类路径都绑定正确文案、Common字体、字号、对齐和颜色。编辑开始前 PrefabStage 为 clean，导入时已无 Stage；未丢弃未保存现场。独立临时预览场景渲染 `after-preview.png` 已目视，五类文本可见。最初 SceneView 图为空，未算通过证据；第一次临时渲染清理 RT 时产生一条 camera.targetTexture 日志，临时代码解除目标后再销毁，复跑未增加日志。临时预览场景已关闭，未保存正式场景。

不声称任务玩法、交互或客户端/服务端全量构建验收。本轮不恢复参考项目其他自定义组件；没有未解决的文本资源问题。双语同步与精确提交推送进行中；只暂存本轮新增组件和编号段，不混入 prefab 中用户既有布局/Unity重序列化或其他任务改动。

## 双语同步与集成

本轮 `qwen3:8b-light` 同步仅实际尝试一次，45014ms 后超时，无可用输出或项目文件落盘，未重试。转原 WorkBuddy Hub 第 3 轮 `run-9a61eb55-53c2-4801-8c34-1927b11dbf35`，只新增两份中文同编号段。Codex 定向对照英文核对路径、字段、身份、轮数、结果与未验证边界；并将中英文 PrefabStage 句子统一明确为“编辑前检查 clean、导入时无 Stage”，消除原措辞歧义。去除本轮新段后历史内容保持，不重译或改写历史。第 3 轮是记录同步，不是资源返工失败。

提交范围仅上述 6 个正式文件；prefab 从 HEAD 构造仅四组件/四引用的索引内容，中英文入口仅集成本轮编号段。用户已有重序列化、布局以及其他任务改动留在工作区；`.meta` 不变，无临时文件入库。提交与推送实际回执在执行后补记于本节。

索引范围校验通过：6文件、prefab仅新增140行、删除0行；四个中英文入口各只新增本轮8行记录。默认 `git diff --cached --check` 仅报告 Unity YAML 空值字段 `m_Name: `、`m_EditorClassIdentifier: ` 的8处尾空格；这是 RO/Unity 序列化原格式，保留原字段。关闭该单项尾空格检查后进行其余差异格式检查，不修改资源格式。

最终双语同步第3轮已 accepted，独立历史保全与精确暂存检查通过；本轮资源与记录已完成，无未解决文本项。资源/记录提交 `51e6f16`（6文件，210行新增，零删除）已推送 `origin/codex/ui-script-cleanup`，远端回执 `e9f9a1b..51e6f16`，未合入主分支。此回执作为单独 docs 提交补记于同分支；无关用户改动仍保留在工作区。
