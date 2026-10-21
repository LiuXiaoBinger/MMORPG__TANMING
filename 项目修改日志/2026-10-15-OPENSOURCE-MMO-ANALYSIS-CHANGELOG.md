# 2026-10-15 开源 MMORPG 七项目源码解析：修改记录

## 一、任务背景

用户要求对已拉取到 F 盘 `F:\MMORPG_OpenSource\` 的 7 个开源 MMORPG / 游戏服务器项目（ET、FishMMO、UnityMMO、NoahGameFrame、OpenCoreMMO、ServUO、Intersect-Engine）做“超级详细、图文并茂（Mermaid + PNG）”的中文源码解析；为每个项目创建文件夹写解析文档，在 `项目分析/开源MMORPG项目解析/` 下建总索引 README；所有中间草稿、脚本、截图、临时数据放 `_temp/opensource-mmo-analysis-20261015/`。

用户追加硬性要求：第一轮 7 篇完成后，必须由**未参与写作的复查角色**逐项目查漏（7 维度），再由原写作 Agent 按遗漏清单补漏，最后复跑链接/图片/围栏/Mermaid/相对路径/事实引用校验，并在结果中说明每篇初版发现的遗漏、补充内容、最终行数/图片数。

本轮为**纯文档源码分析**，未改 `F:\MMORPG_OpenSource` 任何源码、未改 D 盘业务代码。

## 二、修改/新建文件

| 文件 | 操作 | 说明 |
| --- | --- | --- |
| `项目分析/开源MMORPG项目解析/README.md` | 新建 | 总索引（导航/横向对比/借鉴矩阵/许可证合规/学习路线/统计） |
| `项目分析/开源MMORPG项目解析/ET/ET-源码解析.md` | 新建 | 1758 行 |
| `项目分析/开源MMORPG项目解析/ET/assets/*.png` | 新建 | 2 张 |
| `项目分析/开源MMORPG项目解析/FishMMO/FishMMO-源码解析.md` | 新建 | 991 行 |
| `项目分析/开源MMORPG项目解析/FishMMO/assets/*.png` | 新建 | 2 张 |
| `项目分析/开源MMORPG项目解析/UnityMMO/UnityMMO-源码解析.md` | 新建 | 1187 行 |
| `项目分析/开源MMORPG项目解析/UnityMMO/assets/*.png` | 新建 | 3 张 |
| `项目分析/开源MMORPG项目解析/NoahGameFrame/NoahGameFrame-源码解析.md` | 新建 | 1039 行 |
| `项目分析/开源MMORPG项目解析/NoahGameFrame/assets/*.png` | 新建 | 2 张 |
| `项目分析/开源MMORPG项目解析/OpenCoreMMO/OpenCoreMMO-源码解析.md` | 新建 | 833 行 |
| `项目分析/开源MMORPG项目解析/OpenCoreMMO/assets/*.png` | 新建 | 2 张 |
| `项目分析/开源MMORPG项目解析/ServUO/ServUO-源码解析.md` | 新建 | 1471 行 |
| `项目分析/开源MMORPG项目解析/ServUO/assets/*.png` | 新建 | 2 张 |
| `项目分析/开源MMORPG项目解析/Intersect-Engine/Intersect-Engine-源码解析.md` | 新建 | 1220 行 |
| `项目分析/开源MMORPG项目解析/Intersect-Engine/assets/*.png` | 新建 | 2 张 |
| `Docs/AI/CURRENT_TASK.md` | 追加本轮条目 | 协作记录 |
| `Docs/AI/TEST_REPORT.md` | 追加本轮验收 | 文档验收记录 |
| `项目修改日志/2026-10-15-OPENSOURCE-MMO-ANALYSIS-CHANGELOG.md` | 新建 | 本日志 |
| `_temp/opensource-mmo-analysis-20261015/` | 新建（中间产物） | 解析规范、许可证事实、绘图脚本、校验脚本、各项目草稿/复查报告，不随正式交付 |

## 三、产出内容

1. **总索引 README**：7 项目导航表、按技术形态分组、关键工程维度横向对比总表、对 LYFMMORGP 的借鉴价值矩阵、许可证商用合规对照、学习/面试路线、目录结构、每篇统计与遗漏补漏记录、未验证声明。
2. **7 篇主文档**：每篇按统一 26 节大纲，覆盖项目概览、技术栈、许可证、目录结构、工程打开方式、编译运行步骤、整体架构、各端职责、启动流程、登录流程、角色进入场景、移动同步、战斗/技能/AI、背包/任务/交易等系统（按项目实际）、网络协议、数据存储、关键核心类、典型代码走读、可借鉴到 LYFMMORGP 的设计、局限与坑、面试话术与高频追问、附录。
3. **图表**：共 15 张 PNG（每项目 2~3 张，中文架构/拓扑/流程图）+ 63 张 Mermaid 图（目录结构、架构、时序、状态机、流水线等），三必备图（目录结构图/整体架构图/登录时序图）每篇齐全。

## 四、质量流程（写作 → 独立复查 → 补漏 → 校验）

1. **写作**：每个项目一个专属 SuperDoubaoAgent，独立完成“源码扫描 → 官方文档读取 → 关键代码取证 → 图表生成 → 文档撰写”。
2. **独立复查**：派出 5 个未参与写作的复查 Agent（A~E），按 7 维度（目录覆盖、关键系统、构建步骤、图表、代码路径真实性、结论证据、借鉴点与面试问答）逐项目复查，报告写到各自 `drafts/复查遗漏清单.md`。
3. **原作者补漏**：唤醒各写作 Agent 按清单补漏。
4. **复跑校验**：用自动校验脚本 + README 校验脚本复跑。

复查/补漏中纠正的关键事实：
- **ET**：尾部“假结尾”拼接事故与断裂的 §23 借鉴表修复、删 8 个凑数空小节；自研 cn.etetet.* 包为 56 个（非 61，61 含 com.unity 官方包）；Entity 注解为 `[MemoryPackable(NoGenerate)]+[BsonIgnore]`；补 truesync 定点数学库。
- **FishMMO**：怪物 AI 实为 NavMeshAgent + 9 状态 FSM；Redis 工厂接线但无业务调用；伤害为 ScriptableObject 固定 DamageAmount；补任务三层模型/对话树。
- **UnityMMO**：msgagent 池确为 10；tag 改为 msgagent 层 300~499 合并路由；AOI 半径为 RoleMgr.lua:107 硬编码。
- **NoahGameFrame**：删除不存在的 NFIPluginModule.h 引用；json.lua 归 _Out 子模块；补 Mermaid 目录结构图；图编号重排。
- **OpenCoreMMO**：DI 容器实为 Microsoft ServiceCollection（非 Autofac）；模块 15 个；Dispatcher 30s CTS 未传 token、真实防卡靠 try/catch。
- **ServUO**：包表 0x0A 是 Edit、EquipReq 在 0x13；0x73 PingReq 的 ingame=false；补 Party、Targeting；Scripts 目录 17 个。
- **Intersect**：聊天频道表重写；PacketHandler 服务端 3202 行/67 方法；网络 RSA-2048+AesGcm（4096 仅 Web JWT）；迁移分 schema 约 394 + 数据迁移 5 个；删约 100 行注水克隆段。

## 五、验证结果

自动校验（`verify_docs.py` / `verify_readme.py`）+ 主 Agent 人工目检 PNG：

| 项目 | 行数 | Mermaid | PNG | 不重复源码引用 | 缺失引用 |
| --- | --- | --- | --- | --- | --- |
| ET | 1758 | 9 | 2 | 78 | 0 |
| FishMMO | 991 | 8 | 2 | 172 | 2（运行时文件，已说明） |
| UnityMMO | 1187 | 9 | 3 | 82 | 2（未拉子模块，已说明） |
| NoahGameFrame | 1039 | 10 | 2 | 77 | 2（未拉子模块/vcpkg，已说明） |
| OpenCoreMMO | 833 | 8 | 2 | 52 | 0 |
| ServUO | 1471 | 10 | 2 | 66 | 0 |
| Intersect-Engine | 1220 | 9 | 2 | 63 | 2（运行时文件，已说明） |

- 7 篇代码围栏全配对、无空 Mermaid、15 张 PNG 引用全部存在；总 README 14 个相对链接全部有效。
- 合计 590 个不重复源码引用；8 个“缺失”均为运行时生成文件（logging.json/appsetting.json/update.json/version.client.json）或未拉取子模块/vcpkg（skynet.lua/json.lua/config_scene/bufferevent.h），文档已说明，无凭空捏造路径。
- 抽查 ServUO/Intersect/ET 的 PNG 中文渲染与布局正常。

## 六、未解决问题

- 7 个项目全部**仅静态源码走读，未编译/未运行**（各项目缺 Unity/.NET/数据库/C++ 等环境，已在每篇文档显著位置诚实标注）；运行时行为（端口连通、登录走通、移动表现、存档落盘等）均为静态推演，不得当作已验证运行结果。
- 各项目的第三方子模块未拉全部分（UnityMMO skynet/lsocket、NF Dependencies 与 json.lua），相应 C 层未走读。
- 若要把某个具体机制落到 LYFMMORGP，需另立任务并走开发/测试流程。

## 七、回滚方式

删除 `项目分析/开源MMORPG项目解析/` 整个目录与本日志即可；`Docs/AI` 本轮条目可按标题段删除；F 盘源码与 D 盘业务代码、正式资源均未改动。
