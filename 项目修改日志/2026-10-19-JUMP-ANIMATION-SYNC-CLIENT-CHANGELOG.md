# 2026-10-19 远端跳跃动画同步：客户端修改

## 问题与证据

用户操作 A 跳跃，B 已能看到 A 的高度上升，但仍播放行走动画。修前移动协议及客户端移动事件只有坐标、朝向、耗时和移动态；本地 `BeginJump` / `EndJump` 派发的 `JumpStarted` / `JumpEnded` 没有经过远端移动链。高度到达并不表示跳跃动画状态已同步。

## 修改文件与最小方案

- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Entity/PlayerEntity.cs`：上报现有 `JumpCount`；段号变化优先立即发送，不受厘米位移、0.1 秒节流或零累计耗时影响。普通移动仍保留原有节流及正耗时发送规则，实体复用清除上次段号。
- `LYF_MMO/Assets/GM_Game/Scripts/Manger/MainCityMgr.cs`：把通知 `JumpCount` 写入原移动事件。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/event/eventclassArg/IEventArg.cs`：原 `MMoveEventArgs` 增加段号；工厂默认零保证既有输入兼容，克隆与回池重置一起处理。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MMoveComponent.cs`：正段号即时通知既有跳跃表现组件，重复段不重播；零段延迟到影子实际吸附/追到落地目标，或技能根位移已抵达同步目标时才通知落地。纯 Y 追赶不会因水平移动态结束而提前返回。远端只切表现标志及段号，不调用本地起跳，不重置垂直速度、不再模拟一次重力。
- `LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Component/MJumpComponent.cs`：落地后是否继续走路，本地仍读真实方向输入，远端读同步移动态；静止落地仍播放一次 `Jump_End`。

沿用既有移动事件、跳跃组件与影子坐标流程，没有新增同步管理器、请求队列或按高度猜测起跳。默认动作 `IsActiveMovable` 继承恒真，不会因 `IsJump` 变真锁住追赶；技能 `MoveCasting` 等既有门禁保持。

协议由服务端职责修改：现请求 `jump_count=8`、通知 `jump_count=9`，C# 均为 `int JumpCount`，0 为落地，1～3 为本轮滞空起跳次数。客户端引用其生成的 `Network.dll`，SHA256 为 `4429175E2D87C4F8D00CC6E78D6B178955AA7BAE6EAEEAD7428EDED718E6DBBF`。

## 验证结果与边界

- 本轮 280 个真实 GM_Game 源独立 Rebuild：0 错误、38 条既有警告；末稿五源与编译输入 SHA 一致。
- 五源限定 `git diff --check` 通过。修前备份、编译日志和末稿清单位于 `_temp/jump-animation-sync-20261019/client/`。
- 独立审查职责正在执行真实方法提取行为测试，结果由主 Agent 汇总到协作入口和总日志。
- 本客户端开发职责没有调用 Unity MCP、停止当前 Play、重启服务、发布资源或保存正式场景；本轮独立编译不代表正式双客户端画面已验收。新协议与客户端/服务端代码须配套生效，正式运行验收由主 Agent 协调。

## 交接

末稿清单：`_temp/jump-animation-sync-20261019/client/final-manifest.json`；编译输入：`_temp/jump-animation-sync-20261019/client/compiled-source-hashes.json`；日志：`_temp/jump-animation-sync-20261019/client/GM_Game-build.log`。主 Agent 负责四入口、难点复盘及最终验收；本文仅记录客户端职责事实。
