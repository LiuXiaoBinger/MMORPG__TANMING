namespace MMoRpgCommon.SceneEntity
{
    /// <summary>客户端代理和服务端实体共用的最小运行时数据。</summary>
    public class SceneEntityData
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
        /// <summary>位置与朝向。</summary>
        public EntityTransformSnapshot Transform { get; set; }
        /// <summary>最大生命值。</summary>
        public int MaxHp { get; set; }
        /// <summary>当前生命值。</summary>
        public int CurrentHp { get; set; }
        /// <summary>最大法力值。</summary>
        public int MaxMp { get; set; }
        /// <summary>当前法力值。</summary>
        public int CurrentMp { get; set; }

        public SceneEntityData()
        {
            Transform = new EntityTransformSnapshot();
            LifecycleState = EntityLifecycleState.Created;
        }

        /// <summary>复制为不可变边界之外的独立快照，避免同步时引用运行时对象。</summary>
        public EntitySnapshot ToSnapshot()
        {
            EntityTransformSnapshot transform = new EntityTransformSnapshot();
            transform.X = Transform.X;
            transform.Y = Transform.Y;
            transform.Z = Transform.Z;
            transform.Yaw = Transform.Yaw;
            return new EntitySnapshot
            {
                UID = UID,
                EntityType = EntityType,
                LifecycleState = LifecycleState,
                ConfigId = ConfigId,
                MapId = MapId,
                Transform = transform,
                MaxHp = MaxHp,
                CurrentHp = CurrentHp,
                MaxMp = MaxMp,
                CurrentMp = CurrentMp
            };
        }
    }
}




