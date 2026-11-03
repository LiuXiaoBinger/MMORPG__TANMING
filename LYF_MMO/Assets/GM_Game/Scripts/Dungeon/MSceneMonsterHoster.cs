using UnityEngine;

/// <summary>怪物模型与 SceneWorld 中 MonsterEntity 的挂接组件，沿用 MonsterCtrl 的实体路由。</summary>
public class MSceneMonsterHoster : MonsterCtrl
{
    /// <summary>模型未缩放时的水平包围半径，单位米。</summary>
    public float Radius;
    /// <summary>本实例来源波次，用于定位编辑器中的出生记录。</summary>
    public int WaveId;
    /// <summary>运行时实体身份；零表示该模型尚未绑定实体世界。</summary>
    public ulong EntityUid
    {
        get
        {
            // 控制器持有世界中的原实体，不另造影子实体。
            ROObject entity = GetROObject();
            if (entity == null) return 0;
            return entity.UID;
        }
    }
}
