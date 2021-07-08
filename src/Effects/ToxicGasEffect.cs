using BuffStuff;
using ProtoBuf;

namespace ChaosLands
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ToxicGasEffect : Buff
    {
        public int StackAmount = 1;
        public string Gas = "";
        public int Timer = 3;

        public override void OnStart()
        {
            UpdateEffects();
            SetExpiryInRealSeconds(Timer);
        }
        public override void OnStack(Buff oldBuff)
        {
            ToxicGasEffect oldGas = (ToxicGasEffect)oldBuff;

            if (oldGas.Gas != Gas)
            {
                SetExpiryInRealSeconds(Timer);
                return;
            }

            StackAmount = StackAmount > oldGas.StackAmount ? StackAmount : oldGas.StackAmount;
            UpdateEffects();
            SetExpiryInRealSeconds(Timer);
        }
        public override void OnExpire()
        {
            Entity.Stats.Remove("vulenrability", "toxicgas");
            Entity.Stats.Remove("healingeffectivness", "toxicgas");
            Entity.Stats.Remove("airLoss", "toxicgas");
            Entity.Stats.Remove("airRecovery", "toxicgas");
            Entity.Stats.Remove("meleeWeaponsDamage", "toxicgas");
            Entity.Stats.Remove("rangedWeaponsDamage", "toxicgas");
            Entity.Stats.Remove("miningSpeedMul", "toxicgas");
            Entity.Stats.Remove("animalHarvestingTime", "toxicgas");
            Entity.Stats.Remove("animalSeekingRange", "toxicgas");
            Entity.Stats.Remove("armorDurabilityLoss", "toxicgas");
            Entity.Stats.Remove("maxhealthExtraPoints", "toxicgas");
            Entity.Stats.Remove("walkspeed", "toxicgas");
        }

        public void UpdateEffects()
        {

            switch (Gas)
            {
                case "no2":
                    entity.Stats.Set("vulenrability", "toxicgas", StackAmount * 0.05f, true);
                    entity.Stats.Set("healingeffectivness", "toxicgas", StackAmount * -0.05f, true);
                    break;

                case "co":
                    entity.Stats.Set("airLoss", "toxicgas", StackAmount * 0.05f, true);
                    entity.Stats.Set("airRecovery", "toxicgas", StackAmount * -0.05f, true);
                    break;

                case "h2s":
                    entity.Stats.Set("meleeWeaponsDamage", "toxicgas", StackAmount * -0.05f, true);
                    entity.Stats.Set("rangedWeaponsDamage", "toxicgas", StackAmount * -0.05f, true);
                    entity.Stats.Set("miningSpeedMul", "toxicgas", StackAmount * -0.05f, true);
                    entity.Stats.Set("walkspeed", "toxicgas", StackAmount * -0.05f, true);
                    entity.Stats.Set("healingeffectivness", "toxicgas", StackAmount * -0.05f, true);
                    break;

                case "so2":
                    entity.Stats.Set("armorDurabilityLoss", "toxicgas", StackAmount * 0.25f, true);
                    entity.Stats.Set("maxhealthExtraPoints", "toxicgas", StackAmount * -0.5f, true);
                    break;
                case "acid":
                    entity.Stats.Set("armorDurabilityLoss", "toxicgas", StackAmount * 0.25f, true);
                    entity.Stats.Set("maxhealthExtraPoints", "toxicgas", StackAmount * -0.5f, true);
                    entity.Stats.Set("vulenrability", "toxicgas", StackAmount * 0.05f, true);
                    entity.Stats.Set("healingeffectivness", "toxicgas", StackAmount * -0.05f, true);
                    break;
            }
            
        }
    }
}
