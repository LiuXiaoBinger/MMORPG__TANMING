/// <summary>配置转换边界的抽象工厂；公共运行模块仅依赖运行节点，不引用配置程序集。</summary>
public abstract class GeneFactory
{
    /// <summary>按编号创建一棵独立运行树，具体工厂负责配置类型映射和必要字段转换。</summary>
    /// <param name="geneId">要创建的配置编号。</param>
    /// <param name="value">仅覆盖根节点数值，子节点使用各自配置数值。</param>
    /// <returns>拥有独立可变状态的根节点。</returns>
    public abstract GeneBase CreateGene(int geneId, float? value = null);
}
