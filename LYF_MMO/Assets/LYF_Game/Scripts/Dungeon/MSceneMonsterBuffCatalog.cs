using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>当前副本原 BuffTable 与 BuffEffectTable 中实际用到的视觉闭包。</summary>
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
    /// <summary>按当前副本实际 Buff 闭包裁剪的静态列表。</summary>
    public Buff[] Buffs;
    /// <summary>原Monster Visibility shader的正式资源地址。</summary>
    public string VisibilityShader;
    /// <summary>死亡后保留完整模型、等待服务器移除的原 EntityTable 编号；未登记表示均按普通尸体淡出。</summary>
    public int[] NoFadeEntityIds;
    /// <summary>按 Resources 根路径隔离的只读配置，不让同号 Buff 复用另一副本的表现。</summary>
    private static readonly Dictionary<string, MSceneMonsterBuffCatalog> loaded = new Dictionary<string, MSceneMonsterBuffCatalog>();

    /// <summary>读取登记根目录中的 Buff 表，未登记战斗表现时返回空。</summary>
    /// <param name="resourceRoot">当前场景登记的战斗表现 Resources 根路径。</param>
    public static MSceneMonsterBuffCatalog Load(string resourceRoot)
    {
        if (string.IsNullOrEmpty(resourceRoot)) return null;
        // 相同根共享只读数据，不同根独立保存完整原 Buff ID。
        MSceneMonsterBuffCatalog catalog;
        if (!loaded.TryGetValue(resourceRoot, out catalog))
        {
            catalog = JsonUtility.FromJson<MSceneMonsterBuffCatalog>(Resources.Load<TextAsset>(resourceRoot + "/MonsterBuffs").text);
            loaded.Add(resourceRoot, catalog);
        }
        return catalog;
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
