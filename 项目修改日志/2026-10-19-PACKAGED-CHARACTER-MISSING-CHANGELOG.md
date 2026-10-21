# 打包程序人物消失：旧AOT主包与新跳跃热更不配套

- 日期：2026-10-19。
- 目标：查明用户打包客户端人物消失、技能栏空的问题，并准备可验收的配套版本；不扩展业务兼容方案。
- 已修改文件：Docs/AI/CURRENT_TASK.md、TEST_REPORT.md、PROJECT_CONTEXT.md、DECISIONS.md；项目分析/README.md、项目难点.md、新专题PACKAGED_CHARACTER_MISSING_AOT_PROTOCOL.md；本日志。独立角色另维护项目修改日志/2026-10-19-PACKAGED-CHARACTER-MISSING-REVIEW.md。没有业务源码/正式配置/资源修改。
- 事实：Player.log第188行本地RoleID2/UID1模型绑定失败，MissingMethodException MoveOperationReq::set_JumpCount；现Build的GameAssembly.dll为11:29，新协议插件为11:46。原Build实际缓存/manifest和Bundles匹配895热更568320字节，IL调用新setter，而旧Metadata实际类型表无该方法；未HTTP查询资源服实时版本。MainCityMgr原catch销毁模型，而EnterWroldReq尚未发送；中心服入场才回11080，HUD同链中断。
- 设计思路：修复配套完整IL2CPP主包，不增加反射/空catch/绕过上报，不把托管Network.dll复制进旧IL2CPP目录当作修复。保留跳跃同步实现。
- 构建前核验：Unity非Play/非编译/非构建，Windows64 IL2CPP，唯一Boot场景HostPlayMode、原地址及未脏；桥接DEVELOPMENT=0匹配，link保两协议all，无需GenerateAll。未打断用户Play。
- 实际构建：原BuildPipeline完整主包输出D:/unitypro/LYFMMORGP/_temp/packaged-character-missing-20261019/player/GM_MMO.exe，未auto_run/覆盖原Build。BuildReport Succeeded，158.8911883秒，0错4警告（MCP任务墙钟160.416秒）。报告_temp/packaged-character-missing-20261019/client/build-report.txt。
- 警告详情与边界：Mogo/Skybox过时SetTexture combiner语法1项，UI/TMP/Common SDF.asset缺脚本1项，FriendChatPrefab/img缺脚本2项；不称已证实旧警告，也不以此代替本轮缺方法根因，未顺带修改。
- 独立审查：review/final-audit.json PASS，最终真实Metadata Req33/Ntf35方法含4个JumpCount存取，匹配895真实热更两方法引用。新GameAssembly52490752字节SHA8AB5E4A6…，Metadata11537512字节SHAC4653C6D…，完整SHA见机器清单。开发/测试两方各7冻结输入保持，原Build不变。
- 文档验证：本轮新增段根相对路径检查、限定git diff --check、5核心输出存在/哈希通过；client/delivery-verification.json保存机器清单。首轮扫到历史协作旧路径，已限定本轮段并保留first证据，没有批量改历史。
- 未解决：临时包尚未启动/登录、正式双端与入场技能画面未验收。用户手动运行完整player目录的新exe后，验主角可见/能动、绑定成功/EnterWorld/11080、技能图标与双端跳跃。正式GameServer透传配套仍按上轮交接核验。本轮没有发布/服务器重启/改账号或数据库。
- 交接：专题项目分析/PACKAGED_CHARACTER_MISSING_AOT_PROTOCOL.md、项目难点.md本轮补充案例；现场/独立证据_temp/packaged-character-missing-20261019/review，客户端构建及哈希证据同目录client。
