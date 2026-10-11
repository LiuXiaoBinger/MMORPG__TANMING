# CenterServer Unsafe 程序集版本修复记录

## 修改内容

- CenterServer 项目固定引用 `System.Runtime.CompilerServices.Unsafe` 6.0.0.0。
- 在 `LYF_Server/CenterServer/CenterServer.csproj` 增加 `SpecificVersion=True` 和 `Private=True`，确保构建时将对应版本复制到输出目录。
- 未修改协议、数据库表和业务逻辑。

## 设计思路

CenterServer 使用的 `System.Memory`、LZ4、Zstd 等依赖同时存在 4.0.4.1 和 6.0.0.0 的程序集引用。统一让 CenterServer 输出目录使用项目直接引用的 6.0.0.0，并由 `App.config` 的绑定重定向兼容旧引用，避免加载输出目录残留旧 DLL 时发生清单不匹配。

## 验证结果

- `dotnet build LYF_Server/CenterServer/CenterServer.csproj --no-restore --configuration Debug --output D:/unitypro/LYFMMORGP/_temp/unsafe-center-build-final`：0 个错误、0 个警告。
- `CenterServer/bin/Debug/System.Runtime.CompilerServices.Unsafe.dll` 清单版本为 6.0.0.0。
- 生成的 `CenterServer.exe.config` 将 Unsafe 绑定重定向到 6.0.0.0。

## 未解决问题

- 已使用本机 MySQL `127.0.0.1:3308` 启动隔离构建输出并完成 CodeFirst 运行验证，未复现异常。
- 部署时仍需确保输出目录没有被其他服务覆盖为旧版本 DLL。
