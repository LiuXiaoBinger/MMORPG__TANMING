using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>管理当前选中目标：处理点击、瞄准按钮循环选择、技能自动找敌和目标失效清除。</summary>
public sealed class MSkillTargetMgr
{
    /// <summary>当前客户端唯一选择入口，不持有跨场景玩家引用。</summary>
    public static readonly MSkillTargetMgr singleton = new MSkillTargetMgr();
    /// <summary>点击与瞄准切换的选择半径，单位米；不用于自动获取或已选目标的保持。</summary>
    public const float MaxChooseDistance = 18f;
    /// <summary>攻击技能没有有效目标时自动获取敌人的半径，单位米，与技能出招射程独立。</summary>
    public const float AutoChooseDistance = 10f;
    /// <summary>沿 RO 默认值，只在距离最近的前四个候选中循环。</summary>
    private const int _maxSelectCount = 4;
    /// <summary>当前目标是否来自主动点击或瞄准切换；自动选择和清除目标时复位。</summary>
    public bool HasManualTarget { get; private set; }
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
        ResetTarget(target, true);
        MListPool<MEntity>.Release(targets);
    }

    /// <summary>攻击自动获取只查十米内，优先取指定方向内最近敌人，无候选再取其余方向最近敌人。</summary>
    /// <param name="findEnemy">true 选择怪物敌方，false 选择角色友方。</param>
    /// <param name="condition">优先选择的方向条件；现有攻击入口传入前方扇形，空值表示直接取最近。</param>
    /// <returns>找到的目标；无候选时为空，不禁止既有无目标技能。</returns>
    public MEntity FindEnemyBySkillBtn(bool findEnemy, Func<MEntity, bool> condition)
    {
        // 查询返回的仍为世界中的原实体引用。
        MEntity target = MSkillTargetDefine.FindOneTarget(findEnemy,
            AutoChooseDistance, condition);
        if (target == null)
        {
            target = MSkillTargetDefine.FindOneTarget(findEnemy, AutoChooseDistance);
        }
        ResetTarget(target);
        return target;
    }

    /// <summary>统一写入玩家当前目标并通知界面，同目标不重复刷新。</summary>
    /// <param name="target">要选择的当前世界实体，空值取消选择。</param>
    /// <param name="isSelectTarget">点击或瞄准切换传 true，技能自动选择传 false。</param>
    public void ResetTarget(MEntity target, bool isSelectTarget = false)
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null ||
            Global.Instance.SceneWorld.LocalRole == null)
        {
            return;
        }
        // 不在管理器缓存角色，切场景后自动使用新本地玩家。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        // 玩家点中原自动目标时，只更新选择来源即可继续使用同一界面目标。
        HasManualTarget = target != null && isSelectTarget;
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
        // 主动点击的距离限制只在选择这一刻应用，不导致已经锁定的敌人被自动取消。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        if ((target.Position - player.Position).sqrMagnitude > MaxChooseDistance * MaxChooseDistance)
        {
            return false;
        }
        ResetTarget(target, true);
        return true;
    }

    /// <summary>仅在目标离场、死亡或隐藏时取消选择；保留有效目标，不在每帧检查中改变角色朝向。</summary>
    public void UpdateToCheckTarget()
    {
        if (Global.Instance == null || Global.Instance.SceneWorld == null ||
            Global.Instance.SceneWorld.LocalRole == null)
        {
            return;
        }
        // 本帧当前玩家，未选择时无需候选查询。
        PlayerEntity player = Global.Instance.SceneWorld.LocalRole;
        if (player.Target == null)
        {
            return;
        }
        if (!MSkillTargetDefine.IsValidTarget(player.Target))
        {
            ResetTarget(null);
            return;
        }
    }

    /// <summary>判断当前世界中的有效敌人，保持目标不受获取半径或手动选择距离限制。</summary>
    /// <param name="entity">当前目标或候选，可为空。</param>
    /// <returns>本地玩家存在且目标仍为当前世界的有效敌人时为 true。</returns>
    public bool CanAttack(MEntity entity)
    {
        // 目标是当前世界中有效的敌对单位；IsValidTarget 内部已包含全局与世界非空判断。
        bool isEnemyTarget = MSkillTargetDefine.IsValidTarget(entity) &&
            MSkillTargetDefine.IsEnemy(entity);
        // 无效或非敌方目标直接返回，无需再计算距离。
        if (!isEnemyTarget)
        {
            return false;
        }
        // 本地玩家已在当前场景中。
        bool hasLocalRole = Global.Instance.SceneWorld.LocalRole != null;
        // 没有本地玩家就没有距离比较的起点，直接返回 false。
        if (!hasLocalRole)
        {
            return false;
        }
        return true;
    }
}
