# 移动与技能固定 KCP 修改日志

## 需求与范围

用户要求移动等实时操作直接使用 KCP。当前网络专用分支 `codex/network-kcp-development`，基线 `ab6252cbe9ebd57f8327ef38f319cb256a75d1f5`，不合入 main。

Unity↔Gate 的 11180/11181 移动和 11170/11171/11173 技能固定使用同一 KCP；取消绑定前、绑定中和初次失败的 TCP 回退。其余业务及 Gate↔Game 内部链路继续 TCP，不重写第三方库，不扩展大包或协议白名单问题。

## 设计依据

旧实现为兼容未绑定阶段先走 TCP，导致实时消息可能跨通道投递；直接固定 KCP 可消除这类切换，但不能宣称所有 TCP/KCP 业务依赖自动有序。提前绑定并在现有主城创建流程等就绪后加载角色模型，沿既有断线恢复处理失败。现有进入世界流程用首个移动上报传出生位置，该上报与 TCP 入场不再同流，因此将出生坐标放入现有入场请求，由服务器在附近玩家过滤前保存。完整说明更新至 `项目分析/KCP_NETWORK_IMPLEMENTATION.md`；实际难点记录于 `项目分析/项目难点.md`。

## 委派记录

- 实际执行者：WorkBuddy 官方桌面本项目本地工作任务；Codex 设计、核验、测试与提交集成，不代改业务源码。
- Hub：`hub-f7470571-9db1-422c-983d-fa9061f1d06a`。
- 第 1 轮：`run-203c455e-603e-4b4e-9e50-8be8a1e142db`；会话 `5af3d7d9-4c42-406d-a6c7-7a8949f0dcf0`；`runtimeSource=desktop_queue`。
- 第 2 轮：`run-9013f4af-d60f-4a3a-a533-745de3e68226`，沿原会话补角色加载/失败薄边界运行测试及本次相关误导注释。第 1 轮报告明确这些入口只有编译，因此未直接验收。
- 任务及原用户差异冻结：`_temp/network-kcp-only-20261022/`。9 份原用户修改不在本任务暂存范围。

## 文件清单

以下18路径为本次实际集成范围，已核验源码并配套正式网络插件。除日志新增外均为修改，无移动、删除或新增 Unity 资源及 `.meta`。

| 文件（项目根相对路径） | 用途与具体变化 |
| --- | --- |
| `LYF_Server/Network/Socket/NetClient.cs` | Unity 五实时协议无通道/写入失败一律丢弃，删除曾启用标记及其接口；其他业务与内网保留 TCP。 |
| `LYF_Server/Network/Socket/com/Session.cs` | 网关面向 Unity 的实时下行固定 KCP，移除曾启用标记；按既有上游 GameServer 类型保留内部 TCP。 |
| `LYF_MMO/Assets/GM_Game/Scripts/Manger/NetSocketMgr.cs` | 暴露实时就绪状态；首次拒绝、启动失败与绑定超时清理通道、通知放弃并走既有断线恢复。 |
| `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs` | 模型加载前请求绑定，沿现有逐帧入口等就绪；入场携出生位置，删除仅为出生点发送的首移动及旧绑定位置。 |
| `LYF_Server/ProtoBuf/proto/RequestEntity.proto` | `EnterWroldReq` 增加字段 2 `pos`，沿用世界米坐标 `X_Y_Z` 格式。 |
| `LYF_Server/Network/Proto/RequestEntity.cs` | 正式 protoc 重新生成上述入场坐标字段及描述符，不手写其他协议。 |
| `LYF_Server/GameServer/manger/RoleManager.cs` | 入场身份核验后、附近玩家过滤前保存非空出生坐标；旧请求空字段保留原位置。 |
| `LYF_Server/GateServer/Ctrl/GateKcpCtrl.cs` | 修正绑定时机与取消绑定不再回退 TCP 的说明，保持原处理逻辑。 |
| `LYF_Server/GateServer/GateServerApp.cs` | UDP 启动失败日志改为客户端绑定被拒绝，删除保持 TCP 的误导说明。 |
| `LYF_Server/Network/Socket/KcpClient.cs` | 仅修正实时回退相关注释，保留既有状态与逻辑；不修改第三方 KCP。 |
| `LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll` | 配套本轮最终独立编译产物，SHA一致；旧插件备份在本任务plugin-backup，用户原热更程序集不改。 |
| `Docs/AI/PROJECT_CONTEXT.md` | 记录本轮固定 KCP 策略及当前版本边界。 |
| `Docs/AI/DECISIONS.md` | 记录取消实时 TCP 回退、提前绑定与入场位置最小适配决策。 |
| `Docs/AI/CURRENT_TASK.md` | 记录任务身份、分支、交接与集成状态。 |
| `Docs/AI/TEST_REPORT.md` | 记录 Codex 实际编译、Socket/入场与薄边界结果及未测项。 |
| `项目分析/KCP_NETWORK_IMPLEMENTATION.md` | 更新当前调用流程，明确旧切换机制为历史。 |
| `项目分析/项目难点.md` | 记录出生位置依赖、证据、取舍、验证边界和面试讲述。 |
| `项目修改日志/2026-10-22-KCP-ONLY-REALTIME-CHANGELOG.md`（新增） | 本轮唯一修改日志：逐项文件、设计、开发轮次、验证与 Git 回执。 |

## 验证与未解决项

- Codex 独立目录 `_temp/network-kcp-only-20261022/codex-validation/` 执行 `compile_kcp.py`：Network/Gate/Game/Center 各 0 错误，警告日志计数 14/14/28/14（含重复输出，既有警告）。`compile_client.py` 编译 289 份客户端源、0 错误，引用独立新网络 DLL。
- Codex 实际执行 `run_loopback.py`：`PASSED=108 FAILED=0`，包含无绑定/绑定中/失效后实时 TCP 零写入、普通业务 TCP、服务内网 TCP、同通道保序及原生命周期回归。脚本按 GBK 解码 UTF-8 子进程使部分中文乱码，保留原始日志事实；清理阶段有关闭 Socket 回调日志，不当作所有日志零错误或正式长稳证明。
- Codex 实际执行 `run_enterworld.py`：`PASSED=8 FAILED=0`，真实 protobuf 往返与 `RoleManager.HandleClientEnterWorld`；观察角色距出生 8 米、距原点约 107 米，附近广播/补发均收到，未发送首移动。不等同正式 Unity 联机。
- 原 9 份用户文件逐一 SHA 与 `baseline.json` 相同。正式DLL与最终独立构建SHA一致；正式插件引用289源编译0错，protoc逐字节一致，新增语法候选空，差异空白检查通过。源码与隔离验收通过，现场联机尚未验收。
- 真实 Unity 双号、MP 扣除、业务自动重连、实体早到、弱网端到端及 IL2CPP 未验。现有主城实例 `_enterWorldSent` 跨重连行为尚未现场验证；本轮不扩修既有重入流程。UDP 长期不可用会持续无法进入游戏，不增加回退、退避或提示框架。大包上限和协议入口范围问题保留在 `项目分析/KCP_NETWORK_AUDIT_20261022.md`。
- 未启停正式服务或 Unity，未保存场景，未替换用户热更 DLL，未清理临时文件。

- Codex独立执行run_boundary.py：PASSED=39 FAILED=0，真实MainCityMgr/NetSocketMgr与真实TCP/KcpClient，验证Ready前不加载、Ready后仅一次、拒绝/启动失败/初次超时断开及Abandoned。Unity/资源/实体前置是桩，Enabled由测试网关发送；旧代次仅反射当前代次减1（本次0）的释放入口，不代表真实旧回调与重连全链。仅证明模型加载请求，不宣称真实输入/画面通过。
- WorkBuddy第2轮completed；自检Socket112/0与Codex独立108/0分开记录，未冒作同一套最终复跑。第二轮正式源码只补本次相关注释，业务流程不变。Codex最终重建四工程、配套插件并从正式插件路径重编客户端。
- 正式插件SHA256：`3521ceb3c2be2730a1a1f2114a2b23afb595a39183aa3295f2b94c102092b699`；正式插件与codex-validation/build/network/bin/Network.dll相同。原插件备份_temp/network-kcp-only-20261022/plugin-backup/Network.before.dll。

## Git

源码和隔离检查已通过，提交范围为上述18路径；基线ab6252c，当前仅待精确暂存、中文提交与推送codex/network-kcp-development。9份原用户修改及无关未跟踪产物不暂存，不合main。实际提交号、推送回执在操作后追加。
