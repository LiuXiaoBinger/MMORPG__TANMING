# 角色：豆包桌面外部开发子 Agent

## 定位与调用入口

- 主Agent管理需求、委派、测试审查、验收和记录；豆包使用官方Windows桌面客户端执行限定开发，不是Codex原生子Agent。
- 继承 `AGENTS.md`、`Docs/AI` 四份记录和对应客户端／服务端角色约束，仅提供本次必要上下文。
- 唯一调用路线是 `E:/doubao-cli/doubao-windows.cmd`，通过已验证的本机桌面连接；不使用网页、浏览器云电脑或headless代替。
- 项目固定为 `LYFMMORGP`，项目ID `38435310952198914`，本地工作目录 `D:/unitypro/LYFMMORGP`。使用工作模式／工作任务的本地电脑执行，不派到普通对话或其他项目。

## 新建与返工

1. 使用 `cdp status --json`、`projects list --json` 和 `runtimes --json` 确认桌面连接、本项目ID／目录和本地运行环境；后续结合已有会话及返回上下文确认，不重复创建会话。
2. 新任务调用 `sessions create`，明确传 `--runtime local --project 38435310952198914 --workspace D:/unitypro/LYFMMORGP --permission AskOnRisk --wait --json`。该桌面路径启用原生 `agent_mode`，不是普通对话；不要传headless专用 `--mode work`，不要求额外的 `--mode goal`。
3. 任务单包含任务标识、开发角色、本项目和分支、实际基线、允许修改的逐项文件、具体需求、中文代码约束、验收命令与交付格式。仅修改任务授权范围，不扩展功能，不代替主Agent提交或推送。
4. 保存 `conversationId`、`runId` 和返回的 `context`，核对项目、local运行环境及本项目目录。返工使用对应 `sessions send`，重复传本地运行环境、项目、目录和权限参数，附真实失败证据。
5. 发送已受理但超时用 `sessions wait` 配合原会话和runId恢复，不盲目重发。连接或模式不可用记录阻断，不改走网页；Windows启动与CLI边界见 `项目分析/DOUBAO_CLI_WINDOWS.md`。

## 交付与验收

- 豆包在授权范围可直接写本地文件，主Agent核对实际差异及产物后组织编译和测试；不因模型回复“已完成”认定通过，也不把本地目录当作系统权限沙箱。
- 交付逐项列新增、修改、移动和删除文件、用途、检查命令与结果、未解决问题。仅提供补丁时由本地开发子Agent核对基线后落盘，主Agent不代改业务代码。
- 测试失败回传同一豆包任务修复；主Agent统一更新记录、集成并提交推送 `codex/feature-development`，不合 `main`，不覆盖用户无关改动。

## 验证边界

- 2026-10-21已完成官方Windows桌面CLI两轮本地文件实验：会话38448284654642434，实际创建初稿再修改，主读盘通过。该实验使用project none和临时目录，仅作连通与文件执行证据，不是本项目正式绑定任务。
- 本约束从现在起要求项目ID和本项目目录；本次仅更新规则，不额外发送开发任务，不将已有实验宣称为项目绑定开发已验收。
- 旧网页云电脑调用要求已被当前用户明确替换，历史实验记录保留，不再作为执行入口。
