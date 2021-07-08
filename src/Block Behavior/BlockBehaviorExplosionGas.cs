using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class BlockBehaviorExplosionGas : BlockBehavior
    {
        public AssetLocation[] afterBlocks;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            afterBlocks = properties["afterBlocks"].AsObject(new AssetLocation[] { new AssetLocation("chaoslands:gas-no2-8"), new AssetLocation("chaoslands:gas-co-8") }, block.Code.Domain);
        }

        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
        {
            base.OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);

            if (!LandsOfChaosConfig.Loaded.GasesEnabled || afterBlocks == null || afterBlocks.Length < 1) return;

            Block gas = world.BlockAccessor.GetBlock(afterBlocks[world.Rand.Next(0, afterBlocks.Length)]);
            
            world.RegisterCallback((time) => { if (world.BlockAccessor.GetBlock(pos).BlockId == 0) world.BlockAccessor.SetBlock(gas.BlockId, pos); }, 2000);
        }


        public BlockBehaviorExplosionGas(Block block) : base(block)
        {
        }
    }
}
