using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using cfg;

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
        return CalculateDamage(attacker, target, skillDamage, GeneState.None);
    }

    /// <summary>按当前攻击状态选取内外功后，分别结算对应防御。</summary>
    /// <param name="attacker">出手角色此刻的最终攻击区间。</param>
    /// <param name="target">受击怪物的基因容器。</param>
    /// <param name="skillDamage">归入外功的招式固定伤害。</param>
    /// <param name="states">本次出手时攻击者的神力或虚弱状态。</param>
    public int CalculateDamage(RoleBaseInfo attacker, GeneRuntime target, int skillDamage, GeneState states)
    {
        // 内防独立，外破甲不能降低内功防御。
        double magicDefense = Math.Max(0, target.CalculateAttribute(GeneAttributeType.DefInternal, DefInternal));
        // 普攻继续复用外防曲线，基础外攻与固定伤害沿旧公式合并。
        double physicalRate = GetExternalDefenseRate(target);
        // 两种防御都按点数处理，没有把一梦江湖面板点数当成概率。
        double magicRate = Math.Max((magicDefense + 1000) / (magicDefense * 10 + 1000), 0.05);
        // 选择本次外功取值后再追加招式固定伤害。
        double physical = (Math.Max(0, GeneRuntime.ResolveAttack(states, GeneState.MaxExternalAttack, attacker.AtkExternalMin, attacker.AtkExternalMax)) + skillDamage) * physicalRate;
        // 内功攻击部分独立经过内防，使内破甲能影响真实扣血。
        double magical = Math.Max(0, GeneRuntime.ResolveAttack(states, GeneState.MaxInternalAttack, attacker.AtkInternalMin, attacker.AtkInternalMax)) * magicRate;
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

    /// <summary>计算本次命中额外附带的一个元素伤害，由对应抵抗独立结算。</summary>
    /// <param name="attacker">叠加装备及临时基因后的角色属性快照。</param>
    /// <param name="target">受击怪物的属性基因，包含当前该系抵抗与承伤修正。</param>
    /// <param name="effect">该段使用的技能等级行，未配置元素时保留原伤害。</param>
    /// <returns>经对应元素抗性和通用承伤处理、向下取整后的生命点数。</returns>
    internal static int CalculateElementSkillDamage(RoleBaseInfo attacker, GeneRuntime target, SkillEffectInfo effect)
    {
        if (effect == null || effect.ElementAttackAttributeId == 0) return 0;

        // 属性编号按攻击、穿透、抵抗及穿透上限分组，每组内水、毒、火、风、元顺序相同。
        int elementIndex = effect.ElementAttackAttributeId - (int)GeneAttributeType.WaterAttack;
        // 角色这系的最终攻击点数，缺省零；技能自带的固定元素伤害可独立生效。
        double attack;
        attacker.AdvancedAttributes.TryGetValue(effect.ElementAttackAttributeId, out attack);
        // 当前元素的穿透点数，可用于抵消同系抗性。
        double penetration;
        attacker.AdvancedAttributes.TryGetValue((int)GeneAttributeType.WaterPenetration + elementIndex, out penetration);
        // 当前元素的超额穿透上限，以点数保存。
        double penetrationCap;
        attacker.AdvancedAttributes.TryGetValue((int)GeneAttributeType.WaterPenetrationCap + elementIndex, out penetrationCap);
        // 怪物这系的最终抵抗，未挂对应属性基因时以零为基底。
        double resistance = Math.Max(0, target.CalculateAttribute(GeneAttributeType.WaterResistance + elementIndex, 0));
        // 同系穿透先抵消抵抗。剩余抵抗用于减伤，超出的穿透受对应上限约束后折算为增伤百分点。
        double netResistance = resistance - Math.Max(0, penetration);
        // 剩余 100 点抵抗时伤害保留一半，更高的抵抗继续递减。
        double resistanceRate = 100 / (100 + Math.Max(0, netResistance));
        // 抵消后每点有效超额穿透提高 1% 的该系伤害。
        double penetrationRate = 1 + Math.Min(Math.Max(0, -netResistance), Math.Max(0, penetrationCap)) / 100;
        // 万分比仅缩放面板攻击，固定伤害独立加入并同样受该系防御与通用承伤影响。
        double baseDamage = Math.Max(0, attack) * (effect.ElementDamagePermyriad / 10000.0) + effect.ElementFlatDamage;
        // 通用承伤在每个伤害分量只应用一次，100 表示原有的 100% 承伤。
        double damageTaken = Math.Max(0, target.CalculateAttribute(GeneAttributeType.DamageTaken, 100));
        return (int)Math.Min(int.MaxValue, baseDamage * resistanceRate * penetrationRate * damageTaken / 100);
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
