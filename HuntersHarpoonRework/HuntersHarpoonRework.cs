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
using HuntersHarpoonRework.Behaviors;

namespace HuntersHarpoonRework
{
    //Loads R2API Submodules
    [R2APISubmoduleDependency(nameof(LanguageAPI))]

    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class HuntersHarpoonRework : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "OakPrime";
        public const string PluginName = "HuntersHarpoonRework";
        public const string PluginVersion = "0.3.0";

        private readonly Dictionary<string, string> DefaultLanguage = new Dictionary<string, string>();

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            try
            {
                // Either move to takeDamage in HealthComponent or modify this code to create a new damage instance on the victim (through projectile or calling takeDamage)
                /**
                 *  Damage boost based on harpoon buff stacks
                 */
                IL.RoR2.HealthComponent.TakeDamage += (il) =>
                {
                    // onHitEnemy (separate damage instance)
                    /*ILCursor c = new ILCursor(il);
                    c.TryGotoNext(
                        x => x.MatchLdarg(out _),
                        x => x.MatchLdflda(out _),
                        x => x.MatchLdcI4(out _),
                        x => x.MatchCallOrCallvirt<RoR2.DamageInfo>(nameof(RoR2.DamageInfo.procChainMask))
                    );*/
                    ILCursor c = new ILCursor(il);
                    c.TryGotoNext(
                        x => x.MatchLdloc(out _),
                        x => x.MatchCallOrCallvirt<RoR2.CharacterMaster>("get_inventory"),
                        x => x.MatchLdsfld("RoR2.RoR2Content/Items", "NearbyDamageBonus"),
                        x => x.MatchCallOrCallvirt(out _),
                        x => x.MatchStloc(out _)
                    );
                    c.Index += 5;
                    c.Emit(OpCodes.Ldarg_1);
                    c.Emit(OpCodes.Ldloc, 6);
                    c.EmitDelegate<Func<RoR2.DamageInfo, float, float>>((damageInfo, damageVal) =>
                    {
                        float newDamageVal = damageVal;
                        CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
                        int buffCount = body.GetBuffCount(DLC1Content.Buffs.KillMoveSpeed);
                        ProcChainMask mask = damageInfo.procChainMask;
                        if (buffCount > 0 && damageInfo.procCoefficient >= 0.5f && body?.GetComponent<HuntersHarpoonBehavior>()?.generatingStacks == true && !mask.HasProc(ProcType.Missile)
                            && !mask.HasProc(ProcType.ChainLightning) && !mask.HasProc(ProcType.BounceNearby) && !mask.HasProc(ProcType.Thorns) && !mask.HasProc(ProcType.LoaderLightning))
                        {
                            newDamageVal *= (1.0f + (float)buffCount / 200);
                            body.GetComponent<HuntersHarpoonBehavior>().generatingStacks = false;
                            body.SetBuffCount(DLC1Content.Buffs.KillMoveSpeed.buffIndex, 0);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.03f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.015f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.0075f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.00375f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.00185f);
                            EffectData effectData = new EffectData();
                            effectData.origin = body.corePosition;
                            CharacterMotor characterMotor = body.characterMotor;
                            bool flag = false;
                            if ((bool)(UnityEngine.Object)characterMotor)
                            {
                                Vector3 moveDirection = characterMotor.moveDirection;
                                if (moveDirection != Vector3.zero)
                                {
                                    effectData.rotation = Util.QuaternionSafeLookRotation(moveDirection);
                                    flag = true;
                                }
                            }
                            if (!flag)
                                effectData.rotation = body.transform.rotation;
                            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MoveSpeedOnKillActivate"), effectData, true);
                        }
                        return newDamageVal;
                    });
                    c.Emit(OpCodes.Stloc, 6);
                };
                /**
                 *  Adds item behavior
                 */
                IL.RoR2.CharacterBody.OnInventoryChanged += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.TryGotoNext(
                        x => x.MatchLdarg(out _),
                        x => x.MatchLdarg(out _),
                        x => x.MatchCallOrCallvirt<RoR2.CharacterBody>(nameof(RoR2.CharacterBody.inventory))/*,
                        x => x.MatchLdcR4(out _),
                        x => x.MatchMul()*/
                    );
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Action<RoR2.CharacterBody>>(body =>
                    {
                        body.AddItemBehavior<HuntersHarpoonBehavior>(body.inventory.GetItemCount(DLC1Content.Items.MoveSpeedOnKill));
                    });
                };
                /**
                 *  Applies move speed
                 */
                IL.RoR2.CharacterBody.RecalculateStats += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.TryGotoNext(
                        x => x.MatchLdarg(out _),
                        x => x.MatchLdsfld("RoR2.DLC1Content/Buffs", "KillMoveSpeed")
                    );
                    c.Index++;
                    c.RemoveRange(3);         
                    //c.Index += 4;
                    c.EmitDelegate<Func<RoR2.CharacterBody, float>>(body =>
                    {
                        if (body?.GetComponent<HuntersHarpoonBehavior>()?.generatingStacks == false)
                        {
                            return (float)body.GetBuffCount(DLC1Content.Buffs.KillMoveSpeed);
                        }
                        return 0.0f;
                    });
                    /*
                    c.Emit(OpCodes.Ldc_R4, 0.00f);
                    c.Emit(OpCodes.Mul);

                    c.TryGotoNext(
                        x => x.MatchLdloc(out _),
                        x => x.MatchLdcI4(out _),
                        x => x.MatchBle(out _),
                        x => x.MatchLdarg(out _)
                    );
                    //c.RemoveRange(3);
                    c.Index++;
                    c.EmitDelegate<Func<int, int>>(num =>
                    {
                        return 1;
                    });

                    c.TryGotoNext(
                        x => x.MatchLdloc(out _),
                        x => x.MatchLdloc(out _)
                    );
                    c.Index++;
                    c.Emit(OpCodes.Ldarg_0);
                    c.Index += 3;
                    c.Remove();
                    c.EmitDelegate<Func<RoR2.CharacterBody, float, float, float>>((body, whipCount, whipSpeedVal) =>
                    {
                        float moveSpeedVal = 0;
                        /*int harpoonCount = body.inventory.GetItemCount(DLC1Content.Items.MoveSpeedOnKill);
                        if (harpoonCount > 0 && (!body.outOfCombat || whipCount <= 0 ))
                        {

                        }
                        else
                        {
                            moveSpeedVal = whipCount * whipSpeedVal;
                        }
                        if (body.outOfCombat && whipCount > 0)
                        {
                            moveSpeedVal = whipCount * whipSpeedVal;
                        }
                        else if (body.inventory?.GetItemCount(DLC1Content.Items.MoveSpeedOnKill) > 0)
                        {
                            moveSpeedVal = body.GetBuffCount(RoR2Content.Buffs.WhipBoost) * 0.3f;
                        }
                        return moveSpeedVal;
                    });*/



                };
                /**
                 *  Stops old harpoon buff generation logic
                 */ 
                IL.RoR2.GlobalEventManager.OnCharacterDeath += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.TryGotoNext(
                        x => x.MatchLdloc(48),
                        x => x.MatchLdcI4(0),
                        x => x.MatchBle(out _)
                    );
                    c.Index++;
                    c.Emit(OpCodes.Ldc_I4_0);
                    c.Emit(OpCodes.Mul);
                };
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message + " - " + e.StackTrace);
            };
        }
        private void ReplaceSecondaryText()
        {
            this.ReplaceString("HUNTRESS_SECONDARY_DESCRIPTION", "Throw a seeking glaive that bounces up to <style=cIsDamage>6</style> times for <style=cIsDamage>250% damage</style>" +
                ". Damage increases by <style=cIsDamage>15%</style> per bounce.");
        }

        private void ReplaceString(string token, string newText)
        {
            this.DefaultLanguage[token] = Language.GetString(token);
            LanguageAPI.Add(token, newText);
        }
    }
}
