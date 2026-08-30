using System;
using UnityEngine;
using UnityEngine.UI;

namespace ChatTest.UI
{
    /// <summary>场景级测试按钮视图，按钮引用在 Inspector 中通过拖拽赋值。</summary>
    public sealed class ChatDemoView : MonoBehaviour
    {
        [SerializeField] private Button otherMessageButton;
        [SerializeField] private Button localMessageButton;
        [SerializeField] private Button systemMessageButton;

        private Action otherMessageRequested;
        private Action localMessageRequested;
        private Action systemMessageRequested;

        public bool IsConfigured => otherMessageButton != null && localMessageButton != null && systemMessageButton != null;

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

        private void OnDestroy() => Unbind();
        private void InvokeOtherMessage() => otherMessageRequested?.Invoke();
        private void InvokeLocalMessage() => localMessageRequested?.Invoke();
        private void InvokeSystemMessage() => systemMessageRequested?.Invoke();
    }
}
