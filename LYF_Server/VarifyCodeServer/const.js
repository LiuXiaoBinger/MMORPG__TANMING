// Redis 验证码键的固定前缀，后面拼接请求邮箱，使不同邮箱使用不同缓存键。
let code_prefix = "code_";

// gRPC 返回消息里的业务结果码；不是 gRPC 传输层状态码。
const Errors = {
    // 邮件发送 Promise 正常完成；这不代表用户已经收到或阅读邮件。
    Success : 0,
    // 写入验证码或设置有效期失败，调用方可据此区分缓存写入失败。
    RedisErr : 1,
    // 请求处理抛出异常，包括邮件发送失败，最终进入统一异常分支。
    Exception : 2,
};


// 请求处理与缓存键拼接共享这份常量。
module.exports = {code_prefix,Errors}