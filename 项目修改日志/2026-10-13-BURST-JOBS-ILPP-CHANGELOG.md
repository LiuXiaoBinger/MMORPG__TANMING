# Unity.Burst程序集Jobs后处理失败调查

日期：2026-10-13。

用户贴出Unity.Burst.dll的Processing assembly/running JobsILPostProcessor日志。本轮只处理该编译失败现场，不改业务、依赖版本或打包规则。

## 检查范围与事实

Editor.log确认1900b0aEDbg.dag的Unity.Burst.dll后处理Trigger退出码1，输出停在JobsILPostProcessor，后续Error building Player。没有对应CSxxxx业务代码编译错误，不能据文件名断言Burst后处理器本身是根因。

HybridCLR CompileDllCommand没有检查ScriptCompilationResult就打印compile finish，项目BuildDll入口随后继续复制已有DLL。因此finish日志不是这次成功证据；本轮先恢复实际ILPP失败，不顺带重构工具。

## 操作及验证

主保存失败日志、响应参数副本和6个输入SHA。仅把原ILPP响应文件的输出目录改到 _temp/burst-ilpp-20261013/post-processed，用相同123个define、243个reference、同程序集及现有Runner执行一次，Jobs和Burst均完成、退出码0。原失败未在单任务执行中复现，不能把它定性为源码错误、依赖损坏或确定的缓存损坏。

已请求编辑器正常脚本编译；未删Library、未杀Runner、未更换Packages、未修改正式源码或主动复制发布热更。后续恢复结果补充。

## 修改文件

- Docs/AI/CURRENT_TASK.md：当前调查及恢复边界。
- Docs/AI/TEST_REPORT.md：失败实证与隔离后处理结果。
- 本日志：归档本轮文档和恢复操作说明。
- 临时证据统一 _temp/burst-ilpp-20261013，正式输入保持需最终核对；用户仍可能自行构建，不把其产物变化归为Agent修改。

## 未解决问题

尚未证明原始Jobs后处理返回1的具体内部原因；等待编辑器重编和实际CompilePlayerScripts结果，不把历史console错误抹掉当通过，不宣称安装包或AOT完整构建成功。

## 最终恢复补验

RequestScriptCompilation正常完成，原Burst异常不再返回。实际PlayerBuildInterface.CompilePlayerScripts用StandaloneWindows64/None输出到_temp，返回typeDB=True和26个程序集；结果player-compilation-result.txt。用户中断工具等待，但原生编译已经完成并落结果，后续按实际文件确认。临时首诊断结果枚举误用Length未能编译，改ReadOnlyCollection.Count后执行，仅诊断边界修正。

当前用户已Play进入主城且本地/远端实体均存在，控制台0错误警告。客户端只读IL确认当前hotdll包含主城三处末稿，不将它认成缺修复的旧版本。未修改正式源码/依赖配置或清Library/杀进程，没有主动复制发布热更；用户自行构建产生的DLL变化不计本轮Agent修改。未验完整AOT/最终安装包，最初ILPP内部失败触发原因仍未证明。
