using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>召唤物或无仇恨的假怪物实体。</summary>
    public sealed class DummyEntity : MonsterEntity
    {
        /// <summary>建立指定配置的场景对象；这里只保存身份，组件装配和初始化由工厂与实体世界继续完成。</summary>
        /// <param name="uid">本游戏服进程分配的非零运行时编号。</param>
        /// <param name="configId">此实体所用配置行的编号。</param>
        public DummyEntity(ulong uid, int configId)
            : base(uid, EntityType.Dummy, configId)
        {
        }
    }
}








