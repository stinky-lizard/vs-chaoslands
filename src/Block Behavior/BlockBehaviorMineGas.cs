using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class BlockBehaviorMineGas : BlockBehavior
    {
        public AssetLocation[] afterBlocks;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            afterBlocks = properties["afterBlocks"].AsObject(new AssetLocation[0], block.Code.Domain);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            base.OnBlockBroken(world, pos, byPlayer, ref handling);

            if (!LandsOfChaosConfig.Loaded.GasesEnabled || afterBlocks == null || afterBlocks.Length < 1) return;

            Block gas = world.BlockAccessor.GetBlock(afterBlocks[world.Rand.Next(afterBlocks.Length)]);
            world.RegisterCallback((time) => { world.BlockAccessor.SetBlock(gas.BlockId, pos); }, 250);
        }


        public BlockBehaviorMineGas(Block block) : base(block)
        {
        }
    }
}
