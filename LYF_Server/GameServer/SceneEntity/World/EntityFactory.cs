using System;
using MMoRpgCommon.SceneEntity;

namespace GameServer.SceneEntity
{
    /// <summary>创建具体场景对象并按类别装配组件；对象加入 EntityWorld 时才初始化。</summary>
    public static class EntityFactory
    {
        /// <summary>分配运行时编号、创建对象并装配组件；不支持的类别返回 null。</summary>
        /// <param name="type">决定具体对象与组件组合的类别位。</param>
        /// <param name="configId">对象使用的配置编号。</param>
        public static ROObject Create(EntityType type, int configId)
        {
            // 此编号只标识本进程中的场景对象，不替代角色数据库编号。
            ulong uid = SceneUidAllocator.Next();
            // 根据下面的类别优先级选出一种具体对象，再统一挂组件。
            ROObject entity = CreateObject(type, uid, configId);
            if (entity == null)
            {
                return null;
            }
            Assemble(entity);
            return entity;
        }

        /// <summary>按固定优先级选择具体对象；组合类别命中较前分支后立即返回。</summary>
        /// <param name="type">待识别的类别位组合。</param>
        /// <param name="uid">已分配的场景编号。</param>
        /// <param name="configId">对象配置编号。</param>
        private static ROObject CreateObject(EntityType type, ulong uid, int configId)
        {
            if (HasType(type, EntityType.Player))
            {
                return new PlayerEntity(uid, configId);
            }
            if (HasType(type, EntityType.Role))
            {
                return new Role(uid, EntityType.Role, configId);
            }
            if (HasType(type, EntityType.Boss))
            {
                return new BossEntity(uid, configId);
            }
            if (HasType(type, EntityType.Elite))
            {
                return new EliteEntity(uid, configId);
            }
            if (HasType(type, EntityType.Monster))
            {
                return new MonsterEntity(uid, EntityType.Monster, configId);
            }
            if (HasType(type, EntityType.Npc))
            {
                return new NpcEntity(uid, configId);
            }
            if (HasType(type, EntityType.Dummy))
            {
                return new DummyEntity(uid, configId);
            }
            if (HasType(type, EntityType.Trap))
            {
                return new TrapEntity(uid, configId);
            }
            if (HasType(type, EntityType.Pet))
            {
                return new PetEntity(uid, configId);
            }
            if (HasType(type, EntityType.Eagle))
            {
                return new EagleEntity(uid, configId);
            }
            if (HasType(type, EntityType.Collection))
            {
                return new CollectionEntity(uid, configId);
            }
            if (HasType(type, EntityType.MirrorRole))
            {
                return new MirrorRoleEntity(uid, configId);
            }
            if (HasType(type, EntityType.Mercenary))
            {
                return new MercenaryEntity(uid, configId);
            }
            if (HasType(type, EntityType.Projectile))
            {
                return new ProjectileEntity(uid, configId);
            }
            return null;
        }

        /// <summary>为不同实体挂上所需组件标记；NPC、采集物、陷阱和飞行物采用独立组合。</summary>
        /// <param name="entity">已创建但尚未初始化的对象。</param>
        private static void Assemble(ROObject entity)
        {
            // 使用对象构造后实际保存的类别决定组件组合。
            EntityType type = entity.EntityType;
            if (HasType(type, EntityType.Npc))
            {
                Add(entity, EntityComponentType.Npc);
                Add(entity, EntityComponentType.Scene);
                Add(entity, EntityComponentType.View);
                return;
            }
            if (HasType(type, EntityType.Collection))
            {
                Add(entity, EntityComponentType.Entity);
                Add(entity, EntityComponentType.Scene);
                Add(entity, EntityComponentType.Collection);
                Add(entity, EntityComponentType.Sync);
                Add(entity, EntityComponentType.View);
                Add(entity, EntityComponentType.FightGroup);
                return;
            }
            if (HasType(type, EntityType.Trap))
            {
                Add(entity, EntityComponentType.Entity);
                Add(entity, EntityComponentType.Scene);
                Add(entity, EntityComponentType.Trap);
                Add(entity, EntityComponentType.Sync);
                Add(entity, EntityComponentType.View);
                Add(entity, EntityComponentType.FightGroup);
                return;
            }
            if (HasType(type, EntityType.Projectile))
            {
                Add(entity, EntityComponentType.Entity);
                Add(entity, EntityComponentType.Scene);
                Add(entity, EntityComponentType.Projectile);
                Add(entity, EntityComponentType.Move);
                Add(entity, EntityComponentType.Sync);
                Add(entity, EntityComponentType.Lifetime);
                return;
            }

            AddCommon(entity);
            if (HasType(type, EntityType.Role) || HasType(type, EntityType.Player))
            {
                Add(entity, EntityComponentType.Role);
                Add(entity, EntityComponentType.Skill);
                Add(entity, EntityComponentType.View);
                return;
            }
            if (HasType(type, EntityType.MirrorRole))
            {
                Add(entity, EntityComponentType.MirrorRole);
                Add(entity, EntityComponentType.Skill);
                Add(entity, EntityComponentType.AI);
                Add(entity, EntityComponentType.Navigation);
                Add(entity, EntityComponentType.View);
                return;
            }
            if (HasType(type, EntityType.Mercenary))
            {
                Add(entity, EntityComponentType.Monster);
                Add(entity, EntityComponentType.Mercenary);
                Add(entity, EntityComponentType.Summon);
                Add(entity, EntityComponentType.Skill);
                Add(entity, EntityComponentType.Hatred);
                Add(entity, EntityComponentType.View);
                return;
            }
            Add(entity, EntityComponentType.Monster);
            Add(entity, EntityComponentType.Skill);
            Add(entity, EntityComponentType.AI);
            Add(entity, EntityComponentType.Navigation);
            Add(entity, EntityComponentType.View);
            Add(entity, EntityComponentType.Lifetime);
            if (HasType(type, EntityType.Monster) || HasType(type, EntityType.Elite) || HasType(type, EntityType.Boss))
            {
                Add(entity, EntityComponentType.Hatred);
            }
            if (HasType(type, EntityType.Dummy))
            {
                Add(entity, EntityComponentType.Dummy);
                Add(entity, EntityComponentType.Summon);
            }
            if (HasType(type, EntityType.Pet))
            {
                Add(entity, EntityComponentType.Pet);
                Add(entity, EntityComponentType.Summon);
            }
            if (HasType(type, EntityType.Eagle))
            {
                Add(entity, EntityComponentType.Eagle);
                Add(entity, EntityComponentType.Summon);
            }
            if (HasType(type, EntityType.Elite))
            {
                Add(entity, EntityComponentType.Elite);
            }
            if (HasType(type, EntityType.Boss))
            {
                Add(entity, EntityComponentType.Boss);
            }
        }

        /// <summary>登记可战斗实体共用的属性、状态、移动、同步等组件。</summary>
        /// <param name="entity">接受公共组件的实体。</param>
        private static void AddCommon(ROObject entity)
        {
            Add(entity, EntityComponentType.Entity);
            Add(entity, EntityComponentType.Scene);
            Add(entity, EntityComponentType.Attributes);
            Add(entity, EntityComponentType.Gene);
            Add(entity, EntityComponentType.FightGroup);
            Add(entity, EntityComponentType.Move);
            Add(entity, EntityComponentType.BeHit);
            Add(entity, EntityComponentType.Dead);
            Add(entity, EntityComponentType.Sync);
            Add(entity, EntityComponentType.Combat);
        }

        /// <summary>创建一个指定类别的组件并交由实体持有。</summary>
        /// <param name="entity">组件所属对象。</param>
        /// <param name="type">要挂载的组件类别。</param>
        private static void Add(ROObject entity, EntityComponentType type)
        {
            entity.AddComponent(CreateComponent(entity, type));
        }

        /// <summary>判断组合实体类别是否包含指定的单个类别位。</summary>
        /// <param name="value">待检查的实体类别组合值。</param>
        /// <param name="flag">要匹配的实体类别位。</param>
        /// <returns>组合值包含指定类别位时返回 true，否则返回 false。</returns>
        private static bool HasType(EntityType value, EntityType flag)
        {
            return (value & flag) == flag;
        }

        /// <summary>把组件类别转换成具体类；尚无专用类的类别使用轻量标记。</summary>
        /// <param name="entity">组件所属实体。</param>
        /// <param name="type">要创建的组件类别。</param>
        private static IEntityComponent CreateComponent(ROObject entity, EntityComponentType type)
        {
            switch (type)
            {
                case EntityComponentType.Entity: return new EntityComponent(entity);
                case EntityComponentType.Scene: return new SceneComponent(entity);
                case EntityComponentType.Attributes: return new AttributesComponent(entity);
                case EntityComponentType.Gene: return new GeneComponent(entity);
                case EntityComponentType.FightGroup: return new FightGroupComponent(entity);
                case EntityComponentType.Move: return new MoveComponent(entity);
                case EntityComponentType.BeHit: return new BeHitComponent(entity);
                case EntityComponentType.Dead: return new DeadComponent(entity);
                case EntityComponentType.Sync: return new SyncComponent(entity);
                case EntityComponentType.Combat: return new CombatComponent(entity);
                case EntityComponentType.Role: return new RoleComponent(entity);
                case EntityComponentType.Monster: return new MonsterComponent(entity);
                case EntityComponentType.Npc: return new NpcComponent(entity);
                case EntityComponentType.Collection: return new CollectionComponent(entity);
                case EntityComponentType.MirrorRole: return new MirrorRoleComponent(entity);
                case EntityComponentType.Mercenary: return new MercenaryComponent(entity);
                case EntityComponentType.Projectile: return new ProjectileComponent(entity);
                case EntityComponentType.Skill: return new SkillComponent(entity);
                case EntityComponentType.AI: return new AiComponent(entity);
                case EntityComponentType.Navigation: return new NavigationComponent(entity);
                case EntityComponentType.Hatred: return new HatredComponent(entity);
                case EntityComponentType.Summon: return new SummonComponent(entity);
                case EntityComponentType.View: return new ViewComponent(entity);
                case EntityComponentType.Lifetime: return new LifetimeComponent(entity);
                case EntityComponentType.Trap: return new TrapComponent(entity);
                case EntityComponentType.Elite: return new RuntimeEntityComponent(entity, type);
                case EntityComponentType.Boss: return new RuntimeEntityComponent(entity, type);
                case EntityComponentType.Pet: return new RuntimeEntityComponent(entity, type);
                case EntityComponentType.Eagle: return new RuntimeEntityComponent(entity, type);
                case EntityComponentType.Dummy: return new RuntimeEntityComponent(entity, type);
                default: return new RuntimeEntityComponent(entity, type);
            }
        }
    }
}








