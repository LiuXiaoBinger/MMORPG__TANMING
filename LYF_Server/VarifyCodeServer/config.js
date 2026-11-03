// 验证码服务的配置入口：从进程当前工作目录读取配置文件，再导出各模块使用的字段。
// 文件读取接口；导入本模块时立即同步读取配置，不负责监听文件变化。
const fs = require('fs');

// 完整配置对象；相对路径以启动命令的工作目录为准，不以当前脚本目录为准。
let config = JSON.parse(fs.readFileSync('config.json', 'utf8'));
// SMTP 身份与发件人地址；实际值来自本地配置，不在源码中重复保存。
let email_user = config.email.user;
// SMTP 身份校验使用的密码或邮箱授权码，由邮件模块传给发送连接。
let email_pass = config.email.pass;
// 配置中保留的 MySQL 主机；当前验证码调用链未用它建立数据库连接。
let mysql_host = config.mysql.host;
// 配置中保留的 MySQL 端口；这里只转交字段，不修改其原始类型。
let mysql_port = config.mysql.port;
// 缓存验证码的 Redis 主机，由 m_redis 模块建立连接。
let redis_host = config.redis.host;
// Redis 服务监听端口，由连接模块读取。
let redis_port = config.redis.port;
// Redis 认证信息；这里只导出配置引用，不进行认证或输出内容。
let redis_passwd = config.redis.passwd;
// 预留的验证码键前缀；当前请求处理实际使用 const 模块中的同名前缀。
let code_prefix = "code_";


// 以现有字段名导出配置，保持邮件和缓存模块的读取方式一致。
module.exports = {email_pass, email_user, mysql_host, mysql_port,redis_host, redis_port, redis_passwd, code_prefix}