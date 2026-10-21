# 2026-10-13 内置粒子Shader编译通信错误检查

- 需求：处理用户报Particles/Standard Surface编译进程断连，保留前轮技能透明变体修复。
- 修改文件：新增 `项目分析/STANDARD_SURFACE_SHADER_COMPILER_IPC.md`、本日志；专用测试源码、程序集、构建项目、旧Compiler日志副本和输入SHA全部保存于 `_temp/shader-ipc-20261013/client`。
- 正式业务、Shader源码、材质、场景、GraphicsSettings、共享规则修改0。无删除Library/cache、无关闭或替换Shader、无全库Always Include、无清Console。
- 设计：四次IPC崩溃后Compiler已自动重建，六处后续错误随缓存复报；限定单个内置Shader复位后真实同步编译，并以现Foggy_1A2材质/正式collection生成独立测试Bundle验收。只清消息不算修复，不引入正式自动重试/管理框架。
- 静态验证：专用C#脚本独立MSBuild通过0错；编译首稿两项夹具问题（XML encoding名称和ShaderMessage静态类型）只在_temp修正，不改业务。输入五文件SHA已冻结。
- 原生验证：主执行Compile，保存原1 Error后单Shader复位，六通道编译状态True、FORWARD实际SetPass True、after消息0。主执行Build，生成并加载独立测试Bundle，源/包内Foggy材质的Surface消息均0；但独立复核该临时Shader为0程序且无FORWARD，因此不计作字节码/前向渲染通过，纠正此前笼统Bundle通过。原生证据compile-result.txt、bundle-result.txt保留。
- 最终实际包验收：1118正式Foggy材质与两贴图依赖三个文件MD5匹配、无借包，包内Surface为独立于源的实例；before/after消息0，实际FORWARD pass2 compiled/setPass均True。证据 `_temp/shader-compiler-ipc-20261013/review/formal1118-fog-forward-result.txt`。主最终五输入SHA重核全相同，见同目录root-protected-input-recheck-final.json。
- 本轮恢复使用正常编译缓存，没有新增Surface compileSnippet；不宣称全部历史变体重新编译，也不继续额外tuple或清整库缓存来追求无业务意义的新增请求。旧Console报错保留，实际Shader对象与正式1118前向通道已验证正常。
- 当前正式版本：用户新的1118已ForceRebuild结束，不沿用旧1071路径。编辑器恢复与正式资源生效分别记录，不冒称已发布。
- 未决与交接：本轮编辑器状态恢复及1118实际前向通道验收完成；没有重新发布或替换正式包。Unity底层断连触发原因尚未确定，不承诺永不复发；再次有新IPC时保存新日志定位。正式游戏全场景/安装包现场未验，不将单材质通道扩大为全项目验收。四入口由主统一记录。
- 分析引用：`项目分析/STANDARD_SURFACE_SHADER_COMPILER_IPC.md`。

## 主收尾与协作记录

- 主修改Docs/AI/PROJECT_CONTEXT.md、CURRENT_TASK.md、DECISIONS.md、TEST_REPORT.md，补当前恢复、1118实际材质验收、缓存命中及临时包0程序限制、历史1071状态被用户新版本覆盖和下一步。所有新测试响应/日志留_temp，不改正式业务或场景。
- 主已读取独立最终增量审计和两方根日志；四入口diff空白检查退出0。最终Unity原生控制台错误0且源Shader消息0，未调用Console.Clear；之前Shader对象清旧消息可能同步移除其显示项，旧报错文字仍完整保留在compile-before与日志证据。
- 当前验收仅编辑器故障状态恢复与1118雾材质Forward可用；不承诺Compiler内部永久修复，不声称重新构建/发布全部正式包或Player现场通过。
