// Redis 连接参数来自统一配置模块。
const config_module = require('./config')
// 使用 ioredis 驱动创建连接及执行缓存命令。
const Redis=require('ioredis');


// 模块加载时创建并复用 Redis 客户端，保存按邮箱区分的短期验证码。
const RedisClie=new Redis({
    host:config_module.redis_host,
    port:config_module.redis_port,
    password:config_module.redis_passwd,
});

/**
 * 连接发生错误时记录提示并请求退出客户端；此回调没有自行重建连接。
 */
// err 是驱动提供的错误对象；当前回调仅输出固定提示，不读取该参数内容。
RedisClie.on("error",function(err){
    console.log("RedisCli connect error");
    RedisClie.quit();
});
/**
 * 读取指定缓存键的字符串值；键不存在或读取异常时都返回 null。
 * @param {string} key 完整缓存键，验证码调用方会传入前缀与邮箱的组合。
 * @returns {Promise<string|null>} 已缓存的验证码；null 不能区分缺失和读取失败。
 */
async function Get_keybyValve(key){
    try {
        // result 是缓存文本或 null，保持原值供请求处理器决定是否重新生成。
        const result=await RedisClie.get(key);
        if(result==null){
            console.log('result:','<'+result+'>', 'This key cannot be find...')
            return null;
        }
        console.log('Result:','<'+result+'>','Get key success!...');
        return result
    } catch (error) {
        console.log('GetRedis error is', error);
        return null
    }
}

/**
 * 查询单个键是否存在，保留 Redis 返回的数量值，不转换成布尔值。
 * @param {string} key 要检查的完整缓存键。
 * @returns {Promise<number|null>} 通常为 1 或 0；异常或空结果返回 null。
 */
async function exists_key(key){
    try {
        // result 是存在键的数量；传入一个键时，0 也属于正常查询结果。
        const result=await RedisClie.exists(key)
        if(result==null){
            console.log('result:','<'+result+'>', 'This key cannot be find...')
            return null
        }
        console.log('Result:','<'+result+'>','Get key success!...');
        return result
    } catch (error) {
        console.log('GetRedis error is', error);
        return null
    }
}

/**
 * 先写值，再单独设置有效期；两个命令并非一次原子操作。
 * @param {string} key 要写入的完整缓存键。
 * @param {string} value 验证码文本。
 * @param {number} exptime 从设置有效期开始计算的秒数。
 * @returns {Promise<boolean>} 两次命令均未抛异常时返回 true，否则返回 false。
 */
async function set_keyTime(key ,value,exptime){
    try {
        // 先覆盖键值；如果下一条设置有效期的命令失败，此次写入不会自动撤销。
        await RedisClie.set(key,value)
        // 设置过期时间（以秒为单位）
        await RedisClie.expire(key, exptime);
        return true
    } catch (error) {
        console.log('SetRedisExpire error is', error);
        return false;
    }
}

/**
 * 请求客户端关闭连接；没有返回或等待 quit 的 Promise。
 */
function Quit(){
    RedisClie.quit();
}
// 导出读取、存在检查、退出和带有效期写入；本模块不负责邮件发送。
module.exports = {Get_keybyValve, exists_key, Quit, set_keyTime,}