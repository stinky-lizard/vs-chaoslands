using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class BlockBehaviorSparkGas : BlockBehavior
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
            
            if (byPlayer == null || !LandsOfChaosConfig.Loaded.GasesEnabled || !LandsOfChaosConfig.Loaded.GasExplosions || world.Rand.NextDouble() > LandsOfChaosConfig.Loaded.GasPickaxeExplosion) return;

            BlockPos tmpPos = pos.Copy();
            foreach (BlockFacing face in BlockFacing.ALLFACES)
            {
                tmpPos.Set(pos);
                (world.BlockAccessor.GetBlock(tmpPos.AddCopy(face)) as BlockGas)?.Spark(tmpPos, world);
            }
        }

        public BlockBehaviorSparkGas(Block block) : base(block)
        {
        }
    }
}
