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
            if (api.Side != EnumAppSide.Server) return;
            api.ModLoader.GetModSystem<POIRegistry>()?.AddPOI(this);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (Api.Side != EnumAppSide.Server) return;
            Api.ModLoader.GetModSystem<POIRegistry>()?.RemovePOI(this);
        }

        public Vec3d Position => Pos.ToVec3d();

        public string Type => "supportbeam";
    }
}
