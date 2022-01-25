using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using BuffStuff;
using HarmonyLib;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.ServerMods;
using Vintagestory.API.Datastructures;
using ProtoBuf;
using Vintagestory.API.Common.Entities;

namespace ChaosLands
{
    public class Core : ModSystem
    {
        private Harmony harmony;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            try
            {
                LandsOfChaosConfig FromDisk;
                if ((FromDisk = api.LoadModConfig<LandsOfChaosConfig>("LandsOfChaosConfig.json")) == null)
                {
                    api.StoreModConfig<LandsOfChaosConfig>(LandsOfChaosConfig.Loaded, "LandsOfChaosConfig.json");
                }
                else LandsOfChaosConfig.Loaded = FromDisk;
            }
            catch
            {
                api.StoreModConfig<LandsOfChaosConfig>(LandsOfChaosConfig.Loaded, "LandsOfChaosConfig.json");
            }

            api.World.Config.SetBool("LOClocustHordeEnabled", LandsOfChaosConfig.Loaded.LocustHordeEnabled);
            api.World.Config.SetBool("LOCcaveinsEnabled", LandsOfChaosConfig.Loaded.CaveInsEnabled);
            api.World.Config.SetBool("LOCbossesEnabled", LandsOfChaosConfig.Loaded.BossBattlesEnabled);
            api.World.Config.SetBool("LOCtoxicLocustEnabled", LandsOfChaosConfig.Loaded.ToxicLocustEnabled);
            api.World.Config.SetBool("LOCfireLocustEnabled", LandsOfChaosConfig.Loaded.FireLocustEnabled);
            api.World.Config.SetBool("LOCbombLocustEnabled", LandsOfChaosConfig.Loaded.BombLocustEnabled);
            api.World.Config.SetBool("LOCsnailEnabled", LandsOfChaosConfig.Loaded.SnailEnabled);
            api.World.Config.SetBool("LOCstarfishEnabled", LandsOfChaosConfig.Loaded.StarfishEnabled);
            api.World.Config.SetBool("LOCseagrassEnabled", LandsOfChaosConfig.Loaded.SeagrassEnabled);
            api.World.Config.SetBool("LOCkelpEnabled", LandsOfChaosConfig.Loaded.KelpEnabled);
            api.World.Config.SetBool("LOCseasEnabled", LandsOfChaosConfig.Loaded.SeasEnabled);
            api.World.Config.SetBool("LOChaliteEnabled", LandsOfChaosConfig.Loaded.HaliteFormationsEnabled);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("test", typeof(ItemTester));

            api.RegisterBlockClass("BlockLocustHorde", typeof(BlockLocustHorde));

            api.RegisterBlockClass("BlockSupportBeam", typeof(BlockSupportBeam));
            api.RegisterBlockClass("BlockDeepSeaweed", typeof(BlockDeepSeaweed));
            api.RegisterBlockClass("BlockSeagrass", typeof(BlockSeagrass));
            api.RegisterBlockClass("BlockShallowWaterLily", typeof(BlockShallowWaterLily));

            api.RegisterBlockBehaviorClass("CaveIn", typeof(BlockBehaviorCaveIn));

            api.RegisterBlockEntityClass("SupportBeam", typeof(BlockEntitySupportBeam));

            

            api.RegisterEntityBehaviorClass("gear", typeof(EntityBehaviorGear));
            api.RegisterEntityBehaviorClass("locustprops", typeof(EntityBehaviorLocustProps));
            api.RegisterEntityBehaviorClass("watergone", typeof(EntityBehaviorBossWaterRemoval));
            api.RegisterEntityBehaviorClass("bosstalk", typeof(EntityBehaviorBossTalk));

            api.RegisterEntity("EntityNPC", typeof(EntityNPC));

            AiTaskRegistry.Register("npcmeleeattack", typeof(AiTaskNPCMeleeAttack));
            AiTaskRegistry.Register("firemeleeattack", typeof(AiTaskFireMeleeAttack));
            AiTaskRegistry.Register("selfdestruct", typeof(AiTaskSelfDestruct));
            AiTaskRegistry.Register("toxicmeleeattack", typeof(AiTaskVenomMeleeAttack));
            AiTaskRegistry.Register("shield", typeof(AiTaskShield));
            AiTaskRegistry.Register("crabwander", typeof(AiTaskCrabWander));

            harmony = new Harmony("com.jakecool19.chaoslands.worldgen");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Dispose()
        {
            harmony.UnpatchAll(harmony.Id);
            base.Dispose();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            BuffManager.Initialize(api, this);

            BuffManager.RegisterBuffType("ToxicGasEffect", typeof(ToxicGasEffect));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
        }
    }

    public class LandsOfChaosConfig
    {
        public static LandsOfChaosConfig Loaded { get; set; } = new LandsOfChaosConfig();

        //Default caves
        public bool DefaultCavesEnabled { get; set; } = true;

        public bool DefaultCavesBigNearLava { get; set; } = false;

        public bool DefaultCavesExtraBranchy { get; set; } = false;

        public float DefaultCavesTunnelWidth { get; set; } = 1f;

        public float DefaultCavesTunnelHeight { get; set; } = 1f;

        public float DefaultCavesShaftWidth { get; set; } = 1f;

        public float DefaultCavesShaftHeight { get; set; } = 1f;

        //Ocean settings
        public float OceanWeightMult { get; set; } = 0f;

        //Custom Trees
        public float TreeSizeMult { get; set; } = 1;

        public float TreeVineMult { get; set; } = 1;

        public float TreeSpecialLogMult { get; set; } = 1;

        //Cave in Settings
        public double CaveInChance { get; set; } = 0.1;

        public int CaveInRadius { get; set; } = 5;

        public int CaveInDepth { get; set; } = 7;

        public int CaveInDepthOffset { get; set; } = 3;

        public float CaveInSupportBeamRadius { get; set; } = 5f;

        public float CaveInChainChance { get; set; } = 0.75f;

        public float CaveInDamage { get; set; } = 3f;

        public bool CaveInSideways { get; set; } = false;

        public float CaveInSidewaysChance { get; set; } = 0.25f;

        public float CaveInDust { get; set; } = 0f;

        public bool CaveInCracks { get; set; } = false;

        //Gas settings

        public bool GasExplosions { get; set; } = true;

        public double GasPickaxeExplosion { get; set; } = 0.25;

        public int GasSpreadRadius { get; set; } = 7;

        public int GasExplosionRadius { get; set; } = 2;

        #region Control Content

        public bool SeasEnabled { get; set; } = true;

        public bool NoiseCavesEnabled { get; set; } = true;

        public bool LocustHordeEnabled { get; set; } = true;

        public bool CaveInsEnabled { get; set; } = true;

        public bool GasesEnabled { get; set; } = true;

        public bool BreathingEnabled { get; set; } = true;

        public bool BossBattlesEnabled { get; set; } = true;

        public bool FireLocustEnabled { get; set; } = true;

        public bool ToxicLocustEnabled { get; set; } = true;

        public bool BombLocustEnabled { get; set; } = true;

        public bool SnailEnabled { get; set; } = true;

        public bool StarfishEnabled { get; set; } = true;

        public bool SeagrassEnabled { get; set; } = true;

        public bool KelpEnabled { get; set; } = true;

        public bool HaliteFormationsEnabled { get; set; } = false;

        public bool RivuletsEnabled { get; set; } = true;

        public bool CaveBiomesEnabled { get; set; } = true;
        #endregion
    }

    public class ItemTester : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefault;


            Dictionary<string, float> gases = new Dictionary<string, float>();
            gases.Add("why", 9);
            GasHelper.CollectGases(byEntity.SidedPos.AsBlockPos, 5, null);

        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            if (byEntity.Api.Side == EnumAppSide.Server)
            {

            }

            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
        }
    }

    public class GenBossAreana : ModStdWorldGen
    {
        ICoreServerAPI api;
        IWorldGenBlockAccessor worldgenBlockAccessor;

        public override double ExecuteOrder()
        {
            return 0.91;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            this.api = api;

            api.Event.InitWorldGenerator(initWorldGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
            api.Event.InitWorldGenerator(initWorldGen, "superflat");
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "superflat");
            api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
        }

        internal void initWorldGen()
        {
            chunksize = api.World.BlockAccessor.ChunkSize;
            LoadGlobalConfig(api);
        }

        private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
        {
            worldgenBlockAccessor = chunkProvider.GetBlockAccessor(false);
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            if (chunkX > 0 || chunkZ > 0) return;

            BlockPos tmpPos = new BlockPos();

            for (int x = 0; x < chunksize; x++)
            {
                for (int y = 0; y < chunksize; y++)
                {
                    for (int z = 0; z < chunksize; z++)
                    {
                        tmpPos.Set(x,y,z);
                        worldgenBlockAccessor.SetBlock(GlobalConfig.basaltBlockId, tmpPos);
                    }
                }
            }

            api.World.Claims.Add(new LandClaim()
            {
                Areas = new List<Cuboidi>() { new Cuboidi(0, 0, 0, chunksize, chunksize, chunksize) },
                Description = "Sacred Arena",
                ProtectionLevel = 10,
                LastKnownOwnerName = "Dungeon Master",
                AllowUseEveryone = false
            });
        }
    }
}
