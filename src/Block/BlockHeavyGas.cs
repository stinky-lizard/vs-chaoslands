using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class BlockHeavyGas : BlockGas
    {
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldgenRandom)
        {
            if (!base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom)) return false;
            if (!SinkDown(pos, api.World)) Diffuse(pos, api.World);
            return true;
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);

            if (world.Side == EnumAppSide.Server)
            {
                if (!SinkDown(blockPos, world, byItemStack != null ? 1 : LandsOfChaosConfig.Loaded.GasMaxPullAmount + 1)) Diffuse(blockPos, world);
            }
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);

            if (world.Side == EnumAppSide.Server)
            {
                if (!SinkDown(pos, world)) Diffuse(pos, world);
            }
        }

        public bool SinkDown(BlockPos pos, IWorldAccessor world, int first = 1)
        {
            bool evaporate = false;

            /*if (world.BlockAccessor.GetRainMapHeightAt(pos) <= pos.Y)
            {
                evaporate = true;
            }*/

            BlockPos down = pos.DownCopy();
            Block below = world.BlockAccessor.GetBlock(down);
            int mainBlock = int.Parse(LastCodePart());

            if (below.BlockId != 0)
            {
                if (below.FirstCodePart() == FirstCodePart() && below.FirstCodePart(1) == FirstCodePart(1))
                {
                    if (evaporate)
                    {
                        world.BlockAccessor.SetBlock(0, pos);
                        (below as BlockHeavyGas).SinkDown(down, world);
                        return true;
                    }

                    int concentration = int.Parse(below.LastCodePart());

                    if (concentration == 8) return false;

                    if (below.FirstCodePart() == FirstCodePart() && below.FirstCodePart(1) == FirstCodePart(1))
                    {
                        int touchingGas = int.Parse(below.LastCodePart());
                        int gasDiff = (mainBlock + touchingGas) - 8;

                        if (gasDiff <= 0)
                        {
                            touchingGas += mainBlock;

                            Block diffusion = world.BlockAccessor.GetBlock(CodeWithVariant("level", touchingGas.ToString()));

                            world.BlockAccessor.SetBlock(diffusion.BlockId, down);
                            world.BlockAccessor.SetBlock(0, pos);
                            
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else if (evaporate)
                {
                    world.BlockAccessor.SetBlock(0, pos);
                    return true;
                }

                return false;
            }

            world.BlockAccessor.SetBlock(0, pos);

            if (!evaporate)
            {
                world.BlockAccessor.SetBlock(BlockId, down);

                (world.BlockAccessor.GetBlock(down.Up(2)) as BlockHeavyGas)?.SinkDown(down, world);
            }

            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    if ((x != 0 && z != 0) || (x == 0 && z == 0)) continue;

                    down.Set(pos);
                    down.Add(x, 0, z);

                    below = world.BlockAccessor.GetBlock(down);

                    if (below is BlockHeavyGas)
                    {
                        (below as BlockHeavyGas).DelayedSink(down, pos, world, first);
                                                
                        return true;
                    }
                }
            }

            return true;

        }

        public void DelayedSink(BlockPos pos, BlockPos down, IWorldAccessor world, int first = 1)
        {
            if (world.BlockAccessor.GetBlock(down).BlockId != 0 || (first == 1 && world.BlockAccessor.GetBlock(down.Down()).BlockId != 0)) return;

            world.BlockAccessor.SetBlock(0, pos);
            world.BlockAccessor.SetBlock(BlockId, down);

            if (first > LandsOfChaosConfig.Loaded.GasMaxPullAmount) return;

            BlockPos tmpPos = pos.UpCopy();
            Block check = world.BlockAccessor.GetBlock(tmpPos);

            if (check is BlockHeavyGas)
            {
                (check as BlockHeavyGas).SinkDown(tmpPos, world);
            }

            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    if ((x != 0 && z != 0) || (x == 0 && z == 0)) continue;

                    tmpPos.Set(pos);
                    tmpPos.Add(x, 0, z);

                    check = world.BlockAccessor.GetBlock(tmpPos);

                    if (check is BlockHeavyGas)
                    {
                        (check as BlockHeavyGas).DelayedSink(tmpPos, down, world, first + 1);                        
                    }
                }
            }
        }
    }
}
