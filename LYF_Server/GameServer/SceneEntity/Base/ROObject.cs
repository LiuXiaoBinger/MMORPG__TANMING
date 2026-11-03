using System;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>参考项目 ROObject 的服务端纯逻辑对应物，不依赖 Unity GameObject。</summary>
    public abstract class ROObject
    {

        #region 字段与属性
        /// <summary>本实体持有的场景组件，创建阶段装配，销毁时统一释放。</summary>
        private readonly EntityComponentContainer _components = new EntityComponentContainer();

        /// <summary>本游戏服进程内唯一的场景运行时编号，不是数据库角色编号。</summary>
        public ulong UID { get; private set; }

        /// <summary>实体类别位标记，决定工厂为其装配哪些组件。</summary>
        public EntityType EntityType { get; private set; }

        /// <summary>控制是否允许装配组件和执行逐帧更新的生命周期阶段。</summary>
        public EntityLifecycleState LifecycleState { get; private set; }

        /// <summary>创建此实体所使用的配置行编号。</summary>
        public int ConfigId { get; private set; }

        /// <summary>实体当前所在地图的配置编号。</summary>
        public int MapId { get; set; }

        /// <summary>实体当前的位置与朝向，生成快照时复制其各字段。</summary>
        public EntityTransformSnapshot Transform { get; private set; }

        /// <summary>当前生命值上限。</summary>
        public int MaxHp { get; set; }

        /// <summary>当前剩余生命值。</summary>
        public int CurrentHp { get; set; }

        /// <summary>当前法力值上限。</summary>
        public int MaxMp { get; set; }

        /// <summary>当前剩余法力值。</summary>
        public int CurrentMp { get; set; }

        /// <summary>供装配和业务查找组件的容器。</summary>
        public EntityComponentContainer Components { get { return _components; } }
        #endregion

        #region 构造与初始化
        /// <summary>建立实体身份并进入 Created 阶段，暂不初始化组件。</summary>
        /// <param name="uid">本游戏服进程分配的非零实体编号。</param>
        /// <param name="entityType">用于组件装配的实体类别。</param>
        /// <param name="configId">实体使用的配置编号。</param>
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

        /// <summary>仅在首次创建阶段初始化组件，然后允许实体参加逻辑帧更新。</summary>
        public void Initialize()
        {
            if (LifecycleState != EntityLifecycleState.Created)
            {
                return;
            }
            _components.Initialize();
            LifecycleState = EntityLifecycleState.Active;
        }

        /// <summary>只有 Active 状态会继续推进组件，休眠或待销毁对象不更新。</summary>
        /// <param name="deltaMilliseconds">本帧经过的毫秒数。</param>
        public void Update(int deltaMilliseconds)
        {
            if (LifecycleState != EntityLifecycleState.Active)
            {
                return;
            }
            _components.Update(deltaMilliseconds);
        }
        #endregion

        /// <summary>在初始化前挂载组件；开始运行后不再允许改变装配。</summary>
        /// <param name="component">此实体要持有的组件。</param>
        public void AddComponent(EntityComponentBase component)
        {
            if (LifecycleState != EntityLifecycleState.Created)
            {
                throw new InvalidOperationException("实体初始化后不能再挂载组件");
            }
            _components.Add(component);
        }

        /// <summary>释放全部组件并标记已销毁，重复调用不再释放。</summary>
        public void Destroy()
        {
            if (LifecycleState == EntityLifecycleState.Destroyed)
            {
                return;
            }
            _components.Shutdown();
            LifecycleState = EntityLifecycleState.Destroyed;
        }

        #region 核心逻辑
        /// <summary>让活动实体暂停逐帧更新，但保留组件和数据。</summary>
        public void Sleep()
        {
            if (LifecycleState == EntityLifecycleState.Active)
            {
                LifecycleState = EntityLifecycleState.Sleeping;
            }
        }

        /// <summary>让休眠实体重新参加逐帧更新。</summary>
        public void Wake()
        {
            if (LifecycleState == EntityLifecycleState.Sleeping)
            {
                LifecycleState = EntityLifecycleState.Active;
            }
        }

        /// <summary>先暂停对象更新，等待实体世界在帧尾执行真正销毁。</summary>
        public void MarkPendingDestroy()
        {
            if (LifecycleState != EntityLifecycleState.Destroyed)
            {
                LifecycleState = EntityLifecycleState.PendingDestroy;
            }
        }

        /// <summary>复制当前基础数据和位置，调用方修改快照不会改变实体位置。</summary>
        public EntitySnapshot ToSnapshot()
        {
            // 独立的传输数据对象，位置也单独复制，避免共用可变引用。
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
        #endregion
    }
}
