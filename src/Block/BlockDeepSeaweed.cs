using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class BlockDeepSeaweed : BlockSeaweed
    {
        Random random = new Random();
        Block[] blocks;

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            BlockPos belowPos = pos.DownCopy();

            Block block = blockAccessor.GetBlock(belowPos);
            if (block.LiquidCode != "water") return false;

            int depth = 1;
            while (depth < (api.World.SeaLevel * 0.35))
            {
                belowPos.Down();
                block = blockAccessor.GetBlock(belowPos);

                if (block.Fertility > 0)
                {
                    belowPos.Up();
                    if (blockAccessor.GetBlock(belowPos).Code.Path == "water-still-7")
                    {
                        PlaceSeaweed(blockAccessor, belowPos, depth);
                        return true;
                    }
                }
                else
                {
                    if (!block.IsLiquid()) return false;
                }

                depth++;
            }

            return false;

        }

        private void PlaceSeaweed(IBlockAccessor blockAccessor, BlockPos pos, int depth)
        {
            int height = Math.Min(depth - 1, 1 + random.Next(3) + random.Next(3));

            if (blocks == null)
            {
                blocks = new Block[]
                {
                    blockAccessor.GetBlock(CodeWithParts("section")),
                    blockAccessor.GetBlock(CodeWithParts("top")),
                };
            }

            while (height-- > 0)
            {
                blockAccessor.SetBlock(height == 0 ? blocks[1].BlockId : blocks[0].BlockId, pos);
                pos.Up();
            }
        }
    }
}
