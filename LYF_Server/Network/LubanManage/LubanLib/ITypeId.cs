namespace Luban
{
    /// <summary>Luban 类型编号接口，用于多态反序列化。</summary>
    public interface ITypeId
    {
        /// <summary>返回类型编号。</summary>
        int GetTypeId();
    }
}
