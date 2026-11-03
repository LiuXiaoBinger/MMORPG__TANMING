namespace MMoRpgCommon.SceneEntity
{
    /// <summary>客户端代理实体与服务端权威实体共用的组件生命周期契约。</summary>
    public interface IEntityComponent
    {
        /// <summary>组件在所属实体容器中的类别键。</summary>
        EntityComponentType ComponentType { get; }
        /// <summary>实体开始运行前进行一次初始化。</summary>
        void Initialize();
        /// <summary>推进一次逻辑帧。</summary>
        /// <param name="deltaMilliseconds">该帧经过的毫秒数。</param>
        void Update(int deltaMilliseconds);
        /// <summary>实体退出时释放组件持有的资源和引用。</summary>
        void Shutdown();
    }
}
