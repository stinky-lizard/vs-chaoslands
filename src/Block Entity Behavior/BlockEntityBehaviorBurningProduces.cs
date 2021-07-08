using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class BlockEntityBehaviorBurningProduces : BlockEntityBehavior
    {
        static AssetLocation cmo = new AssetLocation("chaoslands:gas-co-1");

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            Blockentity.RegisterGameTickListener(ProduceCO, 5000);
        }

        public void ProduceCO(float dt)
        {
            if ((Blockentity as BlockEntityFirepit)?.IsBurning == true || (Blockentity as BlockEntityForge)?.IsBurning == true || (Blockentity as BlockEntityBloomery)?.IsBurning == true || (Blockentity as BlockEntityCoalPile)?.IsBurning == true)
            {
                Block gas = Blockentity.Api.World.BlockAccessor.GetBlock(cmo);
                BlockPos pos = Blockentity.Pos.Copy();

                for (int y = 0; y < 7; y++)
                {
                    Block over = Blockentity.Api.World.BlockAccessor.GetBlock(pos);
                    if (over.SideSolid[BlockFacing.indexUP] || over.SideSolid[BlockFacing.indexDOWN]) return;
                    if (over.BlockId == 0)
                    {
                        Blockentity.Api.World.BlockAccessor.SetBlock(gas.BlockId, pos);
                        return;
                    }
                    int gasBuildUp = 0;
                    if (over.FirstCodePart() == gas.FirstCodePart() && over.FirstCodePart(1) == gas.FirstCodePart(1) && (gasBuildUp = int.Parse(over.LastCodePart())) < 8)
                    {
                        Block newGas = Blockentity.Api.World.BlockAccessor.GetBlock( over.CodeWithVariant("level", (gasBuildUp + 1).ToString()));
                        Blockentity.Api.World.BlockAccessor.SetBlock(newGas.BlockId, pos);
                        return;
                    }

                    pos.Up();
                }
            }
        }

        public BlockEntityBehaviorBurningProduces(BlockEntity blockentity) : base(blockentity)
        {
        }
    }
}
