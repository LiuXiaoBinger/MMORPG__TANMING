# 主城帧率实际检查

- 独立复核26项通过，主验收复算两段统计并核文档引用与恢复状态；证据_temp/fps-investigation-20261020/root-final-validation.json。仅诊断与记录，无业务编译或正式Player验收；下一步固定镜头独立客户端采样。

- 日期：2026-10-20。
- 用户要求：检查截图36.6FPS主城低帧率，用户随后开启Unity MCP要求重新查。
- 本轮仅诊断：主采原生运行Profiler与渲染归属，fps_cpu_audit/fps_scene_audit只读源码、日志和资源；不修改业务、资源、灯光、质量或正式构建。
- 文档修改：项目分析/MAIN_CITY_FPS_RUNTIME_20261020.md新专题、README.md索引、项目难点.md案例；Docs/AI四入口写当前结论/验证/边界；本根日志及子角色日志归档。中间脚本、原始Profiler和MCP响应均在_temp/fps-investigation-20261020。
- 验证：真实CPU两段300帧，基准排1个MCP诊断尖峰后299帧main均19.927毫秒、SceneView7.949/GameView7.992；Game最大化段300帧main均11.288、SceneView0。期间镜头移动，禁止声称严格A/B或FPS收益。Update脚本阶段约0.407毫秒（不含LateUpdate/FixedUpdate），蒙皮约0.187毫秒，当前不支持作为主因。
- 渲染归属：54活动蒙皮为28CALISTA+23INQUISITOR+3Demon，后快照46任意相机可见；1932Renderer已静态批处理、675LODGroup、66共享材质实例。GPU使用RTX4070，单次GPU计时6.094毫秒不等于分布。
- 临时状态：原Profiler关闭/CPU区域false，调查结束全部恢复；GameView由非最大化临时改最大化后恢复false，场景dirty=false，Console0错误警告。不关闭角色、不改已有操作状态。
- 失败尝试：初次并行MCP请求同id造成错配/超时，改唯一id串行重取；Python输出编码问题以UTF8重取。采样诊断自身尖峰据真实marker剔除，阈值与全部原帧均留存，不清数据制造提升。
- 未完成：严格固定镜头与独立Player CPU/GPU/FrameDebugger采样尚未做，原图36.6FPS非本轮同一时刻。优先单Game视图减少Editor重复绘制，游戏本体改动待实测热点，无稳定FPS承诺。原始约911MB临时文件保留待用户确认清理。
