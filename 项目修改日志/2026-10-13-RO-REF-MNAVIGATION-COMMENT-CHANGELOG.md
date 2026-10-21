# 2026-10-13 RO 原版 MNavigationComponent.OnAfterAction 中文注释（仅注释，外部参考文件）

## 一、需求与范围

- 用户要求为 RO 原版参考文件 `F:\UnityPro\ro\Ro_cs422nb\clientcode\CSProject\MoonClient\Skill\MNavigationComponent.cs` 中 `protected void OnAfterAction()` 方法补充中文注释，并对直接相关接口补充必要 XML 注释。
- 严格范围：**只加注释，不修改任何逻辑、签名、命名空间、格式或代码结构**。
- 明确不改动 D 盘 LYF 移植版 `D:\unitypro\LYFMMORGP\LYF_MMO\Assets\GM_Game\Scripts\SceneEntity\Component\MNavigationComponent.cs`（已确认未触碰）。

## 二、修改文件

- 唯一修改文件：`F:\UnityPro\ro\Ro_cs422nb\clientcode\CSProject\MoonClient\Skill\MNavigationComponent.cs`
- 文件编码/换行保持：UTF-8 无 BOM、全 CRLF（修改前后均无 BOM、无裸 LF、严格 UTF-8 校验通过）。
- 原文件备份：`_temp/ro-nav-comment-20261013/MNavigationComponent.cs.orig`（改动前 48775 字节）。
- 注释脚本（可复核）：`_temp/ro-nav-comment-20261013/add_comments.py`（按整行精确匹配插注释，写入前校验去注释后代码一致，否则不落盘）。

## 三、注释范围（共新增 45 行注释，0 行业务代码改动）

1. 方法级 XML：把 `OnAfterAction` 原“到达回调”单行 `<summary>` 扩充为完整说明（用途、调用方 closeTargetAfter、向服务器发送 RpcC2G_SingingRequest、目标/打击点/空气墙/导航点/Common 坐标编码/发送成功本地预表现，命中以服务器结算为准），并新增 `<remarks>` 列出六步流程。
2. 方法内行内注释（逐段）：
   - 实体与 Buff/技能/属性组件就绪校验、状态机吟唱状态(State_AJ_Singing)互斥；
   - 技能事件参数 `s_args`(Identify/Target/CastPoint) 与原始技能核心 originSkillCore 的语义；
   - 连击(Combo)替换技能，以及发包 SkillId 仍用原始 ID；
   - 替换后核心/主表/效果行/CanSkill 二次校验；
   - 组装 RpcC2G_SingingRequest：SkillId（经 Buff 还原原始ID）、Target、Common；
   - 需目标分支的空气墙（气墙）遮挡检测、真剑百破道 Buff 锁定目标、最近可行导航点吸附 TryGetClosetValidPt；
   - 无目标/地面点分支的气墙检测与 Target=0；
   - 矩形/环形范围技能的二次导航点吸附；
   - MPlayer.CorrectSyncPos 精度档位 2 的网格量化与可行走点吸附；
   - `Common` 整数编码：UUID(1~9999) 与 x/y/z 厘米坐标按每段 ×100000(10^5) 位域打包，坐标 (米+0.005)*100 取整为厘米，服务器按同一位域拆包；
   - 打击点非法仅记错误日志、不阻断；
   - `MNetClient.Send` 发送成功后锁定移动/等待服务器，Buff 吟唱时间修正，CanPerformance 下吟唱事件 MEvent_Singing 或瞬发 FireSkill 本地预表现。
3. 相关接口 XML：仅为该组件的上游移动指令入口 `OnGetEvent(IEventArg e)` 补充 `<summary>` 与 `<param>`（其确定 OnAfterAction 消费的 `_afterAction`、`_tarPosition`）；未做全文件大范围注释。

## 四、验证结果

- 脚本写入前断言：方法 XML 头、OnGetEvent 声明、28 处行内锚点均整行唯一命中，否则中止（中途因外部编辑器折行导致锚点不匹配，已中止并以当前磁盘文本重做，未产生脏写）。
- 代码一致性：独立用备份文件与改后文件做“去除整行注释后逐行比对”，代码行均为 1139 行且**完全相等**，证明未改动任何代码（含签名、逻辑、字符串、空行）。
- 编码/换行：改后仍为 UTF-8 无 BOM、CRLF=1378、裸 LF=0、严格 UTF-8 解码通过。
- 已重新 Read 方法全段（约 293–472 行）与 OnGetEvent（约 1154–1159 行），目检注释位置、缩进、方法闭合正确。

## 五、未解决问题/边界

- 未对 RO 参考工程执行整体编译（外部 Unity 参考客户端，本次为单文件纯注释；“去注释后代码逐行一致 + 编码/换行不变”已可证明不会改变编译结果）。
- 注释依据来自该文件现有代码与 `MPlayer.NextSkillUUID`(1~9999 循环)、`MPlayer.CorrectSyncPos(ref pos, accuracy)`（按精度档位量化并吸附可行走点）的实际定义；服务器端 Common 位域的具体拆包代码未在本次范围内逐一核对（已按客户端打包位域 10^5 与厘米取整如实描述）。
