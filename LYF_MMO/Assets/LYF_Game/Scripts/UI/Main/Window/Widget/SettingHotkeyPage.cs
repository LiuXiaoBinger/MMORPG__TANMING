using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>设置窗口的热键页，管理键盘图、三列改键草稿及保存和放弃操作。</summary>
public sealed class SettingHotkeyPage : MonoBehaviour
{
    /// <summary>键盘图上一枚物理键帽的固定引用。</summary>
    [Serializable]
    private struct Keycap
    {
        /// <summary>对应的物理键，左右修饰键共用同一绑定。</summary>
        public Key key;
        /// <summary>显示键名和动作的小字。</summary>
        public TMP_Text text;
        /// <summary>按动作类别着色的键底。</summary>
        public Image face;
    }

    /// <summary>覆盖页签下方内容的三列编辑器。</summary>
    [SerializeField] private GameObject _editor;
    /// <summary>总览页底部的默认和自定义入口。</summary>
    [SerializeField] private Button _defaults, _customize;
    /// <summary>编辑器底部的放弃和保存按钮。</summary>
    [SerializeField] private Button _discard, _save;
    /// <summary>动作行右侧的按键按钮，顺序与热键目录一致。</summary>
    [SerializeField] private Button[] _buttons;
    /// <summary>动作行当前的绑定或不可用说明。</summary>
    [SerializeField] private TMP_Text[] _values;
    /// <summary>编辑时提示正在等待哪个键及冲突原因。</summary>
    [SerializeField] private TMP_Text _feedback;
    /// <summary>三列各自的滚动视口，重新进入时回到顶部。</summary>
    [SerializeField] private ScrollRect[] _columns;
    /// <summary>键盘图可显示绑定的物理键帽。</summary>
    [SerializeField] private Keycap[] _caps;
    /// <summary>本次编辑中的物理键；放弃或关闭时舍弃。</summary>
    private Key[] _draft;
    /// <summary>等待键盘输入的行号，负一表示未选中。</summary>
    private int _listening = -1;

    /// <summary>在首次打开该页时连接预制体控件。</summary>
    private void Awake()
    {
        _draft = new Key[GameHotkeys.Rows.Length];
        _customize.onClick.AddListener(OpenEditor);
        _defaults.onClick.AddListener(RestoreDefaults);
        _discard.onClick.AddListener(CloseEditor);
        _save.onClick.AddListener(Save);
        // 每行各自捕获索引，禁用条目直接使用不可按的按钮状态。
        for (int index = 0; index < _buttons.Length; index++)
        {
            // 点击回调所属的固定目录下标。
            int row = index;
            _buttons[index].interactable = GameHotkeys.Rows[index].Type != GameHotkeys.Kind.Unavailable;
            _buttons[index].onClick.AddListener(() => Listen(row));
        }
    }

    /// <summary>每次进入热键页时显示已保存的键盘布局。</summary>
    private void OnEnable() => RefreshKeyboard();

    /// <summary>关闭设置或切换页签时放弃未保存输入并交还键盘。</summary>
    private void OnDisable() => CloseEditor();

    /// <summary>将已生效绑定拷入独立草稿并打开三列编辑面板。</summary>
    private void OpenEditor()
    {
        for (int index = 0; index < _draft.Length; index++) _draft[index] = GameHotkeys.Rows[index].Bound;
        _editor.SetActive(true);
        GameHotkeys.Editing = true;
        _feedback.text = "选择要更改的按键";
        RefreshRows();
        Canvas.ForceUpdateCanvases();
        // 三列的滚动位置独立，进入时都显示首项。
        for (int column = 0; column < _columns.Length; column++)
        {
            _columns[column].StopMovement();
            _columns[column].verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>结束录入并收起编辑面板，未提交的草稿不会写入本地。</summary>
    private void CloseEditor()
    {
        _listening = -1;
        GameHotkeys.Editing = false;
        _editor.SetActive(false);
    }

    /// <summary>恢复本项目默认键位并更新总览。</summary>
    private void RestoreDefaults()
    {
        // 默认键取自同一份目录，包含尚未开放的参考行。
        for (int index = 0; index < _draft.Length; index++) _draft[index] = GameHotkeys.Rows[index].Default;
        Save();
    }

    /// <summary>保存草稿，返回键盘总览并显示新动作位置。</summary>
    private void Save()
    {
        GameHotkeys.Save(_draft);
        CloseEditor();
        RefreshKeyboard();
    }

    /// <summary>选中一行动作，随后一个有效的按下沿写入草稿。</summary>
    /// <param name="row">可用动作在目录中的下标。</param>
    private void Listen(int row)
    {
        _listening = row;
        RefreshRows();
        _values[row].text = "请按键";
        _feedback.text = "正在设置「" + GameHotkeys.Rows[row].Name + "」 · Esc 取消";
    }

    /// <summary>从实际设备采集键盘图支持的按键，Esc 退出当前录入。</summary>
    private void Update()
    {
        if (_listening < 0 || Keyboard.current == null) return;
        // 只读取本次录入所用的当前键盘。
        Keyboard keyboard = Keyboard.current;
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            _listening = -1;
            RefreshRows();
            _feedback.text = "已取消本次按键录入";
            return;
        }
        // 键盘图即允许录入的物理键集合，操作系统键及聊天回车不在集合内。
        for (int index = 0; index < _caps.Length; index++)
        {
            // 两侧修饰键映射到共用的物理键码。
            Key key = _caps[index].key;
            if (key == Key.Escape || !GameHotkeys.Pressed(keyboard, key)) continue;
            Bind(key);
            return;
        }
        if (keyboard.anyKey.wasPressedThisFrame) _feedback.text = "此键不可绑定，请选择键盘图中的其他按键";
    }

    /// <summary>检查实际占用后更改草稿，冲突时保留原键并继续等待。</summary>
    /// <param name="key">本次按下的物理键。</param>
    private void Bind(Key key)
    {
        // 未开放的参考键位不占用按键。
        for (int index = 0; index < _draft.Length; index++)
        {
            if (index == _listening || GameHotkeys.Rows[index].Type == GameHotkeys.Kind.Unavailable) continue;
            if (_draft[index] != key) continue;
            _feedback.text = GameHotkeys.Label(key) + " 已用于「" + GameHotkeys.Rows[index].Name + "」，请选择其他按键";
            return;
        }
        _draft[_listening] = key;
        _feedback.text = "「" + GameHotkeys.Rows[_listening].Name + "」已设为 " + GameHotkeys.Label(key);
        _listening = -1;
        RefreshRows();
    }

    /// <summary>从草稿刷新三列；未接入的动作始终标注参考键与不可用。</summary>
    private void RefreshRows()
    {
        for (int index = 0; index < _values.Length; index++)
        {
            // 当前行是否存在可执行的项目入口决定其提示。
            GameHotkeys.Entry row = GameHotkeys.Rows[index];
            _values[index].text = GameHotkeys.Label(_draft[index]);
            if (row.Type == GameHotkeys.Kind.Unavailable) _values[index].text += " · 不可用";
        }
    }

    /// <summary>按保存后的布局显示物理键和动作；战斗、移动及系统使用不同底色。</summary>
    private void RefreshKeyboard()
    {
        for (int capIndex = 0; capIndex < _caps.Length; capIndex++)
        {
            // 每个键帽有自己的图像和文字，先移除上次绑定的显示。
            Keycap cap = _caps[capIndex];
            cap.text.text = GameHotkeys.Label(cap.key);
            cap.face.color = new Color32(211, 225, 238, 255);
            // 当前键帽只标注真正能触发的一个动作。
            for (int rowIndex = 0; rowIndex < GameHotkeys.Rows.Length; rowIndex++)
            {
                // 已应用的动作决定类别色及键帽第二行。
                GameHotkeys.Entry row = GameHotkeys.Rows[rowIndex];
                if (row.Type == GameHotkeys.Kind.Unavailable || row.Bound != cap.key) continue;
                cap.text.text = "<size=75%>" + GameHotkeys.Label(cap.key) + "</size>\n" + row.Name;
                cap.face.color = new Color32(165, 197, 232, 255);
                if (row.Type == GameHotkeys.Kind.Motion) cap.face.color = new Color32(226, 211, 166, 255);
                if (row.Type == GameHotkeys.Kind.Window) cap.face.color = new Color32(165, 210, 200, 255);
                break;
            }
        }
    }
}
