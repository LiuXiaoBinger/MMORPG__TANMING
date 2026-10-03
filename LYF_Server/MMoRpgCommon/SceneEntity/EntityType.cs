namespace MMoRpgCommon.SceneEntity
{
    /// <summary>
    /// 场景实体类别位标记；一个实体可以同时具备多个类别，例如本地玩家同时是 Role 和 Player。
    /// 该枚举只描述场景运行时对象，不承载角色账户数据；判断组合类别时必须使用位运算。
    /// </summary>
    [System.Flags]
    public enum EntityType
    {
        /// <summary>未指定任何实体类别，数值为零。</summary>
        None = 0,

        /// <summary>角色类别，包含本地玩家和其他角色。</summary>
        Role = 1 << 0,

        /// <summary>本地玩家类别；本地玩家创建时必须同时设置 Role 位。</summary>
        Player = 1 << 1,

        /// <summary>普通怪物类别。</summary>
        Monster = 1 << 2,

        /// <summary>精英怪物类别。</summary>
        Elite = 1 << 3,

        /// <summary>首领怪物类别。</summary>
        Boss = 1 << 4,

        /// <summary>非战斗 NPC 类别。</summary>
        Npc = 1 << 5,

        /// <summary>用于占位或测试的假实体类别。</summary>
        Dummy = 1 << 6,

        /// <summary>场景陷阱类别。</summary>
        Trap = 1 << 7,

        /// <summary>宠物实体类别。</summary>
        Pet = 1 << 8,

        /// <summary>鹰或手推车等跟随实体类别。</summary>
        Eagle = 1 << 9,

        /// <summary>可被交互采集的场景物体类别。</summary>
        Collection = 1 << 10,

        /// <summary>玩家镜像或分身类别。</summary>
        MirrorRole = 1 << 11,

        /// <summary>佣兵实体类别。</summary>
        Mercenary = 1 << 12,

        /// <summary>飞行物或投射物类别，保留项目现有 Projectile 命名。</summary>
        Projectile = 1 << 13
    }
}
