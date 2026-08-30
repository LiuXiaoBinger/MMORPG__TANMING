using System;
using UnityEngine;

namespace ChatTest.UI
{
    /// <summary>测试控制器使用的数据模型，不依赖 Unity UI。</summary>
    [Serializable]
    public sealed class ChatDemoModel
    {
        [SerializeField] private int otherMessageNumber;
        [SerializeField] private int localMessageNumber;
        [SerializeField] private int systemMessageNumber;

        public string NextOtherMessage()
        {
            otherMessageNumber++;
            return "收到他人的测试消息 #" + otherMessageNumber;
        }

        public string NextLocalMessage()
        {
            localMessageNumber++;
            return "我的测试消息 #" + localMessageNumber;
        }

        public string NextSystemMessage()
        {
            systemMessageNumber++;
            return "系统测试提示 #" + systemMessageNumber;
        }
    }
}
