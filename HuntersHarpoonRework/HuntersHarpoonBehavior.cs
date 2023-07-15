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

namespace HuntersHarpoonRework.Behaviors
{
    public class HuntersHarpoonBehavior : CharacterBody.ItemBehavior
    {
        private float ticks = 0.0f;
        //public bool isReady = false;
        private void FixedUpdate()
        {
            if (this.body.GetBuffCount(HuntersHarpoonRework.BurstBuildup) < 100 && !this.body.GetNotMoving())
            {
                ticks += (this.body.moveSpeed / this.body.baseMoveSpeed);
                // 5.0f and += 1 was good
                if (ticks > 12.0f)
                {
                    this.body.AddBuff(HuntersHarpoonRework.BurstBuildup);
                    ticks = 0;
                }
                
            }
        }

        private void OnDisable()
        {
            this.body.SetBuffCount(HuntersHarpoonRework.BurstBuildup.buffIndex, 0);
        }
    }

    /*public class ItemBaseStuff : ItemBase
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

        
    }*/
}
