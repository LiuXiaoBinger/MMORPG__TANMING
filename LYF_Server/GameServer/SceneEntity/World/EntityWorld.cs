using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>游戏服场景实体容器，负责注册、查询、固定帧更新和延迟销毁。</summary>
    public sealed class EntityWorld
    {
        /// <summary>按场景运行时编号保存当前世界持有的实体。</summary>
        private readonly Dictionary<ulong, ROObject> _entities = new Dictionary<ulong, ROObject>();

        /// <summary>已申请销毁的实体编号，在逻辑帧末尾统一处理。</summary>
        private readonly List<ulong> _pendingDestroy = new List<ulong>();

        /// <summary>当前登记的实体数，包含尚未在帧尾移除的对象。</summary>
        public int Count
        {
            get { return _entities.Count; }
        }

        /// <summary>遍历当前实体列表的副本，再处理本帧累计的销毁申请。</summary>
        /// <param name="deltaMilliseconds">本帧经过的毫秒数。</param>
        public void Update(int deltaMilliseconds)
        {
            List<ROObject> snapshot = new List<ROObject>(_entities.Values);
            for (int i = 0; i < snapshot.Count; i++)
            {
                snapshot[i].Update(deltaMilliseconds);
            }
            ProcessPendingDestroy();
        }

        /// <summary>登记新实体并初始化；空对象或重复编号返回 false。</summary>
        /// <param name="entity">已完成组件装配的实体。</param>
        public bool Add(ROObject entity)
        {
            if (entity == null || _entities.ContainsKey(entity.UID))
            {
                return false;
            }
            _entities.Add(entity.UID, entity);
            entity.Initialize();
            return true;
        }

        /// <summary>从世界移除实体并立即释放其组件。</summary>
        /// <param name="uid">待移除实体的运行时编号。</param>
        public bool Remove(ulong uid)
        {
            ROObject entity;
            if (!_entities.TryGetValue(uid, out entity))
            {
                return false;
            }
            _entities.Remove(uid);
            entity.Destroy();
            return true;
        }

        #region 查询与判定
        /// <summary>按运行时编号查找当前世界中的原实体。</summary>
        /// <param name="uid">场景实体编号。</param>
        /// <param name="entity">找到的原实体，失败时为 null。</param>
        public bool TryGet(ulong uid, out ROObject entity)
        {
            return _entities.TryGetValue(uid, out entity);
        }

        /// <summary>按编号查找并转换成具体实体类型，不存在或类型不符返回 null。</summary>
        /// <typeparam name="T">期望的具体场景实体类型。</typeparam>
        /// <param name="uid">场景实体编号。</param>
        public T Get<T>(ulong uid) where T : ROObject
        {
            ROObject entity;
            if (!_entities.TryGetValue(uid, out entity))
            {
                return null;
            }
            return entity as T;
        }
        #endregion

        #region 核心逻辑
        /// <summary>暂停目标实体的更新，并把编号加入帧尾销毁列表。</summary>
        /// <param name="uid">要销毁的场景实体编号。</param>
        public void MarkForDestroy(ulong uid)
        {
            ROObject entity;
            if (!_entities.TryGetValue(uid, out entity))
            {
                return;
            }
            entity.MarkPendingDestroy();
            if (!_pendingDestroy.Contains(uid))
            {
                _pendingDestroy.Add(uid);
            }
        }

        /// <summary>复制所有已登记实体的基础状态，返回独立的数据快照列表。</summary>
        public List<EntitySnapshot> BuildSnapshots()
        {
            List<ROObject> snapshot = new List<ROObject>(_entities.Values);
            List<EntitySnapshot> result = new List<EntitySnapshot>(snapshot.Count);
            for (int i = 0; i < snapshot.Count; i++)
            {
                result.Add(snapshot[i].ToSnapshot());
            }
            return result;
        }

        /// <summary>逐个释放所有实体，再清除尚未执行的销毁申请。</summary>
        public void Clear()
        {
            // 复制编号后再删除原字典中的实体，避免遍历时修改字典。
            List<ulong> ids = new List<ulong>(_entities.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                Remove(ids[i]);
            }
            _pendingDestroy.Clear();
        }

        /// <summary>取出并清空待销毁列表，然后逐一移除对应实体。</summary>
        private void ProcessPendingDestroy()
        {
            if (_pendingDestroy.Count == 0)
            {
                return;
            }
            // 本批销毁对象的副本；释放期间产生的新申请留给下一帧。
            List<ulong> pending = new List<ulong>(_pendingDestroy);
            _pendingDestroy.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                Remove(pending[i]);
            }
        }
        #endregion
    }
}
