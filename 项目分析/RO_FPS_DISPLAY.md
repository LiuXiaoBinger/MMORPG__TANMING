# RO帧率显示源码核对

日期：2026-10-20。用户仅询问RO是否有游戏帧率显示，本轮只读参考源码，不移植功能。

## 已确认实现

- `F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/Unity-Logs-Viewer/Reporter/Reporter.cs`：运行时日志面板带FPS，累计真实时间和帧数，满足时间大于0.25秒且帧数大于10时计算frames/dt。面板工具栏和信息页显示fpsText，支持showFps开关；显示字符串上限99，不等于实际帧率被限制99。
- `F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/_Scenes/GameEntry.unity`：Reporter脚本GUID对应组件已启用，序列化show=0，面板默认隐藏。ReporterGUI负责OnGUI绘制，Reporter.Update持续统计。不能据此断言所有发行版本必定开放入口。
- `F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/Source/Generate/ReporterWrap.cs`：fps/fpsText已提供Lua绑定。
- `F:/UnityPro/ro/Ro_cs422nb/clientproj/Assets/Scripts/_Tmp/FpsCount/MyFPSCounter.cs`：另一套独立统计组件，默认保留60个逐帧整数FPS，计算平均/最高/最低，使用1/Time.unscaledDeltaTime。
- 同目录`MyFPSDisplay.cs`：三个UI Text显示上述结果，范围0～99；不是Reporter采用的统计窗口，Reporter仅借用其数字字符串表。
- `_Tmp/libtest/TestAssembly.cs`中的LoadFpsPanel/FPS_Panel创建入口已经注释，未在当前clientproj预制体和场景文本中找到上述两个临时组件GUID的挂载引用，不能把该测试面板说成默认正式HUD。

## 验证范围

已核对源码、脚本meta GUID、GameEntry场景序列化和Lua绑定。未启动RO、未修改RO或本项目业务、未编译运行；只证明当前参考源码有实现及场景接入，不声称实际包已开启常驻显示。若后续用户要求移植，再按最小计数和显示职责接入。

修改记录：`项目修改日志/2026-10-20-RO-FPS-DISPLAY-CHANGELOG.md`。
