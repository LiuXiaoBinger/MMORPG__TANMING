using System.Threading;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>生成游戏服场景实体运行时 ID，避开玩家持久角色编号使用的 uint 区间。</summary>
    public static class SceneUidAllocator
    {
        /// <summary>运行时序列从 uint.MaxValue 之后分配，防止 AI 将玩家 RoleId 误认成同号怪物；进程重启后重新开始。</summary>
        private static long _nextId = uint.MaxValue;

        /// <summary>原子分配大于 uint.MaxValue 的运行时编号；仅在本进程内唯一，不替代跨端怪物 SpawnId。</summary>
        public static ulong Next()
        {
            // 原子递增避免多个创建线程读到同一个编号。
            long value = Interlocked.Increment(ref _nextId);
            return (ulong)value;
        }
    }
}





