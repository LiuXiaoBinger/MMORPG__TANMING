// 验证码服务进程入口：接收 gRPC 请求，复用或生成缓存验证码，然后发送邮件。
// gRPC 服务器实现与监听接口。
const grpc=require('@grpc/grpc-js')
// 手写 proto 加载模块导出的业务包，包含要注册的服务定义。
const message_proto =require('./proto')
// 邮箱缓存键前缀与业务错误码。
const const_module =require('./const')
// 邮件发件人等运行配置；本文件不包含实际身份值。
const config_module = require('./config')
// UUID 生成器，首次生成验证码时从其文本中取前四个字符。
const {v4:uuidv4} =require('uuid')
// Promise 形式的 SMTP 邮件发送入口。
const emailModule =require('./email')
// 验证码缓存读写入口，读取缺失和读取异常均可能返回 null。
const redis_module = require('./m_redis')

/**
 * 为请求邮箱取得验证码并发送邮件，以业务结果码回复这一条 gRPC 请求。
 * @param {Object} call 本次 RPC 调用对象，request.email 是收件邮箱及缓存键的一部分。
 * @param {Function} callback gRPC 响应回调；当前业务失败也使用 null 作为传输层错误参数。
 */
async function GetVarifyCode(call, callback) {
    console.log("email is ", call.request.email)
    try{
        // 同一邮箱已有验证码时复用缓存；读取失败也会返回 null，并走重新生成分支。
        // query_res 是已缓存的验证码或 null，不表示用户账号已通过校验。
        let query_res = await redis_module.Get_keybyValve(const_module.code_prefix+call.request.email);
        console.log("query_res is ", query_res)
        if(query_res==null){

        }
        // 本次发送的验证码；已有缓存直接复用，否则生成新的四字符文本。
        let uniqueId = query_res;
        if(query_res ==null){
            uniqueId = uuidv4();
            if (uniqueId.length > 4) {
                uniqueId = uniqueId.substring(0, 4);
            } 
            // bres 表示两条 Redis 写入命令是否均未抛异常；当前缓存有效期参数为 600 秒。
            let bres = await redis_module.set_keyTime(const_module.code_prefix+call.request.email, uniqueId,600)
            if(!bres){
                callback(null, { email:  call.request.email,
                    error:const_module.Errors.RedisErr
                });
                return;
            }
        }
        
        console.log("uniqueId is ", uniqueId)
        // 邮件正文提示三分钟，而缓存实际设置为十分钟；两处时长目前不是从同一配置取得。
        let text_str =  '您的验证码为'+ uniqueId +'请三分钟内完成注册'
        // 使用刚取得的验证码构造邮件；此处只发送，不校验用户后续提交的验证码。
        // nodemailer 使用的单封邮件参数；信封收件地址与正文邮件头均使用请求邮箱。
        let mailOptions = {
            from: config_module.email_user,
            envelope: {
                from: config_module.email_user,
                to: call.request.email
            },
            to: call.request.email,
            subject: '验证码',
            text: text_str,
        };

        // 等待 SMTP 返回的响应文本；抛出的发送错误由本函数 catch 分支回复。
        let send_res = await emailModule.SendMail(mailOptions);
        console.log("send res is ", send_res)
        
        callback(null, { email:  call.request.email,
            error:const_module.Errors.Success
        }); 


    }catch(error){
        console.log("catch error is ", error)

        callback(null, { email:  call.request.email,
            error:const_module.Errors.Exception
        }); 
    }

}

/** 创建并注册验证码服务，按环境变量或既有默认地址绑定非加密 gRPC 监听。 */
function main() {
    // 本进程复用的服务端实例，注册后由 gRPC 库处理请求回调。
    var server = new grpc.Server()
    // 把协议中的 GetVarifyCode 方法映射到上面的业务函数。
    server.addService(message_proto.VarifyService.service, { GetVarifyCode: GetVarifyCode })
    // 监听地址优先读取 GRPC_ADDRESS；缺省时使用代码中已有的本机地址。
    const address = process.env.GRPC_ADDRESS || '127.0.0.1:50051';
    // error 表示绑定失败；port 为库返回的实际端口，当前成功日志使用 address。
    server.bindAsync(address, grpc.ServerCredentials.createInsecure(), (error, port) => {
        if (error) {
            console.error('grpc server bind failed:', error.message);
            process.exitCode = 1;
            return;
        }

        console.log(`grpc server started at ${address}, port ${port}`);
    });
}

// 模块加载时直接启动监听；本文件不是仅导出函数而不执行服务的工具模块。
main()
