using MMoRpgCommon.SceneEntity;
namespace GameServer.SceneEntity
{
    /// <summary>场景实体的真实属性组件，保存已执行的基因修正并处理生命、法力和状态。</summary>
    public sealed class AttributesComponent : RuntimeEntityComponent, IAttributeComponent
    {
        /// <summary>已由节点 OnCall 写入的属性和状态修正，随宿主生命周期释放。</summary>
        private readonly AttributeModifiers modifiers = new AttributeModifiers();
        /// <summary>读取按来源计数并处理霸体冲突后的有效状态。</summary>
        public GeneState States { get { return modifiers.States; } }
        /// <summary>使用属性组件中已生效的修正计算最终属性。</summary>
        /// <param name="attribute">要读取的属性。</param>
        /// <param name="baseValue">不含基因的原始基础值。</param>
        /// <returns>应用固定值和百分比后的结果。</returns>
        public double CalculateAttribute(GeneAttributeType attribute, double baseValue) { return modifiers.CalculateAttribute(attribute, baseValue); }
        /// <summary>节点执行或撤销时直接更新属性组件。</summary>
        /// <param name="attribute">被修改的属性。</param>
        /// <param name="delta">固定点数或百分点增量。</param>
        /// <param name="valueType">0 为固定值，1 为百分比。</param>
        public void ChangeAttribute(GeneAttributeType attribute, double delta, int valueType) { modifiers.ChangeAttribute(attribute, delta, valueType); }
        /// <summary>增加或撤销节点贡献的一份状态来源。</summary>
        /// <param name="state">本节点提供的状态位。</param>
        /// <param name="delta">增加为 1，撤销为 -1。</param>
        public void ChangeState(GeneState state, int delta) { modifiers.ChangeState(state, delta); }
        /// <summary>当前生效的生命上限。</summary>
        public int MaxHp { get { return System.Math.Max(1, (int)CalculateAttribute(GeneAttributeType.MaxHp, Owner.MaxHp)); } }
        /// <summary>当前生效的法力上限。</summary>
        public int MaxMp { get { return System.Math.Max(0, (int)CalculateAttribute(GeneAttributeType.MaxMp, Owner.MaxMp)); } }
        /// <summary>场景实体的治疗增益，未配置基础值时从零开始。</summary>
        public double HealBonus { get { return CalculateAttribute(GeneAttributeType.HealBonus, 0); } }
        /// <summary>最近一次造成实际生命损失的角色编号，供场景死亡归因。</summary>
        public ulong LastDamageFirerId { get; private set; }
        /// <summary>魔神塔共享生命处理，接收真实来源和已算好的生命增量。</summary>
        internal System.Func<ulong, int, bool> SharedHpChange;
        /// <summary>直接修改实际场景实体生命，并沿用共享生命与伤害归因。</summary>
        /// <param name="delta">具体节点已算好的生命增量。</param>
        /// <param name="firerId">实际施加角色编号。</param>
        public void ChangeHp(int delta, ulong firerId)
        {
            if (Owner.CurrentHp <= 0) return;
            if (SharedHpChange == null || !SharedHpChange(firerId, delta))
                Owner.CurrentHp = System.Math.Max(0, System.Math.Min(MaxHp, Owner.CurrentHp + delta));
            if (delta < 0) LastDamageFirerId = firerId;
        }
        /// <summary>直接改变场景实体法力，普通恢复不复活死亡对象。</summary>
        /// <param name="delta">具体节点已算好的法力增量。</param>
        public void ChangeMp(int delta)
        {
            if (Owner.CurrentHp <= 0) return;
            Owner.CurrentMp = System.Math.Max(0, System.Math.Min(MaxMp, Owner.CurrentMp + delta));
        }
        /// <summary>场景实体释放时清空属性修正和共享生命出口。</summary>
        public override void Shutdown() { modifiers.Clear(); SharedHpChange = null; base.Shutdown(); }

        /// <summary>把此组件绑定到所属场景实体，并登记固定的组件类别。</summary>
        /// <param name="owner">拥有此组件的场景实体，释放组件时解除引用。</param>
        public AttributesComponent(ROObject owner) : base(owner, EntityComponentType.Attributes) { }
    }
}
