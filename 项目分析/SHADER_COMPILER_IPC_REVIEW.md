# Standard Surface 编译器断连独立核对（2026-10-13）

用户提供 `Particles/Standard Surface` 的 `0x80000004 (Not connected)` 报错。本次核对仅涉及 Shader 编译器通信、错误消息留存和实际构建版本，不重新修改此前已通过的两技能特效资源。

## 已核事实

- 本机 Unity 2023.2.20f1c1 的 Editor.log 快照共725031行，624670—624912附近记录四次编译器 `Crashed!`。最后请求均是内置 `Assets/DefaultResourcesExtra/Particles/Standard Surface`、FORWARD/Fragment、D3D11；请求数据长度41677963字节。随后发生读取协议 magic number 失败、IPC断连与Unity自动启动新编译器。
- 日志中的异常 `start=1543221225477` 出现在崩溃请求末尾；仅作为日志原文保存，不据此推断确切引擎内存错误原因。实际崩溃栈提示本平台不支持堆栈。
- 自动恢复后的当前编译器日志存在同一Shader的FORWARD `start=154 ok=1` 和DEFERRED `start=425 ok=1`。说明进程恢复后能够完成这些请求，不证明先前错误已从源Shader对象或构建产物中清除。
- 624936之后的多处相同Shader error，与112或32个变体全部本地缓存命中、实际编译0的构建记录相邻。再次打包的错误可能是既有Shader消息/缓存留存；不能将每一条重复报错等同一次新崩溃，也不能只看到构建成功就认为错误已解决。
- 只读查询本日Application日志事件1000/1001，没有包含UnityShaderCompiler的条目；这不否定Editor.log已记录的崩溃，也不能据此排除系统层因素。
- 当前正式目录为2026-10-13-1118：1580资产、775 Bundle。unityshaders包1888880字节、MD5为d5936dedbfec45c1079c4d2a74ca1cc8；collection所在assets_gm_game_shaders包4533字节、MD5为b5af5f151a204e92664672df1fa6bd45，实文件与清单一致。1071属于此前历史版本，不再作为当前包结论。

## 最小验收要求

1. 原生读取Standard Surface源Shader和当前实际包Shader的错误消息，记录来源、Shader名与错误数。
2. 针对该内置Shader执行经过反射核验的定向重新编译，不删除整项目Library、材质或Shader、不修改shared规则，也不手工杀编译器进程。
3. 记录执行前日志字节位置，核执行后的新日志是否出现Crashed、IPC Exception及0x80000004；同时必须看到实际编译成功或专用请求成功，不能以全部命中旧缓存替代重编验收。
4. 专用实际Bundle只输出到_temp，读取Shader消息并验证至少FORWARD透明相关pass；源和包错误数均为0且新日志没有通信错误，才可报告本轮定向修复通过。正式版本重建/发布另据实际状态记录。

## 最终定向恢复验收

- 主Agent在Unity2023.2当前编辑器实际执行专用测试DLL：保存旧错误后，只对Standard Surface调用反射核验的ClearCachedData、ClearShaderMessages、ClearShaderErrors，再同步CompilePass检查原雾材质副本。源消息从1条IPC Error恢复为0，6个pass的IsPassCompiled均True；FORWARD的SetPass=True。原Console和崩溃记录没有清除。
- 以操作前Editor.log字节52817443为起点，最终53446370字节，新增628927字节内IPC Error=0、Crashed=0。34份compiler日志无新增请求；因此本轮是复位故障Shader对象并使用当前有效编译缓存验证，不能称本轮重新启动native编译器、重新编译全变体或永久解决引擎崩溃原因。
- 原生实际读取1118的Foggy_1A2主包364及两个DependIDs纹理包576/575，三个实文件MD5均符合清单，没有借用预加载包。包材质的Shader实例683040与源176不同；包对象消息前后均0，实际FORWARD pass2同步检查compiled=True、SetPass=True。当前正式材质及其Shader可用通过，非仅源Shader状态检查。
- 专用ForceRebuild测试Bundle的源和包Shader消息均0，但Standard Surface序列化日志明确d3d11 total internal programs=0，且包对象没有名字为FORWARD的pass输出；该测试包不计目标Shader字节码/Forward验收。正式1118材质检查补足本轮实际包可用性依据，不人为添加新变体或删除全缓存。
- 正式雾材质、SavePath配置、collection、JSON、GraphicsSettings五个冻结输入SHA均保持。测试创建内存材质后销毁，逆序只卸自己取得的包句柄，没有挂测试组件或保存正式场景，没有修改或发布1118。

结论：当前编辑器故障Shader状态恢复及实际1118材质Forward可用验收通过；最初四次native编译器崩溃的引擎层精确原因尚无堆栈证据，不能宣称永久修复。正式全量再次构建零IPC或安装包现场未验证。

证据目录：`_temp/shader-compiler-ipc-20261013/review/`；开发专用测试源码及编译/Bundle产物：`_temp/shader-ipc-20261013/client/`。修改日志：`项目修改日志/2026-10-13-SHADER-COMPILER-IPC-INDEPENDENT-REVIEW.md`。
