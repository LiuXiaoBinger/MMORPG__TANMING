using System;
using System.Collections.Generic;

/// <summary>当前角色在本机联系人目录里的自定义分组，与需邀请的长期群聊分别保存。</summary>
[Serializable]
public sealed class FriendContactGroups
{
    /// <summary>按玩家新建顺序保存的联系人分组。</summary>
    public List<Group> groups = new List<Group>();
    /// <summary>在联系人详情中点亮特别关注的角色号。</summary>
    public List<int> stars = new List<int>();

    /// <summary>一个联系人分组的显示名和已选角色编号。</summary>
    [Serializable]
    public sealed class Group
    {
        /// <summary>玩家在新建分组弹窗内输入的名称。</summary>
        public string name;
        /// <summary>本区角色编号，删除好友后由目录按当前好友名单过滤。</summary>
        public List<int> members = new List<int>();
    }
}
