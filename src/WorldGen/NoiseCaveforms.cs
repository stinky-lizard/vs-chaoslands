using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace ChaosLands
{
    class NoiseCaveforms : NoiseBase
    {
        // (Be aware, static vars never get unloaded even when singleplayer server has been shut down)
        public static LandformsWorldProperty landforms;

        public float scale;

        public NoiseCaveforms(long seed, ICoreServerAPI api, float scale) : base(seed)
        {
            LoadLandforms(api);
            this.scale = scale;
        }

        public static void ReloadLandforms(ICoreServerAPI api)
        {
            api.Assets.Reload(new AssetLocation("chaoslands:worldgen/"));
            LoadLandforms(api);
        }

        public static void LoadLandforms(ICoreServerAPI api)
        {
            IAsset asset = api.Assets.Get("chaoslands:worldgen/caveforms.json");
            landforms = asset.ToObject<LandformsWorldProperty>();

            int quantityMutations = 0;

            for (int i = 0; i < landforms.Variants.Length; i++)
            {
                LandformVariant variant = landforms.Variants[i];
                variant.index = i;
                variant.Init(api.WorldManager, i);

                if (variant.Mutations != null)
                {
                    quantityMutations += variant.Mutations.Length;
                }
            }

            landforms.LandFormsByIndex = new LandformVariant[quantityMutations + landforms.Variants.Length];

            // Mutations get indices after the parent ones
            for (int i = 0; i < landforms.Variants.Length; i++)
            {
                landforms.LandFormsByIndex[i] = landforms.Variants[i];
            }

            int nextIndex = landforms.Variants.Length;
            for (int i = 0; i < landforms.Variants.Length; i++)
            {
                LandformVariant variant = landforms.Variants[i];
                if (variant.Mutations != null)
                {
                    for (int j = 0; j < variant.Mutations.Length; j++)
                    {
                        LandformVariant variantMut = variant.Mutations[j];

                        if (variantMut.TerrainOctaves == null)
                        {
                            variantMut.TerrainOctaves = variant.TerrainOctaves;
                        }
                        if (variantMut.TerrainOctaveThresholds == null)
                        {
                            variantMut.TerrainOctaveThresholds = variant.TerrainOctaveThresholds;
                        }
                        if (variantMut.TerrainYKeyPositions == null)
                        {
                            variantMut.TerrainYKeyPositions = variant.TerrainYKeyPositions;
                        }
                        if (variantMut.TerrainYKeyThresholds == null)
                        {
                            variantMut.TerrainYKeyThresholds = variant.TerrainYKeyThresholds;
                        }


                        landforms.LandFormsByIndex[nextIndex] = variantMut;
                        variantMut.Init(api.WorldManager, nextIndex);
                        nextIndex++;
                    }
                }
            }
        }

        public int GetLandformIndexAt(int unscaledXpos, int unscaledZpos, int temp, int rain)
        {
            float xpos = (float)unscaledXpos / scale;
            float zpos = (float)unscaledZpos / scale;

            int xposInt = (int)xpos;
            int zposInt = (int)zpos;

            int parentIndex = GetParentLandformIndexAt(xposInt, zposInt, temp, rain);

            LandformVariant[] mutations = landforms.Variants[parentIndex].Mutations;
            if (mutations != null && mutations.Length > 0)
            {
                InitPositionSeed(unscaledXpos, unscaledZpos);
                float chance = NextInt(101) / 100f;

                for (int i = 0; i < mutations.Length; i++)
                {
                    LandformVariant variantMut = mutations[i];

                    if (variantMut.UseClimateMap)
                    {
                        int distRain = rain - GameMath.Clamp(rain, landforms.Variants[i].MinRain, landforms.Variants[i].MaxRain);
                        double distTemp = temp - GameMath.Clamp(temp, landforms.Variants[i].MinTemp, landforms.Variants[i].MaxTemp);
                        if (distRain > 0 || distTemp > 0) continue;
                    }


                    chance -= mutations[i].Chance;
                    if (chance <= 0)
                    {
                        return mutations[i].index;
                    }
                }
            }

            return parentIndex;
        }


        public int GetParentLandformIndexAt(int xpos, int zpos, int temp, int rain)
        {
            InitPositionSeed(xpos, zpos);

            double weightSum = 0;
            int i;
            for (i = 0; i < landforms.Variants.Length; i++)
            {
                double weight = landforms.Variants[i].Weight;

                if (landforms.Variants[i].UseClimateMap)
                {
                    int distRain = rain - GameMath.Clamp(rain, landforms.Variants[i].MinRain, landforms.Variants[i].MaxRain);
                    double distTemp = temp - GameMath.Clamp(temp, landforms.Variants[i].MinTemp, landforms.Variants[i].MaxTemp);
                    if (distRain > 0 || distTemp > 0) weight = 0;
                }

                landforms.Variants[i].WeightTmp = weight;
                weightSum += weight;
            }

            double rand = weightSum * NextInt(10000) / 10000.0;

            for (i = 0; i < landforms.Variants.Length; i++)
            {
                rand -= landforms.Variants[i].WeightTmp;
                if (rand <= 0) return landforms.Variants[i].index;
            }

            return landforms.Variants[i].index;
        }


    }

    class MapLayerCaveforms : MapLayerBase
    {
        private NoiseCaveforms noiseCaveforms;
        NoiseClimate climateNoise;

        NormalizedSimplexNoise noisegenX;
        NormalizedSimplexNoise noisegenY;
        float wobbleIntensity;

        public float landFormHorizontalScale = 1f;

        public MapLayerCaveforms(long seed, NoiseClimate climateNoise, ICoreServerAPI api) : base(seed)
        {
            this.climateNoise = climateNoise;

            float scale = TerraGenConfig.landformMapScale;

            if (GameVersion.IsAtLeastVersion(api.WorldManager.SaveGame.CreatedGameVersion, "1.11.0-dev.1"))
            {
                scale *= Math.Max(1, api.WorldManager.MapSizeY / 256f);
            }

            noiseCaveforms = new NoiseCaveforms(seed, api, scale);

            int woctaves = 2;
            float wscale = 2f * TerraGenConfig.landformMapScale;
            float wpersistence = 0.9f;
            wobbleIntensity = TerraGenConfig.landformMapScale * 1.5f;
            noisegenX = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1 / wscale, wpersistence, seed + 2);
            noisegenY = NormalizedSimplexNoise.FromDefaultOctaves(woctaves, 1 / wscale, wpersistence, seed + 1231296);
        }


        public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
        {
            int[] result = new int[sizeX * sizeZ];

            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    int offsetX = (int)(wobbleIntensity * noisegenX.Noise(xCoord + x, zCoord + z));
                    int offsetY = (int)(wobbleIntensity * noisegenY.Noise(xCoord + x, zCoord + z));

                    int finalX = (xCoord + x + offsetX);
                    int finalZ = (zCoord + z + offsetY);

                    int climate = climateNoise.GetLerpedClimateAt(finalX / TerraGenConfig.climateMapScale, finalZ / TerraGenConfig.climateMapScale);
                    int rain = (climate >> 8) & 0xff;
                    int temp = TerraGenConfig.GetScaledAdjustedTemperature((climate >> 16) & 0xff, 0);

                    result[z * sizeX + x] = noiseCaveforms.GetLandformIndexAt(
                        finalX,
                        finalZ,
                        temp,
                        rain
                    );
                }
            }

            return result;
        }



    }

    public class GenCaveMaps : ModSystem
    {
        ICoreServerAPI sapi;

        MapLayerBase caveformsGen;

        int noiseSizeCaveform;

        LatitudeData latdata = new LatitudeData();

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;

            api.Event.InitWorldGenerator(initWorldGen, "standard");
            api.Event.InitWorldGenerator(initWorldGen, "superflat");

            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.MapRegionGeneration(OnMapRegionGen, "superflat");
        }


        public void initWorldGen()
        {
            long seed = sapi.WorldManager.Seed;
            noiseSizeCaveform = sapi.WorldManager.RegionSize / TerraGenConfig.landformMapScale;

            ITreeAttribute worldConfig = sapi.WorldManager.SaveGame.WorldConfiguration;
            string climate = worldConfig.GetString("worldClimate", "realistic");
            NoiseClimate noiseClimate;

            float tempModifier = worldConfig.GetString("globalTemperature", "1").ToFloat(1);
            float rainModifier = worldConfig.GetString("globalPrecipitation", "1").ToFloat(1);
            latdata.polarEquatorDistance = worldConfig.GetString("polarEquatorDistance", "50000").ToInt(50000);

            switch (climate)
            {
                case "realistic":
                    int spawnMinTemp = 6;
                    int spawnMaxTemp = 14;

                    string startingClimate = worldConfig.GetString("startingClimate");
                    switch (startingClimate)
                    {
                        case "hot":
                            spawnMinTemp = 28;
                            spawnMaxTemp = 32;
                            break;
                        case "warm":
                            spawnMinTemp = 19;
                            spawnMaxTemp = 23;
                            break;
                        case "cool":
                            spawnMinTemp = -5;
                            spawnMaxTemp = 1;
                            break;
                        case "icy":
                            spawnMinTemp = -15;
                            spawnMaxTemp = -10;
                            break;
                    }

                    noiseClimate = new NoiseClimateRealistic(seed, (double)sapi.WorldManager.MapSizeZ / TerraGenConfig.climateMapScale / TerraGenConfig.climateMapSubScale, latdata.polarEquatorDistance, spawnMinTemp, spawnMaxTemp);
                    latdata.isRealisticClimate = true;
                    latdata.ZOffset = (noiseClimate as NoiseClimateRealistic).ZOffset;
                    break;

                default:
                    noiseClimate = new NoiseClimatePatchy(seed);
                    break;
            }

            noiseClimate.rainMul = rainModifier;
            noiseClimate.tempMul = tempModifier;

            caveformsGen = GetCaveformMapGen(seed + 4, noiseClimate, sapi);
        }

        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
        {
            mapRegion.ModMaps.Add("caveforms", new IntDataMap2D());
            int pad = TerraGenConfig.landformMapPadding;
            mapRegion.ModMaps["caveforms"].Data = caveformsGen.GenLayer(regionX * noiseSizeCaveform - pad, regionZ * noiseSizeCaveform - pad, noiseSizeCaveform + 2 * pad, noiseSizeCaveform + 2 * pad);
            mapRegion.ModMaps["caveforms"].Size = noiseSizeCaveform + 2 * pad;
            mapRegion.ModMaps["caveforms"].TopLeftPadding = mapRegion.ModMaps["caveforms"].BottomRightPadding = pad;


            mapRegion.DirtyForSaving = true;
        }

        public static MapLayerBase GetCaveformMapGen(long seed, NoiseClimate climateNoise, ICoreServerAPI api)
        {
            MapLayerBase caveforms = new MapLayerCaveforms(seed + 12, climateNoise, api);
            caveforms.DebugDrawBitmap(DebugDrawMode.LandformRGB, 0, 0, "LCaveforms 1 - Wobble Landforms");

            return caveforms;
        }

    }
}
