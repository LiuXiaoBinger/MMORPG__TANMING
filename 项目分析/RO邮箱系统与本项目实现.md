# RO 邮箱与 LYFMMORGP 实现

玩家从主城功能菜单的「邮箱」进入。左栏按未读、未读中的重要标记、发送时间排列。选中普通信会自动标为已读，选中附件信会显示物品、数量和绑定状态，点击领取才入包。单封可以领取或删除，一键领取逐封结算，「删除已读」经确认后执行。

## RO 源码与职责

RO 源码路径均相对于 `F:/UnityPro/ro/Ro_cs422nb`。本项目路径均相对于项目根目录。

| 职责 | RO 源码入口 | 在本项目中的对应 |
| --- | --- | --- |
| 邮件数据与协议 | `clientproj/Assets/Scripts/Lua/ModuleMgr/EmailMgr.lua` 的 `RequestGetMailList`（42 行）、`RequestGetOneMail`（144 行）、`RequestMailOp`（295 行）、`OnUpdateMailNtf`（368 行） | `EmailMgr` 管理信封摘要、按 UID 缓存的详情和未读通知 |
| 窗口交互、面板和列表行 | `clientproj/Assets/Scripts/Lua/UI/Handler/EmailHandler.lua`、`UI/Panel/EmailPanel.lua`、`UI/Template/EmailItemTemplate.lua`（后两者相对于 Lua 目录），以及 `clientproj/Assets/artres/Resources/UI/Prefabs/Email.prefab` | 保留 `EmailHandler → EmailPanel → EmailItemTemplate`。本项目由 `MainView` 注册 `WindowBase`，`MainCtrl.ShowMainWindow` 管理显隐 |
| 角色收件箱 | `roserver/masterserver/mail/role_mail.cpp` 的 `GetAllMail`（362 行）、`GetOneMail`（418 行）、`ReadAll`（585 行）、`AddMail`（825 行）和 `InnerDelete`（1003 行） | MS 的 `RoleMail` 分派请求。中心服的 `MailStore` 存储收件记录、筛选有效期、执行删除、处理容量 |
| 领取附件 | `roserver/gameserver/protocol/mail/rpcm2g_givemailprize.cpp` | `GiveMailPrize` 在角色所属 GS 逐信核算背包容量，通过现有 `RoItemComponent.AddItem` 发物品 |
| 协议及模板 | `roserver/tlog/pb-py/proto/pbc_mail.proto`、`config/Table/CSV/MailTable.csv` | 保留列表、详情、操作和未读通知的拆分及操作枚举。MS 的 `MailTable` 读取已接入的系统模板 20、21 |
| 全服投递 | `roserver/masterserver/mail/global_mail_manager.cpp` | 中心服在发送时为本区已建角色写入独立收件记录，离线角色登录可见。该实现使用现有中心库，全服发送写入量随已有角色数增长 |

## 一封信的生命周期

1. 服务端业务调用 `MailAction.SendCustomMail` 或 `SendTemplateMail`。指定角色号可以离线投递，角色号传零表示投递本逻辑区发信时已存在的角色。模板 20、21 分别使用 RO 的背包满、负重满文案，均为 30 天有效；业务可自定义标题、正文、署名、附件和有效天数。
2. MS 解析模板，中心服将每封信的摘要与序列化正文写入 `role_mail`。入库成功后通知在线角色。登录时重新查询未读状态。
3. 客户端先取最近 100 封的摘要，点击某封时按 UID 拉取正文。服务端按角色身份和有效期查询。到期邮件从列表和领取候选中移除。
4. 领取时先计算整封附件的容量，包括同类堆叠、绑定状态及多种附件共同占用的格子。GS 将成功的邮件 UID 和本次角色背包版本一起提交。中心服在同一事务保存背包和已读标记，回执成功后通知玩家。连续点击同一封只发一份。一键领取中某封放不下时，该封保持未读，其他可领取邮件继续结算。
5. 未领附件禁止单删，批删只处理已读信。有效收件达到 100 封后优先淘汰已读，再从最早 100 封里找无附件的信；仅剩未领附件时允许继续增长。达到 500 封再收信会淘汰最早一封。被强制淘汰的未读附件记录保留至既有在途领取可结算，客户端立即失去可见性。

## 服务端接入示例

以下调用位于已连接本区 MS 的 GS 业务代码。返回值表示请求已交给内部连接，入库结果由 MS 回执。

```csharp
// 将两件绑定材料连同模板 20 的提示寄给触发奖励的角色。
MailAction.SendTemplateMail(roleId, 20, new List<MailItem>
{
    new MailItem { ItemId = 8201, Count = 2, IsBind = true }
});

// 给发信时已有的本区角色投递一封七天有效的纯文本通知。
MailAction.SendCustomMail(0, "维护通知", new MailContent
{
    SendName = "运营组",
    Content = "今晚维护后可以继续登录。"
}, 7);
```

模板随 MS 内嵌发布。若某项掉落或活动需要通过邮箱补发，由对应业务在自己的结算点调用上述入口。玩家互寄、帮会代写和付费邮件属于 RO 的其他业务，本次实现覆盖角色系统收件箱。

## 核验

服务端使用独立 MySQL 库、本地随机端口和真实 Gate、GS、MS、Center、Control 进程。检查了离线和全区投递、详情、过期过滤、重复领取、整封容量、一键部分成功、删除、重登、模板编号及两级容量。Unity Play Mode 使用本项目字体、图集、正式邮箱预制体和真实控制器验证选择、详情、红点、按钮射线、单领、批领及确认删除。界面验收的协议回调用隔离测试连接驱动。

隔离数据未进入正式区服。全服投递尚无大区服压测数据，验证未覆盖移动端真机发布。分支热更编译还使用了已存在于工作区的货币兑换依赖，并修正快捷任务页签在两版枚举间的编译兼容，两版沿用相同的 1、2、3 序值。
