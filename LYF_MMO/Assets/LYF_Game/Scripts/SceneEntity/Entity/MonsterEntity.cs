using MMoRpgCommon.SceneEntity;

/// <summary>
/// 怪物数据的基类，保存怪物配置编号及普通、精英、Boss 等档位。
/// 对象池再次借出时清空旧编号与档位；具体组件由对应构建器装配。
/// </summary>
public class MonsterEntity : MEntity
{
    /// <summary>收到死亡动作后驱动本模型淡出的既有播放器，随实体回收释放。</summary>
    private MSceneMonsterCorpsePlayer _dungeonCorpse;

    /// <summary>
    /// 构造怪物实体
    /// </summary>
    /// <param name="uid">运行时唯一 UID</param>
    /// <param name="entityType">怪物具体类型（普通/精英/Boss 等）</param>
    /// <param name="configId">怪物配置表 ID</param>
    public MonsterEntity(ulong uid, EntityType entityType, int configId)
        : base(uid, entityType, configId)
    {
        // 默认等级为普通怪物
        Rank = MonsterRank.Normal;
    }

    /// <summary>怪物配置表 ID</summary>
    public int MonsterId { get; set; }

    /// <summary>怪物等级档位（普通/精英/BOSS/MVP 等）</summary>
    public MonsterRank Rank { get; set; }

    /// <summary>当前出生配置指定的非战斗剧情角色标记，供选敌和技能碰撞共用。</summary>
    public bool NonCombatant { get; set; }

    /// <summary>将本怪物的服务器动作交给实体移动和技能组件。</summary>
    /// <param name="motion">服务端按出生身份同步的动作及其收包时刻。</param>
    /// <param name="resource">当前实例所属副本的表现资源登记。</param>
    internal void ApplyDungeonMotion(MSceneMonsterMotion motion, DungeonSceneResource resource)
    {
        MoveComp.ApplyDungeonMotion(motion);
        Skill.GetDungeonPlayback(resource).Apply(motion);
        if (motion.Action.Phase == MonsterActionPhase.Dead && _dungeonCorpse == null)
        {
            _dungeonCorpse = new MSceneMonsterCorpsePlayer(this, resource);
        }
    }

    /// <summary>在实体状态机刷新后推进技能表现和死亡淡出。</summary>
    /// <param name="deltaSeconds">本帧距上一帧的时间，单位秒。</param>
    public override void LateUpdate(float deltaSeconds)
    {
        if (LifecycleState != EntityLifecycleState.Active) return;
        base.LateUpdate(deltaSeconds);
        Skill.LateUpdateDungeonPlayback();
        if (_dungeonCorpse != null && MoveComp.DungeonMotion.Action.Phase == MonsterActionPhase.Dead)
        {
            _dungeonCorpse.Update(MoveComp.DungeonMotion);
        }
    }

    /// <summary>模型归还前释放本实体死亡表现所持有的资源。</summary>
    protected override void OnDestroying()
    {
        if (_dungeonCorpse != null)
        {
            _dungeonCorpse.Dispose();
            _dungeonCorpse = null;
        }
        base.OnDestroying();
    }

    /// <summary>
    /// 对象池复用时重置怪物 ID 和等级为默认值
    /// </summary>
    /// <param name="uid">新的运行时 UID</param>
    /// <param name="entityType">新的实体类型</param>
    /// <param name="configId">新的配置表 ID</param>
    internal override void PrepareForReuse(ulong uid, EntityType entityType,
        int configId)
    {
        MonsterId = 0;
        Rank = MonsterRank.Normal;
        NonCombatant = false;
        base.PrepareForReuse(uid, entityType, configId);
    }
}
