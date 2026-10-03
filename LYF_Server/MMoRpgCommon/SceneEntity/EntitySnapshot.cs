namespace MMoRpgCommon.SceneEntity
{
    /// <summary>场景实体进入视野和重连时使用的基础快照。</summary>
    public sealed class EntitySnapshot
    {
        public ulong UID { get; set; }
        public EntityType EntityType { get; set; }
        public EntityLifecycleState LifecycleState { get; set; }
        public int ConfigId { get; set; }
        public int MapId { get; set; }
        public EntityTransformSnapshot Transform { get; set; }
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int MaxMp { get; set; }
        public int CurrentMp { get; set; }
    }
}




