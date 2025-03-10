﻿using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;
using Vintagestory.API.MathTools;
using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace ChaosLands
{
    public class GenCaveTerra : ModStdWorldGen
    {
        ICoreServerAPI api;
        int regionMapSize;

        private NormalizedSimplexNoise TerrainNoise;

        LandformsWorldProperty caveforms;
        Dictionary<int, LerpedWeightedIndex2DMap> LandformMapByRegion = new Dictionary<int, LerpedWeightedIndex2DMap>(10);

        SimplexNoise distort2dx;
        SimplexNoise distort2dz;

        int lerpHor;
        int lerpVer;
        int noiseWidth;
        int paddedNoiseWidth;
        int paddedNoiseHeight;
        int noiseHeight;
        float lerpDeltaHor;
        float lerpDeltaVert;

        double[] noiseTemp;
        float horizontalScale;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }


        public override double ExecuteOrder()
        {
            return 0.05;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            if (!LandsOfChaosConfig.Loaded.NoiseCavesEnabled) return;
            this.api = api;
            //test
            
            //DoDecorationPass = false;
            api.Event.InitWorldGenerator(initWorldGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Terrain, "standard");
        }



        public void initWorldGen()
        {
            NoiseCaveforms.LoadLandforms(api);
            LoadGlobalConfig(api);
            LandformMapByRegion.Clear();

            chunksize = api.WorldManager.ChunkSize;

            // Amount of landform regions in all of the map 
            // Until v1.12.9 this calculation was incorrect
            if (GameVersion.IsAtLeastVersion(api.WorldManager.SaveGame.CreatedGameVersion, "1.12.9"))
            {
                regionMapSize = (int)Math.Ceiling((double)api.WorldManager.MapSizeX / api.WorldManager.RegionSize);
            }
            else
            {
                regionMapSize = api.WorldManager.MapSizeX / api.WorldManager.RegionSize;
            }


            // Starting from v1.11 the world height also horizontally scales the world
            horizontalScale = 1f;
            if (GameVersion.IsAtLeastVersion(api.WorldManager.SaveGame.CreatedGameVersion, "1.11.0-dev.1"))
            {
                horizontalScale = Math.Max(1, api.WorldManager.MapSizeY / 256f);
            }
            TerrainNoise = NormalizedSimplexNoise.FromDefaultOctaves(
                TerraGenConfig.terrainGenOctaves, 0.002 / horizontalScale, 0.9, api.WorldManager.Seed
            );

            // We generate the whole terrain here so we instantly know the heightmap
            lerpHor = TerraGenConfig.lerpHorizontal;
            //test
            lerpVer = 1;


            noiseWidth = chunksize / lerpHor;
            noiseHeight = api.WorldManager.MapSizeY / lerpVer;

            paddedNoiseWidth = noiseWidth + 1;
            paddedNoiseHeight = noiseHeight + 1;

            lerpDeltaHor = 1f / lerpHor;
            lerpDeltaVert = 1f / lerpVer;

            noiseTemp = new double[paddedNoiseWidth * paddedNoiseWidth * paddedNoiseHeight];

            if (GameVersion.IsAtLeastVersion(api.WorldManager.SaveGame.CreatedGameVersion, "1.12.0-dev.1"))
            {
                distort2dx = new SimplexNoise(new double[] { 55, 40, 30, 10 }, new double[] { 1 / 500.0, 1 / 250.0, 1 / 125.0, 1 / 65 }, api.World.Seed + 9876 + 0);
                distort2dz = new SimplexNoise(new double[] { 55, 40, 30, 10 }, new double[] { 1 / 500.0, 1 / 250.0, 1 / 125.0, 1 / 65 }, api.World.Seed + 9877 + 0);
            }
            else
            {
                distort2dx = new SimplexNoise(new double[] { 55, 40, 30, 10 }, new double[] { 1 / 500.0, 1 / 250.0, 1 / 125.0, 1 / 65 }, api.World.SeaLevel + 9876 + 0);
                distort2dz = new SimplexNoise(new double[] { 55, 40, 30, 10 }, new double[] { 1 / 500.0, 1 / 250.0, 1 / 125.0, 1 / 65 }, api.World.SeaLevel + 9877 + 0);
            }
        }






        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            caveforms = NoiseCaveforms.landforms;
            IMapChunk mapchunk = chunks[0].MapChunk;
            bool[][] caveMarked = new bool[chunks.Length][];
            bool[] caveChunksMarked = new bool[chunks.Length];
            for (int i = 0; i < caveMarked.Length; i++) caveMarked[i] = new bool[chunks[0].Blocks.Length];

            int climateUpLeft;
            int climateUpRight;
            int climateBotLeft;
            int climateBotRight;

            IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
            int regionChunkSize = api.WorldManager.RegionSize / chunksize;
            float fac = (float)climateMap.InnerSize / regionChunkSize;
            int rlX = chunkX % regionChunkSize;
            int rlZ = chunkZ % regionChunkSize;

            climateUpLeft = climateMap.GetUnpaddedInt((int)(rlX * fac), (int)(rlZ * fac));
            climateUpRight = climateMap.GetUnpaddedInt((int)(rlX * fac + fac), (int)(rlZ * fac));
            climateBotLeft = climateMap.GetUnpaddedInt((int)(rlX * fac), (int)(rlZ * fac + fac));
            climateBotRight = climateMap.GetUnpaddedInt((int)(rlX * fac + fac), (int)(rlZ * fac + fac));

            //int freezingTemp = -17;


            IntDataMap2D landformMap = mapchunk.MapRegion.LandformMap;
            // Amount of pixels for each chunk (probably 1, 2, or 4) in the land form map
            float chunkPixelSize = landformMap.InnerSize / regionChunkSize;
            // Adjusted lerp for the noiseWidth
            float chunkPixelStep = chunkPixelSize / noiseWidth;
            // Start coordinates for the chunk in the region map
            float baseX = (chunkX % regionChunkSize) * chunkPixelSize;
            float baseZ = (chunkZ % regionChunkSize) * chunkPixelSize;


            LerpedWeightedIndex2DMap landLerpMap = GetOrLoadLerpedLandformMap(chunks[0].MapChunk, chunkX / regionChunkSize, chunkZ / regionChunkSize);

            // Terrain octaves
            double[] octNoiseX0, octNoiseX1, octNoiseX2, octNoiseX3;
            double[] octThX0, octThX1, octThX2, octThX3;

            // So it seems we have some kind of off-by-one error here? 
            // When the slope of a mountain goes up (in positive z or x direction), particularly at large word heights (512+)
            // then the last blocks (again in postive x/z dir) are below of where they should be?
            // I have no idea why, but this offset seems to greatly mitigate the issue
            float weirdOffset = 0.25f;
            chunkPixelSize += weirdOffset;

            GetInterpolatedOctaves(landLerpMap[baseX, baseZ], out octNoiseX0, out octThX0);
            GetInterpolatedOctaves(landLerpMap[baseX + chunkPixelSize, baseZ], out octNoiseX1, out octThX1);
            GetInterpolatedOctaves(landLerpMap[baseX, baseZ + chunkPixelSize], out octNoiseX2, out octThX2);
            GetInterpolatedOctaves(landLerpMap[baseX + chunkPixelSize, baseZ + chunkPixelSize], out octNoiseX3, out octThX3);


            double[] terrainNoise3d = GetTerrainNoise3D(octNoiseX0, octNoiseX1, octNoiseX2, octNoiseX3, octThX0, octThX1, octThX2, octThX3, chunkX * noiseWidth, 0, chunkZ * noiseWidth);

            // Store heightmap in the map chunk
            ushort[] rainheightmap = chunks[0].MapChunk.RainHeightMap;
            ushort[] terrainheightmap = chunks[0].MapChunk.WorldGenTerrainHeightMap;


            // Terrain thresholds
            double tnoiseY0;
            double tnoiseY1;
            double tnoiseY2;
            double tnoiseY3;
            double tnoiseGainY0;
            double tnoiseGainY1;
            double tnoiseGainY2;
            double tnoiseGainY3;


            double thNoiseX0;
            double thNoiseX1;
            double thNoiseGainX0;
            double thNoiseGainX1;
            double thNoiseGainZ0;
            double thNoiseZ0;

            float[] terrainThresholdsX0 = new float[api.WorldManager.MapSizeY];
            float[] terrainThresholdsX1 = new float[api.WorldManager.MapSizeY];
            float[] terrainThresholdsX2 = new float[api.WorldManager.MapSizeY];
            float[] terrainThresholdsX3 = new float[api.WorldManager.MapSizeY];



            for (int xN = 0; xN < noiseWidth; xN++)
            {
                for (int zN = 0; zN < noiseWidth; zN++)
                {
                    // Landform thresholds
                    LoadInterpolatedThresholds(landLerpMap[baseX + xN * chunkPixelStep, baseZ + zN * chunkPixelStep], terrainThresholdsX0);
                    LoadInterpolatedThresholds(landLerpMap[baseX + (xN + 1) * chunkPixelStep, baseZ + zN * chunkPixelStep], terrainThresholdsX1);
                    LoadInterpolatedThresholds(landLerpMap[baseX + xN * chunkPixelStep, baseZ + (zN + 1) * chunkPixelStep], terrainThresholdsX2);
                    LoadInterpolatedThresholds(landLerpMap[baseX + (xN + 1) * chunkPixelStep, baseZ + (zN + 1) * chunkPixelStep], terrainThresholdsX3);

                    for (int yN = noiseHeight - 1; yN >= 0; yN--)
                    {
                        // Terrain noise
                        tnoiseY0 = terrainNoise3d[NoiseIndex3d(xN, yN, zN)];
                        tnoiseY1 = terrainNoise3d[NoiseIndex3d(xN, yN, zN + 1)];
                        tnoiseY2 = terrainNoise3d[NoiseIndex3d(xN + 1, yN, zN)];
                        tnoiseY3 = terrainNoise3d[NoiseIndex3d(xN + 1, yN, zN + 1)];

                        tnoiseGainY0 = (terrainNoise3d[NoiseIndex3d(xN, yN + 1, zN)] - tnoiseY0) * lerpDeltaVert;
                        tnoiseGainY1 = (terrainNoise3d[NoiseIndex3d(xN, yN + 1, zN + 1)] - tnoiseY1) * lerpDeltaVert;
                        tnoiseGainY2 = (terrainNoise3d[NoiseIndex3d(xN + 1, yN + 1, zN)] - tnoiseY2) * lerpDeltaVert;
                        tnoiseGainY3 = (terrainNoise3d[NoiseIndex3d(xN + 1, yN + 1, zN + 1)] - tnoiseY3) * lerpDeltaVert;


                        for (int y = lerpVer - 1; y >= 0; y--)
                        {
                            int posY = yN * lerpVer + y;
                            int chunkY = posY / chunksize;
                            int localY = posY % chunksize;

                            // For Terrain noise 
                            double tnoiseX0 = tnoiseY0;
                            double tnoiseX1 = tnoiseY1;

                            double tnoiseGainX0 = (tnoiseY2 - tnoiseY0) * lerpDeltaHor;
                            double tnoiseGainX1 = (tnoiseY3 - tnoiseY1) * lerpDeltaHor;

                            // Landform thresholds lerp
                            thNoiseX0 = terrainThresholdsX0[posY];
                            thNoiseX1 = terrainThresholdsX2[posY];

                            thNoiseGainX0 = (terrainThresholdsX1[posY] - thNoiseX0) * lerpDeltaHor;
                            thNoiseGainX1 = (terrainThresholdsX3[posY] - thNoiseX1) * lerpDeltaHor;

                            for (int x = 0; x < lerpHor; x++)
                            {
                                // For terrain noise
                                double tnoiseZ0 = tnoiseX0;
                                double tnoiseGainZ0 = (tnoiseX1 - tnoiseX0) * lerpDeltaHor;

                                // Landform
                                thNoiseZ0 = thNoiseX0;
                                thNoiseGainZ0 = (thNoiseX1 - thNoiseX0) * lerpDeltaHor;

                                for (int z = 0; z < lerpHor; z++)
                                {
                                    int lX = xN * lerpHor + x;
                                    int lZ = zN * lerpHor + z;

                                    int mapIndex = ChunkIndex2d(lX, lZ);
                                    int chunkIndex = ChunkIndex3d(lX, localY, lZ);

                                    //chunks[chunkY].Blocks[chunkIndex] = 0;

                                    if (tnoiseZ0 > thNoiseZ0)
                                    {
                                        if (posY < TerraGenConfig.seaLevel)
                                        {
                                            if (chunks[chunkY].Blocks[ChunkIndex3d(lX, (posY + 1) % chunksize, lZ)] == GlobalConfig.waterBlockId ||
                                            chunks[chunkY].Blocks[ChunkIndex3d(lX, (posY + 2) % chunksize, lZ)] == GlobalConfig.waterBlockId) continue;

                                            int sideCheck = ChunkIndex3d(xN * lerpHor + x + 1, localY, lZ);
                                            if (sideCheck < chunks[chunkY].Blocks.Length && sideCheck >= 0 && chunks[chunkY].Blocks[sideCheck] == GlobalConfig.waterBlockId) continue;

                                            sideCheck = ChunkIndex3d(xN * lerpHor + x - 1, localY, lZ);
                                            if (sideCheck < chunks[chunkY].Blocks.Length && sideCheck >= 0 && chunks[chunkY].Blocks[sideCheck] == GlobalConfig.waterBlockId) continue;

                                            sideCheck = ChunkIndex3d(lX, localY, zN * lerpHor + z + 1);
                                            if (sideCheck < chunks[chunkY].Blocks.Length && sideCheck >= 0 && chunks[chunkY].Blocks[sideCheck] == GlobalConfig.waterBlockId) continue;

                                            sideCheck = ChunkIndex3d(lX, localY, zN * lerpHor + z - 1);
                                            if (sideCheck < chunks[chunkY].Blocks.Length && sideCheck >= 0 && chunks[chunkY].Blocks[sideCheck] == GlobalConfig.waterBlockId) continue;

                                            sideCheck = ChunkIndex3d(xN * lerpHor + x + 2, localY, lZ);
                                            if (sideCheck < chunks[chunkY].Blocks.Length && sideCheck >= 0 && chunks[chunkY].Blocks[sideCheck] == GlobalConfig.waterBlockId) continue;

                                            sideCheck = ChunkIndex3d(xN * lerpHor + x - 2, localY, lZ);
                                            if (sideCheck < chunks[chunkY].Blocks.Length && sideCheck >= 0 && chunks[chunkY].Blocks[sideCheck] == GlobalConfig.waterBlockId) continue;

                                            sideCheck = ChunkIndex3d(lX, localY, zN * lerpHor + z + 2);
                                            if (sideCheck < chunks[chunkY].Blocks.Length && sideCheck >= 0 && chunks[chunkY].Blocks[sideCheck] == GlobalConfig.waterBlockId) continue;

                                            sideCheck = ChunkIndex3d(lX, localY, zN * lerpHor + z - 2);
                                            if (sideCheck < chunks[chunkY].Blocks.Length && sideCheck >= 0 && chunks[chunkY].Blocks[sideCheck] == GlobalConfig.waterBlockId) continue;
                                        }

                                        if (posY > 0 && chunks[chunkY].Blocks[chunkIndex] == GlobalConfig.defaultRockId)
                                        {
                                            if (terrainheightmap[mapIndex] == posY)
                                            {
                                                terrainheightmap[mapIndex] = rainheightmap[mapIndex] = (ushort)(posY - 1);
                                            }
                                            chunks[chunkY].Blocks[chunkIndex] = posY < 12 ? GlobalConfig.lavaBlockId : 0;
                                            caveMarked[chunkY][chunkIndex] = true;
                                            caveChunksMarked[chunkY] = true;
                                        }
                                    }

                                    tnoiseZ0 += tnoiseGainZ0;
                                    thNoiseZ0 += thNoiseGainZ0;
                                }

                                tnoiseX0 += tnoiseGainX0;
                                tnoiseX1 += tnoiseGainX1;

                                thNoiseX0 += thNoiseGainX0;
                                thNoiseX1 += thNoiseGainX1;
                            }

                            tnoiseY0 += tnoiseGainY0;
                            tnoiseY1 += tnoiseGainY1;
                            tnoiseY2 += tnoiseGainY2;
                            tnoiseY3 += tnoiseGainY3;
                        }
                    }
                }
            }

            for (int i = 0; i < chunks.Length; i++)
            {
                byte[] caveAreas = SerializerUtil.Serialize(caveMarked[i]);
                byte[] caveChunks = SerializerUtil.Serialize(caveChunksMarked[i]);
                chunks[i].SetServerModdata("noiseCaves", caveAreas);
                chunks[i].SetServerModdata("noiseCavesChunks", caveChunks);
            }
        }



        LerpedWeightedIndex2DMap GetOrLoadLerpedLandformMap(IMapChunk mapchunk, int regionX, int regionZ)
        {
            LerpedWeightedIndex2DMap map;
            // 1. Load?
            LandformMapByRegion.TryGetValue(regionZ * regionMapSize + regionX, out map);
            if (map != null) return map;

            IntDataMap2D lmap = mapchunk.MapRegion.ModMaps["caveforms"];
            // 2. Create
            map = LandformMapByRegion[regionZ * regionMapSize + regionX]
                //Removed landform smoothing because it caused no gave gaps between landforms
                = new LerpedWeightedIndex2DMap(lmap.Data, lmap.Size, 0, lmap.TopLeftPadding, lmap.BottomRightPadding);

            return map;
        }


        // Can be called only once per x/z coordinate to get a list of all thresholds for this column
        private void LoadInterpolatedThresholds(WeightedIndex[] indices, float[] values)
        {
            for (int y = 0; y < values.Length; y++)
            {
                float threshold = 0;
                for (int i = 0; i < indices.Length; i++)
                {
                    threshold += caveforms.LandFormsByIndex[indices[i].Index].TerrainYThresholds[y] * indices[i].Weight;
                }

                values[y] = threshold;
            }
        }




        private void GetInterpolatedOctaves(WeightedIndex[] indices, out double[] amps, out double[] thresholds)
        {
            amps = new double[TerraGenConfig.terrainGenOctaves];
            thresholds = new double[TerraGenConfig.terrainGenOctaves];

            for (int octave = 0; octave < TerraGenConfig.terrainGenOctaves; octave++)
            {
                double amplitude = 0;
                double threshold = 0;
                
                for (int i = 0; i < Math.Min(caveforms.LandFormsByIndex.Length, indices.Length); i++)
                {

                    LandformVariant l = caveforms.LandFormsByIndex[indices[i].Index];
                    amplitude += l.TerrainOctaves[octave] * indices[i].Weight;
                    threshold += l.TerrainOctaveThresholds[octave] * indices[i].Weight;
                }

                amps[octave] = amplitude;
                thresholds[octave] = threshold;
            }
        }


        double[] lerpedAmps = new double[TerraGenConfig.terrainGenOctaves];
        double[] lerpedTh = new double[TerraGenConfig.terrainGenOctaves];

        double[] GetTerrainNoise3D(double[] octX0, double[] octX1, double[] octX2, double[] octX3, double[] octThX0, double[] octThX1, double[] octThX2, double[] octThX3, int xPos, int yPos, int zPos)
        {
            for (int x = 0; x < paddedNoiseWidth; x++)
            {
                for (int z = 0; z < paddedNoiseWidth; z++)
                {
                    for (int i = 0; i < TerraGenConfig.terrainGenOctaves; i++)
                    {
                        lerpedAmps[i] = GameMath.BiLerp(octX0[i], octX1[i], octX2[i], octX3[i], (double)x / paddedNoiseWidth, (double)z / paddedNoiseWidth);
                        lerpedTh[i] = GameMath.BiLerp(octThX0[i], octThX1[i], octThX2[i], octThX3[i], (double)x / paddedNoiseWidth, (double)z / paddedNoiseWidth);
                    }

                    float distx = (float)distort2dx.Noise(xPos + x, zPos + z);
                    float distz = (float)distort2dz.Noise(xPos + x, zPos + z);

                    for (int y = 0; y < paddedNoiseHeight; y++)
                    {
                        noiseTemp[NoiseIndex3d(x, y, z)] = TerrainNoise.Noise(
                            (xPos + x) + (distx > 0 ? Math.Max(0, distx - 10) : Math.Min(0, distx + 10)),
                            //test
                            (yPos + y) / 1,
                            (zPos + z) + (distz > 0 ? Math.Max(0, distz - 10) : Math.Min(0, distz + 10)),
                            lerpedAmps,
                            lerpedTh
                        );
                    }
                }
            }

            return noiseTemp;
        }



        private int ChunkIndex3d(int x, int y, int z)
        {
            return (y * chunksize + z) * chunksize + x;
        }

        private int ChunkIndex2d(int x, int z)
        {
            return z * chunksize + x;
        }

        private int NoiseIndex3d(int x, int y, int z)
        {
            return (y * paddedNoiseWidth + z) * paddedNoiseWidth + x;
        }
    }
}
