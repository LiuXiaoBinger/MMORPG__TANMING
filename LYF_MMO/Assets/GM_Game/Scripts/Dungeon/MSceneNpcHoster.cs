using UnityEngine;

/// <summary>副本 NPC 的真实实体挂接组件，保留 NpcCtrl 既有实体访问入口。</summary>
public class MSceneNpcHoster : NpcCtrl
{
    /// <summary>本 NPC 出生波次；相同 NPC 配置可以在不同波次出现。</summary>
    public int WaveId;
    /// <summary>NPC原AI动作快照，场景卸载后随控制器释放。</summary>
    private MSceneMonsterMotion motion;
    /// <summary>与怪物共用的原技能表现播放器，不替换NPC交互控制器。</summary>
    private MSceneMonsterSkillPlayer skillPlayer;

    /// <summary>接收属于此原NPC出生位的服务器动作。</summary>
    /// <param name="state">含接收时刻的原SkillId、位姿与阶段。</param>
    public void ApplyMotion(MSceneMonsterMotion state)
    {
        motion = state;
        if (skillPlayer == null) skillPlayer = new MSceneMonsterSkillPlayer(this, (MEntity)GetROObject());
        skillPlayer.Apply(state);
        LateUpdate();
    }

    /// <summary>NPC释放的真实技能也沿同一个结果点表现链。</summary>
    /// <param name="hit">服务器确认的原技能结果。</param>
    /// <param name="target">本实例真实受击角色。</param>
    public void PlayResult(MonsterPlayerHitData hit, RoleEntity target)
    {
        if (skillPlayer == null) skillPlayer = new MSceneMonsterSkillPlayer(this, (MEntity)GetROObject());
        skillPlayer.PlayResult(hit, target);
    }

    /// <summary>按原NPC权威位姿驱动同一公共模型控制器。</summary>
    private void LateUpdate()
    {
        if (motion == null)
        {
            if (skillPlayer != null) skillPlayer.Update(false);
            return;
        }
        // 此控制器原先持有的NPC实体，保持任务交互入口不变。
        MEntity entity = (MEntity)GetROObject();
        // 原动作的路径或静止坐标。
        MonsterActionData action = motion.Action;
        // 异步加载和网络延迟均按接收时刻计入。
        float ageMs = (float)((Time.realtimeSinceStartupAsDouble - motion.ReceivedAt) * 1000d);
        // 普通静止动作保持原出生位。
        Vector3 position = new Vector3(action.X, action.Y, action.Z);
        if (action.Phase == MonsterActionPhase.Move)
            position = Vector3.Lerp(position, new Vector3(action.ToX, action.ToY, action.ToZ), ageMs / action.DurationMs);
        entity.Position = position;
        entity.Model.Rotation = Quaternion.Euler(0f, action.Face, 0f);
        if (skillPlayer.Update(true)) return;
        // 非技能阶段恢复既有NPC待机或移动槽。
        string state = MAnimator.STATE_IDLE;
        if (action.Phase == MonsterActionPhase.Move && ageMs < action.DurationMs) state = "Move";
        if (entity.Model.Ator.CurrentState != state) entity.Model.Ator.Play(state);
    }

    /// <summary>显示原奸笑头顶特效和声音，持续时间来自原ShowExpressionTable。</summary>
    public void ShowHeadExpression()
    {
        if (skillPlayer == null) skillPlayer = new MSceneMonsterSkillPlayer(this, (MEntity)GetROObject());
        skillPlayer.ShowHeadExpression();
    }

    /// <summary>NPC模型销毁时释放此实例的异步资源与已生成效果。</summary>
    private void OnDestroy()
    {
        if (skillPlayer != null) skillPlayer.Dispose();
    }

    /// <summary>当前模型绑定的 SceneWorld 实体身份。</summary>
    public ulong EntityUid
    {
        get
        {
            // 读取控制器持有的原实体引用。
            ROObject entity = GetROObject();
            if (entity == null) return 0;
            return entity.UID;
        }
    }
}
