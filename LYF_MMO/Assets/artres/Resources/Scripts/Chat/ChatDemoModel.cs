using System;
using UnityEngine;

namespace ChatTest.UI
{
    /// <summary>测试控制器使用的数据模型，不依赖 Unity UI。</summary>
    [Serializable]
    public sealed class ChatDemoModel
    {
        /// <summary>已生成的他人演示消息编号，下一次调用先加 1。</summary>
        [SerializeField] private int otherMessageNumber;
        /// <summary>已生成的本地演示消息编号，与其他两类分开累加。</summary>
        [SerializeField] private int localMessageNumber;
        /// <summary>已生成的系统提示编号，不因切换频道而重置。</summary>
        [SerializeField] private int systemMessageNumber;

        /// <summary>递增他人消息编号，返回带编号的演示文本。</summary>
        public string NextOtherMessage()
        {
            otherMessageNumber++;
            return "收到他人的测试消息 #" + otherMessageNumber;
        }

        /// <summary>递增本地消息编号，返回带编号的演示文本。</summary>
        public string NextLocalMessage()
        {
            localMessageNumber++;
            return "我的测试消息 #" + localMessageNumber;
        }

        /// <summary>递增系统提示编号，返回带编号的演示文本。</summary>
        public string NextSystemMessage()
        {
            systemMessageNumber++;
            return "系统测试提示 #" + systemMessageNumber;
        }
    }
}
