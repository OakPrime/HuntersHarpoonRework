using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using R2API;
using R2API.Utils;
using System.Collections.Generic;
using UnityEngine;
using EntityStates;
using IL.RoR2.Projectile;
using RoR2.Projectile;
using On.RoR2.Projectile;
using ProjectileDotZone = RoR2.Projectile.ProjectileDotZone;
using UnityEngine.UIElements;

namespace HuntersharpoonBehavior
{
    public class EnergyCell : ItemBase
    {
        private const string token = "LIT_ITEM_ENERGYCELL_DESC";
        public override ItemDef ItemDef { get; set; } = Assets.LITAssets.LoadAsset<ItemDef>("EnergyCell");

        [ConfigurableField(ConfigName = "Maximum Attack Speed per Cell", ConfigDesc = "Maximum amount of attack speed per item held.")]
        [TokenModifier(token, StatTypes.Percentage)]
        public static float bonusAttackSpeed = 0.4f;

        public override void AddBehavior(ref CharacterBody body, int stack)
        {
            body.AddItemBehavior<EnergyCellBehavior>(stack);
        }

        public class EnergyCellBehavior : CharacterBody.ItemBehavior, IStatItemBehavior, IOnIncomingDamageServerReceiver
        {
            //?. ..will look up and shout "stop doing everything in the FixedUpdate method!"... and I'll look down and whisper "no".
            //? Jokes aside, this makes sense to do inside FixedUpdate, right? I figure doing it in RecalculateStats wouldn't update properly, since... well, it's only when RecalculateStats is called.
            //? P.S. What do you call "FixedUpdate()"? Like, the name for it? It's a 'method', right? I am adding things inside of the method?

            //1.- Yeah, i think this should be called on fixed update. the other option is to look at what watch metronome does for keeping the speed boost constant.
            //2.- FixedUpdate is a method that gets called automatically by unity, remember that CharacterBody.ItemBehavior inherits from MonoBehavior, and all classes that inherit from MonoBehavior have access to FixedUpdate(), Update() among other methods.
            public float missingHealthPercent;
            public float healthFraction;

            public void OnIncomingDamageServer(DamageInfo damageInfo)
            {
                body.RecalculateStats();
            }

            public void RecalculateStatsEnd()
            {
                body.attackSpeed += body.attackSpeed * (1 - healthFraction) * (float)(Math.Pow(bonusAttackSpeed, 1 / stack));
            }

            public void RecalculateStatsStart()
            {
            }

            private void FixedUpdate()
            {
                missingHealthPercent = (1 - body.healthComponent.combinedHealthFraction);
                healthFraction = body.healthComponent.combinedHealthFraction - 0.1f;
                if (body.healthComponent.combinedHealthFraction > 0.9f)
                {
                    healthFraction = 1;
                }
                if (body.healthComponent.combinedHealthFraction < 0f)
                {
                    healthFraction = 0;
                }
                //? Is there a better way to do this? From what I understand, Math.Floor() and Math.Ceil() are used to round numbers, rather than prevent them from exiting a specific range.
            }
        }
    }
}
