using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class BlockSeagrass : BlockWaterPlant
    {
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            BlockPos belowPos = pos.DownCopy();

            Block block = blockAccessor.GetBlock(belowPos);
            if (block.LiquidCode != "water") return false;

            int depth = 1;

            while (depth < (api.World.SeaLevel * 0.25))
            {
                belowPos.Down();
                block = blockAccessor.GetBlock(belowPos);

                if (block.Fertility > 0)
                {
                    belowPos.Up();
                    if (blockAccessor.GetBlock(belowPos).Code.Path == "water-still-7")
                    {
                        Block placingBlock = blockAccessor.GetBlock(Code);
                        if (placingBlock == null) return false;
                        blockAccessor.SetBlock(placingBlock.BlockId, belowPos);
                        return true;
                    }
                }

                depth++;
            }

            return false;
        }
    }
}
