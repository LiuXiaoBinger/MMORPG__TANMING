
namespace Luban
{
    /// <summary>Luban 配置 Bean 抽象基类，提供类型编号接口。</summary>
    public abstract class BeanBase : ITypeId
    {
        /// <summary>返回该 Bean 的类型编号。</summary>
        public abstract int GetTypeId();
    }
}
