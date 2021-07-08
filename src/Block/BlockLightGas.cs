using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class BlockLightGas : BlockGas
    {
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldgenRandom)
        {
            if (!base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom)) return false;
            if (!FloatUp(pos, api.World)) Diffuse(pos, api.World);
            return true;
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);

            if (world.Side == EnumAppSide.Server)
            {
                if (!FloatUp(blockPos, world, byItemStack != null ? 1 : LandsOfChaosConfig.Loaded.GasMaxPullAmount + 1)) Diffuse(blockPos, world);
            }
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);

            if (world.Side == EnumAppSide.Server)
            {
                if (!FloatUp(pos, world)) Diffuse(pos, world);
            }
        }

        public bool FloatUp(BlockPos pos, IWorldAccessor world, int first = 1)
        {
            bool evaporate = false;

            if (world.BlockAccessor.GetRainMapHeightAt(pos) <= pos.Y)
            {
                evaporate = true;
            }

            BlockPos up = pos.UpCopy();
            Block above = world.BlockAccessor.GetBlock(up);
            int mainBlock = int.Parse(LastCodePart());

            if (above.BlockId != 0)
            {
                if (above.FirstCodePart() == FirstCodePart() && above.FirstCodePart(1) == FirstCodePart(1))
                {
                    if (evaporate)
                    {
                        world.BlockAccessor.SetBlock(0, pos);
                        (above as BlockLightGas).FloatUp(up, world);
                        return true;
                    }

                    int concentration = int.Parse(above.LastCodePart());

                    if (concentration == 8) return false;

                    if (above.FirstCodePart() == FirstCodePart() && above.FirstCodePart(1) == FirstCodePart(1))
                    {
                        int touchingGas = int.Parse(above.LastCodePart());
                        int gasDiff = (mainBlock + touchingGas) - 8;

                        if (gasDiff <= 0)
                        {
                            touchingGas += mainBlock;

                            Block diffusion = world.BlockAccessor.GetBlock(CodeWithVariant("level", touchingGas.ToString()));

                            world.BlockAccessor.SetBlock(diffusion.BlockId, up);
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
                world.BlockAccessor.SetBlock(BlockId, up);

                (world.BlockAccessor.GetBlock(up.Down(2)) as BlockLightGas)?.FloatUp(up, world);
            }

            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    if ((x != 0 && z != 0) || (x == 0 && z == 0)) continue;

                    up.Set(pos);
                    up.Add(x, 0, z);

                    above = world.BlockAccessor.GetBlock(up);

                    if (above is BlockLightGas)
                    {
                        (above as BlockLightGas).DelayedFloat(up, pos, world, first);

                        return true;
                    }
                }
            }

            return true;

        }

        public void DelayedFloat(BlockPos pos, BlockPos up, IWorldAccessor world, int first = 1)
        {
            if (world.BlockAccessor.GetBlock(up).BlockId != 0 || (first == 1 && world.BlockAccessor.GetBlock(up.Up()).BlockId != 0)) return;

            world.BlockAccessor.SetBlock(0, pos);
            world.BlockAccessor.SetBlock(BlockId, up);

            if (first > LandsOfChaosConfig.Loaded.GasMaxPullAmount) return;

            BlockPos tmpPos = pos.DownCopy();
            Block check = world.BlockAccessor.GetBlock(tmpPos);

            if (check is BlockLightGas)
            {
                (check as BlockLightGas).FloatUp(tmpPos, world);
            }

            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    if ((x != 0 && z != 0) || (x == 0 && z == 0)) continue;

                    tmpPos.Set(pos);
                    tmpPos.Add(x, 0, z);

                    check = world.BlockAccessor.GetBlock(tmpPos);

                    if (check is BlockLightGas)
                    {
                        (check as BlockLightGas).DelayedFloat(tmpPos, up, world, first + 1);
                    }
                }
            }
        }
    }
}
