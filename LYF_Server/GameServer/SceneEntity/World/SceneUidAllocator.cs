using System.Threading;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>游戏服进程内生成场景实体运行时 ID。</summary>
    public static class SceneUidAllocator
    {
        private static long _nextId;

        public static ulong Next()
        {
            long value = Interlocked.Increment(ref _nextId);
            return (ulong)value;
        }
    }
}





