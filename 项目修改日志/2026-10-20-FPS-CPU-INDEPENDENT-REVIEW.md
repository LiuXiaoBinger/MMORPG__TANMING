# 主城CPU运行证据独立复核

- 日期：2026-10-20；职责：只读源码与日志初查，随后独立核对主Agent原生采样证据。未修改业务、配置、资源，未操作Unity窗口/Profiler或停止Play。
- 初查：当前Editor日志末段存在正常11180移动发送记录，未发现本轮移动异常持续刷屏。日志JSON、堆栈构造及普通组件闭包等只能列为源码候选，不能在没有采样贡献证据时称低帧率根因；本轮不据此改动它们。
- 独立验证文件：新增_temp/fps-investigation-20261020/cpu/independent_verify.py及independent-validation.json；逐列独立重算两张各300帧导出表。基准按MCP队列耗时1毫秒阈值剔1帧、299有效帧，第二段300有效帧；全部均值和中位数与performance-summary.json一致。
- 树审查：代表帧PlayerLoop与重绘分支分离，SceneView.Paint和GameView.Paint为同深度兄弟；不将Global与Update父节点、蒙皮子节点等嵌套inclusive指标相加。
- 状态：调查前后Profiler完整data内容一致，enabled/recording/CPU均false；GameView恢复非最大化，场景快照dirty=false。只核已有响应，不把静态证据说成独立重复现场操作。
- 验证结果：26项检查通过。业务脚本、蒙皮、Animator更新在该样本未成为主要负载；Scene视图额外绘制有直接证据。scripts列仅Update.ScriptRunBehaviourUpdate，不含LateUpdate/FixedUpdate，已向主Agent反馈文档精确表述。
- 边界与未完成：镜头移动破坏严格同镜头对照，不报告固定提升比例，不用CPU帧倒数承诺FPS；原截图36.6FPS与本轮异时，GPU单次不是分布，正式Player仍待固定镜头CPU/GPU验证。没有业务编译需求，因为本角色仅新增证据脚本/记录。
- 专题：项目分析/MAIN_CITY_FPS_RUNTIME_20261020.md；主日志：项目修改日志/2026-10-20-FPS-INVESTIGATION-CHANGELOG.md。四协作入口由主Agent统一更新，本角色不并发覆盖。
