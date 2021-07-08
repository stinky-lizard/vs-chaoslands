using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class BlockBehaviorPlantAbsorb : BlockBehavior
    {
        static AssetLocation cmo = new AssetLocation("chaoslands:gas-co-*");
        static AssetLocation cdo = new AssetLocation("chaoslands:gas-co2-*");

        public void AbsorbBadAir(BlockPos pos, IWorldAccessor world)
        {
            if (world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.TimeOfDaySunLight) < LandsOfChaosConfig.Loaded.GasPlantAbsorptionMinLightLevel) return;

            BlockPos tmpPos = pos.Copy();

            for (int x = -LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius; x < LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius + 1; x++)
            {
                for (int y = -LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius; y < LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius + 1; y++)
                {
                    for (int z = -LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius; z < LandsOfChaosConfig.Loaded.GasPlantAbsorptionRadius + 1; z++)
                    {
                        tmpPos.Set(pos);
                        Block air = world.BlockAccessor.GetBlock(tmpPos.Add(x,y,z));

                        if (WildcardUtil.Match(cmo, air.Code) || WildcardUtil.Match(cdo, air.Code)) world.BlockAccessor.SetBlock(0, tmpPos);
                    }
                }
            }
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
        {
            AbsorbBadAir(blockPos, world);
            base.OnBlockPlaced(world, blockPos, ref handling);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            AbsorbBadAir(pos, world);
            base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);
        }

        public BlockBehaviorPlantAbsorb(Block block) : base(block)
        {
        }
    }
}
