using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System.Collections.Generic;
using System;
using Vintagestory.API.MathTools;
using System.IO;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Config;

namespace ChaosLands
{
    public class EntityNPC : EntityAgent
    {
        //public override double EyeHeight => base.Properties.EyeHeight - (controls.Sneak ? 0.1 : 0.0);

        protected InventoryBase inv;


        public override bool StoreWithChunk
        {
            get { return true; }
        }


        public override IInventory GearInventory
        {
            get
            {
                return inv;
            }
        }

        public int ActiveSlotNumber
        {
            get { return WatchedAttributes.GetInt("ActiveSlotNumber", 16); }
            set { WatchedAttributes.SetInt("ActiveSlotNumber", GameMath.Clamp(value, 16, 25)); WatchedAttributes.MarkPathDirty("ActiveSlotNumber"); }
        }

        public override ItemSlot RightHandItemSlot
        {
            get
            {
                return inv[ActiveSlotNumber];
            }
        }

        public override ItemSlot LeftHandItemSlot
        {
            get
            {
                return inv[15];
            }
        }

        public override byte[] LightHsv
        {
            get
            {
                byte[] rightHsv = RightHandItemSlot?.Itemstack?.Block?.GetLightHsv(World.BlockAccessor, null, RightHandItemSlot.Itemstack);
                byte[] leftHsv = LeftHandItemSlot?.Itemstack?.Block?.GetLightHsv(World.BlockAccessor, null, LeftHandItemSlot.Itemstack);

                if (rightHsv == null) return leftHsv;
                if (leftHsv == null) return rightHsv;

                float totalval = rightHsv[2] + leftHsv[2];
                float t = leftHsv[2] / totalval;

                return new byte[]
                {
                    (byte)(leftHsv[0] * t + rightHsv[0] * (1-t)),
                    (byte)(leftHsv[1] * t + rightHsv[1] * (1-t)),
                    Math.Max(leftHsv[2], rightHsv[2])
                };
            }
        }

        public EntityNPC() : base()
        {
            inv = new InventoryNPCGear(null, null);
        }

        public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
        {
            base.Initialize(properties, api, chunkindex3d);

            inv.LateInitialize("gearinv-" + EntityId, api);

            AnimManager.HeadController = new EntityHeadController(AnimManager, this, Properties.Client.LoadedShape);
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);

            if (Properties.Attributes.IsTrue("isBoss")) IsOnFire = false;
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();

            if (World.Side == EnumAppSide.Client)
            {
                (Properties.Client.Renderer as EntityShapeRenderer).DoRenderHeldItem = true;
            }

        }

        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            TreeAttribute tree;
            WatchedAttributes["gearInv"] = tree = new TreeAttribute();
            inv.ToTreeAttributes(tree);


            base.ToBytes(writer, forClient);
        }


        public override void FromBytes(BinaryReader reader, bool forClient)
        {
            base.FromBytes(reader, forClient);

            TreeAttribute tree = WatchedAttributes["gearInv"] as TreeAttribute;
            if (tree != null) inv.FromTreeAttributes(tree);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
        {
            base.OnInteract(byEntity, slot, hitPosition, mode);
            if (Properties.Attributes.IsTrue("isBoss")) return;

            if ((byEntity as EntityPlayer)?.Controls.Sneak == true && mode == EnumInteractMode.Interact && byEntity.World.Side == EnumAppSide.Server)
            {
                inv.DiscardAll();
                WatchedAttributes.MarkAllDirty();
            }
        }

        public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
        {
            base.Die(reason, damageSourceForDeath);

            if (Properties.Attributes.IsTrue("isBoss"))
            {
                if (World.Side == EnumAppSide.Server)
                {
                    LandClaim[] claims = World.Claims.Get(SidedPos.AsBlockPos);
                    if (claims != null && claims.Length > 0)
                    {
                        foreach (LandClaim claim in claims)
                        {
                            if (claim.LastKnownOwnerName == "Corrupted Clockmaker")
                            {
                                World.GetNearestEntity(SidedPos.XYZ, 32, 32, (e) =>
                                {
                                    if (e.SidedPos.Y >= SidedPos.Y && e is EntityPlayer)
                                    {
                                        IServerPlayer splr = ((e as EntityPlayer).Player as IServerPlayer);

                                        if (splr != null)
                                        {
                                            splr.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("chaoslands:bossdefeated"), EnumChatType.OthersMessage);
                                        }
                                    }
                                    return false;
                                });

                                World.Claims.Remove(claim);
                                break;
                            }
                        }
                    }
                }

                int choice = World.Rand.Next(17);

                inv[choice].Itemstack?.Attributes.SetInt("durability", 100);
                inv.DropSlots(SidedPos.XYZ, choice);
                return;
            }

                inv.DropAll(this.SidedPos.XYZ);
        }

        public override bool ReceiveDamage(DamageSource damageSource, float damage)
        {
            if (Properties.Attributes.IsTrue("isBoss") && damageSource.Type == EnumDamageType.Fire) return false;
            float dmg = handleDefense(damage, damageSource);
            return base.ReceiveDamage(damageSource, dmg);
        }


        Dictionary<int, EnumCharacterDressType[]> clothingDamageTargetsByAttackTacket = new Dictionary<int, EnumCharacterDressType[]>()
        {
            { 0, new EnumCharacterDressType[] { EnumCharacterDressType.Head, EnumCharacterDressType.Face, EnumCharacterDressType.Neck } },
            { 1, new EnumCharacterDressType[] { EnumCharacterDressType.UpperBody, EnumCharacterDressType.UpperBodyOver, EnumCharacterDressType.Shoulder, EnumCharacterDressType.Arm, EnumCharacterDressType.Hand } },
            { 2, new EnumCharacterDressType[] { EnumCharacterDressType.LowerBody, EnumCharacterDressType.Foot } }
        };
        private float handleDefense(float damage, DamageSource dmgSource)
        {
            // Does not protect against non-attack damages
            EnumDamageType type = dmgSource.Type;
            if (type != EnumDamageType.BluntAttack && type != EnumDamageType.PiercingAttack && type != EnumDamageType.SlashingAttack) return damage;
            if (dmgSource.Source == EnumDamageSource.Internal || dmgSource.Source == EnumDamageSource.Suicide) return damage;

            ItemSlot armorSlot;
            IInventory inv = GearInventory;
            double rnd = Api.World.Rand.NextDouble();


            int attackTarget;

            if ((rnd -= 0.2) < 0)
            {
                // Head
                armorSlot = inv[12];
                attackTarget = 0;
            }
            else if ((rnd -= 0.5) < 0)
            {
                // Body
                armorSlot = inv[13];
                attackTarget = 1;
            }
            else
            {
                // Legs
                armorSlot = inv[14];
                attackTarget = 2;
            }

            // Apply full damage if no armor is in this slot
            if (armorSlot.Empty || !(armorSlot.Itemstack.Item is ItemWearable))
            {
                EnumCharacterDressType[] dressTargets = clothingDamageTargetsByAttackTacket[attackTarget];
                EnumCharacterDressType target = dressTargets[Api.World.Rand.Next(dressTargets.Length)];

                ItemSlot targetslot = GearInventory[(int)target];
                if (!targetslot.Empty)
                {
                    // Wolf: 10 hp damage = 10% condition loss
                    // Ram: 10 hp damage = 2.5% condition loss
                    // Bronze locust: 10 hp damage = 5% condition loss
                    float mul = 0.25f;
                    if (type == EnumDamageType.SlashingAttack) mul = 1f;
                    if (type == EnumDamageType.PiercingAttack) mul = 0.5f;

                    float diff = -damage / 100 * mul;

                    if (Math.Abs(diff) > 0.05)
                    {
                        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/clothrip"), this);
                    }

                    (targetslot.Itemstack.Collectible as ItemWearable)?.ChangeCondition(targetslot, diff);
                }

                return damage;
            }

            ProtectionModifiers protMods = (armorSlot.Itemstack.Item as ItemWearable).ProtectionModifiers;

            int weaponTier = dmgSource.DamageTier;
            float flatDmgProt = protMods.FlatDamageReduction;
            float percentProt = protMods.RelativeProtection;

            for (int tier = 1; tier <= weaponTier; tier++)
            {
                bool aboveTier = tier > protMods.ProtectionTier;

                float flatLoss = aboveTier ? protMods.PerTierFlatDamageReductionLoss[1] : protMods.PerTierFlatDamageReductionLoss[0];
                float percLoss = aboveTier ? protMods.PerTierRelativeProtectionLoss[1] : protMods.PerTierRelativeProtectionLoss[0];

                if (aboveTier && protMods.HighDamageTierResistant)
                {
                    flatLoss /= 2;
                    percLoss /= 2;
                }

                flatDmgProt -= flatLoss;
                percentProt *= 1 - percLoss;
            }

            // Durability loss is the one before the damage reductions
            float durabilityLoss = 0.5f + damage * Math.Max(0.5f, (weaponTier - protMods.ProtectionTier) * 3);
            int durabilityLossInt = GameMath.RoundRandom(Api.World.Rand, durabilityLoss);

            // Now reduce the damage
            damage = Math.Max(0, damage - flatDmgProt);
            damage *= 1 - Math.Max(0, percentProt);

            armorSlot.Itemstack.Collectible.DamageItem(Api.World, this, armorSlot, durabilityLossInt);

            if (armorSlot.Empty)
            {
                Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), this);
            }

            return damage;
        }

        private void onFallToGround(EntityAgent entity, double motionY)
        {
            if (Math.Abs(motionY) > 0.1)
            {
                onFootStep(entity);
            }
        }

        private void onFootStep(EntityAgent entity)
        {
            IInventory gearInv = entity.GearInventory;

            foreach (var slot in gearInv)
            {
                ItemWearable item;
                if (slot.Empty || (item = slot.Itemstack.Collectible as ItemWearable) == null) continue;

                AssetLocation[] soundlocs = item.FootStepSounds;
                if (soundlocs == null || soundlocs.Length == 0) continue;

                AssetLocation loc = soundlocs[Api.World.Rand.Next(soundlocs.Length)];

                float pitch = (float)Api.World.Rand.NextDouble() * 0.5f + 0.7f;
                float volume = (float)Api.World.Rand.NextDouble() * 0.3f + 0.7f;
                Api.World.PlaySoundAt(loc, entity, null, pitch, 16f, volume);
            }
        }
    }
}
