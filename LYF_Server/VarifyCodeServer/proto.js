// 手写协议加载入口：在运行时读取本服务的 .proto，不是自动生成的协议实现。
// 用脚本所在目录定位协议文件，避免随启动工作目录变化。
const path = require('path')
// 将加载后的协议描述转换为 gRPC 服务和消息接口。
const grpc = require('@grpc/grpc-js')
// 同步解析 .proto 的字段和服务定义。
const protoLoader = require('@grpc/proto-loader')

// 验证码业务协议的绝对路径，定位到与本脚本同目录的 message.proto。
const PROTO_PATH = path.join(__dirname, 'message.proto')
// 保留字段原名；长整数和枚举使用字符串表示；补齐默认字段并提供 oneof 所属项标记。
const packageDefinition = protoLoader.loadSync(PROTO_PATH, { keepCase: true, longs: String, enums: String, defaults: true, oneofs: true })
// 整份协议包的运行时对象，下面再选取 message 包。
const protoDescriptor = grpc.loadPackageDefinition(packageDefinition)

// message 包中的消息类型与 VarifyService 服务定义，供服务器注册处理函数。
const message_proto = protoDescriptor.message

// 导出业务包接口，本模块不启动 gRPC 监听。
module.exports = message_proto