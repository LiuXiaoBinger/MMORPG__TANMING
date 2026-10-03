namespace MMoRpgCommon.SceneEntity
{
    /// <summary>场景实体生命周期状态，状态变更只能由实体世界驱动。</summary>
    public enum EntityLifecycleState
    {
        Created = 0,
        Initialized = 1,
        Active = 2,
        Sleeping = 3,
        PendingDestroy = 4,
        Destroyed = 5
    }
}
