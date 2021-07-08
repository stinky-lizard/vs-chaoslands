using Vintagestory.API.Common;
using Vintagestory.API.Server;
using BuffStuff;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace ChaosLands
{
    public class BlockGas: Block, INonBreathable
    {
        public float Concentration => 1 - (float.Parse(LastCodePart()) / Attributes["suffocateAmount"].AsFloat(4));

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldgenRandom)
        {
            if (!LandsOfChaosConfig.Loaded.GasesEnabled) return false;
            return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);

            if (!LandsOfChaosConfig.Loaded.GasesEnabled)
            {
                world.BlockAccessor.SetBlock(0, pos);
                return;
            }

            BlockPos tmpPos = pos.Copy();

            foreach(BlockFacing face in BlockFacing.ALLFACES)
            {
                tmpPos.Set(pos);
                Block postorch = world.BlockAccessor.GetBlock(tmpPos.AddCopy(face));

                if ((postorch as BlockTorch)?.OnTryIgniteBlock(null, tmpPos, 40) == EnumIgniteState.NotIgnitablePreventDefault || (postorch as BlockTorchHolder)?.Empty == false || postorch is BlockLava)
                {
                    Spark(pos, world);
                    return;
                }

                BlockEntity fire = world.BlockAccessor.GetBlockEntity(tmpPos);
                if (fire == null) return;

                if ((fire as BlockEntityFirepit)?.IsBurning == true || (fire as BlockEntityForge)?.IsBurning == true ||
                    (fire as BlockEntityCoalPile)?.IsBurning == true || (fire as BlockEntityBloomery)?.IsBurning == true)
                {
                    Spark(pos, world);
                    return;
                }

            }
        }

        public void Diffuse(BlockPos pos, IWorldAccessor world)
        {
            int mainBlock = int.Parse(LastCodePart());

            BlockPos tmpPos = pos.Copy();

            Block neighbor = world.BlockAccessor.GetBlock(tmpPos);


            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    if ((x != 0 && z != 0) || (x == 0 && z == 0)) continue;

                    tmpPos.Set(pos);
                    tmpPos.Add(x, 0, z);

                    neighbor = world.BlockAccessor.GetBlock(tmpPos);

                    if (neighbor.BlockId == 0 && mainBlock > 1)
                    {
                        mainBlock--;
                        Block diffusion = world.BlockAccessor.GetBlock(CodeWithVariant("level", "1"));
                        Block newGas = world.BlockAccessor.GetBlock(CodeWithVariant("level", mainBlock.ToString()));

                        world.BlockAccessor.SetBlock(diffusion.BlockId, tmpPos);
                        world.BlockAccessor.SetBlock(newGas.BlockId, pos);
                        return;
                    }

                    if (neighbor.FirstCodePart() == FirstCodePart() && neighbor.FirstCodePart(1) == FirstCodePart(1))
                    {
                        int touchingGas = int.Parse(neighbor.LastCodePart());
                        int gasDiff = (mainBlock - touchingGas) - 1;

                        if (touchingGas + 2 > mainBlock || gasDiff < 1) continue;

                        touchingGas += gasDiff;
                        mainBlock -= gasDiff;

                        Block diffusion = world.BlockAccessor.GetBlock(CodeWithVariant("level", touchingGas.ToString()));
                        Block newGas = world.BlockAccessor.GetBlock(CodeWithVariant("level", mainBlock.ToString()));

                        world.BlockAccessor.SetBlock(diffusion.BlockId, tmpPos);
                        world.BlockAccessor.SetBlock(newGas.BlockId, pos);
                        return;
                    }
                }
            }
        }

        public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
        {
            base.OnEntityInside(world, entity, pos);
            if (world.Side != EnumAppSide.Server) return;

            if ((entity as EntityAgent)?.GetEyesBlockId() == BlockId && !BuffManager.IsBuffActive(entity, "ToxicGasEffect"))
            {
                var poison = new ToxicGasEffect();
                poison.StackAmount = int.Parse(LastCodePart());
                poison.Gas = Variant["type"];
                poison.Apply(entity);
            }

            EntityAgent agent = entity as EntityAgent;
            

            if (entity.IsOnFire || agent?.LeftHandItemSlot?.Itemstack?.Collectible is BlockTorch || agent?.RightHandItemSlot?.Itemstack?.Collectible is BlockTorch || (entity as EntityItem)?.Itemstack.Collectible is BlockTorch)
            {
                Spark(pos, world);
            }
        }

        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
        {
            if (world.Side == EnumAppSide.Server && Attributes.IsTrue("flammable"))
            {
                world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1.5f, 1.5f, (e) =>
                {
                    e.Ignite();
                    return false;
                });
                if (world.Rand.NextDouble() <= 0.1) (world as IServerWorldAccessor)?.CreateExplosion(pos, EnumBlastType.RockBlast, 2, 3);
            }

            base.OnBlockExploded(world, pos, explosionCenter, blastType);
        }

        public void Spark(BlockPos pos, IWorldAccessor world)
        {
            if (LandsOfChaosConfig.Loaded.GasExplosions && int.Parse(LastCodePart()) >= Attributes["explosionConc"].AsInt(100))
            {
                (world as IServerWorldAccessor)?.CreateExplosion(pos, EnumBlastType.RockBlast, 3, 1);
            }
        }
    }

    public interface INonBreathable
    {
        float Concentration { get; }
    }
}
