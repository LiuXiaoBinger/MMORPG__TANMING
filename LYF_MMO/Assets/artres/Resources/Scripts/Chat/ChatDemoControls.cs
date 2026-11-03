using UnityEngine;

namespace ChatTest.UI
{
    /// <summary>连接聊天演示按钮、编号数据和 ChatWindows；按钮只生成本地演示消息，不在这里发送网络协议。</summary>
    public sealed class ChatDemoControls : MonoBehaviour
    {
        /// <summary>接收演示消息并更新聊天列表的窗口控制器。</summary>
        [Header("MVC 引用")]
        [SerializeField] private ChatWindows chatController;
        /// <summary>持有他人、本地、系统三个演示按钮的视图。</summary>
        [SerializeField] private ChatDemoView view;
        /// <summary>分别累加三类演示消息的编号，组件停用时不重置。</summary>
        [SerializeField] private ChatDemoModel model = new ChatDemoModel();

        /// <summary>窗口和按钮视图都存在时，为三类按钮绑定模拟消息操作。</summary>
        private void OnEnable()
        {
            if (view == null || chatController == null)
            {
                Debug.LogWarning("ChatDemoControls requires ChatController and ChatDemoView references.", this);
                return;
            }

            view.Bind(SimulateOtherMessage, SimulateLocalMessage, SimulateSystemMessage);
        }

        /// <summary>停用时解除演示按钮回调，保留消息编号与聊天历史。</summary>
        private void OnDisable()
        {
            if (view != null)
                view.Unbind();
        }

        /// <summary>以 OtherPlayer 为发送者，把下一条编号消息加入当前频道。</summary>
        public void SimulateOtherMessage()
        {
            if (chatController == null || model == null)
                return;

            chatController.ReceiveMessage(
                "OtherPlayer",
                model.NextOtherMessage(),
                chatController.CurrentChannel);
        }

        /// <summary>通过窗口的本地发送入口加入下一条演示消息，并触发其 MessageSent 事件。</summary>
        public void SimulateLocalMessage()
        {
            if (chatController == null || model == null)
                return;

            chatController.SendLocalMessage(model.NextLocalMessage());
        }

        /// <summary>先切到系统频道，再加入下一条系统演示消息。</summary>
        public void SimulateSystemMessage()
        {
            if (chatController == null || model == null)
                return;

            // 切到系统频道，点击测试按钮后可以立即看到系统提示。
            chatController.SelectChannel(ChatChannel.System);
            chatController.AddSystemMessage(model.NextSystemMessage());
        }
    }
}
