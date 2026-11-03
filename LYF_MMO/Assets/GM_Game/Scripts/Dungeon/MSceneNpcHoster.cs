/// <summary>副本 NPC 的真实实体挂接组件，保留 NpcCtrl 既有实体访问入口。</summary>
public class MSceneNpcHoster : NpcCtrl
{
    /// <summary>本 NPC 出生波次；相同 NPC 配置可以在不同波次出现。</summary>
    public int WaveId;
    /// <summary>当前模型绑定的 SceneWorld 实体身份。</summary>
    public ulong EntityUid
    {
        get
        {
            // 读取控制器持有的原实体引用。
            ROObject entity = GetROObject();
            if (entity == null) return 0;
            return entity.UID;
        }
    }
}
