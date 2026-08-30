using UnityEngine;

namespace ChatTest.UI
{
    /// <summary>场景级聊天测试按钮的控制器。</summary>
    public sealed class ChatDemoControls : MonoBehaviour
    {
        [Header("MVC 引用")]
        [SerializeField] private ChatWindows chatController;
        [SerializeField] private ChatDemoView view;
        [SerializeField] private ChatDemoModel model = new ChatDemoModel();

        private void OnEnable()
        {
            if (view == null || chatController == null)
            {
                Debug.LogWarning("ChatDemoControls requires ChatController and ChatDemoView references.", this);
                return;
            }

            view.Bind(SimulateOtherMessage, SimulateLocalMessage, SimulateSystemMessage);
        }

        private void OnDisable()
        {
            if (view != null)
                view.Unbind();
        }

        public void SimulateOtherMessage()
        {
            if (chatController == null || model == null)
                return;

            chatController.ReceiveMessage(
                "OtherPlayer",
                model.NextOtherMessage(),
                chatController.CurrentChannel);
        }

        public void SimulateLocalMessage()
        {
            if (chatController == null || model == null)
                return;

            chatController.SendLocalMessage(model.NextLocalMessage());
        }

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
