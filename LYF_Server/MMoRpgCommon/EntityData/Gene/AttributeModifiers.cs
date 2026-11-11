using System.Collections.Generic;

/// <summary>属性组件保存已执行的固定值、百分比和状态修正，不读取基因配置或持有基因实例。</summary>
public class AttributeModifiers
{
    /// <summary>各属性当前累积的固定点数增量。</summary>
    private readonly Dictionary<GeneAttributeType, double> flat = new Dictionary<GeneAttributeType, double>();
    /// <summary>各属性当前累积的百分点增量，10 表示增加 10%。</summary>
    private readonly Dictionary<GeneAttributeType, double> percent = new Dictionary<GeneAttributeType, double>();
    /// <summary>各单一状态位当前仍生效的来源数量。</summary>
    private readonly Dictionary<GeneState, int> states = new Dictionary<GeneState, int>();

    /// <summary>节点执行或撤销时立即更新属性修正。</summary>
    /// <param name="attribute">具体修改的属性。</param>
    /// <param name="delta">固定点数或百分点增量。</param>
    /// <param name="valueType">0 为固定值，1 为百分数。</param>
    public void ChangeAttribute(GeneAttributeType attribute, double delta, int valueType)
    {
        // 两类修正独立累计，百分比不会反复乘入角色裸属性。
        Dictionary<GeneAttributeType, double> values = flat;
        if (valueType == 1) values = percent;
        // 未有修正的属性从零开始累计。
        double current;
        values.TryGetValue(attribute, out current);
        values[attribute] = current + delta;
    }

    /// <summary>从属性组件已累计的数据计算最终值，不扫描基因列表。</summary>
    /// <param name="attribute">要读取的属性。</param>
    /// <param name="baseValue">角色或怪物原有基础值。</param>
    /// <returns>基础值乘百分比后再加固定修正。</returns>
    public double CalculateAttribute(GeneAttributeType attribute, double baseValue)
    {
        // 固定值与百分数均在节点 OnCall 时写入此组件。
        double fixedValue;
        flat.TryGetValue(attribute, out fixedValue);
        // 缺少百分比修正时默认为零。
        double percentValue;
        percent.TryGetValue(attribute, out percentValue);
        return baseValue * (1 + percentValue / 100) + fixedValue;
    }

    /// <summary>按位维护来源数，同一状态的多个来源可以分别撤销。</summary>
    /// <param name="state">要增减来源数的状态位组合。</param>
    /// <param name="delta">添加来源为 1，移除来源为 -1。</param>
    public void ChangeState(GeneState state, int delta)
    {
        // 当前状态定义为眩晕、定身、沉默、霸体、击倒五个独立位。
        for (int bit = (int)GeneState.Stun; bit <= (int)GeneState.Knockdown; bit <<= 1)
        {
            // 每个来源对自己包含的状态各贡献一次。
            GeneState key = (GeneState)bit;
            if ((state & key) == 0) continue;
            // 没有来源的状态以零开始累计。
            int count;
            states.TryGetValue(key, out count);
            states[key] = count + delta;
        }
    }

    /// <summary>合并仍有来源的状态，霸体暂时屏蔽眩晕和定身。</summary>
    public GeneState States
    {
        get
        {
            // 每次只读取组件已有的五个状态计数。
            GeneState result = GeneState.None;
            for (int bit = (int)GeneState.Stun; bit <= (int)GeneState.Knockdown; bit <<= 1)
            {
                // 当前单一状态的生效来源数。
                int count;
                if (states.TryGetValue((GeneState)bit, out count) && count > 0) result |= (GeneState)bit;
            }
            if ((result & GeneState.SuperArmor) != 0) result &= ~(GeneState.Stun | GeneState.Root | GeneState.Knockdown);
            return result;
        }
    }

    /// <summary>宿主回收时清空此前已经执行的全部修正。</summary>
    public void Clear() { flat.Clear(); percent.Clear(); states.Clear(); }
}
