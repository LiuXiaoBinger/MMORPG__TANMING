using System;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROObject 的服务端纯逻辑对应物，不依赖 Unity GameObject。</summary>
    public abstract class ROObject
    {
        private readonly EntityComponentContainer _components = new EntityComponentContainer();

        protected ROObject(ulong uid, EntityType entityType, int configId)
        {
            if (uid == 0UL)
            {
                throw new ArgumentException("实体 UID 必须有效", "uid");
            }
            UID = uid;
            EntityType = entityType;
            ConfigId = configId;
            LifecycleState = EntityLifecycleState.Created;
            Transform = new EntityTransformSnapshot();
        }

        public ulong UID { get; private set; }
        public EntityType EntityType { get; private set; }
        public EntityLifecycleState LifecycleState { get; private set; }
        public int ConfigId { get; private set; }
        public int MapId { get; set; }
        public EntityTransformSnapshot Transform { get; private set; }
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int MaxMp { get; set; }
        public int CurrentMp { get; set; }
        public EntityComponentContainer Components { get { return _components; } }

        public void AddComponent(EntityComponentBase component)
        {
            if (LifecycleState != EntityLifecycleState.Created)
            {
                throw new InvalidOperationException("实体初始化后不能再挂载组件");
            }
            _components.Add(component);
        }

        public void Initialize()
        {
            if (LifecycleState != EntityLifecycleState.Created)
            {
                return;
            }
            _components.Initialize();
            LifecycleState = EntityLifecycleState.Active;
        }

        public void Update(int deltaMilliseconds)
        {
            if (LifecycleState != EntityLifecycleState.Active)
            {
                return;
            }
            _components.Update(deltaMilliseconds);
        }

        public void Sleep()
        {
            if (LifecycleState == EntityLifecycleState.Active)
            {
                LifecycleState = EntityLifecycleState.Sleeping;
            }
        }

        public void Wake()
        {
            if (LifecycleState == EntityLifecycleState.Sleeping)
            {
                LifecycleState = EntityLifecycleState.Active;
            }
        }

        public void MarkPendingDestroy()
        {
            if (LifecycleState != EntityLifecycleState.Destroyed)
            {
                LifecycleState = EntityLifecycleState.PendingDestroy;
            }
        }

        public EntitySnapshot ToSnapshot()
        {
            EntitySnapshot snapshot = new EntitySnapshot();
            snapshot.UID = UID;
            snapshot.EntityType = EntityType;
            snapshot.LifecycleState = LifecycleState;
            snapshot.ConfigId = ConfigId;
            snapshot.MapId = MapId;
            snapshot.Transform = new EntityTransformSnapshot();
            snapshot.Transform.X = Transform.X;
            snapshot.Transform.Y = Transform.Y;
            snapshot.Transform.Z = Transform.Z;
            snapshot.Transform.Yaw = Transform.Yaw;
            snapshot.MaxHp = MaxHp;
            snapshot.CurrentHp = CurrentHp;
            snapshot.MaxMp = MaxMp;
            snapshot.CurrentMp = CurrentMp;
            return snapshot;
        }

        public void Destroy()
        {
            if (LifecycleState == EntityLifecycleState.Destroyed)
            {
                return;
            }
            _components.Shutdown();
            LifecycleState = EntityLifecycleState.Destroyed;
        }
    }
}








