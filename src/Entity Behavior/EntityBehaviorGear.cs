using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API;
using System;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class EntityBehaviorGear : EntityBehavior
    {
        GearBag[] gearBags;
        float totalChance = 0f;

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            gearBags = attributes["gearBags"].AsObject<GearBag[]>();
            if (gearBags == null || gearBags.Length == 0) return;

            for (int i = 0; i < gearBags.Length; i++) { totalChance += gearBags[i].Init(entity.World); }

        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            if (gearBags == null || gearBags.Length == 0 || totalChance == 0f) return;        

            gearBags.Shuffle(entity.World.Rand);
            double choice = entity.World.Rand.NextDouble() * totalChance;

            for (int i = 0; i < gearBags.Length; i++)
            {
                choice -= gearBags[i].Chance;
                if (choice <= 0)
                {
                    gearBags[i].GiveGear(entity as EntityAgent);
                    return;
                }
            }
        }

        public EntityBehaviorGear(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "gear";
        }
    }

    public class GearBag
    {
        public float Chance = 1f;
        public JsonItemStack[] Gear = new JsonItemStack[0];

        public float Init(IWorldAccessor world)
        {
            for (int i = 0; i < Gear.Length; i++)
            {
               Gear[i].Resolve(world, "gear up behavior");
            }

            return Chance;
        }

        public void GiveGear(EntityAgent entity)
        {
            for (int i = 0; i < Math.Min(Gear.Length, entity.GearInventory.Count); i++)
            {
                if (Gear[i].ResolvedItemstack == null) continue;

                entity.TryGiveItemStack(Gear[i].ResolvedItemstack.Clone());
            }
        }
    }
}
