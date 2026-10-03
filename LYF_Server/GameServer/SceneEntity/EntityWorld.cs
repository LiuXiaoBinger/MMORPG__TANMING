using System;
using System.Collections.Generic;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>游戏服场景实体容器，负责注册、查询、固定帧更新和延迟销毁。</summary>
    public sealed class EntityWorld
    {
        private readonly Dictionary<ulong, ROObject> _entities = new Dictionary<ulong, ROObject>();
        private readonly List<ulong> _pendingDestroy = new List<ulong>();

        public int Count
        {
            get { return _entities.Count; }
        }

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

        public bool TryGet(ulong uid, out ROObject entity)
        {
            return _entities.TryGetValue(uid, out entity);
        }

        public T Get<T>(ulong uid) where T : ROObject
        {
            ROObject entity;
            if (!_entities.TryGetValue(uid, out entity))
            {
                return null;
            }
            return entity as T;
        }

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

        public void Update(int deltaMilliseconds)
        {
            List<ROObject> snapshot = new List<ROObject>(_entities.Values);
            for (int i = 0; i < snapshot.Count; i++)
            {
                snapshot[i].Update(deltaMilliseconds);
            }
            ProcessPendingDestroy();
        }

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

        public void Clear()
        {
            List<ulong> ids = new List<ulong>(_entities.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                Remove(ids[i]);
            }
            _pendingDestroy.Clear();
        }

        private void ProcessPendingDestroy()
        {
            if (_pendingDestroy.Count == 0)
            {
                return;
            }
            List<ulong> pending = new List<ulong>(_pendingDestroy);
            _pendingDestroy.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                Remove(pending[i]);
            }
        }
    }
}








