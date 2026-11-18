using UnityEditor;
using UnityEngine;

/// <summary>在编辑器运行时给新版聊天添加一条可识别的本地测试消息。</summary>
public sealed class YimengChatTestWindow : EditorWindow
{
    /// <summary>从 Unity Tools 菜单打开新版聊天的专用测试窗口。</summary>
    [MenuItem("Tools/Chat/新版聊天测试")]
    private static void OpenWindow()
    {
        GetWindow<YimengChatTestWindow>("新版聊天测试");
    }

    /// <summary>仅在 Play Mode 且新版聊天已经打开时启用模拟按钮。</summary>
    private void OnGUI()
    {
        // 共享聊天记录仍由当前主界面的原聊天窗口管理。
        ChatWindows chat = FindOpenChat();
        using (new EditorGUI.DisabledScope(chat == null))
        {
            if (GUILayout.Button("模拟他人消息")) SimulateOtherMessage(chat);
        }
        if (chat == null)
            EditorGUILayout.HelpBox("请进入 Play Mode 并打开新版聊天。测试消息可在「常用」或「世界」查看。", MessageType.Info);
    }

    /// <summary>读取当前主界面已打开的新版聊天对应的原聊天记录入口。</summary>
    private static ChatWindows FindOpenChat()
    {
        if (!EditorApplication.isPlaying || UIRoot.Instance == null) return null;
        // 主界面同时持有新版聊天和提供历史记录的旧聊天窗口。
        MainView mainView = UIRoot.Instance.GetComponentInChildren<MainView>(true);
        if (mainView == null || mainView.YimengChat == null) return null;
        if (!mainView.YimengChat.isActiveAndEnabled) return null;
        return mainView.ChatWindow;
    }

    /// <summary>将一条标记为测试玩家的世界发言添加到本机会话历史。</summary>
    /// <param name="chat">当前主界面用于接收和展示消息的共享记录入口。</param>
    private static void SimulateOtherMessage(ChatWindows chat)
    {
        chat.ReceiveMessage("【测试】江湖来客", "这是一条从 Unity 编辑器添加的本地测试消息。", ChatChannel.World);
    }
}
