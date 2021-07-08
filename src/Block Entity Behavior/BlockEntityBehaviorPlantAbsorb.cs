using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;
using Vintagestory.API;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class BlockEntityBehaviorPlantAbsorb : BlockEntityBehavior
    {
        static AssetLocation cmo = new AssetLocation("chaoslands:gas-co-*");
        static AssetLocation cdo = new AssetLocation("chaoslands:gas-co2-*");

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            Blockentity.RegisterGameTickListener(AbsorbAir, 10000);
        }

        public void AbsorbAir(float dt)
        {
            IWorldAccessor world = Blockentity.Api.World;
            BlockPos pos = Blockentity.Pos;

            if (world.Side == EnumAppSide.Client || world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.TimeOfDaySunLight) < LandsOfChaosConfig.Loaded.GasPlantAbsorptionMinLightLevel) return;

            if (Blockentity is BlockEntityPlantContainer)
            {
                if ((Blockentity as BlockEntityPlantContainer).Inventory[0].Empty) return;
            }

            BlockPos tmpPos = pos.Copy();

            for (int x = -LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius; x < LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius + 1; x++)
            {
                for (int y = -LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius; y < LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius + 1; y++)
                {
                    for (int z = -LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius; z < LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius + 1; z++)
                    {
                        tmpPos.Set(pos);
                        Block air = world.BlockAccessor.GetBlock(tmpPos.Add(x, y, z));

                        if (WildcardUtil.Match(cmo, air.Code) || WildcardUtil.Match(cdo, air.Code)) { world.BlockAccessor.SetBlock(0, tmpPos); break; }
                    }
                }
            }
        }

        public BlockEntityBehaviorPlantAbsorb(BlockEntity blockentity) : base(blockentity)
        {
        }
    }
}
