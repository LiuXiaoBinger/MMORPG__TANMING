// 邮件发送库；本模块只封装发送，不生成或校验验证码。
const nodemailer = require('nodemailer');
// 从本地配置模块取得 SMTP 身份，具体值不写入注释。
const config_module = require("./config")

/**
 * 创建模块内复用的 SMTP 发送器，使用配置中的身份通过 TLS 连接邮件服务。
 */
let transport = nodemailer.createTransport({
    // SMTP 服务器地址及加密连接选项；secure 表示建立连接时即使用 TLS。
    host: 'smtp.163.com',
    port: 465,
    secure: true,
    // 登录邮件服务使用的身份，与单封邮件的收件地址无关。
    auth: {
        user: config_module.email_user, // 发送方邮箱地址
        pass: config_module.email_pass // 邮箱授权码或者密码
    }
});
/**
 * 将回调式发送包装为 Promise，成功返回 SMTP 响应文本，失败继续向调用方抛出。
 * @param {Object} mailOptions_ 邮件内容，包含发件人、收件人、主题、正文及可选信封信息。
 * @returns {Promise<string>} SMTP 服务器响应文本，不代表收件箱最终送达结果。
 */
function SendMail(mailOptions_){
    // resolve 返回发送成功的响应；reject 保留原始发送错误，交给请求处理器归类。
    return new Promise(function(resolve, reject){
        // error 为发送异常；info 为发送完成后的结果对象，其中 response 是服务器回应。
        transport.sendMail(mailOptions_, function(error, info){
            if (error) {
                console.log(error);
                console.log('邮件已成功失败');
                reject(error);
            } else {
                console.log('邮件已成功发送：' + info.response);
                resolve(info.response)
            }
        });
    })

}

// 提供统一的异步发送入口，调用方使用 await 等待 SMTP 返回。
module.exports.SendMail = SendMail

