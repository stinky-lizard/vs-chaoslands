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
            api.World.Config.SetBool("LOCgasesEnabled", LandsOfChaosConfig.Loaded.GasesEnabled);
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

            api.RegisterBlockClass("BlockLocustHorde", typeof(BlockLocustHorde));
            api.RegisterBlockClass("BlockLightGas", typeof(BlockLightGas));
            api.RegisterBlockClass("BlockHeavyGas", typeof(BlockHeavyGas));

            api.RegisterBlockClass("BlockSupportBeam", typeof(BlockSupportBeam));
            api.RegisterBlockClass("BlockDeepSeaweed", typeof(BlockDeepSeaweed));
            api.RegisterBlockClass("BlockSeagrass", typeof(BlockSeagrass));
            api.RegisterBlockClass("BlockShallowWaterLily", typeof(BlockShallowWaterLily));
            api.RegisterBlockClass("BlockLightPlant", typeof(BlockLightPlant));

            api.RegisterBlockBehaviorClass("CaveIn", typeof(BlockBehaviorCaveIn));
            api.RegisterBlockBehaviorClass("PlantAbsorb", typeof(BlockBehaviorPlantAbsorb));
            api.RegisterBlockBehaviorClass("SparkGas", typeof(BlockBehaviorSparkGas));
            api.RegisterBlockBehaviorClass("MineGas", typeof(BlockBehaviorMineGas));
            api.RegisterBlockBehaviorClass("ExplosionGas", typeof(BlockBehaviorExplosionGas));

            api.RegisterBlockEntityClass("SupportBeam", typeof(BlockEntitySupportBeam));

            api.RegisterBlockEntityBehaviorClass("PlantAbsorb", typeof(BlockEntityBehaviorPlantAbsorb));
            api.RegisterBlockEntityBehaviorClass("BurningProduces", typeof(BlockEntityBehaviorBurningProduces));

            api.RegisterEntityBehaviorClass("gear", typeof(EntityBehaviorGear));
            api.RegisterEntityBehaviorClass("air", typeof(EntityBehaviorAir));
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
            if (LandsOfChaosConfig.Loaded.BreathingEnabled)
            {
                HudElementAirBar airBar = new HudElementAirBar(api);
                airBar.TryOpen();
            }
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

        //Gas settings

        public int GasPlantAbsorptionRadius { get; set; } = 2;

        public int GasPlantAbsorptionMinLightLevel { get; set; } = 13;

        public bool GasExplosions { get; set; } = true;

        public double GasPickaxeExplosion { get; set; } = 0.25;

        public int GasMaxPullAmount { get; set; } = 5;

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
        #endregion
    }

    public class BlockLightPlant : Block
    {
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldgenRandom)
        {
            bool result = base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldgenRandom);

            if (result) FloodFillDecorAt(pos.X, pos.Y, pos.Z, blockAccessor, worldgenRandom);

            return result;
        }

        public void FloodFillDecorAt(int posX, int posY, int posZ, IBlockAccessor accessor, LCGRandom rand)
        {
            Queue<Vec4i> bfsQueue = new Queue<Vec4i>();
            HashSet<BlockPos> fillablePositions = new HashSet<BlockPos>();

            bfsQueue.Enqueue(new Vec4i(posX, posY, posZ, 0));
            fillablePositions.Add(new BlockPos(posX, posY, posZ));

            float radius = 10;

            BlockFacing[] faces = BlockFacing.ALLFACES;
            BlockPos curPos = new BlockPos();
            List<BlockPos> modifyPos = new List<BlockPos>();
            List<IWorldChunk> chunksMarked = new List<IWorldChunk>();
            string[] plants = { "lichen", "moss", "barnacle" };
            bool one = false;




            while (bfsQueue.Count > 0)
            {
                Vec4i bpos = bfsQueue.Dequeue();

                foreach (BlockFacing facing in faces)
                {
                    curPos.Set(bpos.X + facing.Normali.X, bpos.Y + facing.Normali.Y, bpos.Z + facing.Normali.Z);

                    Block block = accessor.GetBlock(curPos);
                    bool inBounds = bpos.W < radius;

                    if (inBounds)
                    {
                        if (block.BlockId == 0 && !fillablePositions.Contains(curPos))
                        {
                            bfsQueue.Enqueue(new Vec4i(curPos.X, curPos.Y, curPos.Z, bpos.W + 1));
                            fillablePositions.Add(curPos.Copy());
                        }
                        else if (block.SideSolid[facing.Opposite.Index])
                        {
                            Block attach = accessor.GetBlock(new AssetLocation("game:attachingplant-" + plants[rand.NextInt(plants.Length)]));
                            IWorldChunk dirty = accessor.GetChunkAtBlockPos(curPos);
                            if (dirty != null)
                            {
                                dirty.AddDecor(accessor, attach, curPos, facing.Opposite);
                                if (!chunksMarked.Contains(dirty))
                                {
                                    chunksMarked.Add(dirty);
                                    modifyPos.Add(curPos.Copy());
                                }

                                if (!one)
                                {
                                    System.Diagnostics.Debug.WriteLine(curPos);
                                    one = true;
                                }
                            }

                        }

                    }
                }

            }

            foreach (BlockPos dirty in modifyPos)
            {
                accessor.MarkChunkDecorsModified(dirty);
            }
        }
    }
}
