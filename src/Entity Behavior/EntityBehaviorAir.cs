using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API;
using Vintagestory.API.Common.Entities;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class EntityBehaviorAir : EntityBehavior
    {
        ITreeAttribute airTree;

        public bool waterBreather = false;
        float damageOn = 0;
        double timer;
        static AssetLocation exhalegas = new AssetLocation("chaoslands:gas-co2-1");

        public float Air
        {
            get { return airTree.GetFloat("currentair"); }
            set { airTree.SetFloat("currentair", GameMath.Clamp(value, 0, MaxAir)); entity.WatchedAttributes.MarkPathDirty("air"); }
        }

        public float MaxAir
        {
            get { return airTree.GetFloat("maxair"); }
            set { airTree.SetFloat("maxair", value); entity.WatchedAttributes.MarkPathDirty("air"); }
        }

        public float BaseMaxAir
        {
            get { return airTree.GetFloat("basemaxair"); }
            set
            {
                airTree.SetFloat("basemaxair", value);
                entity.WatchedAttributes.MarkPathDirty("air");
            }
        }

        public Dictionary<string, float> MaxAirModifiers = new Dictionary<string, float>();

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            timer = entity.World.Calendar.TotalHours;
            waterBreather = typeAttributes.IsTrue("waterBreather");
            airTree = entity.WatchedAttributes.GetTreeAttribute("air");

            if (airTree == null)
            {
                entity.WatchedAttributes.SetAttribute("air", airTree = new TreeAttribute());

                Air = typeAttributes["currentair"].AsFloat(20);
                BaseMaxAir = typeAttributes["maxair"].AsFloat(20);
                UpdateMaxAir();
                return;
            }

            Air = airTree.GetFloat("currentair");
            BaseMaxAir = airTree.GetFloat("basemaxair");

            if (BaseMaxAir == 0) BaseMaxAir = typeAttributes["maxair"].AsFloat(20);


            UpdateMaxAir();
        }

        public void UpdateMaxAir()
        {
            float totalMaxAir = BaseMaxAir;
            foreach (var val in MaxAirModifiers) totalMaxAir += val.Value;

            totalMaxAir += entity.Stats.GetBlended("maxairExtraPoints") - 1;

            bool wasFullAir = Air >= MaxAir;

            MaxAir = totalMaxAir;

            if (wasFullAir) Air = MaxAir;
        }

        public override void OnGameTick(float deltaTime)
        {
            //base.OnGameTick(deltaTime);
            if (!entity.Alive) return;

            if (LandsOfChaosConfig.Loaded.GasesEnabled && entity.World.Calendar.TotalHours - timer >= 1)
            {
                timer = entity.World.Calendar.TotalHours;
                bool plantExchange = false;
                BlockPos pos = entity.SidedPos.AsBlockPos.Copy();


                foreach (BlockFacing face in BlockFacing.ALLFACES)
                {
                    pos.Set(entity.SidedPos.AsBlockPos);
                    if (entity.World.BlockAccessor.GetBlock(pos.Add(face)).HasBehavior<BlockBehaviorPlantAbsorb>())
                    {
                        plantExchange = true;
                        break;
                    }
                }

                if (!plantExchange)
                {
                    Block gas = entity.World.BlockAccessor.GetBlock(exhalegas);


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
            }

            if (EntityUnderwater())
            {
                //In water
                if (waterBreather)
                {
                    if (Air < MaxAir) { Air += deltaTime * entity.Stats.GetBlended("airRecovery"); }
                }
                else
                {
                    if (Air > 0) Air -= deltaTime * entity.Stats.GetBlended("airLoss");
                }
            }
            else
            {
                //On land
                if (waterBreather)
                {
                    if (Air > 0) Air -= deltaTime * entity.Stats.GetBlended("airLoss");
                }
                else
                {
                    Air += AirQuality(deltaTime);
                }
            }



            if (Air <= 0)
            {
                damageOn += deltaTime;

                if (damageOn >= 1)
                {
                    entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Suffocation, Source = EnumDamageSource.Drown }, 1f);
                    damageOn = 0;
                }
            }
        }

        public bool EntityUnderwater()
        {
            if (!entity.Swimming) return false;

            Vec3d head = entity.SidedPos.XYZ.AddCopy(0, entity.CollisionBox.Height, 0);
            Block liquid = entity.World.BlockAccessor.GetBlock(head.AsBlockPos);

            return liquid.IsLiquid() && (entity.World.BlockAccessor.GetBlock(head.AsBlockPos.Add(0, 1, 0)).IsLiquid() || head.Y - (0.25 * entity.CollisionBox.Height) < head.AsBlockPos.Y + ((liquid.LiquidLevel + 1) / 8));
        }

        public float AirQuality(float air)
        {
            Block gas = entity.World.GetBlock((entity as EntityAgent).GetEyesBlockId());
            if (gas is INonBreathable)
            {
                float conc = (gas as INonBreathable).Concentration;
                
                if (conc > 0) return conc * entity.Stats.GetBlended("airRecovery") * air;
                return conc * entity.Stats.GetBlended("airLoss") * air;
            }

            bool solid = true;

            foreach (bool face in gas.SideSolid)
            {
                solid &= face;
            }

            if (solid) return -1f * entity.Stats.GetBlended("airLoss") * air;

            return air * entity.Stats.GetBlended("airRecovery");
        }

        public EntityBehaviorAir(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "air";
        }
    }
}
