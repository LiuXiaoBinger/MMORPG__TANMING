using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

/// <summary>当前正式副本怪物的 RO DEF/MDEF 最小切片及本项目外功、内功伤害适配。</summary>
public class MonsterDefenseAttributes
{
    /// <summary>原 EntityTable.Id，只包含当前两个正式副本实际引用的战斗怪物。</summary>
    public int Id { get; set; }
    /// <summary>原 EntityTable.DEF，作为外功防御点数，不是减伤百分比。</summary>
    public int DefExternal { get; set; }
    /// <summary>原 EntityTable.MDEF，作为内功防御点数，不是减伤百分比。</summary>
    public int DefInternal { get; set; }
    /// <summary>游戏服进程内共享的只读基础防御索引，临时破甲另由怪物 GeneRuntime 保存。</summary>
    private static readonly Dictionary<int, MonsterDefenseAttributes> definitions = new Dictionary<int, MonsterDefenseAttributes>();

    /// <summary>从随游戏服部署的必要切片读取基础防御，不依赖参考项目运行目录。</summary>
    static MonsterDefenseAttributes()
    {
        // 正式嵌入资源只保留 Id、DEF、MDEF 的映射，不载入整份参考实体表。
        List<MonsterDefenseAttributes> rows;
        using (StreamReader reader = new StreamReader(typeof(MonsterDefenseAttributes).Assembly.GetManifestResourceStream("MonsterDefenseAttributes.json")))
            rows = new JavaScriptSerializer().Deserialize<List<MonsterDefenseAttributes>>(reader.ReadToEnd());
        for (int i = 0; i < rows.Count; i++) definitions.Add(rows[i].Id, rows[i]);
    }

    /// <summary>读取当前正式怪物必备的基础防御，缺配置时保留明确的配置错误。</summary>
    /// <param name="creatureId">RO 场景实体配置编号。</param>
    /// <returns>不会被战斗过程改写的基础属性。</returns>
    public static MonsterDefenseAttributes Get(int creatureId) { return definitions[creatureId]; }

    /// <summary>把角色最终外功、内功点数接到当前普攻；两种攻击分别使用目标的对应最终防御。</summary>
    /// <param name="attacker">角色裸属性叠加装备及临时基因后的最终面板。</param>
    /// <param name="target">目标怪物实际持有的基因容器，包含限时破甲。</param>
    /// <param name="skillDamage">现有普攻 Hurt 固定伤害，归入外功部分。</param>
    /// <returns>经过防御及承伤修正的生命点数，由调用方按剩余 HP 裁切。</returns>
    public virtual int CalculateDamage(RoleBaseInfo attacker, GeneRuntime target, int skillDamage)
    {
        // 内防独立，外破甲不能降低内功防御。
        double magicDefense = Math.Max(0, target.CalculateAttribute(GeneAttributeType.DefInternal, DefInternal));
        // 普攻继续复用外防曲线，基础外攻与固定伤害沿旧公式合并。
        double physicalRate = GetExternalDefenseRate(target);
        // 两种防御都按点数处理，没有把一梦江湖面板点数当成概率。
        double magicRate = Math.Max((magicDefense + 1000) / (magicDefense * 10 + 1000), 0.05);
        // 本项目普攻以区间均值消费当前外功攻击，不额外引入暴击或命中概率公式。
        double physical = (Math.Max(0, ((double)attacker.AtkExternalMin + attacker.AtkExternalMax) / 2) + skillDamage) * physicalRate;
        // 内功攻击部分独立经过内防，使内破甲能影响真实扣血。
        double magical = Math.Max(0, ((double)attacker.AtkInternalMin + attacker.AtkInternalMax) / 2) * magicRate;
        return Math.Max(0, (int)((physical + magical) * target.CalculateAttribute(GeneAttributeType.DamageTaken, 100) / 100));
    }

    /// <summary>已经按外功攻击计算好的专属技能值，只经过目标外防和承伤修正。</summary>
    /// <param name="target">目标怪物的基因容器，受临时破甲影响。</param>
    /// <param name="externalDamage">技能自身合成的外功伤害，单位生命点数。</param>
    /// <returns>外防结算后的数值，由调用方追加最终增伤及扣血。</returns>
    public int CalculateExternalSkillDamage(GeneRuntime target, int externalDamage)
    {
        // 承伤系数包含目标自己的既有战斗基因，百分数 100 为原值。
        double damageTaken = target.CalculateAttribute(GeneAttributeType.DamageTaken, 100);
        return Math.Max(0, (int)(externalDamage * GetExternalDefenseRate(target) * damageTaken / 100));
    }

    /// <summary>使用统一的外功防御曲线，基础防御与实际破甲一起结算。</summary>
    /// <param name="target">目标怪物的属性基因。</param>
    /// <returns>进攻值经过外防后保留的比例。</returns>
    private double GetExternalDefenseRate(GeneRuntime target)
    {
        // RO 怪物外防分支以 4000 为基础系数，最低保留 5%。
        double defense = Math.Max(0, target.CalculateAttribute(GeneAttributeType.DefExternal, DefExternal));
        return Math.Max((defense + 4000) / (defense * 10 + 4000), 0.05);
    }
}
