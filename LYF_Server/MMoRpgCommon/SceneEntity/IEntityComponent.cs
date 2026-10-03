namespace MMoRpgCommon.SceneEntity
{
    /// <summary>客户端代理实体与服务端权威实体共用的组件生命周期契约。</summary>
    public interface IEntityComponent
    {
        EntityComponentType ComponentType { get; }
        void Initialize();
        void Update(int deltaMilliseconds);
        void Shutdown();
    }
}
