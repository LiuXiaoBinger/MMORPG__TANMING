using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>RO 同名选择管理器，统一承接点击、瞄准切换与技能自动找敌，不负责攻击结算。</summary>
public sealed class MSkillTargetMgr
{
    /// <summary>当前客户端唯一选择入口，不持有跨场景玩家引用。</summary>
    public static readonly MSkillTargetMgr singleton = new MSkillTargetMgr();
    /// <summary>沿 RO GlobalTableFight 的 MaxChooseRange 默认选择半径十八米；与技能射程独立，点击、切换和自动选敌共用。</summary>
    public const float MaxChooseDistance = 18f;
    /// <summary>沿 RO 默认值，只在距离最近的前四个候选中循环。</summary>
    private const int _maxSelectCount = 4;
    /// <summary>统一选中目标变化通知，空值表示取消选择，供正式界面显示。</summary>
    public event Action<MEntity> TargetChanged;

    /// <summary>单例只统一选择职责，不创建目标实体或服务器状态。</summary>
    private MSkillTargetMgr() { }

    /// <summary>沿 RO 瞄准按钮流程，过滤怪物等级后在最近四个有效敌人中循环。</summary>
    /// <param name="monsterType">怪物等级位掩码，负一表示所有已接入怪物等级。</param>
    public void FindEnemyByAimBtn(int monsterType = -1)
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null ||
            Global.Instance.SceneWorld.LocalRole == null)
        {
            return;
        }
        // 当前玩家只在此次选择期间读取。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        // 临时候选列表借自现有强类型列表池。
        List<MEntity> targets = MListPool<MEntity>.Get();
        MSkillTargetDefine.FindAllTarget(targets, true, MaxChooseDistance);
        // 倒序去除不属于本次等级掩码的候选。
        for (int index = targets.Count - 1; index >= 0; index--)
        {
            // 怪物已由敌方过滤保证类型，等级从普通零开始。
            MonsterEntity monster = (MonsterEntity)targets[index];
            if (monsterType != -1 && ((1 << (int)monster.Rank) & monsterType) == 0)
            {
                targets.RemoveAt(index);
            }
        }
        // 没有敌人时沿 RO 取消当前选择。
        MEntity target = null;
        // 限制本轮参与循环的候选数量。
        int targetCount = Math.Min(targets.Count, _maxSelectCount);
        if (targetCount > 0)
        {
            target = targets[0];
            // 当前目标若在列表里，下一次选后一个，末项回到首项。
            for (int index = 0; index < targetCount; index++)
            {
                if (targets[index] == player.Target)
                {
                    target = targets[(index + 1) % targetCount];
                    break;
                }
            }
        }
        ResetTarget(target);
        MListPool<MEntity>.Release(targets);
    }

    /// <summary>沿 RO 技能自动选择入口，取得最近的合格候选并同步当前目标。</summary>
    /// <param name="findEnemy">true 选择怪物敌方，false 选择角色友方。</param>
    /// <param name="condition">该次技能的额外条件，允许为空。</param>
    /// <returns>找到的目标；无候选时为空，不禁止既有无目标技能。</returns>
    public MEntity FindEnemyBySkillBtn(bool findEnemy, Func<MEntity, bool> condition)
    {
        // 查询返回的仍为世界中的原实体引用。
        MEntity target = MSkillTargetDefine.FindOneTarget(findEnemy,
            MaxChooseDistance, condition);
        ResetTarget(target);
        return target;
    }

    /// <summary>统一写入玩家当前目标并通知界面，同目标不重复刷新。</summary>
    /// <param name="target">要选择的当前世界实体，空值取消选择。</param>
    /// <param name="isSelectTarget">保留 RO 点击选择参数；本项目无自动战斗头像特例。</param>
    public void ResetTarget(MEntity target, bool isSelectTarget = false)
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null ||
            Global.Instance.SceneWorld.LocalRole == null)
        {
            return;
        }
        // 不在管理器缓存角色，切场景后自动使用新本地玩家。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        if (player.Target == target)
        {
            return;
        }
        player.Target = target;
        TargetChanged?.Invoke(target);
    }

    /// <summary>RO 同名主动选敌入口，只接受存活敌方且在选择距离内的对象。</summary>
    /// <param name="target">点击或其他入口提交的敌方候选。</param>
    /// <returns>目标被接受时为 true。</returns>
    public bool OnSelectTarget(MEntity target)
    {
        if (!CanAttack(target))
        {
            return false;
        }
        ResetTarget(target, true);
        return true;
    }

    /// <summary>检查当前目标的离场、死亡、隐藏与超距，失效后取消选择。</summary>
    public void UpdateToCheckTarget()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null ||
            Global.Instance.SceneWorld.LocalRole == null)
        {
            return;
        }
        // 本帧当前玩家，未选择时无需候选查询。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        if (player.Target != null &&
            (!MSkillTargetDefine.IsValidTarget(player.Target) ||
             (player.Target.Position - player.Position).sqrMagnitude >
             MaxChooseDistance * MaxChooseDistance))
        {
            ResetTarget(null);
        }
    }

    /// <summary>提供点击与技能共用的有效敌方判定，不创建玩家阵营规则。</summary>
    /// <param name="entity">当前目标或候选，可为空。</param>
    /// <returns>当前世界有效敌人且在十八米选择半径内时为 true。</returns>
    public bool CanAttack(MEntity entity)
    {
        // 目标是当前世界中有效的敌对单位；IsValidTarget 内部已包含全局与世界非空判断。
        bool isEnemyTarget = MSkillTargetDefine.IsValidTarget(entity) &&
            MSkillTargetDefine.IsEnemy(entity);
        // 非敌对目标不满足时，后续本地玩家读取与距离计算都不得执行，保持原短路语义。
        if (!isEnemyTarget)
        {
            return false;
        }
        // 本地玩家已在当前场景中。
        bool hasLocalRole = Global.Instance.SceneWorld.LocalRole != null;
        // 本地玩家不存在时原表达式短路返回 false，距离表达式不得解引用 LocalRole。
        if (!hasLocalRole)
        {
            return false;
        }
        // 目标位于本地玩家十八米选择半径内。
        bool isWithinChooseDistance =
            (entity.Position - Global.Instance.SceneWorld.LocalRole.Position).sqrMagnitude <=
            MaxChooseDistance * MaxChooseDistance;
        return isWithinChooseDistance;
    }
}
