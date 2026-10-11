namespace MMoRpgCommon.SceneEntity
{
    /// <summary>普通实体组件容器的稳定类型索引；新增类型只追加编号，既有编号保持兼容。</summary>
    public enum EntityComponentType
    {
        None = 0,
        Entity = 1,
        Scene = 2,
        Attributes = 3,
        Buff = 4,
        FightGroup = 5,
        Move = 6,
        BeHit = 7,
        Dead = 8,
        Sync = 9,
        Combat = 10,
        Role = 11,
        Monster = 12,
        Npc = 13,
        Collection = 14,
        MirrorRole = 15,
        Mercenary = 16,
        Projectile = 17,
        Skill = 18,
        AI = 19,
        Navigation = 20,
        Hatred = 21,
        Summon = 22,
        View = 23,
        Lifetime = 24,
        Trap = 25,
        Elite = 26,
        Boss = 27,
        Pet = 28,
        Eagle = 29,
        Dummy = 30,
        /// <summary>角色跳跃动画表现组件，独立于待机和其他视图组件注册。</summary>
        Jump = 31,
        /// <summary>实体普通程序位移组件，在输入移动提交前求值；保留此前所有组件编号。</summary>
        Displacement = 32
    }
}
