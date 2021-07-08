using Vintagestory.API.Common;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System.Collections.Generic;

namespace ChaosLands
{
    public class BlockSupportBeam : Block
    {
        ICoreClientAPI capi;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            capi = api as ICoreClientAPI;
        }

        public string GetOrientations(IWorldAccessor world, BlockPos pos)
        {
            string orientations =
                GetSupportBeamCode(world, pos, BlockFacing.NORTH) +
                GetSupportBeamCode(world, pos, BlockFacing.EAST) +
                GetSupportBeamCode(world, pos, BlockFacing.SOUTH) +
                GetSupportBeamCode(world, pos, BlockFacing.WEST)
            ;

            if (orientations.Length == 0) orientations = "empty";
            return orientations;
        }

        private string GetSupportBeamCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
        {
            if (ShouldConnectAt(world, pos, facing)) return "" + facing.Code[0];
            return "";
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            string orientations = GetOrientations(world, blockSel.Position);
            Block block = world.BlockAccessor.GetBlock(CodeWithVariant("type", orientations));

            if (block == null) block = this;

            if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode) && IsSupported(blockSel.Position, orientations, world))
            {
                world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                return true;
            }

            return false;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            string orientations = GetOrientations(world, pos);

            if (!IsSupported(pos, orientations, world))
            {
                world.BlockAccessor.BreakBlock(pos, null);
                return;
            }

            AssetLocation newBlockCode = CodeWithVariant("type", orientations);

            if (!Code.Equals(newBlockCode))
            {
                Block block = world.BlockAccessor.GetBlock(newBlockCode);
                if (block == null) return;

                world.BlockAccessor.SetBlock(block.BlockId, pos);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
            }
            else
            {
                base.OnNeighbourBlockChange(world, pos, neibpos);
            }
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "type" }, new string[] { "empty" }));
            return new ItemStack[] { new ItemStack(block) };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "type" }, new string[] { "empty" }));
            return new ItemStack(block);
        }



        public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
        {
            Block block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side));

            bool attrexists = block.Attributes?["supportBeamConnect"][side.Code].Exists == true;
            if (attrexists)
            {
                return block.Attributes["supportBeamConnect"][side.Code].AsBool(true);
            }

            return block is BlockSupportBeam;
        }


        static string[] OneDir = new string[] { "n", "e", "s", "w" };
        static string[] TwoDir = new string[] { "ns", "ew" };
        static string[] AngledDir = new string[] { "ne", "es", "sw", "nw" };
        static string[] ThreeDir = new string[] { "nes", "new", "nsw", "esw" };

        static string[] GateLeft = new string[] { "egw", "ngs" };
        static string[] GateRight = new string[] { "gew", "gns" };

        static Dictionary<string, KeyValuePair<string[], int>> AngleGroups = new Dictionary<string, KeyValuePair<string[], int>>();

        static BlockSupportBeam()
        {
            AngleGroups["n"] = new KeyValuePair<string[], int>(OneDir, 0);
            AngleGroups["e"] = new KeyValuePair<string[], int>(OneDir, 1);
            AngleGroups["s"] = new KeyValuePair<string[], int>(OneDir, 2);
            AngleGroups["w"] = new KeyValuePair<string[], int>(OneDir, 3);

            AngleGroups["ns"] = new KeyValuePair<string[], int>(TwoDir, 0);
            AngleGroups["ew"] = new KeyValuePair<string[], int>(TwoDir, 1);

            AngleGroups["ne"] = new KeyValuePair<string[], int>(AngledDir, 0);
            AngleGroups["es"] = new KeyValuePair<string[], int>(AngledDir, 1);
            AngleGroups["sw"] = new KeyValuePair<string[], int>(AngledDir, 2);
            AngleGroups["nw"] = new KeyValuePair<string[], int>(AngledDir, 3);

            AngleGroups["nes"] = new KeyValuePair<string[], int>(ThreeDir, 0);
            AngleGroups["new"] = new KeyValuePair<string[], int>(ThreeDir, 1);
            AngleGroups["nsw"] = new KeyValuePair<string[], int>(ThreeDir, 2);
            AngleGroups["esw"] = new KeyValuePair<string[], int>(ThreeDir, 3);


            AngleGroups["egw"] = new KeyValuePair<string[], int>(GateLeft, 0);
            AngleGroups["ngs"] = new KeyValuePair<string[], int>(GateLeft, 1);

            AngleGroups["gew"] = new KeyValuePair<string[], int>(GateRight, 0);
            AngleGroups["gns"] = new KeyValuePair<string[], int>(GateRight, 1);
        }

        public override AssetLocation GetRotatedBlockCode(int angle)
        {
            string type = Variant["type"];

            if (type == "empty" || type == "nesw") return Code;


            int angleIndex = angle / 90;

            var val = AngleGroups[type];

            string newFacing = val.Key[GameMath.Mod(val.Value + angleIndex, val.Key.Length)];

            return CodeWithVariant("type", newFacing);
        }

        public bool IsSupported(BlockPos pos, string orientations, IWorldAccessor world)
        {
            BlockPos tmpPos = pos.Copy();

            if (orientations == "ns")
            {
                tmpPos.Add(0, 0, 1);
                if (!(world.BlockAccessor.GetBlock(tmpPos) is BlockSupportBeam)) return false;

                tmpPos.Add(0, 0, -2);
                if (!(world.BlockAccessor.GetBlock(tmpPos) is BlockSupportBeam)) return false;

                return true;
            }

            if (orientations == "ew")
            {
                tmpPos.Add(1, 0, 0);
                if (!(world.BlockAccessor.GetBlock(tmpPos) is BlockSupportBeam)) return false;

                tmpPos.Add(-2, 0, 0);
                if (!(world.BlockAccessor.GetBlock(tmpPos) is BlockSupportBeam)) return false;

                return true;
            }

            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 1; y++)
                {
                    for (int z = -1; z < 2; z++)
                    {
                        if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) != 1) continue;
                        tmpPos.Set(pos);
                        Block support = world.BlockAccessor.GetBlock(tmpPos.Add(x, y, z));

                        if ((support is BlockSupportBeam && support.LastCodePart() != "ns" && support.LastCodePart() != "ew") || (y == -1 && support.SideSolid[BlockFacing.indexUP])) return true;
                    }
                }
            }

            return false;
        }
    }
}
