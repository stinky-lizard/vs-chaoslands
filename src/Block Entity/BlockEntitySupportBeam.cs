using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace ChaosLands
{
    public class BlockEntitySupportBeam : BlockEntity, IPointOfInterest
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            api.ModLoader.GetModSystem<POIRegistry>()?.AddPOI(this);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            Api.ModLoader.GetModSystem<POIRegistry>()?.RemovePOI(this);
        }

        public Vec3d Position => Pos.ToVec3d();

        public string Type => "supportbeam";
    }
}
