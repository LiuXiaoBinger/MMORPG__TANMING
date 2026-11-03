using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>佣兵实体；怪物公共组件上叠加召唤和佣兵专用组件。</summary>
    public sealed class MercenaryEntity : MonsterEntity
    {
        /// <summary>建立指定配置的场景对象；这里只保存身份，组件装配和初始化由工厂与实体世界继续完成。</summary>
        /// <param name="uid">本游戏服进程分配的非零运行时编号。</param>
        /// <param name="configId">此实体所用配置行的编号。</param>
        public MercenaryEntity(ulong uid, int configId)
            : base(uid, EntityType.Mercenary, configId)
        {
        }
    }
}








