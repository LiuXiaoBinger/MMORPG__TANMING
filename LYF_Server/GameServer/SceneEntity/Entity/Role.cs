using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>角色场景实体基类，细分为玩家和镜像角色。</summary>
    public class Role : RoleEntity
    {
        /// <summary>建立指定配置的场景对象；这里只保存身份，组件装配和初始化由工厂与实体世界继续完成。</summary>
        /// <param name="uid">本游戏服进程分配的非零运行时编号。</param>
        /// <param name="configId">此实体所用配置行的编号。</param>
        /// <param name="entityType">本次对象采用的类别位标记。</param>
        public Role(ulong uid, EntityType entityType, int configId)
            : base(uid, entityType, configId)
        {
        }
    }
}








