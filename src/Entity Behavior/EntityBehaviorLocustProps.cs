using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class EntityBehaviorLocustProps : EntityBehavior
    {
        float timer;
        static AssetLocation toxicgas = new AssetLocation("chaoslands:gas-h2s-1");

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            if (entity.LastCodePart() == "explosive") entity.Stats.Set("walkspeed", "h2boost", 1f, true);
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            EntityBehaviorHealth bh = entity.GetBehavior<EntityBehaviorHealth>();

            if (bh != null && entity.LastCodePart() == "fire") bh.onDamaged += FireProtection;
        }

        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();

            EntityBehaviorHealth bh = entity.GetBehavior<EntityBehaviorHealth>();

            if (bh != null && entity.LastCodePart() == "fire") bh.onDamaged += FireProtection;
        }

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);
            if (!entity.Alive) return;
            timer += deltaTime;

            if (timer >= 10)
            {
                timer = 0;
                string type = entity.LastCodePart();

                if (type == "fire")
                {
                    //if (entity.World.Rand.NextDouble() > 0.5) entity.Ignite();
                    return;
                }

                if (type == "toxic")
                {
                    Block gas = entity.World.BlockAccessor.GetBlock(toxicgas);
                    BlockPos pos = entity.SidedPos.AsBlockPos.Copy();

                    for (int y = 0; y < 7; y++)
                    {
                        Block over = entity.World.BlockAccessor.GetBlock(pos);
                        if (over.SideSolid[BlockFacing.indexUP] || over.SideSolid[BlockFacing.indexDOWN]) break;
                        if (over.BlockId == 0)
                        {
                            entity.World.BlockAccessor.SetBlock(gas.BlockId, pos);
                            break;
                        }
                        int gasBuildUp = 0;
                        if (over.FirstCodePart() == gas.FirstCodePart() && over.FirstCodePart(1) == gas.FirstCodePart(1) && (gasBuildUp = int.Parse(over.LastCodePart())) < 8)
                        {
                            Block newGas = entity.World.BlockAccessor.GetBlock(over.CodeWithVariant("level", (gasBuildUp + 1).ToString()));
                            entity.World.BlockAccessor.SetBlock(newGas.BlockId, pos);
                            break;
                        }

                        pos.Up();
                    }
                }

                if (type == "corrupt")
                {
                    Block gas = entity.World.BlockAccessor.GetBlock(entity.SidedPos.AsBlockPos);
                    string evolveInto = null;

                    if (WildcardUtil.Match(new AssetLocation("chaoslands:gas-ch4-*"), gas.Code) || WildcardUtil.Match(new AssetLocation("chaoslands:gas-h2-*"), gas.Code))
                    {
                        evolveInto = "chaoslands:locust-corrupt-explosive";
                    }
                    else if (WildcardUtil.Match(new AssetLocation("chaoslands:gas-coaldust-*"), gas.Code) || WildcardUtil.Match(new AssetLocation("chaoslands:gas-h2s-*"), gas.Code))
                    {
                        evolveInto = "chaoslands:locust-corrupt-fire";
                    }
                    else if (WildcardUtil.Match(new AssetLocation("chaoslands:gas-co-*"), gas.Code) || WildcardUtil.Match(new AssetLocation("chaoslands:gas-so2-*"), gas.Code) || WildcardUtil.Match(new AssetLocation("chaoslands:gas-no2-*"), gas.Code))
                    {
                        evolveInto = "chaoslands:locust-corrupt-toxic";
                    }

                    if (evolveInto == null) return;

                    EntityProperties evolutionType = entity.World.GetEntityType(new AssetLocation(evolveInto));
                    Entity evolution = entity.World.ClassRegistry.CreateEntity(evolutionType);

                    evolution.ServerPos.SetFrom(entity.ServerPos);
                    evolution.Pos.SetFrom(evolution.ServerPos);

                    entity.Die(EnumDespawnReason.Expire, null);
                    entity.World.SpawnEntity(evolution);
                }
            }
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            if (entity.LastCodePart() == "explosive" && damageSource.Type == EnumDamageType.Fire && entity.World.Side == EnumAppSide.Server)
            {
                entity.Die(EnumDespawnReason.Combusted, damageSource);
               (entity.World as IServerWorldAccessor).CreateExplosion(entity.SidedPos.AsBlockPos, EnumBlastType.RockBlast, 3, 3); 
            }

            base.OnEntityReceiveDamage(damageSource, damage);
        }

        public float FireProtection(float dmg, DamageSource source)
        {
            if (source.Type == EnumDamageType.Fire) return 0;

            return dmg;
        }

        public EntityBehaviorLocustProps(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "locustprops";
        }
    }
}
