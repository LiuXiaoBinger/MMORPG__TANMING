# 上游 KCP 来源说明（第三方边界）

| 项 | 值 |
| --- | --- |
| 仓库 | https://github.com/limpo1989/kcp-csharp |
| 固定提交 | 6ed5b68645c8afe97dbcbd71fb6f469a98f49420 |
| 提交时间 | 2023-07-24 |
| 许可证 | MIT（Copyright (c) 2016 limpo1989），原文见同目录 `LICENSE-KCP.txt` |
| 算法来源 | skywind3000/KCP（ikcp 协议）移植 |

## 收录内容

- `KCP.cs`：上游 `KCP/KCP.cs` 逐字拷贝，未修改算法、常量或编码函数。
- `ByteBuffer.cs`：上游 `KCP/ByteBuffer.cs` 逐字拷贝。
- 未收录上游 `UDPSession.cs`：其自带 Socket 生命周期与线程模型，本项目改由 `Socket/UdpNetwork.cs`、`Socket/KcpPeer.cs`、`Socket/KcpClient.cs` 承担，避免与现有会话体系重复。

## 边界约定

- 本目录保留上游 `namespace KcpProject`，作为第三方边界，便于对照与升级；业务适配代码位于 `Socket/` 且使用全局命名空间。
- 不在此目录添加业务逻辑、认证、路由或封包格式；这些均属本项目代码。
- 升级上游时须同时更新本文件的提交号并复核 `LICENSE-KCP.txt`。
