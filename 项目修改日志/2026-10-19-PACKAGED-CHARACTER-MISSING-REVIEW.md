# 打包客户端人物消失：独立测试记录

## 范围与证据

- 测试职责只读检查真实 Player.log、正式旧包及热更代码；不改业务、不停用户会话、不部署、不覆盖正式 Build。临时检查工具与依赖均在 `_temp/packaged-character-missing-20261019/review/`。
- 本记录与问题分析互引：专题 `项目分析/PACKAGED_CHARACTER_MISSING_AOT_PROTOCOL.md`（客户端职责维护）；协作入口由主职责统一更新，避免并行覆盖。
- 原 Player.log 复制为 `Player.snapshot.log`，11117101 字节，SHA256 `8F763A0CF85960E2AFC608CF7235CC27D4019EDFA9CEB516A18E53DC1DC96711`，没有清空或改写原日志。日志启动路径为 `LYF_MMO/Build/GM_MMO_Data`，取证时无存活 GM_MMO 进程，因此 exe 路径由日志而非存活进程确认。

## 已定位问题

1. 日志第171行本地持久角色 ID 为2，第188行表现绑定失败实体 UID 为1，两者不是同一种 ID。实际异常为 `System.MissingMethodException: MethodNotFind MoveOperationReq::set_JumpCount`。快照中出现29896次该缺方法信息（包括绑定失败一次），绑定成功日志零次。
2. 实际旧包 `GameAssembly.dll` 生成于11:29，真实 `global-metadata.dat` 不含 `JumpCount` 字符串。旧包备份 Managed/Network.dll 亦无两个消息的 JumpCount 存取方法。当前源码协议插件与12:59新生成的裁剪AOT含这些方法，但不能据此声称正式包已经更新。
3. 正式旧包缓存的2026-10-19-895 DLL Bundle哈希与清单一致。直接读取该真实Bundle的 `GM_Game.dll` 为568320字节，SHA256 `057685A6A982E2FFAE821A7EBE291304BDB60916CC00E96BC947283BE6C8BC23`，与正式 Dlls 字节逐字相同。实际热更IL已经调用 `MoveOperationReq.set_JumpCount`，故不是只看源码推断版本差异。
4. 实际热更IL与源码一致：`MainCityMgr.CreateRole` 先 `PlayerEntity.UpdateMove`，后发送入场请求。前者缺方法抛异常后进入原catch，销毁刚创建的模型；入场请求未发送。`CenterRoleCtrl.OnEnterWroldHandle` 在入场请求后返回11080技能列表，故同一次中断也能解释技能槽空白。没有据截图猜测模型资源丢包或Shader故障。

## 方案与当前结果

- 方案是使用新协议构建配套IL2CPP主包；复制托管Network.dll到旧IL2CPP安装目录不能替换已编译的原生类型。无需为此次部署不配套给业务增加兜底、等待或反射。
- 已冻结7项正式输入，见 `frozen-inputs.json`；正式 Build 未修改。原包专项结果 `old-package-audit.json`，直接提取的实际热更IL为 `actual-UpdateMove.il.txt`、`actual-CreateRole.il.txt`。
- 客户端职责已在 `_temp/packaged-character-missing-20261019/player/` 构建完整配套主包。独立读取真实 `client/build-report.txt`：Succeeded，BuildReport耗时158.8911883秒，0错误、4警告。警告是Mogo/Skybox不再支持SetTexture combiner语法1项、Common SDF资源缺脚本1项、FriendChatPrefab/img缺脚本2项；不将这些称为已确认的历史警告，不扩大修改无关资源。
- 独立直接解析最终临时包实际元数据中的Network方法表：MoveOperationReq由31方法变33方法，SyncMoveNtf由33变35，均含JumpCount getter/setter；实际895热更引用的两个方法均能在最终类型表中找到。这里核的是最终输出，不是Library或备份源码。结果 `new-native-types.json`、`actual-hot-jump-references.json`、`final-audit.json`。
- 实际新GameAssembly.dll为52490752字节，SHA256 `8AB5E4A63A5DE3885821C20703D0EC59E01A6F0C08649B2D5CF1A1211F17D4D8`；新global-metadata.dat为11537512字节，SHA256 `C4653C6D66ACA17F657BED5D1DDE6338CF28C028E5E56D57F51A00B7502BB8C3`。
- 独立重核开发与测试各自冻结的7项输入全部保持，正式Build的GameAssembly及Metadata没有覆盖，热更字节、正式协议插件、Boot和配置保持。895为此次原Build实际缓存/清单与Bundles核对结果，不将其扩称为HTTP实时资源服务版本查询。
- 专项结果PASS，未启动临时包、未登录账号，尚未验收新主包登录、模型绑定、技能列表及双端跳跃。最终实际接口核验通过不等于修复后端到端画面验证完成。新主包是整个player输出目录，不能只拿exe替换旧程序。
