using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>热键目录与本机绑定，物理键独立于服务器保存的技能槽标识。</summary>
public static class GameHotkeys
{
    /// <summary>区分未开放条目、技能槽、移动动作、选敌和窗口入口。</summary>
    public enum Kind { Unavailable, Skill, Motion, Target, Window }

    /// <summary>录屏动作的一行，保存草稿前始终按已应用的键响应。</summary>
    public sealed class Entry
    {
        /// <summary>键盘图与改键列表共用的中文动作名称。</summary>
        public readonly string Name;
        /// <summary>恢复默认时使用的物理键，未开放行仅用它标注参考键位。</summary>
        public readonly Key Default;
        /// <summary>负责执行此动作的游戏入口类型。</summary>
        public readonly Kind Type;
        /// <summary>原技能槽、移动方向或窗口的稳定标识，跨改键保持一致。</summary>
        public readonly string Command;
        /// <summary>已保存并实际用于游戏的物理键。</summary>
        public Key Bound { get; internal set; }
        /// <summary>该动作独立的本机存档名称。</summary>
        internal string Preference => "LYF.Hotkey." + Type + "." + Command;

        /// <summary>载入该动作已保存的键位，首次启动使用默认值。</summary>
        /// <param name="name">界面动作名。</param>
        /// <param name="key">默认物理键。</param>
        /// <param name="type">执行入口，缺省表示未开放。</param>
        /// <param name="command">执行入口内稳定不变的命令。</param>
        internal Entry(string name, Key key, Kind type = Kind.Unavailable, string command = "")
        {
            Name = name;
            Default = key;
            Type = type;
            Command = command;
            Bound = key;
            if (type != Kind.Unavailable) Bound = (Key)PlayerPrefs.GetInt(Preference, (int)key);
        }
    }

    /// <summary>快慢跑按钮和设置页共用的绑定，沿用已有的本机改键存档。</summary>
    public static readonly Entry RunMode = new Entry("快/慢跑", Key.F4, Kind.Motion, "run");

    /// <summary>与预制体三列从上到下的行对应，项目补充动作排在各列末尾。</summary>
    public static readonly Entry[] Rows =
    {
        new Entry("普攻", Key.Digit1, Kind.Skill, "Q"),
        new Entry("技1", Key.Digit2, Kind.Skill, "1"),
        new Entry("技2", Key.Digit3, Kind.Skill, "2"),
        new Entry("技3", Key.Digit4, Kind.Skill, "3"),
        new Entry("技4", Key.Digit5, Kind.Skill, "4"),
        new Entry("技5", Key.Digit6, Kind.Skill, "5"),
        new Entry("技6", Key.Digit7, Kind.Skill, "6"),
        new Entry("技7", Key.Digit8, Kind.Skill, "7"),
        new Entry("技8", Key.Digit9, Kind.Skill, "8"),
        new Entry("绝学", Key.R, Kind.Skill, "R"),
        new Entry("火铳", Key.Backquote),
        new Entry("前进", Key.W, Kind.Motion, "up"),
        new Entry("后退", Key.S, Kind.Motion, "down"),
        new Entry("左移", Key.A, Kind.Motion, "left"),
        new Entry("右移", Key.D, Kind.Motion, "right"),
        new Entry("加蓝", Key.F1),
        new Entry("加血", Key.F2),
        new Entry("暗器/嘲讽", Key.F5),
        new Entry("阵法", Key.N),
        new Entry("铭兵", Key.P),
        new Entry("额外", Key.X),
        new Entry("副本技", Key.U),
        new Entry("创1", Key.K),
        new Entry("创2", Key.L),
        new Entry("创3", Key.Semicolon),
        new Entry("创4", Key.Quote),
        new Entry("副技能", Key.E, Kind.Skill, "E"),
        new Entry("备用槽", Key.Digit0, Kind.Skill, "9"),
        new Entry("跳跃", Key.Space, Kind.Motion, "jump"),
        new Entry("轻功", Key.Q, Kind.Skill, "V"),
        new Entry("下坠", Key.E),
        new Entry("翻滚", Key.F),
        new Entry("齐飞", Key.V),
        new Entry("滑步", Key.F, Kind.Skill, "F"),
        new Entry("包裹", Key.B, Kind.Window, "B"),
        new Entry("武学", Key.J, Kind.Window, "L"),
        new Entry("好友", Key.H, Kind.Window, "H"),
        new Entry("队伍", Key.T, Kind.Window, "T"),
        new Entry("上/下马", Key.F3),
        new Entry("切换", Key.Tab, Kind.Target, "target"),
        new Entry("设置", Key.Escape, Kind.Window, "Settings"),
        new Entry("帮派", Key.O),
        new Entry("任务", Key.Y, Kind.Window, "Y"),
        new Entry("地图", Key.M, Kind.Window, "M"),
        RunMode,
        new Entry("观光/上浮", Key.F7),
        new Entry("起飞/下潜", Key.F6),
        new Entry("使用", Key.G),
        new Entry("双门派", Key.Z),
        new Entry("角色", Key.I, Kind.Window, "I"),
    };

    /// <summary>旧布局允许其他动作独占 F4；这类存档沿用原按键，快慢跑仍可点击。</summary>
    static GameHotkeys()
    {
        if (RunMode.Bound != Key.F4) return;
        // 只检查有游戏入口的行，未开放行不占用快捷键。
        for (int index = 0; index < Rows.Length; index++)
        {
            // 已读取本机存档的动作，和快慢跑共用同一份目录。
            Entry row = Rows[index];
            if (row != RunMode && row.Type != Kind.Unavailable && row.Bound == Key.F4)
            {
                RunMode.Bound = Key.None;
                break;
            }
        }
    }

    /// <summary>保存或恢复默认后通知输入动作和快捷栏重读物理键。</summary>
    public static event Action Changed;
    /// <summary>全屏设置打开期间屏蔽世界输入。</summary>
    public static bool SettingsVisible { get; internal set; }
    /// <summary>三列编辑器打开时由编辑器独占键盘。</summary>
    public static bool Editing { get; internal set; }

    /// <summary>提交整套草稿并通知所有已在场的输入消费者。</summary>
    /// <param name="draft">按目录顺序排列的物理键，与行数相同。</param>
    public static void Save(Key[] draft)
    {
        // 下标同时定位目录行与本次编辑的键。
        for (int index = 0; index < Rows.Length; index++)
        {
            // 未开放条目既不保存，也不占用真实按键。
            Entry row = Rows[index];
            if (row.Type == Kind.Unavailable) continue;
            row.Bound = draft[index];
            if (row.Default == row.Bound) PlayerPrefs.DeleteKey(row.Preference);
            else PlayerPrefs.SetInt(row.Preference, (int)row.Bound);
        }
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    /// <summary>从玩家看到的物理键反查实际技能槽的显示字样。</summary>
    /// <param name="slot">服务端和点击施法共用的原始槽号。</param>
    public static string SkillLabel(string slot)
    {
        // 相同命令字符串可用于不同类别，只匹配技能行。
        for (int index = 0; index < Rows.Length; index++)
        {
            if (Rows[index].Type == Kind.Skill && Rows[index].Command == slot) return Label(Rows[index].Bound);
        }
        return slot;
    }

    /// <summary>统一显示数字、符号和常用组合键的简短键帽名称。</summary>
    /// <param name="key">Input System 物理键码。</param>
    public static string Label(Key key)
    {
        if (key == Key.None) return "—";
        if (key >= Key.Digit1 && key <= Key.Digit0) return key.ToString().Substring(5);
        switch (key)
        {
            case Key.LeftShift: return "Shift";
            case Key.LeftCtrl: return "Ctrl";
            case Key.LeftAlt: return "Alt";
            case Key.Escape: return "Esc";
            case Key.Backquote: return "`";
            case Key.Minus: return "-";
            case Key.Equals: return "=";
            case Key.LeftBracket: return "[";
            case Key.RightBracket: return "]";
            case Key.Backslash: return "\\";
            case Key.Semicolon: return ";";
            case Key.Quote: return "'";
            case Key.Comma: return ",";
            case Key.Period: return ".";
            case Key.Slash: return "/";
            case Key.UpArrow: return "↑";
            case Key.DownArrow: return "↓";
            case Key.LeftArrow: return "←";
            case Key.RightArrow: return "→";
            default: return key.ToString();
        }
    }

    /// <summary>取得 Input System 键名，用于覆盖移动、跳跃和奔跑动作的绑定。</summary>
    /// <param name="key">用户选定的物理键。</param>
    public static string ControlPath(Key key)
    {
        if (key == Key.None) return string.Empty;
        if (key >= Key.Digit1 && key <= Key.Digit0) return "<Keyboard>/" + Label(key);
        if (key == Key.LeftShift) return "<Keyboard>/shift";
        if (key == Key.LeftCtrl) return "<Keyboard>/ctrl";
        if (key == Key.LeftAlt) return "<Keyboard>/alt";
        return "<Keyboard>/" + key.ToString().ToLowerInvariant();
    }

    /// <summary>按下沿检测兼容左右两侧的 Shift、Ctrl 和 Alt。</summary>
    /// <param name="keyboard">当前键盘设备。</param>
    /// <param name="key">已经合并左右修饰键的物理键。</param>
    public static bool Pressed(Keyboard keyboard, Key key)
    {
        if (key == Key.LeftShift) return keyboard.shiftKey.wasPressedThisFrame;
        if (key == Key.LeftCtrl) return keyboard.ctrlKey.wasPressedThisFrame;
        if (key == Key.LeftAlt) return keyboard.altKey.wasPressedThisFrame;
        return keyboard[key].wasPressedThisFrame;
    }
}
