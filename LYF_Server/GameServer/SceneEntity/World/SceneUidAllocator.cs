using System.Threading;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>游戏服进程内生成场景实体运行时 ID。</summary>
    public static class SceneUidAllocator
    {
        /// <summary>本进程已分配编号的计数，进程重启后重新开始。</summary>
        private static long _nextId;

        /// <summary>用原子递增分配非零场景编号；仅保证同一进程内不重复。</summary>
        public static ulong Next()
        {
            // 原子递增避免多个创建线程读到同一个编号。
            long value = Interlocked.Increment(ref _nextId);
            return (ulong)value;
        }
    }
}





