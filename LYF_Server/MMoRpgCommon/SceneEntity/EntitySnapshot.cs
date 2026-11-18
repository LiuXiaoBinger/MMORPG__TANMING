namespace MMoRpgCommon.SceneEntity
{
    /// <summary>场景实体进入视野和重连时使用的基础快照。</summary>
    public sealed class EntitySnapshot
    {
        /// <summary>实体全局 UID。</summary>
        public ulong UID { get; set; }
        /// <summary>实体类型分类。</summary>
        public EntityType EntityType { get; set; }
        /// <summary>实体当前生命周期阶段。</summary>
        public EntityLifecycleState LifecycleState { get; set; }
        /// <summary>配置表 ID。</summary>
        public int ConfigId { get; set; }
        /// <summary>所在场景/地图 ID。</summary>
        public int MapId { get; set; }
        /// <summary>位置与朝向快照。</summary>
        public EntityTransformSnapshot Transform { get; set; }
        /// <summary>最大生命值。</summary>
        public int MaxHp { get; set; }
        /// <summary>当前生命值。</summary>
        public int CurrentHp { get; set; }
        /// <summary>最大法力值。</summary>
        public int MaxMp { get; set; }
        /// <summary>当前法力值。</summary>
        public int CurrentMp { get; set; }
        /// <summary>怒气上限，单位怒气点。</summary>
        public int MaxRage { get; set; }
        /// <summary>当前怒气点数。</summary>
        public int CurrentRage { get; set; }
        /// <summary>真气上限，单位真气点。</summary>
        public int MaxTrueQi { get; set; }
        /// <summary>当前真气点数。</summary>
        public int CurrentTrueQi { get; set; }
    }
}




