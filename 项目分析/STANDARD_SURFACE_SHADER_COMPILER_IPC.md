# 内置粒子Shader编译通信错误

## 当前问题与范围

用户报 `Particles/Standard Surface` 的 `0x80000004 (Not connected)`。本轮检查Unity内置Shader、ShaderCompiler进程、原生编译日志及当前资源构建。前轮HS_Blend_CG透明变体入包修复保持，不把两个不同Shader的故障混为同一个问题。

## 已确认事实

- 当前正式资源版本已经由用户构建为 `2026-10-13-1118`：报告时间18:45:50、440秒、775包、约1.94GB、ForceRebuild。旧1071不再是当前目录版本，不能继续以其状态评价本轮构建。
- `Editor.log` 行624670–624912有四次连续Shader Compiler IPC Exception/Crashed，发生于内置Surface的D3D11 FORWARD片元编译，后续有Protocol magic读取失败。调用属于Shader编译进程通信，而非C#技能脚本空引用或材质贴图缺失。
- 此后六处Surface错误再次上报，其中编译阶段显示缓存命中、`compiled 0 variants`；没有新IPC Exception。只再点ForceRebuild或清Console不能证明已重新编译该Shader。
- 四个Compiler自动重启后的日志记录同Surface FORWARD `start=154`、DEFERRED `start=425`并 `ok=1`。Unity及Compiler均为2023.2.20f1c1、父进程2228，未发现版本混装证据。
- 故障请求包含异常 `start=1543221225477`；这是协议/编译请求异常的线索，尚不能确认Unity内部触发原因。当前内存仍有余量，未取得该时刻内存耗尽或外部软件杀进程的实证，不将它们写成根因。
- 内置资源路径 `Resources/unity_builtin_extra`，源码不可按项目Shader直接Reimport。项目确有四个正式材质引用fileID210，包括主城Foggy_1A2和Light_Crystals_A Blue/Yellow。正式变体集合已有Surface九个变体，无需全库Always Include或替换Shader。

## 最小处理与验收

先保存错误及编译日志，随后仅对 `Particles/Standard Surface` 调用Unity `ClearCachedData` 和单Shader消息复位，并对内存材质同步CompilePass。清消息是编译前复位步骤，不能独自作为验收结果。

专用临时脚本 `StandardSurfaceCompileProbe` 读取正式Foggy_1A2材质，编译六个通道并生成测试Bundle：输入只包含既有材质及正式变体集合，产物全部进入 `_temp/shader-ipc-20261013/client`。原材质、正式场景、Graph、GraphicsSettings及共享打包规则均不修改。

验收须核新增Compiler请求实际编译成功、源Shader无错误、实际包内Shader无错误及本次日志无新增IPC；不将`Build succeed`、Shader supported或缓存命中当作替代。正式1118包与编辑器Shader对象需分开检查，测试包通过不自动更新正式包。

## 验证进度

专用脚本独立编译通过后，主Agent已实际执行。源Shader原有1条Error；限定复位后六个通道均 `compiled=True`，FORWARD、ShadowCaster及两个编辑选择通道 `SetPass=True`，编译后消息0。空名通道与DEFERRED的SetPass False分别保留，不视为前向渲染失败。

测试Bundle `bundle-20261013112538` 实际构建、加载且消息0，但独立检查发现其Surface为0个程序且没有FORWARD通道。因此该临时包结果只证明对象能读取，不能作为字节码编译或前向渲染通过证据；此前笼统的临时Bundle通过结论在此更正。

最终验收改为当前正式1118里的真实Foggy材质及两贴图依赖：三个实际文件MD5均匹配，没有借用其它版本或已加载Bundle。包内Surface实例与源Shader不同，前后消息均0；包内实际FORWARD通道索引2，`compiled=True`、`SetPass=True`。源六通道编译就绪且FORWARD可用，当前Editor中该Shader消息0，已恢复本轮正常前向使用。

Compiler增量没有新Surface `compileSnippet ok=1`，本轮按Unity现有编译缓存机制恢复并使用已经成功编译的数据，不宣称全部历史变体重新生成。自动重启后真实FORWARD/DEFERRED成功日志支持已有编译数据可用；不为凑新请求继续构造额外变体或清整库缓存。Unity内部断连的底层触发原因仍未定，若再次出现新的IPC，应保留新日志再定位。

前置证据包括四IPC和六Surface错误全文摘录、32份原Compiler日志副本及五个正式输入SHA，均在 `_temp/shader-ipc-20261013/client`。实际 `compile-result.txt`、`bundle-result.txt` 与 `compiler-after-compile-delta.json` 保留。最终独立证据位于 `_temp/shader-compiler-ipc-20261013/review/formal1118-fog-forward-result.txt`；主复核 `root-protected-input-recheck-final.json` 的五输入SHA全部不变，材质/前轮三个变体资源/GraphicsSettings均未改。

修改记录：`项目修改日志/2026-10-13-STANDARD-SURFACE-SHADER-IPC-CHANGELOG.md`。
