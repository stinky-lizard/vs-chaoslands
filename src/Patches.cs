using BuffStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace ChaosLands
{
    [HarmonyPatch(typeof(GenCaves), "CarveTunnel")]
    public class CrazyCaves
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return LandsOfChaosConfig.Loaded.DefaultCavesTunnelWidth != 1 || LandsOfChaosConfig.Loaded.DefaultCavesTunnelHeight != 1 || LandsOfChaosConfig.Loaded.DefaultCavesExtraBranchy || LandsOfChaosConfig.Loaded.DefaultCavesBigNearLava;
        }

        [HarmonyPrefix]
        static void Prefix(ref float horizontalSize, ref float verticalSize, ref bool extraBranchy, ref bool largeNearLavaLayer)
        {
            //Crazy caves was 1.5
            horizontalSize *= Math.Min(LandsOfChaosConfig.Loaded.DefaultCavesTunnelWidth, 1.5f);
            verticalSize *= Math.Min(1.5f, LandsOfChaosConfig.Loaded.DefaultCavesTunnelHeight);
            extraBranchy = LandsOfChaosConfig.Loaded.DefaultCavesExtraBranchy ? true : extraBranchy;
            largeNearLavaLayer = LandsOfChaosConfig.Loaded.DefaultCavesBigNearLava ? true : largeNearLavaLayer;
        }
    }

    [HarmonyPatch(typeof(GenCaves), "CarveShaft")]
    public class CrazyCavesShafts
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return LandsOfChaosConfig.Loaded.DefaultCavesShaftWidth != 1 || LandsOfChaosConfig.Loaded.DefaultCavesShaftHeight != 1;
        }

        [HarmonyPrefix]
        static void Prefix(ref float horizontalSize, ref float verticalSize)
        {
            //Crazy caves was 1.5
            horizontalSize *= Math.Min(1.5f, LandsOfChaosConfig.Loaded.DefaultCavesShaftWidth);
            verticalSize *= Math.Min(1.5f, LandsOfChaosConfig.Loaded.DefaultCavesShaftHeight);
        }
    }

    [HarmonyPatch(typeof(GenCaves), "StartServerSide")]
    public class TurnOffDefaultCaves
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return !LandsOfChaosConfig.Loaded.DefaultCavesEnabled;
        }

        [HarmonyPrefix]
        static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(ITreeGenerator))]
    [HarmonyPatch("GrowTree")]
    public class TreeGrowth
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return LandsOfChaosConfig.Loaded.TreeSizeMult != 1 || LandsOfChaosConfig.Loaded.TreeVineMult != 1 || LandsOfChaosConfig.Loaded.TreeSpecialLogMult != 1;
        }

        [HarmonyPrefix]
        static void Prefix(ref float sizeModifier, ref float vineGrowthChance, ref float otherBlockChance)
        {
            sizeModifier *= LandsOfChaosConfig.Loaded.TreeSizeMult;
            vineGrowthChance *= LandsOfChaosConfig.Loaded.TreeVineMult;
            otherBlockChance *= LandsOfChaosConfig.Loaded.TreeSpecialLogMult;
        }
    }

    [HarmonyPatch(typeof(EntityBehaviorHealth))]
    public class VulenrabilityStat
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPatch("Initialize")]
        [HarmonyPostfix]
        static void DefenseBonus(EntityBehaviorHealth __instance)
        {
            __instance.onDamaged += (dmg, source) => { return dmg * __instance.entity.Stats.GetBlended("vulenrability"); };
        }
    }

    [HarmonyPatch(typeof(EntitySidedProperties))]
    public class BreatheOverride
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return LandsOfChaosConfig.Loaded.BreathingEnabled;
        }

        [HarmonyPatch("loadBehaviors")]
        [HarmonyPostfix]
        static void ChangeToAir(Entity entity, EntityProperties properties, EntitySidedProperties __instance, JsonObject[] ___BehaviorsAsJsonObj)
        {
            for (int i = 0; i < __instance.Behaviors.Count; i++)
            {
                if (__instance.Behaviors[i] is EntityBehaviorBreathe)
                {
                    EntityBehavior air = new EntityBehaviorAir(entity);
                    air.Initialize(properties, ___BehaviorsAsJsonObj[i]);

                    __instance.Behaviors[i] = air;
                    break;
                }
            }
        }
    }

    //Most blocks when blown up leave behind carbon monoxide or nitrogen dioxide
    [HarmonyPatch(typeof(Block))]
    public class BombNitrogen
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return LandsOfChaosConfig.Loaded.GasesEnabled;
        }

        [HarmonyPatch("OnBlockExploded")]
        [HarmonyPostfix]
        static void ChangeToAir(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, Block __instance)
        {
            BlockBehaviorExplosionGas boom = world.BlockAccessor.GetBlock(pos).GetBehavior<BlockBehaviorExplosionGas>();
            Block gas;
            if (boom != null) return;
            gas = world.BlockAccessor.GetBlock(new AssetLocation(world.Rand.NextDouble() > 0.5 ? "chaoslands:gas-co-2" : "chaoslands:gas-no2-2"));

            world.RegisterCallback((time) => { if (world.BlockAccessor.GetBlock(pos).BlockId == 0) world.BlockAccessor.SetBlock(gas.BlockId, pos); }, 3000);
            
        }
    }

    //Entities burn up and produce carbon monoxide
    [HarmonyPatch(typeof(Entity))]
    public class SmokeDeath
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return LandsOfChaosConfig.Loaded.GasesEnabled;
        }

        [HarmonyPatch("Die")]
        [HarmonyPostfix]
        static void BurnToSmoke(EnumDespawnReason reason, DamageSource damageSourceForDeath, Entity __instance)
        {
            if (damageSourceForDeath?.Type != EnumDamageType.Fire) return;

            Block gas = __instance.World.BlockAccessor.GetBlock(new AssetLocation("chaoslands:gas-co-8"));
            BlockPos pos = __instance.ServerPos.AsBlockPos;

            for (int y = 0; y < 7; y++)
            {
                Block over = __instance.World.BlockAccessor.GetBlock(pos);
                if (over.SideSolid[BlockFacing.indexUP] || over.SideSolid[BlockFacing.indexDOWN]) return;
                if (over.BlockId == 0)
                {
                    __instance.World.BlockAccessor.SetBlock(gas.BlockId, pos);
                    return;
                }
                int gasBuildUp = 0;
                if (over.FirstCodePart() == gas.FirstCodePart() && over.FirstCodePart(1) == gas.FirstCodePart(1) && (gasBuildUp = int.Parse(over.LastCodePart())) < 8)
                {
                    Block newGas = __instance.World.BlockAccessor.GetBlock(over.CodeWithVariant("level", (gasBuildUp + 1).ToString()));
                    __instance.World.BlockAccessor.SetBlock(newGas.BlockId, pos);
                    return;
                }

                pos.Up();
            }
        }
    }

    //Toxic gases and dust when gathering charcoal
    [HarmonyPatch(typeof(BlockLayeredSlowDig))]
    public class CharcoalSuffocation
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return LandsOfChaosConfig.Loaded.GasesEnabled;
        }

        [HarmonyPatch("GetPrevLayer")]
        [HarmonyPostfix]
        static void CharcoalRemains(IWorldAccessor world, ref Block __result)
        {
            if (__result != null) return;
            Block gas = world.BlockAccessor.GetBlock(new AssetLocation(world.Rand.NextDouble() > 0.25 ? "chaoslands:gas-coaldust-8" : "chaoslands:gas-co-8"));
            __result = gas;
        }
    }

    [HarmonyPatch(typeof(EntityInLiquid))]
    public class CrabWalking
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoApply")]
        static bool walkUnderwater(EntityInLiquid __instance, float dt, Entity entity, EntityPos pos, EntityControls controls, ref float ___push)
        {
            if (entity.Swimming && entity.Alive)
            {
                bool seafloor = false;
                if (entity.Properties.Attributes != null) seafloor = entity.Properties.Attributes.IsTrue("seafloor");

                string playerUID = entity is EntityPlayer ? ((EntityPlayer)entity).PlayerUID : null;


                if ((controls.TriesToMove || controls.Jump) && entity.World.ElapsedMilliseconds - __instance.lastPush > 2000 && playerUID != null)
                {
                    ___push = 8f;
                    __instance.lastPush = entity.World.ElapsedMilliseconds;
                    entity.PlayEntitySound("swim", playerUID == null ? null : entity.World.PlayerByUid(playerUID));
                }
                else
                {
                    ___push = Math.Max(1f, ___push - 0.1f * dt * 60f);
                }

                Block inblock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y), (int)pos.Z);
                Block aboveblock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y + 1), (int)pos.Z);
                Block twoaboveblock = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y + 2), (int)pos.Z);
                float waterY = (int)pos.Y + inblock.LiquidLevel / 8f + (aboveblock.IsLiquid() ? 9 / 8f : 0) + (twoaboveblock.IsLiquid() ? 9 / 8f : 0);
                float bottomSubmergedness = waterY - (float)pos.Y;

                // 0 = at swim line
                // 1 = completely submerged
                float swimlineSubmergedness = GameMath.Clamp(bottomSubmergedness - ((float)entity.SwimmingOffsetY), 0, 1);
                swimlineSubmergedness = Math.Min(1, swimlineSubmergedness + 0.075f);


                double yMot = 0;
                if (controls.Jump)
                {
                    yMot = 0.005f * swimlineSubmergedness * dt * 60;
                }
                else
                {
                    yMot = controls.FlyVector.Y * (1 + ___push) * 0.03f * swimlineSubmergedness;
                }

                pos.Motion.Add(
                    controls.FlyVector.X * (1 + ___push) * 0.03f,
                    seafloor ? 0 : yMot,
                    controls.FlyVector.Z * (1 + ___push) * 0.03f
                );
            }


            Block block = entity.World.BlockAccessor.GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
            if (block.PushVector != null)
            {
                pos.Motion.Add(block.PushVector);
            }

            // http://fooplot.com/plot/kg6l1ikyx2
            /*float x = entity.Pos.Motion.Length();
            if (x > 0)
            {
                pos.Motion.Normalize();
                pos.Motion *= (float)Math.Log(x + 1) / 1.5f;
            }*/

            return false;
        }
    }

    [HarmonyPatch(typeof(EntityApplyGravity))]
    public class CrabAnchor
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoApply")]
        static bool stickSeafloor(EntityInLiquid __instance, float dt, Entity entity, EntityPos pos, EntityControls controls)
        {
            bool seafloor = false;
            if (entity.Properties.Attributes != null) seafloor = entity.Properties.Attributes.IsTrue("seafloor");

            if ((entity.Swimming && !seafloor) && controls.TriesToMove) return false;
            if (!entity.ApplyGravity) return false;


            if (pos.Y > -100)
            {
                pos.Motion.Y -= (GlobalConstants.GravityPerSecond + Math.Max(0, -0.015f * pos.Motion.Y)) * (entity.FeetInLiquid ? 0.33f : 1f) * dt;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(WorldGenStructure))]
    public class UnderwaterStructure
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("TryGenerateUnderwater")]
        static bool spawnUnderwater(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, ref bool __result, WorldGenStructure __instance, LCGRandom ___rand, BlockPos ___tmpPos, BlockSchematicStructure[][] ___schematicDatas, int[] ___replaceblockids, int ___climateUpLeft, int ___climateUpRight, int ___climateBotLeft, int ___climateBotRight)
        {
            __result = false;
            int num = ___rand.NextInt(___schematicDatas.Length);
            int orient = ___rand.NextInt(4);
            BlockSchematicStructure schematic = ___schematicDatas[num][orient];


            int widthHalf = (int)Math.Ceiling(schematic.SizeX / 2f);
            int lengthHalf = (int)Math.Ceiling(schematic.SizeZ / 2f);

            // Probe all 4 corners + center if they are below sea level


            if (blockAccessor.GetTerrainMapheightAt(pos) + 1 != worldForCollectibleResolve.SeaLevel) return false;

            ___tmpPos.Set(pos.X - widthHalf, 0, pos.Z - lengthHalf);
            if (blockAccessor.GetTerrainMapheightAt(___tmpPos) + 1 != worldForCollectibleResolve.SeaLevel) return false;

            ___tmpPos.Set(pos.X + widthHalf, 0, pos.Z - lengthHalf);
            if (blockAccessor.GetTerrainMapheightAt(___tmpPos) + 1 != worldForCollectibleResolve.SeaLevel) return false;

            ___tmpPos.Set(pos.X - widthHalf, 0, pos.Z + lengthHalf);
            if (blockAccessor.GetTerrainMapheightAt(___tmpPos) + 1 != worldForCollectibleResolve.SeaLevel) return false;

            ___tmpPos.Set(pos.X + widthHalf, 0, pos.Z + lengthHalf);
            if (blockAccessor.GetTerrainMapheightAt(___tmpPos) + 1 != worldForCollectibleResolve.SeaLevel) return false;


            //Lower it to sea floor

            ___tmpPos.Set(pos);
            Block sub;
            while ((sub = blockAccessor.GetBlock(pos)).IsLiquid() || sub.BlockId == 0)
            {
                pos.Y--;
            }
            pos.Y += 2;


            // Ensure deeply submerged in water

            ___tmpPos.Set(pos.X - widthHalf, pos.Y + schematic.SizeY + __instance.OffsetY, pos.Z - lengthHalf);
            if (!blockAccessor.GetBlock(___tmpPos).IsLiquid()) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y + schematic.SizeY + __instance.OffsetY, pos.Z - lengthHalf);
            if (!blockAccessor.GetBlock(___tmpPos).IsLiquid()) return false;

            ___tmpPos.Set(pos.X - widthHalf, pos.Y + schematic.SizeY + __instance.OffsetY, pos.Z + lengthHalf);
            if (!blockAccessor.GetBlock(___tmpPos).IsLiquid()) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y + schematic.SizeY + __instance.OffsetY, pos.Z + lengthHalf);
            if (!blockAccessor.GetBlock(___tmpPos).IsLiquid()) return false;




            if (!__instance.satisfiesMinDistance(pos, worldForCollectibleResolve)) return false;
            if (__instance.isStructureAt(pos, worldForCollectibleResolve)) return false;

            pos.Y -= 2;

            __instance.LastPlacedSchematicLocation.Set(pos.X, pos.Y, pos.Z, pos.X + schematic.SizeX, pos.Y + schematic.SizeY, pos.Z + schematic.SizeZ);
            __instance.LastPlacedSchematic = schematic;
            schematic.Place(blockAccessor, worldForCollectibleResolve, pos);
            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("TryGenerateRuinAtSurface")]
        static bool noSpawnRuinOnIce(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, ref bool __result, WorldGenStructure __instance, LCGRandom ___rand, BlockPos ___tmpPos, BlockSchematicStructure[][] ___schematicDatas, int[] ___replaceblockids, int ___climateUpLeft, int ___climateUpRight, int ___climateBotLeft, int ___climateBotRight)
        {
            __result = false;
            int num = ___rand.NextInt(___schematicDatas.Length);
            int orient = ___rand.NextInt(4);
            BlockSchematicStructure schematic = ___schematicDatas[num][orient];


            int widthHalf = (int)Math.Ceiling(schematic.SizeX / 2f);
            int lengthHalf = (int)Math.Ceiling(schematic.SizeZ / 2f);




            // Probe all 4 corners + center if they either touch the surface or are sightly below ground

            int centerY = blockAccessor.GetTerrainMapheightAt(pos);


            ___tmpPos.Set(pos.X - widthHalf, 0, pos.Z - lengthHalf);
            int topLeftY = blockAccessor.GetTerrainMapheightAt(___tmpPos);

            ___tmpPos.Set(pos.X + widthHalf, 0, pos.Z - lengthHalf);
            int topRightY = blockAccessor.GetTerrainMapheightAt(___tmpPos);

            ___tmpPos.Set(pos.X - widthHalf, 0, pos.Z + lengthHalf);
            int botLeftY = blockAccessor.GetTerrainMapheightAt(___tmpPos);

            ___tmpPos.Set(pos.X + widthHalf, 0, pos.Z + lengthHalf);
            int botRightY = blockAccessor.GetTerrainMapheightAt(___tmpPos);


            int maxY = GameMath.Max(centerY, topLeftY, topRightY, botLeftY, botRightY);
            int minY = GameMath.Min(centerY, topLeftY, topRightY, botLeftY, botRightY);
            int diff = Math.Abs(maxY - minY);

            if (diff > 3) return false;

            pos.Y = minY;


            // Ensure not deeply submerged in water or in ice
            Block check;

            ___tmpPos.Set(pos.X - widthHalf, pos.Y + 1 + __instance.OffsetY, pos.Z - lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y + 1 + __instance.OffsetY, pos.Z - lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X - widthHalf, pos.Y + 1 + __instance.OffsetY, pos.Z + lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y + 1 + __instance.OffsetY, pos.Z + lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;


            pos.Y--;

            if (!__instance.satisfiesMinDistance(pos, worldForCollectibleResolve)) return false;
            if (__instance.isStructureAt(pos, worldForCollectibleResolve)) return false;

            __instance.LastPlacedSchematicLocation.Set(pos.X, pos.Y, pos.Z, pos.X + schematic.SizeX, pos.Y + schematic.SizeY, pos.Z + schematic.SizeZ);
            __instance.LastPlacedSchematic = schematic;
            schematic.PlaceRespectingBlockLayers(blockAccessor, worldForCollectibleResolve, pos, ___climateUpLeft, ___climateUpRight, ___climateBotLeft, ___climateBotRight, ___replaceblockids);

            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("TryGenerateAtSurface")]
        static bool noSpawnOnIce(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, ref bool __result, WorldGenStructure __instance, LCGRandom ___rand, BlockPos ___tmpPos, BlockSchematicStructure[][] ___schematicDatas, int[] ___replaceblockids, int ___climateUpLeft, int ___climateUpRight, int ___climateBotLeft, int ___climateBotRight)
        {
            __result = false;
            int chunksize = blockAccessor.ChunkSize;

            int num = ___rand.NextInt(___schematicDatas.Length);
            int orient = ___rand.NextInt(4);
            BlockSchematicStructure schematic = ___schematicDatas[num][orient];


            int widthHalf = (int)Math.Ceiling(schematic.SizeX / 2f);
            int lengthHalf = (int)Math.Ceiling(schematic.SizeZ / 2f);



            // Probe all 4 corners + center if they are on the same height

            int centerY = blockAccessor.GetTerrainMapheightAt(pos);

            ___tmpPos.Set(pos.X - widthHalf, 0, pos.Z - lengthHalf);
            int topLeftY = blockAccessor.GetTerrainMapheightAt(___tmpPos);

            ___tmpPos.Set(pos.X + widthHalf, 0, pos.Z - lengthHalf);
            int topRightY = blockAccessor.GetTerrainMapheightAt(___tmpPos);

            ___tmpPos.Set(pos.X - widthHalf, 0, pos.Z + lengthHalf);
            int botLeftY = blockAccessor.GetTerrainMapheightAt(___tmpPos);

            ___tmpPos.Set(pos.X + widthHalf, 0, pos.Z + lengthHalf);
            int botRightY = blockAccessor.GetTerrainMapheightAt(___tmpPos);


            int diff = GameMath.Max(centerY, topLeftY, topRightY, botLeftY, botRightY) - GameMath.Min(centerY, topLeftY, topRightY, botLeftY, botRightY);
            if (diff != 0) return false;

            pos.Y += centerY - pos.Y + 1 + __instance.OffsetY;
            Block check;

            // Ensure not floating on water
            ___tmpPos.Set(pos.X - widthHalf, pos.Y - 1, pos.Z - lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y - 1, pos.Z - lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X - widthHalf, pos.Y - 1, pos.Z + lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y - 1, pos.Z + lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            // Ensure not submerged in water
            ___tmpPos.Set(pos.X - widthHalf, pos.Y, pos.Z - lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y, pos.Z - lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X - widthHalf, pos.Y, pos.Z + lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y, pos.Z + lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;



            ___tmpPos.Set(pos.X - widthHalf, pos.Y + 1, pos.Z - lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y + 1, pos.Z - lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X - widthHalf, pos.Y + 1, pos.Z + lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;

            ___tmpPos.Set(pos.X + widthHalf, pos.Y + 1, pos.Z + lengthHalf);
            if ((check = blockAccessor.GetBlock(___tmpPos)).IsLiquid() || (check is BlockLakeIce)) return false;


            if (!__instance.satisfiesMinDistance(pos, worldForCollectibleResolve)) return false;
            if (__instance.isStructureAt(pos, worldForCollectibleResolve)) return false;

            __instance.LastPlacedSchematicLocation.Set(pos.X, pos.Y, pos.Z, pos.X + schematic.SizeX, pos.Y + schematic.SizeY, pos.Z + schematic.SizeZ);
            __instance.LastPlacedSchematic = schematic;
            schematic.PlaceRespectingBlockLayers(blockAccessor, worldForCollectibleResolve, pos, ___climateUpLeft, ___climateUpRight, ___climateBotLeft, ___climateBotRight, ___replaceblockids);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(LandformVariant))]
    public class LandformControl
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Init")]
        static void ControlWeight(LandformVariant __instance)
        {
            if (__instance.Code.Path.StartsWith("ocean"))
            {
                __instance.Weight *= LandsOfChaosConfig.Loaded.OceanWeightMult;
            }
        }
    }

    [HarmonyPatch(typeof(GenRivulets))]
    public class DisableRivulets
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return !LandsOfChaosConfig.Loaded.RivuletsEnabled;
        }

        [HarmonyPrefix]
        [HarmonyPatch("StartServerSide")]
        static bool NoRivers()
        {
            return false;
        }
    }
}
