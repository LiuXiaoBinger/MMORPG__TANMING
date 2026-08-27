const config_module = require('./config')
const Redis=require('ioredis');


//node.js创建redis客户端
const RedisClie=new Redis({
    host:config_module.redis_host,
    port:config_module.redis_port,
    password:config_module.redis_passwd,
});

/**
 * 监听错误信息
 */
RedisClie.on("error",function(err){
    console.log("RedisCli connect error");
    RedisClie.quit();
});
/**
 * 根据key获取value
 * @param {*} key 
 * @returns 
 */
async function Get_keybyValve(key){
    try {
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
 * 根据key查询redis中是否存在key
 * @param {*} key 
 * @returns 
 */
async function exists_key(key){
    try {
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
 * 设置key和value，并过期时间
 * @param {*} key 
 * @param {*} value 
 * @param {*} exptime 
 * @returns 
 */
async function set_keyTime(key ,value,exptime){
    try {
        // 设置键和值
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
 * 退出函数
 */
function Quit(){
    RedisClie.quit();
}
module.exports = {Get_keybyValve, exists_key, Quit, set_keyTime,}