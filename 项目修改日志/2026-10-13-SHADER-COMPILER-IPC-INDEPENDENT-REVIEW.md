# Standard Surface 编译器断连独立核对日志（2026-10-13）

- 范围：只读Editor.log、34份编译器日志、当前进程标识、Windows Application错误事件、正式1118清单及两个Shader相关包文件。没有修改业务脚本、正式资源、场景或构建规则，没有杀进程、清缓存或重启Unity。
- 新增正式文件：`项目分析/SHADER_COMPILER_IPC_REVIEW.md`及本日志。专用取证脚本和日志副本仅保存在`_temp/shader-compiler-ipc-20261013/review/`，未挂正式场景。
- 设计思路：区分实际四次编译器崩溃与后续缓存命中后重复序列化的错误，检查定向重编所需证据，避免将shared依赖问题和ShaderCompiler通信问题混为同一原因。
- 已验证：Editor.log725031行快照中4次Crashed；自动重启后的compiler日志同Shader请求ok=1；本日Application1000/1001没有匹配事件；1118有1580资产/775包，unityshaders与collection包实文件大小/MD5均匹配清单。
- 临时夹具修正：初版File.ReadAllLines/ReadAllText无法读取Unity持续持有的日志；只在_temp改为FileShare.ReadWrite共享读取，最终取证执行退出0、读取成功。没有为夹具修改Unity进程或项目。
- 未解决：实际定向重编和新Bundle零错误验收由主Agent统一执行，当前尚未有该结果。正式1118并未由本Agent替换或发布；上述事实不足以证明错误已消除。
- 交接：按分析文档最小四步验收；主Agent统一把最终源/包错误数、新增日志结果和正式版本边界写入`Docs/AI/CURRENT_TASK.md`与`Docs/AI/TEST_REPORT.md`，避免多Agent同时覆盖协作入口。

## 最终独立验收追加

- 主实际执行专用复位/同步CompilePass：源Standard Surface旧IPC Error1→0，六pass均compiledTrue、Forward SetPassTrue。反射复位仅单Shader，不清Console或整Library，不修改业务/Shader/材质。
- 独立最终增量审计：52817443→53446370字节，新增628927字节IPC0/Crashed0；编译器34份日志无新增成功请求。有效缓存正常可用；不得宣传本轮native编译器新Snippet或所有变体重新编译。
- 专用测试Bundle虽消息0，目标Standard Surface序列化internal programs0、没有Forward命名pass，不能独立据此通过目标包内字节码验收。已明确该限制并保留原构建结果，没有构造新tuple或清整cache迎合测试。
- 新增_temp原生方法体formal1118-fog-forward-probe.cs.txt；主执行后独立读取结果：当前正式Foggy主包364＋清单DependIDs576/575三文件MD5全部True，无借用现包；材质包Shader与源不同实例，消息前后0，实际Forward pass2 compiledTrue/SetPassTrue。内存副本销毁，自取句柄逆序卸载，正式资产不写回。
- 主五冻结输入SHA复核全部一致，独立已读root-protected-input-recheck-final.json。项目分析及本日志diff空白检查通过，无业务源码或资源改动。
- 验收结论：当前编辑器故障状态恢复和现1118真实材质Forward可用通过；native最初崩溃的永久引擎修复、完整正式再次打包零IPC及安装包现场仍未验证，未发布/替换正式包。四协作入口由主统一更新。
