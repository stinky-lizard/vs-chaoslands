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
using Vintagestory.API.Util;

namespace ChaosLands
{
    public class CaveBlock
    {
        AssetLocation Code;
        int Id;
        Dictionary<int, int> RockStrataVariants;
        bool RockDependent;

        public CaveBlock(ICoreServerAPI api, RockStrataConfig rockstrata, AssetLocation code)
        {
            if (code == null) return;

            Code = code;

            if (Code.Path.Contains("{rocktype}"))
            {
                RockDependent = true;
                RockStrataVariants = new Dictionary<int, int>();

                for (int i = 0; i < rockstrata.Variants.Length; i++)
                {
                    if (rockstrata.Variants[i].IsDeposit) continue;

                    string rocktype = rockstrata.Variants[i].BlockCode.Path.Split('-')[1];

                    Block rockBlock = api.World.GetBlock(rockstrata.Variants[i].BlockCode);
                    Block rocktypedBlock = api.World.GetBlock(code.CopyWithPath(code.Path.Replace("{rocktype}", rocktype)));
                    if (rockBlock != null && rocktypedBlock != null)
                    {
                        RockStrataVariants[rockBlock.BlockId] = rocktypedBlock.BlockId;
                    }
                }
            }
            else
            {
                Block desiredBlock = api.World.GetBlock(code);

                if (desiredBlock == null) return;
                
                Id = desiredBlock.BlockId;
            }
        }

        public int GetBlockId(int prevBlock)
        {
            if (RockDependent)
            {
                int variantId;

                if (RockStrataVariants.TryGetValue(prevBlock, out variantId)) return variantId; else return 0;
            }

            return Id;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class CaveBiome
    {
        //Thresholds
        [JsonProperty]
        public string Code;
        [JsonProperty]
        public float MinY = 0;
        [JsonProperty]
        public float MaxY = 0.42f;
        [JsonProperty]
        public float MinRain = 0;
        [JsonProperty]
        public float MaxRain = 1;
        [JsonProperty]
        public float MinTemp = -40;
        [JsonProperty]
        public float MaxTemp = 60;
        [JsonProperty]
        public bool GroundBlockIgnore = false;
        [JsonProperty]
        public bool CeilingBlockIgnore = false;
        //[JsonProperty]
        //public bool InteriorBlockIgnore = false;
        [JsonProperty]
        public bool InsideBlocksIgnore = false;

        //Blocks
        [JsonProperty]
        public string[] HangingBlocksJ = new string[0];
        [JsonProperty]
        public float HangingBlockChance = 0;
        [JsonProperty]
        public string[] FloorBlocksJ = new string[0];
        [JsonProperty]
        public float FloorBlockChance = 0;
        [JsonProperty]
        public string[] GroundBlocksJ = new string[0];
        [JsonProperty]
        public string[] CeilingBlocksJ = new string[0];
        [JsonProperty]
        public AssetLocation InteriorBlock = new AssetLocation("game:air");
        [JsonProperty]
        public Dictionary<string, float> Atmosphere;
        [JsonProperty]
        public CaveBiome[] Variants;

        //Runtime fields
        public CaveBlock airBlock;
        public CaveBlock[] hangingBlocks;
        public CaveBlock[] floorBlocks;
        public CaveBlock[] ceilingBlocks;
        public CaveBlock[] groundBlocks;

        public void Init(ICoreServerAPI api, RockStrataConfig rockstrata)
        {
            airBlock = new CaveBlock(api, rockstrata, InteriorBlock);
            AssetLocation[] fb = AssetLocation.toLocations(FloorBlocksJ);
            AssetLocation[] cb = AssetLocation.toLocations(CeilingBlocksJ);
            AssetLocation[] gb = AssetLocation.toLocations(GroundBlocksJ);
            AssetLocation[] hb = AssetLocation.toLocations(HangingBlocksJ);
            List<CaveBlock> ceiling = new List<CaveBlock>();
            List<CaveBlock> floor = new List<CaveBlock>();
            List<CaveBlock> ground = new List<CaveBlock>();
            List<CaveBlock> hanging = new List<CaveBlock>();

            if (cb != null && cb.Length > 0)
            {
                for (int i = 0; i < cb.Length; i++)
                {
                    ceiling.Add(new CaveBlock(api, rockstrata, cb[i]));
                }

                ceilingBlocks = ceiling.ToArray();
            }

            if (fb != null && fb.Length > 0)
            {
                for (int i = 0; i < fb.Length; i++)
                {
                    floor.Add(new CaveBlock(api, rockstrata, fb[i]));
                }

                floorBlocks = floor.ToArray();
            }

            if (gb != null && gb.Length > 0)
            {
                for (int i = 0; i < gb.Length; i++)
                {
                    ground.Add(new CaveBlock(api, rockstrata, gb[i]));
                }

                groundBlocks = ground.ToArray();
            }

            if (hb != null && hb.Length > 0)
            {
                for (int i = 0; i < hb.Length; i++)
                {
                    hanging.Add(new CaveBlock(api, rockstrata, hb[i]));
                }

                hangingBlocks = hanging.ToArray();
            }

            if (Variants != null && Variants.Length > 0)
            {
                for (int i = 0; i < Variants.Length; i++) Variants[i].Init(api, rockstrata);
            }
        }

        public CaveBiome GetBiomeOrVariant(float temp, float rain, float y)
        {
            if (Variants != null && Variants.Length > 0)
            {
                for (int i = 0; i < Variants.Length; i++)
                {
                    if (Variants[i].ValidConds(temp, rain, y)) return Variants[i];
                }
            }

            if (ValidConds(temp, rain, y)) return this;

            return null;
        }

        public bool ValidConds(float temp, float rain, float y)
        {
            return temp >= MinTemp && temp <= MaxTemp && rain >= MinRain && rain <= MaxRain && y >= MinY && y <= MaxY;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class CaveBiomesConfig
    {
        [JsonProperty]
        public bool Enabled;

        [JsonProperty]
        public CaveBiome[] Biomes;

        public void Init(ICoreServerAPI api, RockStrataConfig rockstrata)
        {
            foreach (CaveBiome biome in Biomes)
            {
                biome.Init(api, rockstrata);
            }
        }
    }

    public class GenCaveBiomes : ModStdWorldGen
    {
        private ICoreServerAPI api;
        RockStrataVariant dummyRock;
        LCGRandom rnd;
        CaveBiomesConfig caveBiomesConfig;
        int mapheight;
        SimplexNoise distort2dx;
        SimplexNoise distort2dz;
        IWorldGenBlockAccessor worldgenBlockAccessor;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override double ExecuteOrder()
        {
            return 0.41;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            return;
            if (LandsOfChaosConfig.Loaded.NoiseCavesEnabled && DoDecorationPass)
            {
                this.api.Event.InitWorldGenerator(InitWorldGen, "standard");
                this.api.Event.ChunkColumnGeneration(this.OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
                api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
            }

            dummyRock = new RockStrataVariant() { SoilpH = 6.5f, WeatheringFactor = 1f };

            distort2dx = new SimplexNoise(new double[] { 14, 9, 6, 3 }, new double[] { 1 / 100.0, 1 / 50.0, 1 / 25.0, 1 / 12.5 }, api.World.SeaLevel + 20980);
            distort2dz = new SimplexNoise(new double[] { 14, 9, 6, 3 }, new double[] { 1 / 100.0, 1 / 50.0, 1 / 25.0, 1 / 12.5 }, api.World.SeaLevel + 20981);
        }

        private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
        {
            worldgenBlockAccessor = chunkProvider.GetBlockAccessor(false);
        }

        public void InitWorldGen()
        {
            LoadGlobalConfig(api);

            IAsset asset = api.Assets.Get("game:worldgen/rockstrata.json");
            RockStrataConfig rockstrata = asset.ToObject<RockStrataConfig>();

            asset = api.Assets.Get("chaoslands:worldgen/cavebiomes.json");
            caveBiomesConfig = asset.ToObject<CaveBiomesConfig>();
            caveBiomesConfig.Init(api, rockstrata);

            rnd = new LCGRandom(api.WorldManager.Seed);

            mapheight = api.WorldManager.MapSizeY;
        }

        private void OnChunkColumnGeneration(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            rnd.InitPositionSeed(chunkX, chunkZ);

            //Get the marked caves
            bool[][] caveMarked = new bool[chunks.Length][];
            bool[] caveChunks = new bool[chunks.Length];
            for (int i = 0; i < caveMarked.Length; i++)
            {
                try
                {
                    byte[] data = chunks[i].GetServerModdata("noiseCaves");
                    byte[] chunkData = chunks[i].GetServerModdata("noiseCavesChunks");
                    caveMarked[i] = SerializerUtil.Deserialize<bool[]>(data);
                    caveChunks[i] = SerializerUtil.Deserialize<bool>(chunkData);
                }
                catch
                {
                    caveMarked[i] = new bool[chunks[0].Blocks.Length];
                }
            }

            //Setup gas chunks
            Dictionary<int, Dictionary<string, float>>[] chunkGas = new Dictionary<int, Dictionary<string, float>>[chunks.Length];
            for (int i = 0; i < chunkGas.Length; i++)
            {
                chunkGas[i] = new Dictionary<int, Dictionary<string, float>>();
            }

            IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;

            int regionChunkSize = api.WorldManager.RegionSize / chunksize;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;

            // Amount of data points per chunk
            float climateStep = (float)climateMap.InnerSize / regionChunkSize;
            int posY = api.World.SeaLevel;
            //int skipped = 0;

            for (int x = 0; x < chunksize; x++)
            {
                for (int z = 0; z < chunksize; z++)
                {
                    float distx = (float)distort2dx.Noise(chunkX * chunksize + x, chunkZ * chunksize + z);
                    float distz = (float)distort2dz.Noise(chunkX * chunksize + x, chunkZ * chunksize + z);

                    double posRand = (double)GameMath.MurmurHash3(x + chunkX * chunksize, 1, z + chunkZ * chunksize) / int.MaxValue;
                    double transitionRand = (posRand + 1);

                    int climate = climateMap.GetUnpaddedColorLerped(
                        rdx * climateStep + climateStep * (float)(x + distx) / chunksize,
                        rdz * climateStep + climateStep * (float)(z + distz) / chunksize
                    );

                    int tempUnscaled = (climate >> 16) & 0xff;
                    int rndnum = (int)(distx / 5);
                    float temp = TerraGenConfig.GetScaledAdjustedTemperatureFloat(tempUnscaled, 0);
                    
                    float rainRel = TerraGenConfig.GetRainFall((climate >> 8) & 0xff, posY + rndnum) / 255f;

                    //if (chunkX == 15625 && chunkZ == 15625 && x == 0 && z == 0) System.Diagnostics.Debug.WriteLine(temp + "|" + rainRel);

                    for (int cy = 0; cy < chunks.Length; cy++)
                    {
                        if (!caveChunks[cy]) 
                        { 
                            //skipped++;  
                            continue; 
                        }


                        for (int ry = 1; ry < chunksize; ry++)
                        {
                            int y = ry + (cy * chunksize);
                            int chunkIndex = y / 32;
                            int picked = ChunkIndex3d(x, y % chunksize, z);
                            if (!caveMarked[chunkIndex][picked]) continue;

                            CaveBiome biome = null;

                            for (int i = 0; i < caveBiomesConfig.Biomes.Length; i++)
                            {
                                biome = caveBiomesConfig.Biomes[i].GetBiomeOrVariant(temp, rainRel, (float)y / (float)mapheight);
                                if (biome != null) break;
                            }

                            if (biome == null) continue;

                            Dictionary<string, float> atmosphere = (biome.Atmosphere != null && biome.Atmosphere.Count > 0) ? biome.Atmosphere : null;

                            if (atmosphere != null) chunkGas[chunkIndex][picked] = atmosphere;

                            bool hasGround = !GetCave(caveMarked, x, y - 1, z);
                            bool hasCeiling = !GetCave(caveMarked, x, y + 1, z);


                            if (chunks[chunkIndex].Blocks[picked] == 0 || biome.InsideBlocksIgnore)
                            {
                                if (hasGround && (GetBlock(chunks, x, y - 1, z) != 0 || biome.InsideBlocksIgnore) && biome.floorBlocks != null && biome.floorBlocks.Length > 0 && rnd.NextFloat() <= biome.FloorBlockChance)
                                {
                                    //First try to place floor block
                                    SetBlock(chunks, x, y, z, biome.floorBlocks[rnd.NextInt(biome.floorBlocks.Length)].GetBlockId(GetBlock(chunks, x, y, z)));
                                }
                                else if (hasCeiling && (GetBlock(chunks, x, y + 1, z) != 0 || biome.InsideBlocksIgnore) && biome.hangingBlocks != null && biome.hangingBlocks.Length > 0 && rnd.NextFloat() <= biome.HangingBlockChance)
                                {
                                    //Second, try to place hanging block
                                    SetBlock(chunks, x, y, z, biome.hangingBlocks[rnd.NextInt(biome.hangingBlocks.Length)].GetBlockId(GetBlock(chunks, x, y, z)));
                                }
                                else if (biome.airBlock != null)
                                {
                                    //Finally try interior block
                                    SetBlock(chunks, x, y, z, biome.airBlock.GetBlockId(GetBlock(chunks, x, y, z)));
                                }
                            }

                            //Try placing the ground blocks
                            if (hasGround && biome.groundBlocks != null && biome.groundBlocks.Length > 0)
                            {
                                for (int i = 1; i < biome.groundBlocks.Length + 1; i++)
                                {
                                    int gY = y - i;

                                    if (gY < 1) break;

                                    Block atPos = worldgenBlockAccessor.GetBlock(GetBlock(chunks, x, gY, z));

                                    if ((atPos.BlockMaterial == EnumBlockMaterial.Stone || biome.GroundBlockIgnore) && !GetCave(caveMarked, x, gY, z))
                                    {
                                        if (atmosphere != null) chunkGas[gY / chunksize][ChunkIndex3d(x, gY % chunksize, z)] = atmosphere;
                                        SetBlock(chunks, x, gY, z, biome.groundBlocks[i - 1].GetBlockId(atPos.BlockId));
                                    }
                                }
                            }

                            //Try placing the ceiling blocks
                            if (hasCeiling && biome.ceilingBlocks != null && biome.ceilingBlocks.Length > 0)
                            {
                                for (int i = 1; i < biome.ceilingBlocks.Length + 1; i++)
                                {
                                    int gY = y + i;

                                    if (gY > mapheight) break;

                                    Block atPos = worldgenBlockAccessor.GetBlock(GetBlock(chunks, x, gY, z));

                                    if ((atPos.BlockMaterial == EnumBlockMaterial.Stone || biome.CeilingBlockIgnore) && !GetCave(caveMarked, x, gY, z))
                                    {
                                        if (atmosphere != null) chunkGas[gY / chunksize][ChunkIndex3d(x, gY % chunksize, z)] = atmosphere;
                                        SetBlock(chunks, x, gY, z, biome.ceilingBlocks[i - 1].GetBlockId(atPos.BlockId));
                                    }
                                }
                            }
                        }

                    }

                }
            }

            //System.Diagnostics.Debug.WriteLine("Chunks Skipped = " + (skipped / (chunksize*chunksize)));

            for (int i = 0; i < chunkGas.Length; i++)
            {
                chunks[i].SetModdata("gases", SerializerUtil.Serialize<Dictionary<int, Dictionary<string, float>>>(chunkGas[i]));
            }
        }

        private int ChunkIndex3d(int x, int y, int z)
        {
            return (y * chunksize + z) * chunksize + x;
        }

        private void SetBlock(IServerChunk[] chunks, int x, int posY, int z, int blockId)
        {
            chunks[posY / chunksize].Blocks[ChunkIndex3d(x, posY % chunksize, z)] = blockId;
        }

        private int GetBlock(IServerChunk[] chunks, int x, int posY, int z)
        {
            return chunks[posY / chunksize].Blocks[ChunkIndex3d(x, posY % chunksize, z)];
        }

        private bool GetCave(bool[][] caves, int x, int posY, int z)
        {
            if (posY > mapheight || posY < 1) return false;
            return caves[posY / chunksize][ChunkIndex3d(x, posY % chunksize, z)];
        }
    }
}
