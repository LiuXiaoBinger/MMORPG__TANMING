namespace MMoRpgCommon.SceneEntity
{
    /// <summary>普通实体组件容器的稳定类型索引；新增类型只追加编号，既有编号保持兼容。</summary>
    public enum EntityComponentType
    {
        /// <summary>未指定组件类别组件的稳定查找编号。</summary>
        None = 0,
        /// <summary>基础实体标记组件的稳定查找编号。</summary>
        Entity = 1,
        /// <summary>场景归属组件的稳定查找编号。</summary>
        Scene = 2,
        /// <summary>战斗属性组件的稳定查找编号。</summary>
        Attributes = 3,
        /// <summary>增益与减益效果组件的稳定查找编号。</summary>
        Buff = 4,
        /// <summary>阵营关系组件的稳定查找编号。</summary>
        FightGroup = 5,
        /// <summary>移动组件的稳定查找编号。</summary>
        Move = 6,
        /// <summary>受击响应组件的稳定查找编号。</summary>
        BeHit = 7,
        /// <summary>死亡状态组件的稳定查找编号。</summary>
        Dead = 8,
        /// <summary>网络同步组件的稳定查找编号。</summary>
        Sync = 9,
        /// <summary>战斗入口组件的稳定查找编号。</summary>
        Combat = 10,
        /// <summary>角色身份组件的稳定查找编号。</summary>
        Role = 11,
        /// <summary>怪物身份组件的稳定查找编号。</summary>
        Monster = 12,
        /// <summary>NPC 身份组件的稳定查找编号。</summary>
        Npc = 13,
        /// <summary>采集交互组件的稳定查找编号。</summary>
        Collection = 14,
        /// <summary>镜像角色身份组件的稳定查找编号。</summary>
        MirrorRole = 15,
        /// <summary>佣兵身份组件的稳定查找编号。</summary>
        Mercenary = 16,
        /// <summary>飞行物身份组件的稳定查找编号。</summary>
        Projectile = 17,
        /// <summary>技能组件的稳定查找编号。</summary>
        Skill = 18,
        /// <summary>自动行为组件的稳定查找编号。</summary>
        AI = 19,
        /// <summary>路径查找组件的稳定查找编号。</summary>
        Navigation = 20,
        /// <summary>仇恨关系组件的稳定查找编号。</summary>
        Hatred = 21,
        /// <summary>召唤关系组件的稳定查找编号。</summary>
        Summon = 22,
        /// <summary>视野组件的稳定查找编号。</summary>
        View = 23,
        /// <summary>存活时间组件的稳定查找编号。</summary>
        Lifetime = 24,
        /// <summary>陷阱身份组件的稳定查找编号。</summary>
        Trap = 25,
        /// <summary>精英怪身份组件的稳定查找编号。</summary>
        Elite = 26,
        /// <summary>首领怪身份组件的稳定查找编号。</summary>
        Boss = 27,
        /// <summary>宠物身份组件的稳定查找编号。</summary>
        Pet = 28,
        /// <summary>鹰类随从身份组件的稳定查找编号。</summary>
        Eagle = 29,
        /// <summary>假实体身份组件的稳定查找编号。</summary>
        Dummy = 30,
        /// <summary>角色跳跃动画表现组件，独立于待机和其他视图组件注册。</summary>
        Jump = 31,
        /// <summary>实体普通程序位移组件，在输入移动提交前求值；保留此前所有组件编号。</summary>
        Displacement = 32
    }
}
