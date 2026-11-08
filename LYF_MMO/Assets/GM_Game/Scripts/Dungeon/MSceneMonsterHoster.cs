using UnityEngine;

/// <summary>怪物模型与 SceneWorld 中 MonsterEntity 的挂接组件，沿用 MonsterCtrl 的实体路由。</summary>
public class MSceneMonsterHoster : MonsterCtrl
{
    /// <summary>模型未缩放时的水平包围半径，单位米。</summary>
    public float Radius;
    /// <summary>本实例来源波次，用于定位编辑器中的出生记录。</summary>
    public int WaveId;
    /// <summary>本模型最近收到的权威动作，未启用 AI 的波次为空。</summary>
    private MSceneMonsterMotion motion;
    /// <summary>按真实SkillId执行的原技能表现，生命周期跟随当前怪物模型。</summary>
    private MSceneMonsterSkillPlayer skillPlayer;
    /// <summary>普通怪尸体的原可见度变化，实体删除仍等待服务器离场。</summary>
    private MSceneMonsterCorpsePlayer corpsePlayer;

    /// <summary>接管一个新路径段或攻击，保留收包时刻用于恢复迟到的表现。</summary>
    /// <param name="state">与本模型同一出生位的最新动作。</param>
    public void ApplyMotion(MSceneMonsterMotion state)
    {
        motion = state;
        if (skillPlayer == null) skillPlayer = new MSceneMonsterSkillPlayer(this, (MonsterEntity)GetROObject());
        skillPlayer.Apply(state);
        LateUpdate();
    }

    /// <summary>结束上一次普攻的专属动画倍率，保证随后的其他动作使用原倍率。</summary>
    public void EndAttackPlayback()
    {
        if (skillPlayer == null) return;
        skillPlayer.RestoreSpeed();
        if (((MonsterEntity)GetROObject()).IsDead) skillPlayer.Clear();
    }

    /// <summary>把真实结算携带的原结果序号交给同一怪物的表现宿主。</summary>
    /// <param name="hit">服务端的实际命中结果。</param>
    /// <param name="target">本实例实际受击角色。</param>
    public void PlayResult(MonsterPlayerHitData hit, RoleEntity target)
    {
        if (skillPlayer == null) skillPlayer = new MSceneMonsterSkillPlayer(this, (MonsterEntity)GetROObject());
        skillPlayer.PlayResult(hit, target);
    }

    /// <summary>按权威路径段插值并驱动公共图。受击和死亡由实体的正式状态机播放。</summary>
    private void LateUpdate()
    {
        if (motion == null)
        {
            if (skillPlayer != null) skillPlayer.Update(false);
            return;
        }
        // 已绑定好公共控制器和实体剪辑的怪物。
        MonsterEntity monster = (MonsterEntity)GetROObject();
        // 起点为服务器创建本段或入场快照时的位置。
        MonsterActionData action = motion.Action;
        if (monster.IsDead && action.Phase != MonsterActionPhase.Dead) return;
        // 从收包至今的真实毫秒，同时包含异步模型加载耗时。
        float ageMs = (float)((Time.realtimeSinceStartupAsDouble - motion.ReceivedAt) * 1000d);
        // 最终位置为当前服务器导航段上的点，静止与死亡直接采用回包坐标。
        Vector3 position = new Vector3(action.X, action.Y, action.Z);
        // 路段或攻击耗尽后保持待机，等待服务端下一个动作。
        string state = MAnimator.STATE_IDLE;
        if (action.Phase == MonsterActionPhase.Move)
        {
            // 路点含导航面高度，能沿服务端路线爬坡和转弯。
            Vector3 destination = new Vector3(action.ToX, action.ToY, action.ToZ);
            if (ageMs < action.DurationMs)
            {
                position = Vector3.Lerp(position, destination, ageMs / action.DurationMs);
                state = "Move";
                if (action.TargetRoleId != 0) state = "FMove";
            }
            else position = destination;
        }
        monster.Position = position;
        monster.Model.Rotation = Quaternion.Euler(0f, action.Face, 0f);
        if (action.Phase == MonsterActionPhase.Dead || monster.IsDead)
        {
            if (skillPlayer != null) skillPlayer.Clear();
            if (action.Phase == MonsterActionPhase.Dead)
            {
                if (corpsePlayer == null) corpsePlayer = new MSceneMonsterCorpsePlayer(monster);
                corpsePlayer.Update(motion);
            }
            return;
        }
        // 受击由原实体状态机播放，技能特效与服务器结果时钟仍继续推进。
        bool allowAnimation = monster.CurrentState != StateDefine.kStateBehit;
        if (skillPlayer != null && skillPlayer.Update(allowAnimation)) return;
        if (!allowAnimation) return;
        // 行走和待机仍使用同一公共控制器；技能剪辑由真实SkillId单独绑定。
        MAnimator animator = monster.Model.Ator;
        EndAttackPlayback();
        if (animator.CurrentState != state) animator.Play(state);
    }

    /// <summary>显示原奸笑头顶特效和声音，持续时间来自原ShowExpressionTable。</summary>
    public void ShowHeadExpression()
    {
        if (skillPlayer == null) skillPlayer = new MSceneMonsterSkillPlayer(this, (MEntity)GetROObject());
        skillPlayer.ShowHeadExpression();
    }

    /// <summary>模型销毁时取消迟到加载并释放其动画、声音和特效引用。</summary>
    private void OnDestroy()
    {
        if (skillPlayer != null) skillPlayer.Dispose();
        if (corpsePlayer != null) corpsePlayer.Dispose();
    }

    /// <summary>运行时实体身份；零表示该模型尚未绑定实体世界。</summary>
    public ulong EntityUid
    {
        get
        {
            // 控制器持有世界中的原实体，不另造影子实体。
            ROObject entity = GetROObject();
            if (entity == null) return 0;
            return entity.UID;
        }
    }
}
