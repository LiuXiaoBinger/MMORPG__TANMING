# PresentTable 表加载接入说明

## 修改范围

- `LYF_Server/Network/LubanManage/LubanMgr.cs`

## 实现内容

- 新增 `_presentTables` 字典，使用 `PresentTable.Id` 作为键保存表现配置。
- 在 `LubanMgr.Init()` 中直接接收 `tables.TbPresentTable.DataMap`，复用 Luban 已完成反序列化的数据，避免重复复制配置对象。
- 新增 `GetPresentTables()` 返回全部表现配置。
- 新增 `GetPresentTableById(int id)` 按表现配置 ID 查询单条配置，未初始化或不存在时返回 `null`。

## 设计思路

`Tables` 已经负责加载 `tbpresenttable.bytes` 并创建 `TbPresentTable`。`LubanMgr` 只需要保存其 `DataMap` 引用并提供统一查询入口，不再重复读取二进制文件或自行解析字段。表现配置中的 Prefab、动画、音效、碰撞等字段由生成的 `PresentTable` 类型承载。

## 验证结果

- 新增表现表区域未使用 `foreach`、三目运算符、命名空间声明或弱类型载荷。
- Network Debug 构建成功，0 个错误、4 条既有警告。
- GameServer Debug 构建成功，0 个错误、11 条既有警告。
- 构建产物和中间文件位于 `D:/unitypro/LYFMMORGP/_temp/present-table-build-final/`。

## 未解决问题

- 尚未启动真实服务器进程验证指定表现 ID 的运行时查询结果。
- 尚未进行 Unity 客户端表现资源加载和四端联调。

## 最终复核

最终复核构建使用 D:/unitypro/LYFMMORGP/_temp/present-table-build-final2/，Network 和 GameServer 均为 0 个错误。
