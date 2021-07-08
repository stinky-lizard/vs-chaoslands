using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ChaosLands
{
    public class AiTaskShield : AiTaskBase
    {
        public AiTaskShield(EntityAgent entity) : base(entity)
        {
        }

        public int minduration;
        public int maxduration;
        float range = 3;
        string[] seekEntityCodesExact;
        string[] seekEntityCodesBeginsWith;
        Entity targetEntity;
        EntityPartitioning partitionUtil;

        public long shieldUntilMs;

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            partitionUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();
            minduration = (int)taskConfig["minduration"]?.AsInt(2000);
            maxduration = (int)taskConfig["maxduration"]?.AsInt(4000);
            range = taskConfig["range"]?.AsFloat(12) ?? 12;

            if (taskConfig["onNearbyEntityCodes"] != null)
            {
                string[] codes = taskConfig["onNearbyEntityCodes"].AsArray<string>(new string[] { "player" });

                List<string> exact = new List<string>();
                List<string> beginswith = new List<string>();

                for (int i = 0; i < codes.Length; i++)
                {
                    string code = codes[i];
                    if (code.EndsWith("*")) beginswith.Add(code.Substring(0, code.Length - 1));
                    else exact.Add(code);
                }

                seekEntityCodesExact = exact.ToArray();
                seekEntityCodesBeginsWith = beginswith.ToArray();
            }

            base.LoadConfig(taskConfig, aiConfig);
        }

        public override bool ShouldExecute()
        {
            bool result = (whenInEmotionState == null || entity.HasEmotionState(whenInEmotionState)) && (whenNotInEmotionState == null || !entity.HasEmotionState(whenNotInEmotionState)) && cooldownUntilMs < entity.World.ElapsedMilliseconds && (entity.RightHandItemSlot.Itemstack?.Collectible.Class == "ItemShield" || entity.LeftHandItemSlot.Itemstack?.Collectible.Class == "ItemShield");
            if (!result) return false;

            targetEntity = (EntityAgent)partitionUtil.GetNearestEntity(entity.ServerPos.XYZ, range, (e) => {
                if (!e.Alive || !e.IsInteractable || e.EntityId == this.entity.EntityId) return false;

                for (int i = 0; i < seekEntityCodesExact.Length; i++)
                {
                    if (e.Code.Path == seekEntityCodesExact[i])
                    {
                        if (e.Code.Path == "player")
                        {
                            float rangeMul = e.Stats.GetBlended("animalSeekingRange");

                            IPlayer player = entity.World.PlayerByUid(((EntityPlayer)e).PlayerUID);
                            return (player.WorldData.CurrentGameMode != EnumGameMode.Creative && player.WorldData.CurrentGameMode != EnumGameMode.Spectator && (player as IServerPlayer).ConnectionState == EnumClientState.Playing);
                        }
                        return true;
                    }
                }


                for (int i = 0; i < seekEntityCodesBeginsWith.Length; i++)
                {
                    if (e.Code.Path.StartsWithFast(seekEntityCodesBeginsWith[i])) return true;
                }

                return false;
            });

            return result && targetEntity != null;

        }

        public override void StartExecute()
        {
            base.StartExecute();
            shieldUntilMs = entity.World.ElapsedMilliseconds + minduration + entity.World.Rand.Next(maxduration - minduration);
            entity.ServerControls.Sneak = true;
        }

        public override bool ContinueExecute(float dt)
        {      
            if (!entity.ServerControls.Sneak) entity.ServerControls.Sneak = true;
            return targetEntity.Alive && targetEntity.SidedPos.DistanceTo(entity.SidedPos.XYZ) <= range && shieldUntilMs >= entity.World.ElapsedMilliseconds;
        }

        public override void FinishExecute(bool cancelled)
        {
            entity.ServerControls.Sneak = false;
            targetEntity = null;
            base.FinishExecute(cancelled);
        }
    }
}
