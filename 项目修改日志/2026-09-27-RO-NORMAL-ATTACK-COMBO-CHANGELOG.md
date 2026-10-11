# 2026-09-27 RO 普攻三段轮换修改日志

参考与设计：`项目分析/RO_NORMAL_ATTACK_USING_TIMES_DESIGN.md`。客户端、独立测试与最终验收由对应 Agent 追加各自章节。

## 服务端开发与协议同步

- 修改文件：`LYF_Server/GameServer/Components/RoleSkillComponent.cs`、`LYF_Server/ProtoBuf/proto/ResultEntity.proto`、`LYF_Server/Network/Proto/ResultEntity.cs`、`LYF_MMO/Assets/GM_Game/Plugins/Net/Network.dll`。
- 协议先新增 `SkillCastRet.using_times=11`、`SkillCastStartNtf.using_times=7`，保留旧字段号和请求结构；在 `_temp/combo-server/proto` 使用 protoc 3.21.9 生成后同步正式生成代码。生成代码为第三方序列化边界，不手动修改其命名空间或生成语法。
- 对照 RO 服务端 `skill_core.cpp:585-610` 与 `1099-1119`：各角色各技能维护成功使用计数及上次逻辑 Tick，首次为零、间隔超过默认六秒重置、有效连续使用按当前表现数量取模。拒绝和缓存重传在推进前返回，不消耗下一动作。离线释放组件时清空计数。
- 表现行按效果、职业、职业表现表的模型键和默认武器零筛选，再按 `VariantIndex` 排序，与客户端装配一致。本项目当前没有武器表现属性，沿用默认武器，不添加额外武器系统。
- 多动作技能活动时长取选中表现行的 `DurationMs`，单动作和无表现技能保持 `CastMs+RecoveryMs`。Q 原 MP20/CD500 配置未修改。
- 开发构建：`dotnet build LYF_Server/GameServer/GameServer.csproj --no-restore --configuration Debug -p:OutputPath=D:/unitypro/LYFMMORGP/_temp/combo-server/bin/ -p:BaseIntermediateOutputPath=D:/unitypro/LYFMMORGP/_temp/combo-server/obj/ --verbosity quiet -clp:ErrorsOnly`，结果零错误、11 条警告。
- DLL 同步：上述输出的 `Network.dll` 复制至客户端正式插件，两份 SHA256 一致为 `D4FBE52ACCB6A58BE66A816D353138F31F553865472B00EF886B94E022004B90`。
- 最新三段表现表已复制至 `_temp/combo-server/bin/LubanManage/Tb/tbskillpresentationinfo.bytes`，759 字节，SHA256 与正式表同为 `502F5FC7D19DD28924ECF0B689105F1356FD66C781709AA043D5AE8D98C1E3CF`；独立测试 Agent 另建最终隔离输出做组件断言，开发侧不重复执行测试。
- 静态检查：修改业务文件未检出 `namespace`、`foreach`、三目、`dynamic` 或 `object[]`；目标差异空白检查通过。
- 待独立测试/审查：真实源码组件段序 0→1→2→0、六秒重置、失败/重传不推进、第三段完整时长；真实四服与 Game View 按键视觉由用户手动验证。本节构建结论不代表运行联调通过。
- 临时产物：协议生成、构建输出及中间文件全部位于 `_temp/combo-server/`，保留等待产品经理确认清理。

## 客户端开发与配置

- 修改文件：`LYF_MMO/Assets/GM_Game/Scripts/SceneEntity/Skill/MSkillCore.cs`、`MSkillFactory.cs`、`MSkill.cs`、`SceneEntity/Component/Action/MSkillComponent.cs`、`controller/UIController/SkillCtrl.cs`；新增单类型文件 `MSkillData.cs`、`MSkillPrediction.cs` 及对应 `.meta`，并补充 `LYF_MMO/GM_Game.csproj` 的独立构建索引。
- 工厂按原有效果、职业、模型键、默认武器收集表现行并按 `VariantIndex` 排序。技能核心依 RO `UsingTimes` 逐次轮换，首次第零段、间隔超过十秒重新第零段；无硬编码 Q/10010、动画路径或段数。
- 单次 `MSkill` 捕获本段路径、段号与独立时长。多段技能动画结束后移入等待列表，立即腾出动作槽；独立预测记录仍按请求序号等待权威结果或动作时长加两秒超时。旧结果与超时只清对应旧段，不能覆盖新段 CD；全角色最后请求序号保护 MP/组 CD，避免旧 Q 结果覆盖后来 E 的快照。
- 已退出动作槽的旧段立即归还并断开动画器租约引用；后续回包/超时清理旧实例不会误释放第四段再次使用的第一段动画。单段技能保留既有占用行为。
- 客户端消费结果和开始通知的 `UsingTimes`；自己的已绑定预表现仅校正核心供下次使用的计数，不重播；远端按权威序号选段。
- 表现表追加两行：`3001002/VariantIndex=1/Anim_MJS_Atk02/1967ms`、`3001003/VariantIndex=2/Anim_MJS_Atk03/2500ms`；现有第一段为 `2167ms`。时长沿动画索引换算到最近整数毫秒，误差小于一毫秒；现有 `animInfo.txt` 已登记三段，未修改。
- 配置使用 spreadsheets 技能和 bundled artifact-tool 窄追加，原五行值矩阵未变，新行复制邻行格式；前后渲染核验正常且错误公式零命中。原 Excel 工作簿曾被占用，主 Agent 通过 Excel COM 仅关闭已保存且完整路径匹配的指定工作簿后，客户端重新读取原五行并窄追加两段到正式 `Luaban/MiniTemplate/Datas/#SkillPresentationInfo.xlsx`。重新生成与先前部署的正式二进制哈希一致；暂存版本和导出自动检查文件统一保留于 `_temp/ro-skill-workbooks/`。
- Luban 生成并部署正式 `LYF_Server/Network/LubanManage/Tb/tbskillpresentationinfo.bytes`，七条表现行、759 字节，SHA256 为 `502F5FC7D19DD28924ECF0B689105F1356FD66C781709AA043D5AE8D98C1E3CF`。旧技能表与效果表重新生成后二进制和正式版本哈希一致，未变更 MP20/CD500 或其他数值。
- 客户端开发构建：`dotnet build LYF_MMO/GM_Game.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/skill-combo-client-build --verbosity quiet -clp:ErrorsOnly`，最终零错误、42 条既有警告（上一轮增量构建为 38 条）。范围禁用语法扫描无命中，目标 `git diff --check` 通过（只有既有 CRLF 提示）。
- 独立审查及运行验收尚在进行，未以编译代替 Game View 手动按 Q 的视觉验收。未添加自动键盘探针、输入队列或自动三连播放。

## 最终独立验收与交接

- 独立终审通过，无剩余阻断项。最终客户端隔离构建 0 错误、42 条既有警告，服务端隔离构建 0 错误、11 条警告；证据位于 `_temp/skill-combo-review/`。开发侧增量构建的 38 条警告不替代最终完整构建记录。
- 正式客户端核心源码断言通过段序、十秒重置、失败不推进、重装继承和旧回包/超时隔离；服务端真实组件断言通过六秒边界、拒绝/重传不推进、三段时长与结束帧、协议往返和其他单动作回归。旧实例退出动作槽后断开其动画器租约，后续旧实例 Reset 不释放新段同路径动画。
- 正式 Excel 源已解除占用并写入，仅追加两段；再次 Luban 生成与正式部署 bytes 一致。原五行业务值一致；归档检查的空音频字符串与正式空白 null 都表示无音频，不主张 JSON 逐字一致。原技能主表与效果数值哈希保持不变。
- 规范复核：目标业务无新增 namespace、foreach、三目或弱类型业务载荷，中文注释已补齐；`git -c core.whitespace=cr-at-eol diff --check` 通过。协作记录已同步 `Docs/AI/CURRENT_TASK.md`、`Docs/AI/TEST_REPORT.md`。
- 尚未执行用户 Game View 手动按键和真实四服联调；内存服务端夹具的无网关发送失败日志为预期。下一步手动逐次 Q 核对 Atk01→02→03→01、长间隔重置、第三段完整播放与移动阻断；联机检查本人不重播、远端段数、MP/CD。当前 11171 先于 11170 的极端乱序限制沿用原技能链，未新增额外网络恢复机制。
