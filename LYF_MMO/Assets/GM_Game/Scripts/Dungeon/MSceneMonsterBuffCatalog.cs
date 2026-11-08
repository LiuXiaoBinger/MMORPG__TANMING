using System;
using UnityEngine;

/// <summary>魔神塔原BuffTable与BuffEffectTable中实际用到的视觉闭包。</summary>
[Serializable]
public sealed class MSceneMonsterBuffCatalog
{
    /// <summary>单个原Buff的持续特效、连线与模型颜色表现。</summary>
    [Serializable]
    public sealed class Buff
    {
        /// <summary>原BuffTable完整主键。</summary>
        public int Id;
        /// <summary>原Fx各挂点数据，持续至此Buff实例移除。</summary>
        public MSceneMonsterFxData[] Effects;
        /// <summary>原Link925曲线预制体，空值无连线。</summary>
        public string LinkPath;
        /// <summary>原Type26 ChangeEffect类型，0无此效果、2半透、3颜色。</summary>
        public int ChangeEffect;
        /// <summary>原半透或颜色效果指定的模型可见度。</summary>
        public float Alpha;
        /// <summary>原ChangeAlphaAndColor的RGB除以255后的颜色。</summary>
        public Color Color;
    }
    /// <summary>按原32个Buff闭包裁剪的静态列表。</summary>
    public Buff[] Buffs;
    /// <summary>原Monster Visibility shader的正式资源地址。</summary>
    public string VisibilityShader;
    /// <summary>首次副本加载时读取一次的只读配置。</summary>
    private static MSceneMonsterBuffCatalog instance;

    /// <summary>读取当前副本原Buff表现目录。</summary>
    public static MSceneMonsterBuffCatalog Load()
    {
        if (instance == null)
            instance = JsonUtility.FromJson<MSceneMonsterBuffCatalog>(Resources.Load<TextAsset>("Dungeons/Moshenta/MonsterBuffs").text);
        return instance;
    }

    /// <summary>按原完整BuffId定位其表现定义。</summary>
    /// <param name="buffId">服务器原表完整主键。</param>
    public Buff Find(int buffId)
    {
        for (int index = 0; index < Buffs.Length; index++)
            if (Buffs[index].Id == buffId) return Buffs[index];
        return null;
    }
}
