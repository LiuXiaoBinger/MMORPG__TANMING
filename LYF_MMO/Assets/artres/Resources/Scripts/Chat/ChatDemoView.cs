using System;
using UnityEngine;
using UnityEngine.UI;

namespace ChatTest.UI
{
    /// <summary>场景级测试按钮视图，按钮引用在 Inspector 中通过拖拽赋值。</summary>
    public sealed class ChatDemoView : MonoBehaviour
    {
        /// <summary>生成他人演示消息的按钮，可在 Inspector 指定。</summary>
        [SerializeField] private Button otherMessageButton;
        /// <summary>生成本地演示消息的按钮。</summary>
        [SerializeField] private Button localMessageButton;
        /// <summary>生成系统演示消息的按钮。</summary>
        [SerializeField] private Button systemMessageButton;

        /// <summary>控制器提供的他人消息操作，Unbind 时清空。</summary>
        private Action otherMessageRequested;
        /// <summary>控制器提供的本地消息操作，Unbind 时清空。</summary>
        private Action localMessageRequested;
        /// <summary>控制器提供的系统消息操作，Unbind 时清空。</summary>
        private Action systemMessageRequested;

        /// <summary>三个按钮引用是否全部存在；只报告配置状态，不负责补建按钮。</summary>
        public bool IsConfigured => otherMessageButton != null && localMessageButton != null && systemMessageButton != null;

        /// <summary>先移除本视图旧监听，再保存新操作，并为存在的按钮注册转发方法。</summary>
        /// <param name="onOtherMessage">他人消息按钮对应的操作，可为空。</param>
        /// <param name="onLocalMessage">本地消息按钮对应的操作，可为空。</param>
        /// <param name="onSystemMessage">系统消息按钮对应的操作，可为空。</param>
        public void Bind(Action onOtherMessage, Action onLocalMessage, Action onSystemMessage)
        {
            Unbind();
            otherMessageRequested = onOtherMessage;
            localMessageRequested = onLocalMessage;
            systemMessageRequested = onSystemMessage;

            if (otherMessageButton != null)
                otherMessageButton.onClick.AddListener(InvokeOtherMessage);
            if (localMessageButton != null)
                localMessageButton.onClick.AddListener(InvokeLocalMessage);
            if (systemMessageButton != null)
                systemMessageButton.onClick.AddListener(InvokeSystemMessage);
        }

        /// <summary>只移除本视图注册的三个按钮监听，并清空保存的操作。</summary>
        public void Unbind()
        {
            if (otherMessageButton != null)
                otherMessageButton.onClick.RemoveListener(InvokeOtherMessage);
            if (localMessageButton != null)
                localMessageButton.onClick.RemoveListener(InvokeLocalMessage);
            if (systemMessageButton != null)
                systemMessageButton.onClick.RemoveListener(InvokeSystemMessage);

            otherMessageRequested = null;
            localMessageRequested = null;
            systemMessageRequested = null;
        }

        /// <summary>销毁时执行同一解绑流程。</summary>
        private void OnDestroy() => Unbind();
        /// <summary>他人消息按钮点击时执行当前保存的操作。</summary>
        private void InvokeOtherMessage() => otherMessageRequested?.Invoke();
        /// <summary>本地消息按钮点击时执行当前保存的操作。</summary>
        private void InvokeLocalMessage() => localMessageRequested?.Invoke();
        /// <summary>系统消息按钮点击时执行当前保存的操作。</summary>
        private void InvokeSystemMessage() => systemMessageRequested?.Invoke();
    }
}
