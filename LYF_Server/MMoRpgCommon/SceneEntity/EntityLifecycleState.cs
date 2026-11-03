namespace MMoRpgCommon.SceneEntity
{
    /// <summary>场景实体的生命周期阶段；实体自身的方法负责改变状态，实体世界负责按帧推进与移除。</summary>
    public enum EntityLifecycleState
    {
        /// <summary>已建立对象，可继续装配组件。</summary>
        Created = 0,
        /// <summary>保留的初始化阶段编号；当前服务端初始化会直接进入 Active。</summary>
        Initialized = 1,
        /// <summary>正常参加逐帧更新。</summary>
        Active = 2,
        /// <summary>暂时停止更新，保留实体数据和组件。</summary>
        Sleeping = 3,
        /// <summary>已停止更新，等待帧尾释放。</summary>
        PendingDestroy = 4,
        /// <summary>组件已释放，不再参加世界更新。</summary>
        Destroyed = 5
    }
}
