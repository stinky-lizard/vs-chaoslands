using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class BlockBehaviorCaveIn : BlockBehavior
    {
        bool breakInstead;
        bool transformInstead;
        AssetLocation[] exceptions;
        AssetLocation transformInto;
        

        AssetLocation fallSound;

        Cuboidi attachmentArea;

        public BlockBehaviorCaveIn(Block block) : base(block)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            breakInstead = properties["breakInstead"].AsBool(false);
            transformInstead = properties["transformInstead"].AsBool(false);
            transformInto = new AssetLocation(properties["transformInto"].AsString("game:air"));
            exceptions = properties["exceptions"].AsObject(new AssetLocation[0], block.Code.Domain);

            attachmentArea = properties["attachmentArea"].AsObject<Cuboidi>(null);

            string sound = properties["fallSound"].AsString(null);
            if (sound != null)
            {
                fallSound = AssetLocation.Create(sound, block.Code.Domain);
            }
            
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            base.OnBlockBroken(world, pos, byPlayer, ref handling);

            if (byPlayer == null || !LandsOfChaosConfig.Loaded.CaveInsEnabled || byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative) return;
            if (world.Rand.NextDouble() > LandsOfChaosConfig.Loaded.CaveInChance) return;
            if (world.Api.ModLoader.GetModSystem<POIRegistry>()?.GetNearestPoi(pos.ToVec3d(), LandsOfChaosConfig.Loaded.CaveInSupportBeamRadius * 2, (block) => { if (block.Type == "supportbeam" && CheckForSupport(pos, block.Position)) return true; return false; }) != null) return;

            BlockPos tmpPos = pos.Copy();
            world.BlockAccessor.SetBlock(0, pos);

            for (int x = -LandsOfChaosConfig.Loaded.CaveInRadius; x <= LandsOfChaosConfig.Loaded.CaveInRadius; x++)
            {
                for (int y = -LandsOfChaosConfig.Loaded.CaveInRadius; y <= LandsOfChaosConfig.Loaded.CaveInRadius ; y++)
                {
                    for (int z = -LandsOfChaosConfig.Loaded.CaveInRadius; z <= LandsOfChaosConfig.Loaded.CaveInRadius; z++)
                    {
                        tmpPos.Set(pos);
                        tmpPos.Add(x, y, z);
                        if (tmpPos.Equals(pos)) continue;

                        EnumHandling bla = EnumHandling.PassThrough;
                        string bla2 = "";
                        if (world.Rand.NextDouble() <= LandsOfChaosConfig.Loaded.CaveInChainChance) world.BlockAccessor.GetBlock(tmpPos).GetBehavior<BlockBehaviorCaveIn>()?.TryFalling(world, tmpPos, ref bla, ref bla2, world.Rand.Next(LandsOfChaosConfig.Loaded.CaveInDepth - LandsOfChaosConfig.Loaded.CaveInDepthOffset, LandsOfChaosConfig.Loaded.CaveInDepth + LandsOfChaosConfig.Loaded.CaveInDepthOffset + 1));
                    }
                }
            }
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);

            /*if (world.Side == EnumAppSide.Client) return;
            if (world.Rand.NextDouble() > 0.00) return;
            
            EnumHandling bla = EnumHandling.PassThrough;
            string bla2 = "";
            TryFalling(world, pos, ref bla, ref bla2, 20);*/
            

        }

        private bool TryFalling(IWorldAccessor world, BlockPos pos, ref EnumHandling handling, ref string failureCode, int depth)
        {
            if (world.Side != EnumAppSide.Server || world.Claims.Get(pos)?.Length > 0) return false;

            ICoreServerAPI sapi = (world as IServerWorldAccessor).Api as ICoreServerAPI;
            if (!sapi.Server.Config.AllowFallingBlocks) return false;

            if (breakInstead)
            {
                world.BlockAccessor.BreakBlock(pos, null);
                handling = EnumHandling.PreventSubsequent;
                return true;
            }

            if (LandsOfChaosConfig.Loaded.CaveInCracks && transformInstead)
            {
                Block transformation = world.GetBlock(transformInto);

                if (transformation != null)
                {
                    world.BlockAccessor.SetBlock(transformation.BlockId, pos);
                    handling = EnumHandling.PreventSubsequent;
                    return true;
                }
            }

            if (IsReplacableBeneath(world, pos) || (LandsOfChaosConfig.Loaded.CaveInSideways && world.Rand.NextDouble() < LandsOfChaosConfig.Loaded.CaveInSidewaysChance && IsReplacableBeneathAndSideways(world, pos)))
            {
                if (world.Api.ModLoader.GetModSystem<POIRegistry>()?.GetNearestPoi(pos.ToVec3d(), LandsOfChaosConfig.Loaded.CaveInSupportBeamRadius * 2, (block) => { if (block.Type == "supportbeam" && CheckForSupport(pos, block.Position)) return true; return false; }) != null)
                {
                    handling = EnumHandling.PreventDefault;
                    failureCode = "supportbeam";
                    return false;
                }

                // Prevents duplication
                Entity entity = world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1, 1.5f, (e) =>
                {
                    return e is EntityBlockFalling && ((EntityBlockFalling)e).initialPos.Equals(pos);
                });

                if (entity == null)
                {
                    EntityBlockFalling entityblock = new EntityBlockFalling(block, world.BlockAccessor.GetBlockEntity(pos), pos, fallSound, LandsOfChaosConfig.Loaded.CaveInDamage, LandsOfChaosConfig.Loaded.CaveInSideways, LandsOfChaosConfig.Loaded.CaveInDust);
                    world.SpawnEntity(entityblock);
                }
                else
                {
                    handling = EnumHandling.PreventDefault;
                    failureCode = "entityintersecting";
                    return false;
                }

                if (depth > 0)
                {
                    world.BlockAccessor.SetBlock(0, pos);
                    BlockPos tmpPos = pos.AddCopy(0, 1, 0);
                    world.BlockAccessor.GetBlock(tmpPos).GetBehavior<BlockBehaviorCaveIn>()?.TryFalling(world, tmpPos, ref handling, ref failureCode, depth - 1);
                }
                handling = EnumHandling.PreventSubsequent;
                return true;
            }

            handling = EnumHandling.PassThrough;
            return false;
        }

        private bool IsReplacableBeneathAndSideways(IWorldAccessor world, BlockPos pos)
        {
            for (int i = 0; i < 4; i++)
            {
                BlockFacing facing = BlockFacing.HORIZONTALS[i];

                Block nBlock = world.BlockAccessor.GetBlockOrNull(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y, pos.Z + facing.Normali.Z);
                Block nBBlock = world.BlockAccessor.GetBlockOrNull(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y - 1, pos.Z + facing.Normali.Z);

                if (nBlock != null && nBBlock != null && nBlock.Replaceable >= 6000 && nBBlock.Replaceable >= 6000)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsReplacableBeneath(IWorldAccessor world, BlockPos pos)
        {
            Block bottomBlock = world.BlockAccessor.GetBlock(pos.X, pos.Y - 1, pos.Z);
            return (bottomBlock != null && bottomBlock.Replaceable > 6000);
        }

        private bool CheckForSupport(BlockPos pos, Vec3d support)
        {
            return
                pos.Y > support.Y && pos.Y <= support.Y + LandsOfChaosConfig.Loaded.CaveInSupportBeamRadius
                && pos.X <= support.X + LandsOfChaosConfig.Loaded.CaveInSupportBeamRadius && pos.X >= support.X - LandsOfChaosConfig.Loaded.CaveInSupportBeamRadius
                && pos.Z <= support.Z + LandsOfChaosConfig.Loaded.CaveInSupportBeamRadius && pos.Z >= support.Z - LandsOfChaosConfig.Loaded.CaveInSupportBeamRadius
                ;
        }
    }
}
