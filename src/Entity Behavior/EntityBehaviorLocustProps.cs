using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;

namespace ChaosLands
{
    public class EntityBehaviorLocustProps : EntityBehavior
    {
        float timer;

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
                    if (entity.Api.Side == EnumAppSide.Server)
                    {
                        Dictionary<string, float> spewOut = new Dictionary<string, float>();
                        spewOut.Add("co", 0.1f);
                        spewOut.Add("no2", 0.1f);
                        spewOut.Add("so2", 0.1f);

                        entity.Api.Event.PushEvent("spreadGas", GasHelper.SerializeGasTreeData(entity.ServerPos.AsBlockPos, spewOut));
                    }
                }

                if (type == "corrupt" && entity.Api.Side == EnumAppSide.Server)
                {
                    Dictionary<string, float> gases = null;
                    if (gases != null && gases.Count > 0)
                    {
                        float most = 0;
                        string mostGas = null;

                        foreach (var gas in gases)
                        {
                            if (gas.Value > most)
                            {
                                most = gas.Value;
                                mostGas = gas.Key;
                            }
                        }

                        if (most < 0.25f) return;

                        string evolveInto = null;

                        if (mostGas == "ch4" || mostGas == "h2")
                        {
                            evolveInto = "chaoslands:locust-corrupt-explosive";
                        }
                        else if (mostGas == "coaldust" || mostGas == "h2s")
                        {
                            evolveInto = "chaoslands:locust-corrupt-fire";
                        }
                        else if (mostGas == "co" || mostGas == "so2" || mostGas == "no2")
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
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            if (entity.LastCodePart() == "explosive" && damageSource.Type == EnumDamageType.Fire && entity.World.Side == EnumAppSide.Server)
            {
                entity.Die(EnumDespawnReason.Combusted, damageSource);
                (entity.World as IServerWorldAccessor)?.CreateExplosion(entity.SidedPos.AsBlockPos, EnumBlastType.RockBlast, 3, 3);
            }

            base.OnEntityReceiveDamage(damageSource, ref damage);
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
