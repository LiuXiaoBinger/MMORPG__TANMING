using System;

/// <summary>基因提供的状态位；运行模块负责查询，具体动作限制由业务接入方执行。</summary>
[Flags]
public enum GeneState
{
    /// <summary>没有状态效果。</summary>
    None = 0,
    /// <summary>眩晕，禁止移动和施法。</summary>
    Stun = 1,
    /// <summary>定身，禁止移动但允许施法。</summary>
    Root = 2,
    /// <summary>沉默，禁止施法但允许移动。</summary>
    Silence = 4,
    /// <summary>霸体，存续期间屏蔽眩晕和定身，不驱散原基因。</summary>
    SuperArmor = 8,
    /// <summary>击倒，禁止移动和施法；霸体存续期间同样被屏蔽。</summary>
    Knockdown = 16,
    /// <summary>神力外持续期间，外功结算取区间上限。</summary>
    MaxExternalAttack = 32,
    /// <summary>神力内持续期间，内功结算取区间上限。</summary>
    MaxInternalAttack = 64,
    /// <summary>虚弱期间内外功均取下限，覆盖同时生效的神力。</summary>
    MinAttack = 128
}
