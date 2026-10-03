namespace MMoRpgCommon.SceneEntity
{
    /// <summary>客户端代理和服务端实体共用的最小运行时数据。</summary>
    public class SceneEntityData
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




