using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>精英怪的场景对象；实体工厂根据 Elite 类别装配怪物公共组件和精英标记。</summary>
    public sealed class EliteEntity : MonsterEntity
    {
        /// <summary>建立指定配置的场景对象；这里只保存身份，组件装配和初始化由工厂与实体世界继续完成。</summary>
        /// <param name="uid">本游戏服进程分配的非零运行时编号。</param>
        /// <param name="configId">此实体所用配置行的编号。</param>
        public EliteEntity(ulong uid, int configId)
            : base(uid, EntityType.Elite, configId)
        {
        }
    }
}








