# 2026-10-13 双账号主城互见与移动：独立测试审查记录

## 修改文件与范围

- 业务开发最终仅 `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`，SHA256 `75A4EB68F8660CA6F933C68E0F532BAA1C5BB59BCD85BE23D9DB9DBD0196DEA5`。独立审查未修改业务源码。
- 对照本轮开发 before 快照，实际差异仅三处业务：本地模型完成后先现有11180坐标上报再11070入场；远端初次使用已有基础消息Pos坐标；既有OtherRoleCtrl绑定真实实体，避免其旧重力/根运动继续写同一模型。补CreateRole三个参数中文用途注释。
- 未添加确认等待、重试、动态AOI、请求队列或新状态管理；本地主角既有出生位置保持。现有网络队列和协议保持，服务器无业务变更。
- 测试临时来源、编译与执行产物统一 `_temp/two-account-city-20261013/review/`。测试未挂正式场景/预制体，未登录账号、写数据库、重启服务、重新发布资源或刷新用户Play。

## 故障依据和设计核对

- 首帧异步模型尚未完成时原代码即发送11070，基础坐标空值按原点读取。正式出生点到原点约103.7米，超过服务器现有100米附近范围，可能导致两个方向均漏11100入场基础通知。双方随后移动到附近只有11181，客户端移动处理器不负责创建未出现的实体。
- 独立修前复现使用真实OnlineRole/组件容器、RoleManager、现有消息队列和MoveOperationHandler，以及实际临时回环TCP封包；先A空坐标入城再出生位置上报、B空坐标入城，均没有11100。两端后来移动已邻近，实际仅产生11181，不补基础实体通知。`before-runtime.log`记录8断言。
- 独立修后契约仍从空Pos角色开始，先现有11180通过真实业务队列消费，再HandleClientEnterWorld；实际向A发送B、向B补发A的11100，网关GateRoleCtrl按真实Session发送原数据；双方移动和停止通过11181与GateMoveCtrl下行。`contract-runtime.log`记录19断言。测试不预置最终位置冒称发送链通过。
- 客户端真实调用顺序静态核对：Global.Update→SceneWorld/SceneEntityWorld→PlayerEntity.Update→现有UpdateMove。MMove在模型加载前每帧自然累计CostTime；MModel.SetGameObject同步FinishLoad置IsLoaded，再调用UpdateMove消费时间。没有为测试强行修改业务时间。
- RoleEntity不具PlayerEntity.AfterAddedToWorld角色业务钩子；MMove构造已订阅，不添加此错误候选修复。正式Mock仅挂专用New Scene，不据用户截图断言假角色。
- 主统一执行的真实Prefab隔离探针证明旧OtherRoleCtrl未绑定时可再次写坐标，绑定真实实体后额外写入为零。高处/无地面的测试情境不能解释用户真人现场的具体下落幅度。

## 验证结果

- 最终客户端280真实业务源独立Rebuild：0错误、39既有警告。警告签名与独立首编译基线一致，无新增；开发自身工程显示38旧警告，报告保留实际各自结果，不混用统计。
- GameServer及引用独立Rebuild：0错误、11既有警告；GateServer及引用0错误、4既有警告。专项编译保留原Google.Protobuf引用版本CS1702警告，运行成功。
- 最终280客户端输入SHA重新核对，零漂移；服务器源/项目246路径清单作为本轮输入记录。真实专项运行程序集与测试来源分别有SHA清单，不把已构建DLL同当前源码不经核实等同。
- 精确diff `maincity-final2.diff`仅上述单源授权范围。主原生远端最终probe中的位置段与正式末稿逐字一致出现两次（远端和本地分支），绑定段逐字一致出现一次，见`final-audit.json`。
- 变更范围内无新增namespace/foreach/三目/弱类型业务数据或超过三项连续布尔判断；新增局部与参数注释有实际用途。
- 首测试命令受Python控制台编码影响出现UnicodeEncodeError，未产生业务失败；临时启动改`python -X utf8`后运行结果与中文日志可读。未为夹具编码问题修改业务。

## 未解决和交接

- 独立结论：本轮源码审查、编译及真实服务端队列/回环协议契约通过。未执行两正式客户端完整异步入城后的现场互见/移动，未将源码修复写入已发布热更包。
- 主现场仅一名本地玩家，输入Waiting/未聚焦、movable=true；历史连续11180表明曾可移动。静止CostTime累计不是故障，当前不能宣称用户“本地无法移动”已被正式复现或完全验收。
- 下一步原正式热更编译/资源发布流程更新后，双账号重新入城验收互见、双方移动/停止与远端正确坐标；现有账号/服务保留，不由测试擅自刷新或踢人。
- 主Agent整合本轮结果到 `Docs/AI/CURRENT_TASK.md` 和 `Docs/AI/TEST_REPORT.md`；本独立记录由主最终交接引用。业务分析与总日志按根目录归档约束引用。
