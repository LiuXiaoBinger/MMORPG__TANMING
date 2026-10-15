using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;
using UnityEngine;

/// <summary>沿 RO 目标查询职责筛选当前世界实体；本项目暂用直线距离，未接导航路程或自动战斗配置。</summary>
public static class MSkillTargetDefine
{
    /// <summary>取得排序后的首个候选，不保存跨场景实体引用。</summary>
    /// <param name="findEnemy">true 查询怪物敌方，false 查询角色友方。</param>
    /// <param name="range">选择半径，单位米；负数表示不限距离。</param>
    /// <param name="conditionFunc">本次技能的额外筛选条件，空表示不附加条件。</param>
    /// <returns>最近的有效候选，没有候选时为空。</returns>
    public static MEntity FindOneTarget(bool findEnemy, float range = -1f,
        Func<MEntity, bool> conditionFunc = null)
    {
        // 查询时借用现有强类型列表池，使用后归还容器。
        List<MEntity> targets = MListPool<MEntity>.Get();
        FindAllTarget(targets, findEnemy, range, conditionFunc);
        // 没有候选时不强制创建或替换目标。
        MEntity target = null;
        if (targets.Count > 0)
        {
            target = targets[0];
        }
        MListPool<MEntity>.Release(targets);
        return target;
    }

    /// <summary>从统一实体世界取得敌友候选，过滤生命周期、真实死亡和额外条件，再按距离排序。</summary>
    /// <param name="targets">由调用方持有的结果列表，每次查询先清空。</param>
    /// <param name="findEnemy">true 取怪物敌方，false 取角色友方。</param>
    /// <param name="range">选择半径，单位米；负数不限距离。</param>
    /// <param name="conditionFunc">当前技能需要的附加条件，为空时不额外过滤。</param>
    public static void FindAllTarget(List<MEntity> targets, bool findEnemy,
        float range = -1f, Func<MEntity, bool> conditionFunc = null)
    {
        targets.Clear();
        if (Global.Instance == null || Global.Instance.SceneWorld == null ||
            Global.Instance.SceneWorld.LocalRole == null)
        {
            return;
        }
        Global.Instance.SceneWorld.GetOpponent(targets,
            Global.Instance.SceneWorld.LocalRole, findEnemy, range);
        // 倒序删除无效对象，只修改结果容器，保留世界中原实体引用。
        for (int index = targets.Count - 1; index >= 0; index--)
        {
            if (!IsValidTarget(targets[index]) ||
                (conditionFunc != null && !conditionFunc(targets[index])))
            {
                targets.RemoveAt(index);
            }
        }
        targets.Sort(SortTargetByRng);
    }

    /// <summary>当前未接入玩家阵营，只有正式怪物实体（含精英、首领）归敌方。</summary>
    /// <param name="entity">被查询的当前场景实体。</param>
    /// <returns>怪物实体返回 true；其他玩家、NPC、宠物等不自动认作敌人。</returns>
    public static bool IsEnemy(MEntity entity)
    {
        return entity is MonsterEntity;
    }

    /// <summary>检查目标仍在当前世界并可见存活；未初始化的零上限属性不判死亡。</summary>
    /// <param name="entity">当前选择或候选实体，允许为空。</param>
    /// <returns>仍为当前世界同一活跃对象且模型有效时为 true。</returns>
    public static bool IsValidTarget(MEntity entity)
    {
        if (entity == null)
        {
            return false;
        }
        if (entity.LifecycleState != EntityLifecycleState.Active ||
            !entity.Model.IsLoaded || !entity.Model.Visible)
        {
            return false;
        }
        if (!entity.Model.GameObject.NativeObject.activeInHierarchy ||
            Global.Instance == null || Global.Instance.SceneWorld == null)
        {
            return false;
        }
        // 世界索引中的实际对象，防止继续选中已移除的引用。
        ROObject current;
        if (!Global.Instance.SceneWorld.TryGetEntity(entity.UID, out current) ||
            !ReferenceEquals(current, entity))
        {
            return false;
        }
        // 角色基础数据是其他玩家的实际血量来源，怪物读取已装配属性组件。
        RoleEntity role = entity as RoleEntity;
        if (role != null && role.BaseInfo != null)
        {
            return role.BaseInfo.MaxHp <= 0 || role.BaseInfo.CurrHp > 0;
        }
        return entity.AttrComp.MaxHp <= 0 || entity.AttrComp.CurrentHp > 0;
    }

    /// <summary>以玩家到目标的三维直线距离排序，同距离用 UID 保持循环选择顺序。</summary>
    /// <param name="a">第一个候选实体。</param>
    /// <param name="b">第二个候选实体。</param>
    /// <returns>负数表示 a 优先，正数表示 b 优先。</returns>
    public static int SortTargetByRng(MEntity a, MEntity b)
    {
        // 当前玩家的位置只用于本次同步排序，不缓存跨场景数据。
        Vector3 position = Global.Instance.SceneWorld.LocalRole.Position;
        // 平方距离可保持排序关系，避免开平方。
        int result = (a.Position - position).sqrMagnitude.CompareTo(
            (b.Position - position).sqrMagnitude);
        if (result == 0)
        {
            return a.UID.CompareTo(b.UID);
        }
        return result;
    }

    /// <summary>保留 RO 同名血量排序接口；默认选择仍按距离，不新增设置系统。</summary>
    /// <param name="a">第一个候选实体。</param>
    /// <param name="b">第二个候选实体。</param>
    /// <returns>血量比例更低者优先，同值沿距离顺序。</returns>
    public static int SortTargetByHP(MEntity a, MEntity b)
    {
        // 已初始化实体使用真实血量比例，未初始化时按满血处理。
        float hpA = GetHpPercent(a);
        // 第二个候选的当前血量比例，范围零到一。
        float hpB = GetHpPercent(b);
        // 先比较血量比例，再以距离稳定排序。
        int result = hpA.CompareTo(hpB);
        if (result == 0)
        {
            return SortTargetByRng(a, b);
        }
        return result;
    }

    /// <summary>读取当前候选的真实血量比例，未初始化属性时返回一。</summary>
    /// <param name="entity">已装配到当前世界的候选实体。</param>
    /// <returns>当前生命除以生命上限，未初始化时按满血。</returns>
    private static float GetHpPercent(MEntity entity)
    {
        // 其他玩家以最新角色同步基础数据为准。
        RoleEntity role = entity as RoleEntity;
        if (role != null && role.BaseInfo != null && role.BaseInfo.MaxHp > 0)
        {
            return (float)role.BaseInfo.CurrHp / role.BaseInfo.MaxHp;
        }
        if (entity.AttrComp.MaxHp > 0)
        {
            return (float)entity.AttrComp.CurrentHp / entity.AttrComp.MaxHp;
        }
        return 1f;
    }
}
