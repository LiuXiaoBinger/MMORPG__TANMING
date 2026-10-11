# 商城购买协议与链路设计

## 1. 设计范围

本设计对应 Unity 客户端 -> GateServer -> GameServer 的 NPC/商城购买请求。GameServer 处理器名称为 `RequestBuyShopItem`，注册器只登记协议和处理器，不承载购买业务。

现有配置已经提供 `ShopInfo`（商品配置 ID、物品配置 ID、价格、货币类型、购买后绑定标记）和 NPC 商品限购数据；现有服务端尚未提供可提交的角色货币余额、物品 UID/库存、背包占格、购买幂等记录和原子事务。因此协议链路可以先接通，但不能伪造扣费或发货成功。

## 2. 协议定义

协议源文件仍放在 `LYF_Server/ProtoBuf/proto/RequestEntity.proto` 和 `ResultEntity.proto`，生成代码必须由协议生成流程产生，不能手工修改生成文件。

### 2.1 请求

```proto
message BuyShopItemReq {
    uint32 shop_type = 1;       // 商店类型/分类，由服务端校验
    repeated BuyShopItem items = 2; // 本次购买的商品明细，服务端按配置反查价格
    uint32 npc_id = 3;          // NPC 配置 ID，用于定位 NPC 商品和限购记录
    uint64 request_id = 4;      // 客户端生成的非零幂等请求号
    uint32 role_id = 5;         // 角色 ID，仅用于与会话角色一致性校验
}

message BuyShopItem {
    uint32 shop_id = 1;        // ShopInfo.Id，不能填写物品配置 ID
    uint32 buy_count = 2;      // 购买该商品的份数，必须大于零
}
```

保留用户给出的 `shop_type` 和 `items` 字段及其编号。`npc_id` 是必要补充：`RoleNpcShopPurchaseTable` 的唯一定位包含 `RoleID + NpcID + ShopID`，没有 NPC 无法正确校验限购。`request_id` 用于断线重试和重复点击去重；不能使用客户端时间或本地递增值作为唯一持久化依据，客户端应为每次业务请求生成随机/持久递增的非零值。

客户端不提交 `item_id`、价格、货币类型或绑定标记。`role_id` 仅用于一致性校验，服务端身份必须从 GateSessionId/UnitySessionId 会话映射取得，不能信任客户端字段建立身份。物品和结算资源必须从服务端 `ShopInfo`、角色会话和权威库存中取得。

### 2.2 错误与响应

```proto
message ErrorInfo {
    CmdCode code = 1;          // 机器可判断的错误码
    string message = 2;        // 可选诊断信息，不作为客户端逻辑判断依据
}

message BuyShopItemRes {
    ErrorInfo error = 1;       // 成功时 code 为 Succeed，失败时返回明确错误
    uint64 request_id = 2;     // 原样回显请求号，便于客户端匹配预测 UI
}
```

现有 `CmdCode` 的 0-13 保持不变，新增值建议追加，禁止重排旧值：

| 值 | 名称 | 使用场景 |
| ---: | --- | --- |
| 14 | `FeatureNotReady` | 当前权威购买事务尚未接入，暂不能成功购买 |
| 15 | `DuplicateRequest` | 相同请求号对应不同请求内容，拒绝处理 |
| 16 | `NotEnoughCurrency` | 货币余额不足 |
| 17 | `ShopItemNotFound` | 商品不存在或不属于指定 NPC/商店 |
| 18 | `PurchaseLimitReached` | 超过日/周/月/年/永久限购 |
| 19 | `BagFull` | 发货后背包容量不足 |
| 20 | `InvalidSession` | 会话、角色或 Gate 映射无效 |
| 21 | `InvalidQuantity` | 商品列表为空、数量为零、溢出或超出单次上限 |
| 22 | `InvalidShop` | 商店类型、NPC 或商品归属不匹配 |
| 23 | `TransactionFailed` | 权威扣费/发货事务失败并已回滚 |

成功响应不代表客户端可自行扣除货币或添加物品；客户端应等待后续权威角色/背包刷新。若当前版本没有 `FeatureNotReady`，临时使用 `ServerError`，但不得返回 `Succeed`。

### 2.3 命令号

在现有 `NetDefine` 末尾追加：

```csharp
public const ushort CMD_BuyShopItemCode = 11130; // 客户端购买商城商品
```

`11130` 紧接现有 `11121`，当前无冲突。GameServer -> CenterServer 的事务提交协议应另分配内部命令号（建议 `11131` 起），不得让客户端伪造或直接调用内部提交协议；本轮不实现该内部协议。

## 3. 服务端校验规则

所有校验在 GameServer 逻辑线程执行，网络回调线程只复制 `BasePackage` 并入队。

1. 来源必须是 GateServer；`GateSessionId`、`UnitySessionId`、传输对象和目标 Gate 会话必须存在。
2. 角色从 `(GateSessionId, UnitySessionId)` 会话映射取得，禁止信任请求中的角色 ID。角色必须在线且处于可交易状态。
3. `request_id != 0`；`items` 非空且不超过服务端配置的单次条目上限（建议 50）。同一请求内 `shop_id` 不允许重复，避免限购和金额计算歧义。
4. `shop_type`、`npc_id` 必须存在且匹配当前 NPC 的商店类型。每个 `shop_id` 必须属于该 NPC；服务端通过 `ShopInfo.Id` 反查物品配置、单价、货币类型和绑定标记。
5. `buy_count` 必须在 `1..MaxBuyCountPerItem`，建议默认上限 999；总价使用 `long` 计算并检查溢出。客户端提交的价格、货币、物品 ID（若扩展字段出现）一律忽略或拒绝。
6. 限购计数按购买份数累加，而不是按请求条数累加。当前表可直接支持 `Unlimited`、`Daily`、`Permanent`；现有表没有周/月/年计数，`Weekly`、`Monthly`、`Yearly` 在数据库扩展完成前必须返回明确错误，不能错误地复用每日或永久计数。
7. 校验必须先完成，再进入扣费/发货事务。任意一项失败时整批失败，不允许部分购买。

## 4. 幂等与事务

幂等键为 `(RoleID, request_id)`，并保存请求内容摘要、最终错误码和必要的响应数据。相同键且内容摘要相同，直接返回上次结果；相同键但内容不同，返回 `DuplicateRequest`。幂等记录必须持久化到 CenterServer 或同等权威存储，不能只放 GameServer 内存，否则重启后会重复扣费。

正式购买采用全有或全无事务：

1. GameServer 校验会话、NPC/商品配置、数量和限购，并计算服务端价格。
2. GameServer 将包含 `RoleID`、`request_id`、商品快照、总价和限购变更的内部提交请求发给 CenterServer。
3. CenterServer 在一个数据库事务/可靠事务边界中锁定幂等记录、校验并扣除货币、写入物品明细或库存、更新 `RoleNpcShopPurchaseTable`，成功后提交；任一步失败全部回滚。
4. GameServer 只在收到 CenterServer 的成功回包后向 GateServer 返回 `Succeed`。超时、连接断开或未知提交状态不得直接重试扣费；应先按 `request_id` 查询事务结果。
5. 成功后由 GameServer/CenterServer 推送角色货币和背包权威刷新，购买响应只负责确认请求结果。

建议在购买事务表或幂等表中保存 `RoleID、RequestID、PayloadHash、State、ErrorCode、CreatedAt、UpdatedAt`，并对 `(RoleID, RequestID)` 建唯一索引。`RoleNpcShopPurchaseTable` 当前只包含每日和永久计数字段；支持周/月/年时应增加明确的周期类型和周期起点（或独立周期计数表），不能通过覆盖现有字段实现。

## 5. Unity -> GateServer -> GameServer 流程

1. Unity 点击购买后生成非零 `request_id`，构造 `BuyShopItemReq`，写入当前 `BasePackage` 的 `UnitySessionId`、`GateSessionId` 和命令号 `11130`。
2. GateServer 客户端入口注册 `CMD_BuyShopItemCode`，仅检查包完整性后原样转发给 GameServer，必须保留两个会话字段；GateServer 不扣费、不改背包、不计算价格。
3. GameServer 网络回调将输入复制成 `GameCommand` 放入总队列。逻辑帧消费后，由 `GameMessageProtocolRegister` 注册的 `(GateServer, CMD_BuyShopItemCode)` 找到 `RequestBuyShopItem`，处理器解析 protobuf、执行来源/会话校验和领域服务调用。
4. 权威事务未接入时，处理器返回 `BuyShopItemRes { error.code = FeatureNotReady/ServerError, request_id }`，不得伪造成功；已接入时按上一节等待 CenterServer 提交结果。
5. GameServer 使用原始会话字段和命令号向 GateServer 回包。GateServer 按 `UnitySessionId` 找到客户端会话，透传 `BuyShopItemRes`；客户端按 `request_id` 结束购买按钮的等待状态，并根据错误码刷新或提示。

网络线程、Gate 转发线程和 Center 数据库线程均不能直接修改 GameServer 角色状态；扣费、发货和限购计数只在明确的权威事务边界内执行。

## 6. 本轮可验收范围

后端可实现并验收：协议源文件、生成代码、`NetDefine` 命令号、Gate 双向注册/透传、GameServer `RequestBuyShopItem` 独立处理器及注册表映射、来源和会话校验、protobuf 异常处理、空列表/非法数量/未知商品/重复请求号的错误回包。客户端可接入请求构造、`request_id` 匹配和错误展示。

本轮不可宣称完成：真实余额扣除、物品发放、背包空间扣减、限购计数持久化、CenterServer 原子事务和重启后的幂等恢复。这些必须在权威货币/物品模型和数据库事务实现后，由测试覆盖成功、失败回滚、超时重试和并发重复请求。

## 7. 用户基线协议兼容性复核（2026-09-05）

用户给出的最小协议只有 `BuyShopItemReq.shop_type=1`、`BuyShopItemReq.items=2` 和 `BuyShopItemRes.error=1`。当前实现追加了 `npc_id=3`、`request_id=4`、`role_id=5`，并在响应中追加 `request_id=2`。字段编号未改变，Protobuf 的字段追加本身保持 wire 兼容。

需要注意：当前 `RequestBuyShopItem` 将 `request_id` 和 `role_id` 校验为非零，因此只发送两个基线字段的旧客户端会被拒绝。字段格式兼容不等于行为兼容。后端必须在实现阶段明确采用以下方案之一：

1. **扩展协议为正式契约（推荐）**：NPC 购买必须携带 `npc_id` 和非零 `request_id`；`role_id` 仅作为可选的一致性校验字段。客户端、GateServer、GameServer 一起升级，并在接口文档中声明扩展字段的业务要求。
2. **兼容最小协议**：缺省 `request_id` 时由 GameServer 逻辑线程生成内部幂等键，缺省 `role_id` 时不做该项校验；但 NPC 购买仍必须能唯一定位 NPC。若商品无法唯一反查 NPC，必须返回 `InvalidShop`，不能猜测 NPC 或绕过限购。

架构结论：`npc_id` 对现有限购键 `(RoleID,NpcID,ShopID)` 是业务必要字段；`request_id` 对重试、重复点击和事务幂等是工程必要字段；`role_id` 不是身份来源，只能作为可选会话一致性检查。服务端始终从 Gate 会话确定角色身份，不能信任客户端角色 ID。

### 7.1 可执行验收标准

- `shop_type=1`、`items=2` 字段编号保持不变，扩展字段只能追加。
- `BuyShopItemRes.error` 在成功和失败时均存在，客户端以 `CmdCode` 判断结果。
- 若采用扩展正式契约，缺少 `npc_id`/`request_id` 的请求得到明确错误，并同步更新客户端、服务端文档。
- 若采用最小协议兼容，旧客户端不因缺少 `role_id` 被拒绝；NPC 无法唯一定位时不得扣费或发货。
- GateServer 只透传并保留 `UnitySessionId`、`GateSessionId`，不执行购买业务。
- GameServer 仅在逻辑线程处理；同一 `(RoleID,request_id)` 相同载荷至多提交一次，不同载荷返回 `DuplicateRequest`。
- 价格、货币、物品、限购和归属全部从服务端配置及权威数据读取。
- 测试覆盖最小/扩展请求、缺少字段、重复请求号、错误角色、未知商品、NPC 归属错误和会话失效。

## 8. 2026-09-05 字段精简决定

根据最新需求，正式协议删除 `BuyShopItemReq.npc_id`、`BuyShopItemReq.request_id` 和 `BuyShopItemRes.request_id`。
当前请求只保留 `shop_type`、`items` 和 `role_id`；商品所属商店通过 `ShopItemInfo.ShopId` 与 `ShopTable` 校验，不再从客户端提交 NPC ID。
同时移除客户端请求号匹配、超时缓存和服务端购买请求幂等表，商城管理器只允许一次进行中的购买。
