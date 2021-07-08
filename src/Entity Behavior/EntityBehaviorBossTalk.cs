using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace ChaosLands
{
    public class EntityBehaviorBossTalk : EntityBehavior
    {
        EntityPartitioning entityUtil;
        string[] bossHealthy;
        string[] bossLow;
        string[] bossDied;
        float timer;

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            entityUtil = entity.Api.ModLoader.GetModSystem<EntityPartitioning>();

            bossHealthy = attributes["bossHealthy"].AsArray<string>(new string[] { "Hello World"});
            bossLow = attributes["bossLow"].AsArray<string>(new string[] { "Hello World" });
            bossDied = attributes["bossDied"].AsArray<string>(new string[] { "Hello World" });
        }

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);
            if (!entity.Alive) return;
            timer += deltaTime;

            if (timer >= 30)
            {
                timer = 0;

                if (entity.HasEmotionState("aggressiveondamage"))
                {
                    if (entity.HasEmotionState("fleeondamage")) SpeakToPlayers(bossLow[entity.World.Rand.Next(bossLow.Length)]);
                    else SpeakToPlayers(bossHealthy[entity.World.Rand.Next(bossHealthy.Length)]);
                }
            }
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            SpeakToPlayers(bossDied[entity.World.Rand.Next(bossDied.Length)]);
        }

        public void SpeakToPlayers(string message)
        {
            entityUtil.WalkEntities(entity.SidedPos.XYZ, 15, (e) => {

                if (e.SidedPos.Y >= entity.SidedPos.Y && e is EntityPlayer)
                {
                    IServerPlayer splr = ((e as EntityPlayer).Player as IServerPlayer);

                    if (splr != null)
                    {
                        splr.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get(message), EnumChatType.OthersMessage);
                    }
                }


                return true;
            });
        }

        public EntityBehaviorBossTalk(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "bosstalk";
        }
    }
}
