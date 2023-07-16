using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using R2API;
using System.Collections.Generic;
using UnityEngine;
using HuntersHarpoonRework.Behaviors;
using BepInEx.Logging;
using UnityEngine.AddressableAssets;

namespace HuntersHarpoonRework
{

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
        public const string PluginVersion = "1.0.1";

        private readonly Dictionary<string, string> DefaultLanguage = new Dictionary<string, string>();

        public static BuffDef BurstBuildup;
        
        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);
            BurstBuildup = ScriptableObject.CreateInstance<BuffDef>();
            BurstBuildup.name = "Hunter's Harpoon Buildup";
            BurstBuildup.buffColor = Color.white;
            BurstBuildup.canStack = true;
            BurstBuildup.isDebuff = false;
            BurstBuildup.eliteDef = null;
            BurstBuildup.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion();
            R2API.ContentAddition.AddBuffDef(BurstBuildup);

            try
            {
                // Either move to takeDamage in HealthComponent or modify this code to create a new damage instance on the victim (through projectile or calling takeDamage)
                /**
                 *  Damage boost based on harpoon buff stacks
                 */
                IL.RoR2.GlobalEventManager.OnHitEnemy += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.TryGotoNext(
                        x => x.MatchLdloc(out _),
                        x => x.MatchCallOrCallvirt(out _),
                        x => x.MatchBrtrue(out _)
                    );
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate<Action<RoR2.DamageInfo>>(damageInfo =>
                    {
                        CharacterBody body = damageInfo.attacker.GetComponent<CharacterBody>();
                        ProcChainMask mask = damageInfo.procChainMask;
                        int buffCount = body.GetBuffCount(BurstBuildup);
                        if (buffCount > 99 && damageInfo.procCoefficient >= 0.5f && !mask.HasProc(ProcType.Missile)
                            && !mask.HasProc(ProcType.ChainLightning) && !mask.HasProc(ProcType.BounceNearby) && !mask.HasProc(ProcType.Thorns) && !mask.HasProc(ProcType.LoaderLightning))
                        {
                            body.GetComponent<SkillLocator>()?.primary?.RunRecharge(1.0f);
                            body.GetComponent<SkillLocator>()?.secondary?.RunRecharge(1.0f);
                            body.GetComponent<SkillLocator>()?.utility?.RunRecharge(1.0f);
                            body.GetComponent<SkillLocator>()?.special?.RunRecharge(1.0f);
                            body.SetBuffCount(BurstBuildup.buffIndex, 0);
                            body.SetBuffCount(DLC1Content.Buffs.KillMoveSpeed.buffIndex, 0);
                            float itemCount = body.master?.inventory?.GetItemCount(DLC1Content.Items.MoveSpeedOnKill) ?? 0.0f;
                            for (int i = 1; i <= 5; i++)
                            {
                                if (itemCount > 0)
                                {
                                    body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, (1.0f + 2.0f * itemCount) / i);
                                    Log.LogInfo("Adding timed buff for: " + (1.0f + 2.0f * itemCount) / i + " seconds");
                                }           
                            }
                            /*body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, 3.0f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, 1.5f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, 0.75f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, 0.375f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, 0.185f);*/
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
                    });
                };

                /*IL.RoR2.HealthComponent.TakeDamage += (il) =>
                {
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
                        ProcChainMask mask = damageInfo.procChainMask;
                        int buffCount = body.GetBuffCount(BurstBuildup);
                        if (buffCount > 9 && damageInfo.procCoefficient >= 0.5f && !mask.HasProc(ProcType.Missile)
                            && !mask.HasProc(ProcType.ChainLightning) && !mask.HasProc(ProcType.BounceNearby) && !mask.HasProc(ProcType.Thorns) && !mask.HasProc(ProcType.LoaderLightning))
                        {
                            //newDamageVal *= (1.0f + (float)buffCount / 200);
                            body.GetComponent<SkillLocator>()?.primary?.RunRecharge(1.0f);
                            body.GetComponent<SkillLocator>()?.secondary?.RunRecharge(1.0f);
                            body.GetComponent<SkillLocator>()?.utility?.RunRecharge(1.0f);
                            body.GetComponent<SkillLocator>()?.special?.RunRecharge(1.0f);
                            body.SetBuffCount(BurstBuildup.buffIndex, 0);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.3f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.15f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.075f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.0375f);
                            body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, buffCount * 0.0185f);
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
                };*/

                /**
                 *  Adds item behavior
                 */
                IL.RoR2.CharacterBody.OnInventoryChanged += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.TryGotoNext(
                        x => x.MatchLdarg(out _),
                        x => x.MatchLdarg(out _),
                        x => x.MatchCallOrCallvirt<RoR2.CharacterBody>(nameof(RoR2.CharacterBody.inventory))
                    );
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Action<RoR2.CharacterBody>>(body =>
                    {
                        body.AddItemBehavior<HuntersHarpoonBehavior>(body.inventory.GetItemCount(DLC1Content.Items.MoveSpeedOnKill));
                    });
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
