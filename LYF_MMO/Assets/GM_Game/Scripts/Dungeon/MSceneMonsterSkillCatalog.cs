using System;
using UnityEngine;

/// <summary>魔神塔40个原技能的受控表现切片，与服务器原技能ID对齐。</summary>
[Serializable]
public sealed class MSceneMonsterSkillCatalog
{
    /// <summary>本场景依赖闭包中按技能号排序的完整表现行。</summary>
    public MSceneMonsterSkillData[] Skills;
    /// <summary>正式Resources配置的进程内只读缓存。</summary>
    private static MSceneMonsterSkillCatalog loaded;

    /// <summary>按服务端真实技能号读取原时间轴，非魔神塔技能返回空。</summary>
    /// <param name="skillId">MonsterActionData携带的原技能编号。</param>
    public static MSceneMonsterSkillData Find(int skillId)
    {
        if (loaded == null)
            loaded = JsonUtility.FromJson<MSceneMonsterSkillCatalog>(Resources.Load<TextAsset>("Dungeons/Moshenta/MonsterSkills").text);
        // 小型固定闭包直接按索引查找，不建立另一套技能管理器。
        for (int index = 0; index < loaded.Skills.Length; index++)
            if (loaded.Skills[index].Id == skillId) return loaded.Skills[index];
        return null;
    }
}
