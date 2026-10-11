# OwnerUID 清理变更说明（2026-09-15）

## 修改范围

- 客户端 `SceneEntity`、`SceneWorldSystem`、NPC 管理器和背包测试入口。
- 服务端 `GameServer/SceneEntity` 实体基类、实体子类和工厂。
- 公共层 `MMoRpgCommon/SceneEntity` 的场景数据与快照。

## 实现内容

- 删除 `ROObject`、`MObject` 及服务端实体基类中的 `OwnerUID` 属性。
- 删除场景快照和公共场景数据中的 `OwnerUID` 字段及复制逻辑。
- 所有实体构造器、Builder、Factory、World、对象池复用入口统一只接收 `UID` 与 `ConfigId`。
- 宠物、镜像角色等主人关系不再写入实体身份字段，由上层使用已有 UID 映射维护。
- 保留 `RoleId`、`ConfigId` 等业务数据语义，不新增替代身份变量。

## 验证结果

- `MMoRpgCommon`、`GameServer`、Unity 客户端 `GM_Game` 均构建成功，0 错误（保留既有警告）。
- 业务源码扫描 `OwnerUID|ownerUid` 命中 0。

## 未解决问题

- 尚未执行 Unity Play Mode、对象池多轮复用和网络联调；需在专用测试场景继续验证。
