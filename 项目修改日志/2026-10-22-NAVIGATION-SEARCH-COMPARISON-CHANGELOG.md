# 2026-10-22 RO两端寻路核查与源码查找对比实验

## 需求与范围

用户询问RO是否只在客户端寻路、服务器有没有寻路，并要求实际比较豆包最高模型/最高推理与当前Codex会话的源码查找能力。

本轮仅查本地源码和记录事实，不修改客户端或服务器业务代码，不实现服务器寻路或校验，不改场景/资源，不启动Unity或正式服务。

分支：`codex/feature-development`。实际基线：`1fbe5a2bca1135aa68dcc92da4dd79bdad459acb`。不合入或推送main。

## 逐项文件清单

| 操作 | 文件 | 用途与具体变化 |
| --- | --- | --- |
| 新增 | 项目分析/RO_NAVIGATION_CLIENT_SERVER_SEARCH_COMPARISON.md | 记录RO客户端C#到C++ Detour A*链、服务器AI寻路及加载链、玩家上报移动校验、跨地图规划和LYF差异；加入同题实验协议、15项评分标准、Codex初稿耗时/哈希、豆包连接阻断和续接步骤。 |
| 修改 | 项目分析/README.md | 加入本轮两端寻路及对比实验专题索引，明确豆包未运行、不能声称能力差距。 |
| 修改 | Docs/AI/CURRENT_TASK.md | 置顶记录本轮需求、核实结论、限定范围、冻结实验和未完成项，保留历史任务。 |
| 修改 | Docs/AI/TEST_REPORT.md | 置顶记录源码核查、初稿计时/哈希、连接失败和验证边界，保留历史测试。 |
| 新增 | 项目修改日志/2026-10-22-NAVIGATION-SEARCH-COMPARISON-CHANGELOG.md | 本轮逐项文件、设计思路、验收和Git收尾记录。 |

没有移动、删除文件，没有新增Unity资源或.meta文件。正式提交只含以上5文档，不混入DLL、字体、客户端下既有Docs/AI、编辑器布局、Yoo清单、.workbuddy或其它用户产物。

## 分析与实验思路

寻路结论必须追到真实调用和算法实现：RO客户端C#组件经管理器和SWIG进入C++；服务器AI经导航事件进入RONavigationComponent并调用MNavigation::PathFind。玩家移动合法性与地图级路线分别追踪，不能混为每次玩家移动都由服务器重算完整A*。LYF的Common共享编译与服务器运行接入也分别核对。

实验预先冻结同题与15项源码证据，不把另一方答案喂给豆包。当前Codex具有LYF开发上下文和部分RO预读优势，因此披露为非盲测的单轮工具协作观察。耗时不含连接排查、整理和Git，不能推导纯模型速度或通用能力差距。

## 验证结果与边界

- RO真实源码核对：客户端业务/管理器/SWIG/C++查询/Detour及地图加载链成立；服务器AI实际调用、导航组件PathFind、场景加载、编译宏、移动合法点与速度/距离检查、跨图规划分别成立。
- LYF源码核对：客户端纯C# A*与地图加载已接入；Common链接8份同源导航代码；服务器空导航组件与正式移动保存/广播链成立。限定源码检索未发现运行寻路/导航地图加载调用，不冒充运行验收。
- Codex初稿已冻结：178.604秒，SHA256 `84B3ADB8D062464879C24F4136EFDE0023D7201D185B3276F803437D6C763630`。当前具体运行模型/推理档位未独立核实，不以本机默认配置当证明。
- 豆包官方桌面CLI连接实际`available=false`、`fetch failed`；未读取当前最高模型配置、未派发任务、无conversationId/runId及答案。已请用户保存并托盘退出；未强杀或改用其他执行路径。对比实验尚未完成，不给比较评分、胜负或倍率。
- 仅文档变更，采用源码定位、文档链接/范围、初稿哈希和Git差异校验，无须编译未变化的业务代码。实际检查28处源码引用均存在且行号有效，4份入口/日志均引用本轮专题，Codex初稿哈希保持；`git diff --check`通过。证据为同目录`document-check.json`，真实连接失败为`doubao-cdp-status.json`。

## 临时证据与后续

证据目录：`_temp/source-search-comparison-20261022/`。原始`protocol.md`、`codex-answer.md`、`codex-start.txt`、`codex-end.txt`、`status-before.txt`用于冻结题目、初稿、计时和原工作区状态；新增`document-check.json`记录28处引用及所引源文件哈希、初稿保护结果，`doubao-cdp-status.json`记录真实连接失败。它们是实验中间产物，未经确认不清理，不提交。

分析正文：`项目分析/RO_NAVIGATION_CLIENT_SERVER_SEARCH_COMPARISON.md`。桌面入口说明：`项目分析/DOUBAO_CLI_WINDOWS.md`。

待用户确认退出后，按本项目官方桌面local工作任务恢复连接，读取可用模型并核最高配置，保存真实会话设置和任务编号；沿同题取得初稿，再冻结评分。当前无须返工业务开发；未完成项为豆包实验和双方比较。

## Git记录

提交范围仅上述5文档；中文提交正文引用本日志。暂存清单逐项核对且`git diff --cached --check`通过，无业务代码或用户无关改动混入。

主体提交`e352696b534f1da10e366fed8ec61f1d2c12e510`已成功推送`origin/codex/feature-development`，`git ls-remote --heads`回读远端同SHA。没有操作main或强制推送。本节仅追记已取得的提交/推送回执，另作中文文档收尾提交；最终收尾号和推送结果在交付回复报告。

## 2026-10-22 续接：有效对比完成（取代前述待连接状态）

用户确认已退出并可启动后恢复官方桌面连接，完成Pro最高推理的新会话。第一次多行.cmd提示截断、补发后发现Git报告污染，旧会话作废；没有用污染答案评分。新会话38448622087397122/run59302623239101442已原生completed，实际文件存在，模型key5/effort7、agentMode1、本项目local目录均回读确认。

### 本次续接逐项文件

| 操作 | 文件 | 用途与具体变化 |
| --- | --- | --- |
| 修改 | 项目分析/RO_NAVIGATION_CLIENT_SERVER_SEARCH_COMPARISON.md | 改正.bytes用途；补BFS和实体常规装配入口核查；把未运行章节换成配置、冻结初稿、15项评分、统一计时、双方错误、污染作废及公平性边界。 |
| 修改 | 项目分析/README.md | 专题索引更新为已完成及结果限制。 |
| 修改 | Docs/AI/CURRENT_TASK.md | 置顶本次完成状态、真实任务标识、文件清单和后续边界，保留历史。 |
| 修改 | Docs/AI/TEST_REPORT.md | 置顶主Agent源码复核、配置回读、哈希保护、评分依据及未实测范围。 |
| 修改 | 项目修改日志/2026-10-22-NAVIGATION-SEARCH-COMPARISON-CHANGELOG.md | 追加本次恢复过程、逐项变更、设计、验证与Git结果。 |

无新增正式文件、移动或删除，无Unity资源或.meta变化。临时新增/更新均在_temp/source-search-comparison-20261022：豆包模型/项目/runtime与原生任务回执、有效/作废初稿及提示/计时、frozen-answer-metadata.json、read-doubao-settings.mjs、final_verify.py、update_final_docs.py、final-score-verification.json、final-document-check.json和Git回执；不提交、不未经确认清理。

### 设计、验证与未解决边界

- 保持预设核心证据覆盖口径，双方15/15；不把额外摘录不准确藏在满分后，也不在看到答案后改评分规则。5类已发现问题逐条列真实源码：Codex.bytes用途描述；豆包DllImport与条件、NavMeshSet摘录、AI事件摘录、玩家寻路全局性措辞。
- 同一计时口径为开始至答案文件落盘：Codex178.225137秒/36行；豆包1520.731016秒/624行。冻结SHA256分别84B3ADB8D062464879C24F4136EFDE0023D7201D185B3276F803437D6C763630、7EC5B1A134B7096094FCD595DB073FE071D898B86EA55EE71180A493A3B6A200，复算保持。原178.604秒与连接断开时刻不再混用。
- final_verify.py的15个关键源码断言及两初稿哈希/行数通过；附加错误真实代码行归档。RO两端寻路、玩家移动检查、跨图BFS与LYF共享编译/运行未接的结论成立。只做静态分析，未启动Unity/正式服务、未改业务代码，不重编译无变化业务；豆包旧DLL反射自报未复跑。
- 非盲测、Codex开发与预读优势、四份入口线索、一轮三题、不同写作规模、Codex精确型号未独立核实均记录，不能报告通用能力排名或纯模型速度倍率。新豆包轮次声明未读旧答案，无系统调用级访问审计；这是实验边界，非业务待修问题。
- 正式说明：项目分析/RO_NAVIGATION_CLIENT_SERVER_SEARCH_COMPARISON.md。此前“尚无答案/待退出”仅为历史，现已解除。当前用户两项问题均已完成；服务端导航开发和正式游戏运行不在本轮范围。

### 本次Git范围与结果

实际续接基线3c1a8e24f72ea350a83edab816da200e2063363f，业务相对原实验基线未由本轮修改。仅精确暂存上述5文档，在codex/feature-development中文提交并正常推送；保留已有DLL、字体、客户端Docs/AI、设置、Yoo及其他用户文件，不合入main，不强推。提交前执行文档引用/初稿保护、git diff --check及暂存清单检查；实际回执随后追加，最终回复报告提交号及推送状态。

提交前实际检查通过：15项核心断言、两份冻结初稿哈希及行数、32处正式源码链接、前轮28处源文件哈希不变、本实验原生配置和completed、5文件修改范围及git diff --check。服务端EntityWorld/EntityFactory实例化、运行导航查询的限定rg检索均无命中（退出1表示未找到）。证据final-score-verification.json与final-document-check.json；另有临时verify_final_documents.py用于复核。

本次主体提交92b09d70569761126910a76815b7f3a36e5ca011已正常推送origin/codex/feature-development，git ls-remote回读远端同SHA。提交仅含上述5文档，既有用户改动保留。随后仅本日志追加此真实回执并作收尾提交；收尾号及远端核验在最终回复报告，没有操作main。
