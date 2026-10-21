# 打包客户端不进入登录：检查记录

日期：2026-10-12。需求为检查用户打包后停在“没有资源更新”界面的原因。

检查结论：资源初始化及三份补充元数据已成功，加载热更GM_Game时因旧主程序缺少MMoRpgCommon/KnapsackType抛出TypeLoadException，协程在进入登录场景之前中断。运行的Build主包为8月26日产物，当前热更/生成AOT产物为10月12日；需要重构客户端主程序及配套资源，不能仅重新打YooAsset包。

正式修改范围仅协作与分析文档：`Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`、`项目分析/BUILD_LOGIN_STUCK_AOT_MISMATCH.md`及本记录。开发子Agent负责核对程序集产物；未修改启动业务、技能逻辑或AOT配置，未替换用户主包、发布资源、清缓存或关闭进程。

设计思路：以实际Player错误为依据，区分资源包构建、HybridCLR裁剪临时主包和真正客户端Build。当前link已保留KnapsackType，不擅自增加共享DLL热加载或其他框架。

验证：现有运行日志、主包路径及日期、ScriptingAssemblies列表、当前link及裁剪AOT类型核对。Unity MCP没有可连接会话；本轮没有新主程序运行验收。未解决项为重新构建配套完整主包并确认登录场景加载。

进一步核验：开发解析真实IL2CPP29元数据，旧主包没有MMoRpgCommon镜像或KnapsackType定义，今日裁剪临时工程的metadata均有。Cecil确认两份共享插件相同且定义该类型、当前裁剪DLL定义该类型、GM_Game引用它；主核证据与原Player错误一致。未因双插件擅自删除正式资产。

操作说明与后续验收见`项目分析/BUILD_LOGIN_STUCK_AOT_MISMATCH.md`；证据目录`_temp/build-login-stuck-20261012/client`。
