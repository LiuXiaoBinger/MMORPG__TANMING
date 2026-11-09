using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>当前副本原技能的受控表现切片，与服务器原技能 ID 对齐。</summary>
[Serializable]
public sealed class MSceneMonsterSkillCatalog
{
    /// <summary>本场景依赖闭包中按技能号排序的完整表现行。</summary>
    public MSceneMonsterSkillData[] Skills;
    /// <summary>按 Resources 根路径隔离的只读目录，不让相同技能号串用另一副本的数据。</summary>
    private static readonly Dictionary<string, MSceneMonsterSkillCatalog> loaded = new Dictionary<string, MSceneMonsterSkillCatalog>();

    /// <summary>读取登记根目录中的技能表，未登记战斗表现时返回空。</summary>
    /// <param name="resourceRoot">当前场景登记的战斗表现 Resources 根路径。</param>
    public static MSceneMonsterSkillCatalog Load(string resourceRoot)
    {
        if (string.IsNullOrEmpty(resourceRoot)) return null;
        // 相同根共享只读数据，不同根即使技能 ID 相同也保持独立。
        MSceneMonsterSkillCatalog catalog;
        if (!loaded.TryGetValue(resourceRoot, out catalog))
        {
            catalog = JsonUtility.FromJson<MSceneMonsterSkillCatalog>(Resources.Load<TextAsset>(resourceRoot + "/MonsterSkills").text);
            loaded.Add(resourceRoot, catalog);
        }
        return catalog;
    }

    /// <summary>按服务端真实技能号读取本目录时间轴，未登记的技能返回空。</summary>
    /// <param name="skillId">MonsterActionData 携带的原技能编号。</param>
    public MSceneMonsterSkillData Find(int skillId)
    {
        // 小型固定闭包直接按索引查找，不建立另一套技能管理器。
        for (int index = 0; index < Skills.Length; index++)
            if (Skills[index].Id == skillId) return Skills[index];
        return null;
    }
}
