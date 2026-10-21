# 当前编辑器Jobs IL后处理错误调查

日期：2026-10-19。

## 需求与范围

用户要求查看编辑器报错原因。使用unity-mcp-orchestrator技能读取当前会话/状态/控制台及Editor.log，核HybridCLR现有构建入口。只诊断和正常编译验证，不修改业务、插件源码或正式资源，不扩大为完整AOT/主包发布。

## 事实与判断

- 当前Unity2023.2.20f1c1处于Boot非Play，Console四条异常：两个Jobs IL后处理失败、Player编译阻断、GenerateStripedAOTDlls失败。
- Unity.Collections.dll（123 defines、265 references）和Unity.VisualScripting.State.Editor.dll（124、272）的ILPP Trigger均退出1，输出停在JobsILPostProcessor。无error CSxxxx，不把包名或进度文字认定为具体业务代码错误。
- 同段Runner gRPC PostProcessAssembly/ProgressMessageStreamer报‘Can't write the message because the request is complete’。这是确实出现的通信/请求生命周期异常，但与两个退出1的最初触发因果尚未证明；未断言缓存损坏、Burst版本不兼容或内存不足。
- HybridCLR的CompileDllCommand调用后未检查ScriptCompilationResult就打印compile finish；现有StripAOTDllCommand在BuildPlayer失败后抛异常。finish不能推翻前面的真实失败。本轮未改第三方工具。

## 操作与结果

1. 保存Console、相关日志、两个rsp副本及六个正式输入哈希到_temp/editor-ilpp-20261019。
2. 请求一次正常脚本编译并等域重载；两个ILPP错误消失，原构建失败两记录暂保留。
3. 用内存诊断执行真实CompilePlayerScripts，Windows x64/None，输出全部到_temp。返回typeDB=True及26程序集，结果见player-compilation-result.txt；日志明确Player Tundra success，随后Editor Tundra success。
4. 末次Console查询0错误0警告；未手动清Console。日志仍有CS0436/CS0618等既有警告，不能称全编译日志零警告。编辑器空闲、非Play、无重载等待。
5. 复核六正式输入哈希保持，记录文档路径/空白检查及编译结果验证，末次verification-result.json为准。

没有删除Library、终止进程、变更依赖、复制正式DLL、保存场景或重启服务。

## 修改文件

- Docs/AI/CURRENT_TASK.md：当前诊断、恢复结果和后续交接。
- Docs/AI/TEST_REPORT.md：实际编译及边界。
- Docs/AI/PROJECT_CONTEXT.md：本次复发对象与证据摘要。
- 项目分析/项目难点.md：问题十八追加本轮两个程序集失败及恢复事实，保留10月13日历史。
- 本日志：归档操作和验证说明。
- 临时证据与诊断脚本仅在_temp/editor-ilpp-20261019，不是业务或正式测试组件。

## 未解决事项与交接

当前脚本编译链恢复，最初ILPP内部退出原因没有完全确定；正常重编通过不等于永久修复。未重跑完整GenerateAll/AOT/安装包，也未发布任何产物。后续沿用户原构建流程执行时检查真实返回和日志；再次复发保留Runner/Trigger原始错误，不仅重复提供最后的GenerateStripedAOTDlls failed。

面试复盘入口：项目分析/项目难点.md的问题十八。临时文件保留，不自动清理。
