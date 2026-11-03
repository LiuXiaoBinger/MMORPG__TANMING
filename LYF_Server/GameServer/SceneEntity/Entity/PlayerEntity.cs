using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>玩家在场景中的运行时实体，持久角色数据由角色管理器单独维护。</summary>
    public sealed class PlayerEntity : Role
    {
        /// <summary>建立指定配置的场景对象；这里只保存身份，组件装配和初始化由工厂与实体世界继续完成。</summary>
        /// <param name="uid">本游戏服进程分配的非零运行时编号。</param>
        /// <param name="configId">此实体所用配置行的编号。</param>
        public PlayerEntity(ulong uid, int configId)
            : base(uid, EntityType.Role | EntityType.Player, configId)
        {
        }
    }
}








