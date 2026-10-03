using UnityEngine;

/// <summary>
/// 实体统一输入参数。
/// 事件类别固定为 MEventType.Input，参数只保留当前玩家控制需要的明确字段。
/// </summary>
public sealed class InputEventArgs : IEventArg
{
    /// <summary>动作配置名称，例如 Shift、Jump 或 Movement；文本输入为空。</summary>
    public string ActionName { get; private set; }
    /// <summary>动作生命周期阶段；文本输入不使用此字段。</summary>
    public InputActionPhase ActionPhase { get; private set; }
    /// <summary>Movement 动作向量，按钮和文本输入为零向量。</summary>
    public Vector2 Movement { get; private set; }
    /// <summary>可选按键编码，当前由输入层保留供后续快捷键扩展。</summary>
    public KeyCode Key { get; private set; }
    /// <summary>文本快捷键字符，动作输入为默认字符。</summary>
    public char InputCharacter { get; private set; }
    /// <summary>按钮动作是否按下。</summary>
    public bool IsPressed { get; private set; }
    /// <summary>参数生命周期标记；true 表示当前实例已经归还输入参数对象池，不能继续读取。</summary>
    public bool Deprecated { get; set; }

    /// <summary>
    /// 创建空的输入参数实例。
    /// 该构造函数仅供 <see cref="MEventPool{T}"/> 通过 new() 约束创建池实例，业务代码必须调用静态工厂方法。
    /// </summary>
    public InputEventArgs()
    {
        ResetData();
    }

    /// <summary>从对象池获取并填写动作输入参数。</summary>
    /// <param name="actionName">动作配置名称。</param>
    /// <param name="actionPhase">动作生命周期阶段。</param>
    /// <param name="movement">动作向量值。</param>
    /// <param name="isPressed">按钮动作按下状态。</param>
    /// <returns>本次派发专用的池化输入参数，分发结束后由分发器回收。</returns>
    public static InputEventArgs CreateAction(string actionName, InputActionPhase actionPhase,
        Vector2 movement, bool isPressed)
    {
        // 从当前参数类型专属池获取实例，避免每次输入事件产生托管堆分配。
        InputEventArgs eventArgs = MEventPool<InputEventArgs>.GetEvent();
        eventArgs.ResetData();
        eventArgs.ActionName = actionName;
        eventArgs.ActionPhase = actionPhase;
        eventArgs.Movement = movement;
        eventArgs.IsPressed = isPressed;
        return eventArgs;
    }

    /// <summary>从对象池获取并填写文本输入参数。</summary>
    /// <param name="inputCharacter">本次输入字符。</param>
    /// <returns>本次派发专用的池化输入参数，分发结束后由分发器回收。</returns>
    public static InputEventArgs CreateText(char inputCharacter)
    {
        // 文本输入与动作输入复用同一强类型参数，但由字符字段承载实际数据。
        InputEventArgs eventArgs = MEventPool<InputEventArgs>.GetEvent();
        eventArgs.ResetData();
        eventArgs.ActionPhase = InputActionPhase.Performed;
        eventArgs.InputCharacter = inputCharacter;
        return eventArgs;
    }

    /// <summary>复制统一输入参数。</summary>
    /// <returns>独立的输入参数副本。</returns>
    public IEventArg Clone()
    {
        // 克隆也必须使用对象池，供需要跨帧保存输入快照的监听器自行管理其生命周期。
        InputEventArgs clone = CreateAction(ActionName, ActionPhase, Movement, IsPressed);
        clone.Key = Key;
        clone.InputCharacter = InputCharacter;
        return clone;
    }

    /// <summary>清空输入数据并归还对象池；重复回收由对象池生命周期标记拦截。</summary>
    public void Recycle()
    {
        ResetData();
        MEventPool<InputEventArgs>.Recycle(this);
    }

    /// <summary>清空上一轮派发遗留的输入数据，避免池复用后把旧按键或移动向量传给新监听器。</summary>
    private void ResetData()
    {
        ActionName = string.Empty;
        ActionPhase = InputActionPhase.Performed;
        Movement = Vector2.zero;
        Key = KeyCode.None;
        InputCharacter = '\0';
        IsPressed = false;
    }
}
