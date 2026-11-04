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
    /// <summary>相邻两刀使用同名 Skill 状态，每个新挥刀通知只重新起手一次。</summary>
    private bool attackPending;
    /// <summary>这只兽人正在用服务端攻速播放普通攻击时为真。</summary>
    private bool playingAttack;
    /// <summary>进入攻击前的动画倍率，受击、死亡及返回站立时恢复。</summary>
    private float previousSpeed;

    /// <summary>接管一个新路径段或攻击，保留收包时刻用于恢复迟到的表现。</summary>
    /// <param name="state">与本模型同一出生位的最新动作。</param>
    public void ApplyMotion(MSceneMonsterMotion state)
    {
        motion = state;
        attackPending = state.Action.Phase == MonsterActionPhase.Attack;
        LateUpdate();
    }

    /// <summary>结束上一次普攻的专属动画倍率，保证随后的其他动作使用原倍率。</summary>
    public void EndAttackPlayback()
    {
        if (!playingAttack) return;
        ((MEntity)GetROObject()).Model.Ator.Speed = previousSpeed;
        playingAttack = false;
    }

    /// <summary>按权威路径段插值并驱动公共图。受击和死亡由实体的正式状态机播放。</summary>
    private void LateUpdate()
    {
        if (motion == null) return;
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
        if (action.Phase == MonsterActionPhase.Dead || monster.IsDead) return;
        if (monster.CurrentState == StateDefine.kStateBehit) return;
        // 只修改这只兽人实例的覆盖器，公共状态图本身不承载攻速或选招。
        MAnimator animator = monster.Model.Ator;
        if (action.Phase == MonsterActionPhase.Attack && ageMs < action.DurationMs)
        {
            if (!playingAttack)
            {
                previousSpeed = animator.Speed;
                playingAttack = true;
            }
            // 1000118401 的 RO 脚本基础时长为 1 秒，实际总时长由服务端按攻速换算。
            float speed = 1000f / (action.ElapsedMs + (float)action.DurationMs);
            animator.Speed = speed;
            if (attackPending || animator.CurrentState != MAnimator.STATE_SKILL)
            {
                // Skill 槽内是真实兽人片段；迟到时按已过的片段时间定位，而非按整段攻击百分比。
                AnimationClip clip = animator.GetOrCreateOverrideController()[MAnimator.STATE_SKILL];
                float normalized = Mathf.Clamp01((action.ElapsedMs + ageMs) * 0.001f * speed / clip.length);
                animator.Play(MAnimator.STATE_SKILL, 0, normalized);
                attackPending = false;
            }
            return;
        }
        EndAttackPlayback();
        if (animator.CurrentState != state) animator.Play(state);
        attackPending = false;
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
