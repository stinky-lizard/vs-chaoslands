using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class BlockShallowWaterLily : BlockWaterLily
    {
        public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockPos tmpPos = pos.DownCopy();
            Block block = blockAccessor.GetBlock(tmpPos);
            tmpPos.Y--;
            Block soil = blockAccessor.GetBlock(tmpPos);
            return block.IsLiquid() && block.LiquidLevel == 7 && block.LiquidCode.Contains("water") && soil.Fertility > 0;
        }
    }
}
