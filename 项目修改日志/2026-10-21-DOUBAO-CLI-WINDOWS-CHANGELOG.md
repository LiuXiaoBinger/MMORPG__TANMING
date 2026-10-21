# 2026-10-21 豆包官方桌面 CLI 的 Windows 配置与实验

## 最终验收：本机桌面两轮通过

- 用户确认已退出后，核实无Doubao进程，再用官方exe和原登录数据带回环调试参数启动；127.0.0.1:9225监听，cdp.available=true，本地runtime=READY，现有项目列表读取成功。
- 派发任务DOUBAO-DESKTOP-WINDOWS-SMOKE-20261021；为隔离实验使用project none、空测试目录、no-skills、AskOnRisk。第一轮conversationId=38448284654642434，runId=59244078475941122，completed，实际创建result.json（初稿/7）；主读文件确认后发返工。
- 第二轮同conversationId，runId=59243790148828162，completed，实际修改为修改稿/11/已核对布尔true。主本机解析逐项通过，sessions read回读4条消息成功。主未代写测试文件。
- 最终SHA256=D5A29480EE9BC96FF90E3821B5CBEA17AAE1BA6973EDB51F8DBF9D3B3548112E。真实证据：_temp/doubao-cli-20261021/{create-result.txt,initial-file.json,send-result.txt,read-back.json,final-verification.json,listener.json,capabilities.json,runtimes.json,projects.json}及live-smoke/result.json；CLI运行回执保存在E:/doubao-cli/.local-config/turns（不上传）。
- 补充新增 E:/doubao-cli/start-doubao-windows.cmd：退出后启动带回环调试端口的官方客户端；修改本机.git/info/exclude排除此入口。
- 新增 项目分析/DOUBAO_CLI_WINDOWS.md：启动与CLI命令、超时恢复、权限语义、两轮实证和macOS专用命令边界；修改 项目分析/README.md：添加该使用说明索引。
- 更新Docs/AI两份协作入口最终结果。未修改上游源码、游戏代码，无新增/移动/删除其他正式资源。仅本机配置与文档，无项目代码提交推送；外部仓库git status为空。
- 当前可用结论限定本机当前版本的任务发送、续写、消息读取和文件执行。显式Goal模式、附件、长期运行、开机恢复、完整游戏开发未验；capabilities的Goal版本null来自macOS plutil探测，非Windows客户端过旧证据。没有新增MCP或接入Agent Hub。

以下保留配置与退出前检查过程，待退出状态已由以上真实实验解除。

## 目标与方案

用户明确要求类似 WorkBuddy 网关的官方桌面客户端接入，本轮采用 Fullstop000/doubao-cli；不以网页、云电脑网页操作或 headless 服务替代桌面验证。上游桌面文档只支持 macOS，因此先用现有环境覆盖路径和 Windows 启动方式验证核心协议，不预先移植源码。

## 文件清单

- 新增 E:/doubao-cli 仓库：来源 https://github.com/Fullstop000/doubao-cli，基线 6d7b1c66430db3bbc095a3d9c1cf15c7ce81f8e6，版本 0.20.0；npm ci 安装96个依赖，无上游源码修改。
- 新增 E:/doubao-cli/.env.windows：指定官方 Doubao.exe、原有 User Data、127.0.0.1:9225、E:/doubao-cli/.local-config，关闭自动更新；无账号凭据。
- 新增 E:/doubao-cli/doubao-windows.cmd：固定本机 Node，加载 Windows 环境并透传 CLI 参数；使用 ASCII 和 CRLF，修正首次 UTF-8 中文批处理注释引起的 CMD 错误。
- 修改 E:/doubao-cli/.git/info/exclude：排除上述机器配置、命令入口和本地配置数据，避免上传。
- 新增 _temp/doubao-cli-20261021：依赖下载缓存与隔离实验、端口检查证据目录；不放正式账号配置。
- 修改 Docs/AI/CURRENT_TASK.md、Docs/AI/TEST_REPORT.md：记录本轮实际配置及待完成的桌面连接实验。
- 新增本日志：记录范围、检查及交接。

没有移动或删除文件；未修改 Unity 客户端、服务端、正式场景或其他网关。

## 当前验证

- 本机 Node v24.14.0、npm 11.9.0 满足 Node >=22；npm ci 成功。
- 官方客户端进程存在，路径 C:/Users/lyf/AppData/Local/Doubao/Application/app/Doubao.exe；可执行文件 ProductVersion/FileVersion 为147.0.7727.149（Chromium文件版本，不当作豆包产品发布版本）。
- CLI --version 返回0.20.0；profiles 能读取现有 Default 配置与账号元信息，未读取或输出 Cookie/Token。
- cdp status 明确 available=false/fetch failed；9225、9226无监听，当前官方客户端启动命令没有调试端口参数。
- 已向用户请求保存后从托盘退出豆包，未强杀进程。退出后才可带回环调试端口重新启动并验证原生协议、任务收发及文件创建/同会话修改。
- 证据：_temp/doubao-cli-20261021/cdp-before-restart.json。

## Git 与交接

项目分支 codex/feature-development，外部仓库保持上游源码不变，本机配置已忽略；本轮尚无项目业务代码提交或推送，未合 main。现阶段仅配置/profile读取通过，不报告桌面任务可用。下一步等待用户退出，再使用原数据目录启动官方客户端，不切换网页/headless，不上传游戏仓库。
