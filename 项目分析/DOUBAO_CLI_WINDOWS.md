# 豆包官方桌面 CLI：本机 Windows 使用与验证

## 配置与启动

源码为 https://github.com/Fullstop000/doubao-cli，位于 E:/doubao-cli，版本0.20.0、提交6d7b1c66430db3bbc095a3d9c1cf15c7ce81f8e6。上游桌面说明仅支持macOS；本次通过环境变量指定Windows官方客户端与现有登录数据，没有修改上游源码或切换headless。

本机私有配置 E:/doubao-cli/.env.windows 指向官方 Doubao.exe、原有 User Data、回环端口9225及 E:/doubao-cli/.local-config；机器配置和命令入口都通过 .git/info/exclude 排除。没有复制凭据或上传游戏仓库。

当前豆包已带连接参数运行。以后若从普通快捷方式启动导致端口不通，先保存任务并从托盘退出豆包，再双击 E:/doubao-cli/start-doubao-windows.cmd。它沿用原数据和登录，开启 --remote-debugging-port=9225 --remote-debugging-address=127.0.0.1；无需反复重启已连接的客户端。

## 调用示例

PowerShell中执行：

```powershell
& E:/doubao-cli/doubao-windows.cmd cdp status --json
& E:/doubao-cli/doubao-windows.cmd runtimes --json
& E:/doubao-cli/doubao-windows.cmd projects list --json
& E:/doubao-cli/doubao-windows.cmd sessions read 38448284654642434 --limit 5 --json
```

本项目新任务与返工固定使用LYFMMORGP工作任务和本地目录；以下命令仅为手动示例，不会自动执行：

```powershell
& E:/doubao-cli/doubao-windows.cmd sessions create '你的限定任务' --runtime local --project 38435310952198914 --workspace 'D:/unitypro/LYFMMORGP' --no-skills --permission AskOnRisk --wait --timeout 120 --json
& E:/doubao-cli/doubao-windows.cmd sessions send '返回的conversationId' '同会话修改要求' --runtime local --project 38435310952198914 --workspace 'D:/unitypro/LYFMMORGP' --no-skills --permission AskOnRisk --wait --timeout 120 --json
```

项目ID固定为38435310952198914、名称LYFMMORGP，派发前通过projects list确认对应目录；后续任务返回context也需一致。旧project none临时目录实验只作历史证据，不作为本项目正式派工入口。桌面工作任务使用原生agent_mode；不传headless专用--mode work，不要求--mode goal。AskOnRisk沿用官方桌面审批；--workspace与--no-skills不是系统文件沙箱。每轮需要时都传权限参数，省略会回到上游FullAccess默认值。

保存conversationId和runId。若发送已受理但等待超时，使用 sessions wait conversationId --run runId --timeout 120 --json恢复，不重复创建或重发。普通命令后台调用原生客户端；本次没有注册豆包MCP服务器，也没有把豆包加入WorkBuddy Agent Hub。

## 已验证结果与边界

- 用户确认托盘退出后，启动原官方客户端；127.0.0.1:9225监听且cdp.available=true。本地runtime=READY，项目列表可读。
- 隔离实验工作目录为 D:/unitypro/LYFMMORGP/_temp/doubao-cli-20261021/live-smoke，projectId=null；主Agent仅创建空目录和派工，没有代写成果。
- 会话38448284654642434：第一轮59244078475941122完成，实际创建result.json，来源豆包官方桌面、阶段初稿、数值7；主实际读盘确认后发送第二轮。
- 同会话第二轮59243790148828162完成，实际文件为来源豆包官方桌面、阶段修改稿、数值11、已核对布尔true；本机解析逐项通过。CLI sessions read也成功读取同会话消息。
- 最终文件SHA256：D5A29480EE9BC96FF90E3821B5CBEA17AAE1BA6973EDB51F8DBF9D3B3548112E。证据位于 _temp/doubao-cli-20261021，最终验收 final-verification.json。
- 以上证明本机当前版本的桌面任务发送、同会话续写、读取和真实本地文件执行；没有验证完整游戏开发、长期运行、重启自动恢复、附件或云电脑。
- 上游 status 的进程/产品版本探测及 cdp launch 使用macOS系统命令；Windows启动用本机入口，判断连接使用cdp status。capabilities的Goal模式显示version=null/unsupported_version，不能据此认定当前客户端太旧；显式 --mode goal未验证，普通本地工作任务已实测通过。

本项目桌面工作任务约束及逐项修改记录见 项目修改日志/2026-10-21-DESKTOP-EXECUTOR-RULES-CHANGELOG.md。
