namespace MMoRpgCommon.SceneEntity
{
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
        Jump = 31
    }
}
