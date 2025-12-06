using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BrynzaAPI.Interop;
using EntityStates;
using HarmonyLib;
using KinematicCharacterController;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using R2API;
using R2API.AddressReferencedAssets;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using Rewired;
using Rewired.Utils;
using Rewired.Utils.Classes.Data;
using RiskOfOptions;
using RiskOfOptions.Components.Panel;
using RiskOfOptions.Options;
using RoR2;
using RoR2.CameraModes;
using RoR2.ContentManagement;
using RoR2.ConVar;
using RoR2.Networking;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.UI;
using RoR2BepInExPack.GameAssetPaths;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static BrynzaAPI.Assets;
using static BrynzaAPI.BrynzaAPI;
using static BrynzaAPI.ContentPacks;
using static BrynzaAPI.GiveItemsDelegateDef;
using static BrynzaAPI.SniperHurtboxTracker;
using static RoR2.CameraModes.CameraModeBase;
using static System.Collections.Specialized.BitVector32;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: HG.Reflection.SearchableAttribute.OptIn]
[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]
[module: UnverifiableCode]
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace BrynzaAPI
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(R2API.CharacterBodyAPI.PluginGUID)]
    [BepInDependency(R2API.DamageAPI.PluginGUID)]
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [System.Serializable]
    public class BrynzaAPI : BaseUnityPlugin
    {
        public const string ModGuid = "com.brynzananas.brynzaapi";
        public const string ModName = "Brynza API";
        public const string ModVer = "1.3.0";
        public static FixedConditionalWeakTable<CharacterMotor, List<OnHitGroundServerDelegate>> onHitGroundServerDictionary = new FixedConditionalWeakTable<CharacterMotor, List<OnHitGroundServerDelegate>>();
        public delegate void OnHitGroundServerDelegate(CharacterMotor characterMotor, ref CharacterMotor.HitGroundInfo hitGroundInfo);
        public static bool riskOfOptionsLoaded = false;
        public static BepInEx.Configuration.ConfigFile ConfigMain;
        public static Dictionary<string, List<INetworkConfig>> modConfigs = new Dictionary<string, List<INetworkConfig>>();
        public static List<GameObject> activeSniperHurtboxTrackers = new List<GameObject>();
        public static event Action<GameObject> onSniperHurtboxAdded;
        public static event Action<GameObject> onSniperHurtboxRemoved;
        [Obsolete]
        private static Vector3 worldCrosshairPositionOverride = Vector3.zero;
        public static Dictionary<string, string> _tokenKeywords = new Dictionary<string, string>();
        private static string[] tokenKeywordsKeys = new string[0];
        public static HGButton loadoutSectionButton;
        public static GameObject loadoutSectionHolder;
        public const string LoadoutMainSectionToken = "LOADOUT_SECTION_MAIN";
        public static Dictionary<ConfigFile, Dictionary<ConfigDefinition, string>> defaultConfigValues = [];
        private static bool takeServerConfigValues
        {
            get { return (Run.instance || SceneManager.GetActiveScene().name == "lobby"); }
        }
        public static Dictionary<string, string> tokenKeywords
        {
            get { return _tokenKeywords; }
            set { tokenKeywordsKeys = value.Keys.ToArray(); _tokenKeywords = value; }
        }
        private Harmony harmonyPatcher;
        public static BaseUnityPlugin instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        public void Awake()
        {
            instance = this;
            Log = Logger;
            ConfigMain = Config;
            BunnyHopBuff = Utils.CreateBuff("bapiBunnyHop", null, Color.white, false, false, false, true, true);
            StrafeBuff = Utils.CreateBuff("bapiStrafing", null, Color.white, false, false, false, true, true);
            KeepVelocityBuff = Utils.CreateBuff("bapiKeepVelocity", null, Color.white, false, false, false, true, true);
            AirControlFromVelocityAddBuff = Utils.CreateBuff("bapiAirControlFromVelocityAdd", null, Color.white, false, false, false, true, true);
            NetworkingAPI.RegisterMessageType<SyncConfigsNetMessage>();
            NetworkingAPI.RegisterMessageType<RequestSyncConfigsNetMessage>();
            riskOfOptionsLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(RiskOfOptions.PluginInfo.PLUGIN_GUID);
            loadoutSectionButton = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/CharacterSelectUIMain.prefab").WaitForCompletion(), "LoadoutSectionButton", false).transform.Find("SafeArea/LeftHandPanel (Layer: Main)/SurvivorInfoPanel, Active (Layer: Secondary)/SubheaderPanel (Overview, Skills, Loadout)/GenericMenuButton (Overview)").gameObject.GetComponent<HGButton>();
            RectTransform rectTransform = loadoutSectionButton.gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(64f, 64f);
            loadoutSectionButton.onClick.RemoveAllListeners();
            Utils.AddLanguageToken(LoadoutMainSectionToken, "Main");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "Основной", "ru");
            //Languages further have been written using Yandex Translator. If it's wrong, please let me know!
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "الرئيسية", "ar");
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "Главни", "bg");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "主要", "zh-CN");
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "主要", "zh-TW");
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "Hlaveň", "cs");
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "Vigtigste", "nl");
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "Main", "fi");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "Principale", "fr");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "Wichtigsten", "de");
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "Κύριος", "el");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "Principale", "it");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "メイン", "ja");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "주요", "ko");
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "Viktigste", "no");
            //Utils.AddLanguageToken(LoadoutMainSectionToken, "Principal", "pt");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "Principal", "pt-BR");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "Ana", "tr");
            Utils.AddLanguageToken(LoadoutMainSectionToken, "Principal", "es");
            SetHooks();
        }
        public delegate void TestDelegate();
        public void OnDestroy()
        {
            UnsetHooks();
        }
        #region Hooks
        private void SetHooks()
        {
            if (hooksEnabled) return;
            hooksEnabled = true;
            IL.RoR2.Skills.SkillDef.OnFixedUpdate += SkillDef_OnFixedUpdate;
            IL.RoR2.Skills.SkillDef.OnExecute += SkillDef_OnExecute;
            IL.RoR2.UI.CrosshairManager.UpdateCrosshair += CrosshairManager_UpdateCrosshair1;
            IL.RoR2.CameraModes.CameraModePlayerBasic.UpdateInternal += CameraModePlayerBasic_UpdateInternal;
            IL.RoR2.CameraModes.CameraModePlayerBasic.CollectLookInputInternal += CameraModePlayerBasic_CollectLookInputInternal;
            On.EntityStates.GenericCharacterMain.HandleMovements += GenericCharacterMain_HandleMovements;
            IL.RoR2.GenericSkill.Awake += GenericSkill_Awake;
            //On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats1;
            //On.RoR2.GenericSkill.RecalculateMaxStock += GenericSkill_RecalculateMaxStock;
            //On.RoR2.GenericSkill.CalculateFinalRechargeInterval += GenericSkill_CalculateFinalRechargeInterval1;
            //IL.RoR2.GenericSkill.RecalculateMaxStock += GenericSkill_RecalculateMaxStock1;
            //IL.RoR2.GenericSkill.CalculateFinalRechargeInterval += GenericSkill_CalculateFinalRechargeInterval;
            IL.RoR2.CharacterMotor.PreMove += CharacterMotor_PreMove;
            IL.RoR2.Projectile.ProjectileExplosion.DetonateServer += ProjectileExplosion_DetonateServer;
            IL.EntityStates.GenericCharacterMain.ApplyJumpVelocity += GenericCharacterMain_ApplyJumpVelocity;
            On.RoR2.GlobalEventManager.OnCharacterHitGroundServer += GlobalEventManager_OnCharacterHitGroundServer;
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
            On.RoR2.Run.Start += Run_Start;
            On.RoR2.RoR2Application.OnLoad += RoR2Application_OnLoad;
            On.RoR2.UI.CharacterSelectController.OnEnable += CharacterSelectController_OnEnable;
            IL.RoR2.FogDamageController.MyFixedUpdate += FogDamageController_MyFixedUpdate;
            //On.RoR2.HurtBox.OnEnable += HurtBox_OnEnable;
            //On.RoR2.HurtBox.OnDisable += HurtBox_OnDisable;
            IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
            IL.EntityStates.GenericCharacterMain.ProcessJump_bool += GenericCharacterMain_ProcessJump_bool;
            IL.RoR2.CharacterMotor.OnLanded += CharacterMotor_OnLanded;
            IL.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            //IL.RoR2.GenericSkill.SetBonusStockFromBody += GenericSkill_SetBonusStockFromBody;
            //On.RoR2.GenericSkill.CanApplyAmmoPack += GenericSkill_CanApplyAmmoPack;
            //IL.RoR2.CameraModes.CameraModePlayerBasic.UpdateCrosshair += CameraModePlayerBasic_UpdateCrosshair;
            //IL.RoR2.CameraRigController.LateUpdate += CameraRigController_LateUpdate;
            //IL.RoR2.CameraRigController.SetCameraState += CameraRigController_SetCameraState;
            //On.RoR2.CameraRigController.SetCameraState += CameraRigController_SetCameraState1;
            On.RoR2.BulletAttack.ProcessHit += BulletAttack_ProcessHit;
            On.RoR2.UI.LoadoutPanelController.Awake += LoadoutPanelController_Awake;
            IL.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot += Row_FromSkillSlot;
            On.RoR2.UI.LoadoutPanelController.DestroyRows += LoadoutPanelController_DestroyRows;
            On.RoR2.UI.LoadoutPanelController.Row.FinishSetup += Row_FinishSetup;
            IL.RoR2.UI.LoadoutPanelController.Row.FromSkin += Row_FromSkin;
            On.RoR2.UI.LoadoutPanelController.Rebuild += LoadoutPanelController_Rebuild;
            //IL.RoR2.HealthComponent.Heal += HealthComponent_Heal;
            //IL.Unity.GeneratedNetworkCode._WritePhysForceInfo_None += GeneratedNetworkCode__WritePhysForceInfo_None;
            //IL.Unity.GeneratedNetworkCode._ReadPhysForceInfo_None += GeneratedNetworkCode__ReadPhysForceInfo_None;
            //IL.RoR2.BodyCatalog.SetBodyPrefabs += BodyCatalog_SetBodyPrefabs;
            On.RoR2.CharacterModel.UpdateRendererMaterials += CharacterModel_UpdateRendererMaterials;
            IL.RoR2.CharacterModel.UpdateMaterials += CharacterModel_UpdateMaterials;
            IL.RoR2.BulletAttack.DefaultHitCallbackImplementation += BulletAttack_DefaultHitCallbackImplementation;
            On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo += NetworkExtensions_Write_NetworkWriter_DamageInfo;
            On.RoR2.NetworkExtensions.ReadDamageInfo += NetworkExtensions_ReadDamageInfo;
            //IL.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool += HealthComponent_TakeDamageForce_DamageInfo_bool_bool;
            //IL.RoR2.BulletAttack.Fire += BulletAttack_Fire;
            IL.RoR2.BlastAttack.HandleHits += BlastAttack_HandleHits;
            IL.RoR2.BlastAttack.PerformDamageServer += BlastAttack_PerformDamageServer;
            On.RoR2.BlastAttack.BlastAttackDamageInfo.Write += BlastAttackDamageInfo_Write;
            On.RoR2.BlastAttack.BlastAttackDamageInfo.Read += BlastAttackDamageInfo_Read;
            //IL.RoR2.Util.BuildPrefabTransformPath += Util_BuildPrefabTransformPath;
            IL.RoR2.AimAnimator.UpdateAnimatorParameters += AimAnimator_UpdateAnimatorParameters;
            IL.RoR2.PlayerCharacterMasterController.PollButtonInput += PlayerCharacterMasterController_PollButtonInput;
            On.RoR2.CharacterBody.TriggerJumpEventGlobally += CharacterBody_TriggerJumpEventGlobally;
            RoR2Application.onLoadFinished += OnRoR2Loaded;
            harmonyPatcher = new Harmony(ModGuid);
            harmonyPatcher.CreateClassProcessor(typeof(Patches)).Patch();
        }

        private void UnsetHooks()
        {
            if (!hooksEnabled) return;
            hooksEnabled = false;
            IL.RoR2.Skills.SkillDef.OnFixedUpdate -= SkillDef_OnFixedUpdate;
            IL.RoR2.Skills.SkillDef.OnExecute -= SkillDef_OnExecute;
            IL.RoR2.UI.CrosshairManager.UpdateCrosshair -= CrosshairManager_UpdateCrosshair1;
            IL.RoR2.CameraModes.CameraModePlayerBasic.UpdateInternal -= CameraModePlayerBasic_UpdateInternal;
            IL.RoR2.CameraModes.CameraModePlayerBasic.CollectLookInputInternal -= CameraModePlayerBasic_CollectLookInputInternal;
            On.EntityStates.GenericCharacterMain.HandleMovements -= GenericCharacterMain_HandleMovements;
            IL.RoR2.GenericSkill.Awake -= GenericSkill_Awake;
            IL.RoR2.CharacterMotor.PreMove -= CharacterMotor_PreMove;
            IL.RoR2.Projectile.ProjectileExplosion.DetonateServer -= ProjectileExplosion_DetonateServer;
            IL.EntityStates.GenericCharacterMain.ApplyJumpVelocity -= GenericCharacterMain_ApplyJumpVelocity;
            On.RoR2.GlobalEventManager.OnCharacterHitGroundServer -= GlobalEventManager_OnCharacterHitGroundServer;
            ContentManager.collectContentPackProviders -= ContentManager_collectContentPackProviders;
            On.RoR2.Run.Start -= Run_Start;
            On.RoR2.RoR2Application.OnLoad -= RoR2Application_OnLoad;
            On.RoR2.UI.CharacterSelectController.OnEnable -= CharacterSelectController_OnEnable;
            IL.RoR2.FogDamageController.MyFixedUpdate -= FogDamageController_MyFixedUpdate;
            IL.RoR2.HealthComponent.TakeDamageProcess -= HealthComponent_TakeDamageProcess;
            IL.EntityStates.GenericCharacterMain.ProcessJump_bool -= GenericCharacterMain_ProcessJump_bool;
            IL.RoR2.CharacterMotor.OnLanded -= CharacterMotor_OnLanded;
            IL.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
            On.RoR2.BulletAttack.ProcessHit -= BulletAttack_ProcessHit;
            On.RoR2.UI.LoadoutPanelController.Awake -= LoadoutPanelController_Awake;
            IL.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot -= Row_FromSkillSlot;
            On.RoR2.UI.LoadoutPanelController.DestroyRows -= LoadoutPanelController_DestroyRows;
            On.RoR2.UI.LoadoutPanelController.Row.FinishSetup -= Row_FinishSetup;
            IL.RoR2.UI.LoadoutPanelController.Row.FromSkin -= Row_FromSkin;
            On.RoR2.UI.LoadoutPanelController.Rebuild -= LoadoutPanelController_Rebuild;
            On.RoR2.CharacterModel.UpdateRendererMaterials -= CharacterModel_UpdateRendererMaterials;
            IL.RoR2.CharacterModel.UpdateMaterials -= CharacterModel_UpdateMaterials;
            IL.RoR2.BulletAttack.DefaultHitCallbackImplementation -= BulletAttack_DefaultHitCallbackImplementation;
            On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo -= NetworkExtensions_Write_NetworkWriter_DamageInfo;
            On.RoR2.NetworkExtensions.ReadDamageInfo -= NetworkExtensions_ReadDamageInfo;
            IL.RoR2.BlastAttack.HandleHits -= BlastAttack_HandleHits;
            IL.RoR2.BlastAttack.PerformDamageServer -= BlastAttack_PerformDamageServer;
            On.RoR2.BlastAttack.BlastAttackDamageInfo.Write -= BlastAttackDamageInfo_Write;
            On.RoR2.BlastAttack.BlastAttackDamageInfo.Read -= BlastAttackDamageInfo_Read;
            IL.RoR2.AimAnimator.UpdateAnimatorParameters -= AimAnimator_UpdateAnimatorParameters;
            IL.RoR2.PlayerCharacterMasterController.PollButtonInput -= PlayerCharacterMasterController_PollButtonInput;
            On.RoR2.CharacterBody.TriggerJumpEventGlobally -= CharacterBody_TriggerJumpEventGlobally;
            RoR2Application.onLoadFinished -= OnRoR2Loaded;
        }
        private bool hooksEnabled = false;
        private void OnRoR2Loaded()
        {
            StartCoroutine(AddLanguageTokens());
        }
        private IEnumerator AddLanguageTokens()
        {
            while (LanguageTokensToAddOnLoad.languageTokensToAddOnLoad.Count > 0)
            {
                LanguageTokensToAddOnLoad.languageTokensToAddOnLoad[0].Dispose();
                yield return null;
            }
            yield break;
        }

        private void CharacterBody_TriggerJumpEventGlobally(On.RoR2.CharacterBody.orig_TriggerJumpEventGlobally orig, CharacterBody self)
        {
            orig(self);
            self.SetLastJumpTime(Run.FixedTimeStamp.now);
        }

        private void PlayerCharacterMasterController_PollButtonInput(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloc(16),
                    x => x.MatchStloc(6)
                ))
            {
                c.Index++;
                Instruction instruction = c.Next;
                Instruction instruction2 = c.Next.Next;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(GetFlag);
                bool GetFlag(PlayerCharacterMasterController playerCharacterMasterController) => playerCharacterMasterController.body.HasModdedBodyFlag(Assets.SprintAllTime);
                c.Emit(OpCodes.Brfalse_S, instruction);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldloc, 13);
                c.Emit(OpCodes.Ldc_I4, 18);
                //bool GetButtonInput(Player player, int i) => player.GetButton(i);
                c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(Player), nameof(Player.GetButton), [typeof(int)]));
                //c.Emit(OpCodes.Stloc, 6);
                //c.Emit(OpCodes.Br, instruction2);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
        }

        private void AimAnimator_UpdateAnimatorParameters(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdfld<AimAnimator>(nameof(AimAnimator.pitchClipCycleEnd)),
                    x => x.MatchLdcR4(0f)
                ))
            {
                c.Index++;
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SetPitch);
                float SetPitch(AimAnimator aimAnimator) => aimAnimator.GetPitchClipCycleStart();
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
            if (
                c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcR4(0f),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<AimAnimator>(nameof(AimAnimator.yawClipCycleEnd))
                ))
            {
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SetYaw);
                float SetYaw(AimAnimator aimAnimator) => aimAnimator.GetYawClipCycleStart();
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 2 failed!");
            }
        }

        private static void BlastAttackDamageInfo_Write(On.RoR2.BlastAttack.BlastAttackDamageInfo.orig_Write orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkWriter writer)
        {
            orig(ref self, writer);
            writer.Write(self.GetForceMassIsOne());
            writer.Write(self.GetForceAlwaysApply());
            writer.Write(self.GetForceDisableAirControlUntilCollision());
        }

        private static void BlastAttackDamageInfo_Read(On.RoR2.BlastAttack.BlastAttackDamageInfo.orig_Read orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkReader reader)
        {
            orig(ref self, reader);
            self.SetForceMassIsOne(reader.ReadBoolean());
            self.SetForceAlwaysApply(reader.ReadBoolean());
            self.SetForceDisableAirControlUntilCollision(reader.ReadBoolean());
        }

        private static void BlastAttack_PerformDamageServer(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            int locid = 9;
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchNewobj<DamageInfo>(),
                    x => x.MatchStloc(out locid)
                ))
            {
                c.Emit(OpCodes.Ldloc, locid);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SetBlastAttackForce1);
                void SetBlastAttackForce1(DamageInfo damageInfo, ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo)
                {
                    damageInfo.SetForceMassIsOne(blastAttackDamageInfo.GetForceMassIsOne());
                    damageInfo.SetForceAlwaysApply(blastAttackDamageInfo.GetForceAlwaysApply());
                    damageInfo.SetForceDisableAirControlUntilCollision(blastAttackDamageInfo.GetForceDisableAirControlUntilCollision());
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
        }

        private static void BlastAttack_HandleHits(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            int locid = 9;
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchLdloca(out locid),
                    x => x.MatchInitobj<BlastAttack.BlastAttackDamageInfo>()
                ))
            {
                c.Emit(OpCodes.Ldloca, locid);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SetBlastAttackForce1);
                void SetBlastAttackForce1(ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo, BlastAttack blastAttack)
                {
                    blastAttackDamageInfo.SetForceMassIsOne(blastAttack.GetForceMassIsOne());
                    blastAttackDamageInfo.SetForceAlwaysApply(blastAttack.GetForceAlwaysApply());
                    blastAttackDamageInfo.SetForceDisableAirControlUntilCollision(blastAttack.GetForceDisableAirControlUntilCollision());
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
        }

        private static void BulletAttack_Fire(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<BulletAttack>(nameof(BulletAttack.weapon)),
                    x => x.MatchCall<UnityEngine.Object>("op_Implicit"),
                    x => x.MatchBrtrue(out iLLabel)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(GetNoWeaponIfOwner);
                bool GetNoWeaponIfOwner(BulletAttack bulletAttack) => bulletAttack.GetNoWeaponIfOwner();
                c.Emit(OpCodes.Brtrue_S, iLLabel);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
        }

        private static string pendingDefaultValue;
        [HarmonyPatch]
        class Patches
        {
            [HarmonyPatch(typeof(BepInEx.Configuration.ConfigFile), nameof(BepInEx.Configuration.ConfigFile.Reload), MethodType.Normal)]
            [HarmonyILManipulator]
            private static void ConfigFile_Reload(MonoMod.Cil.ILContext il)
            {
                ILCursor c = new ILCursor(il);
                if (
                    c.TryGotoNext(MoveType.Before,
                        x => x.MatchLdloc(4),
                        x => x.MatchLdstr("#")
                        //x => x.MatchCallvirt<String>(nameof(String.Split))
                    ))
                {
                    c.Emit(OpCodes.Ldloc, 4);
                    c.EmitDelegate(GetDefaultValue);
                    void GetDefaultValue(string line)
                    {
                        if (!line.Contains("# Default value:")) return;
                        pendingDefaultValue = line.Replace("# Default value: ", "");
                    }
                    if (
                        c.TryGotoNext(MoveType.After,
                            x => x.MatchLdloc(8),
                            x => x.MatchLdloc(7)
                            //x => x.MatchCallvirt(typeof(Dictionary<ConfigDefinition, string>), "set_Item")
                        ))
                    {
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldloc, 8);
                        c.EmitDelegate(SetDefaultValue);
                        void SetDefaultValue(ConfigFile configFile, ConfigDefinition configDefinition)
                        {
                            if (pendingDefaultValue == null) return;
                            if (defaultConfigValues.ContainsKey(configFile))
                            {
                                Dictionary<ConfigDefinition, string> keyValuePairs = defaultConfigValues[configFile];
                                if (!keyValuePairs.ContainsKey(configDefinition)) keyValuePairs[configDefinition] = pendingDefaultValue;
                            }
                            else
                            {
                                Dictionary<ConfigDefinition, string> keyValuePairs = [];
                                keyValuePairs.Add(configDefinition, pendingDefaultValue);
                                defaultConfigValues.Add(configFile, keyValuePairs);
                            }
                        }
                    }
                    else
                    {
                        Log.LogError(il.Method.Name + " IL Hook 2 failed!");
                    }
                }
                else
                {
                    Log.LogError(il.Method.Name + " IL Hook 1 failed!");
                }
            }
        }
        private static void HealthComponent_TakeDamageForce_DamageInfo_bool_bool(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            int locId = 3;
            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloc(out locId),
                    x => x.MatchLdarg(1),
                    x => x.MatchLdfld<DamageInfo>(nameof(DamageInfo.force)),
                    x => x.MatchLdarg(2),
                    x => x.MatchLdarg(3),
                    x => x.MatchCallvirt<CharacterMotor>(nameof(CharacterMotor.ApplyForce))
                ))
            {
                Instruction instruction = c.Next.Next.Next.Next.Next.Next.Next;
                Instruction instruction1 = c.Next.Next;
                c.Index++;
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate(HasIsMassIsOne);
                bool HasIsMassIsOne(DamageInfo damageInfo) => damageInfo.GetForceMassIsOne();
                c.Emit(OpCodes.Brfalse_S, instruction1);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_2);
                c.Emit(OpCodes.Ldarg_3);
                c.EmitDelegate(ApplyForce);
                void ApplyForce(CharacterMotor characterMotor, DamageInfo damageInfo, bool awlaysApply, bool disableAirControlUntilCollision)
                {
                    PhysForceInfo physForceInfo = new PhysForceInfo
                    {
                        disableAirControlUntilCollision = disableAirControlUntilCollision,
                        ignoreGroundStick = awlaysApply,
                        force = damageInfo.force,
                        massIsOne = damageInfo.GetForceMassIsOne(),
                    };
                    characterMotor.ApplyForceImpulse(physForceInfo);
                }
                c.Emit(OpCodes.Br, instruction);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private DamageInfo NetworkExtensions_ReadDamageInfo(On.RoR2.NetworkExtensions.orig_ReadDamageInfo orig, NetworkReader reader)
        {
            DamageInfo damageInfo = orig(reader);
            damageInfo.SetForceMassIsOne(reader.ReadBoolean());
            damageInfo.SetForceAlwaysApply(reader.ReadBoolean());
            damageInfo.SetForceDisableAirControlUntilCollision(reader.ReadBoolean());
            return damageInfo;
        }

        private void NetworkExtensions_Write_NetworkWriter_DamageInfo(On.RoR2.NetworkExtensions.orig_Write_NetworkWriter_DamageInfo orig, NetworkWriter writer, DamageInfo damageInfo)
        {
            orig(writer, damageInfo);
            writer.Write(damageInfo.GetForceMassIsOne());
            writer.Write(damageInfo.GetForceAlwaysApply());
            writer.Write(damageInfo.GetForceDisableAirControlUntilCollision());
        }

        private void BulletAttack_DefaultHitCallbackImplementation(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            int locId = 3;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchNewobj<DamageInfo>(),
                    x => x.MatchStloc(out locId)
                ))
            {
                c.Emit(OpCodes.Ldloc, locId);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SetForceBooleans);
                void SetForceBooleans(DamageInfo damageInfo, BulletAttack bulletAttack)
                {
                    damageInfo.SetForceMassIsOne(bulletAttack.GetForceMassIsOne());
                    damageInfo.SetForceAlwaysApply(bulletAttack.GetForceAlwaysApply());
                    damageInfo.SetForceDisableAirControlUntilCollision(bulletAttack.GetForceDisableAirControlUntilCollision());

                }
                if (c.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<DamageInfo>(nameof(DamageInfo.force))
                ))
                {
                    c.Emit(OpCodes.Ldloc, locId);
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate(SetBonusForce);
                    void SetBonusForce(DamageInfo damageInfo, BulletAttack bulletAttack) => damageInfo.force += bulletAttack.GetBonusForce();
                }
                else
                {
                    Log.LogError(il.Method.Name + " IL Hook failed!");
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void CharacterModel_UpdateMaterials(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<CharacterModel>(nameof(CharacterModel.fade))
                ))
            {
                Instruction instruction = c.Next;
                Instruction instruction2 = c.Next.Next.Next;
                c.Emit(OpCodes.Ldloc, 6);
                c.EmitDelegate(FixFading);
                bool FixFading(CharacterModel.RendererInfo rendererInfo)
                {
                    if (rendererInfo.GetDontFadeWhenNearCamera()) return true;
                    Material material = rendererInfo.renderer ? rendererInfo.renderer.material : null;
                    if (!material || !material.HasInt("_FadeCloseOn")) return false;
                    return material.GetInt("_FadeCloseOn") == 0;
                }
                c.Emit(OpCodes.Brfalse_S, instruction);
                c.Emit(OpCodes.Ldc_R4, 1f);
                c.Emit(OpCodes.Br_S, instruction2);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void CharacterModel_UpdateRendererMaterials(On.RoR2.CharacterModel.orig_UpdateRendererMaterials orig, CharacterModel self, Renderer renderer, Material defaultMaterial, bool ignoreOverlays)
        {
            Material material = defaultMaterial;
            ParticleSystemRenderer particleSystemRenderer = renderer as ParticleSystemRenderer;
            bool fixTrailMaterial = false;
            if (particleSystemRenderer && particleSystemRenderer.trailMaterial)
            {
                fixTrailMaterial = true;
            }
            orig(self, renderer, defaultMaterial, ignoreOverlays);
            if (fixTrailMaterial)
            {
                particleSystemRenderer.trailMaterial = material;
            }
        }

        //private void BodyCatalog_SetBodyPrefabs(ILContext il)
        //{
        //    ILCursor c = new ILCursor(il);
        //    if (c.TryGotoNext(MoveType.After,
        //            x => x.MatchLdloc(3),
        //            x => x.MatchCallvirt(typeof(UnityEngine.Object).GetPropertyGetter("name")),
        //            x => x.MatchStloc(4)
        //        ))
        //    {
        //        c.Emit(OpCodes.Ldloca, 0);
        //        c.Emit(OpCodes.Ldarg_0);
        //        c.EmitDelegate(GetBodyIndex);
        //        void GetBodyIndex(ref PhysForceInfo physForceInfo, NetworkReader networkReader)
        //        {
        //            physForceInfo.ignoreGroundStick = networkReader.ReadBoolean();
        //            physForceInfo.disableAirControlUntilCollision = networkReader.ReadBoolean();
        //        }
        //        //c.Emit(OpCodes.Ldarg_0);
        //        //c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(NetworkReader), nameof(NetworkReader.ReadBoolean)));
        //        //c.Emit(OpCodes.Call, AccessTools.Field(typeof(PhysForceInfo), nameof(PhysForceInfo.ignoreGroundStick)));
        //        //c.Emit(OpCodes.Nop);
        //        //c.Emit(OpCodes.Ldarg_0);
        //        //c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(NetworkReader), nameof(NetworkReader.ReadBoolean)));
        //        //c.Emit(OpCodes.Call, AccessTools.Field(typeof(PhysForceInfo), nameof(PhysForceInfo.disableAirControlUntilCollision)));
        //        //c.Emit(OpCodes.Nop);
        //    }
        //    else
        //    {
        //        Debug.LogError(il.Method.Name + " IL Hook failed!");
        //    }
        //}

        private static void Util_BuildPrefabTransformPath(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(UnityEngine.Object), "op_Implicit"),
                    x => x.MatchBrtrue(out _),
                    x => x.MatchLdstr(out _),
                    x => x.MatchLdstr(out _),
                    x => x.MatchNewobj(out _),
                    x => x.MatchThrow()
                ))
            {
                c.RemoveRange(15);
                if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloc(1),
                    x => x.MatchCall(typeof(UnityEngine.Object), "op_Implicit"),
                    x => x.MatchBrtrue(out _),
                    x => x.MatchLdloca(out _),
                    x => x.MatchCall(out _),
                    x => x.MatchStloc(2),
                    x => x.MatchLdstr(out _),
                    x => x.MatchLdloc(2),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall<string>(nameof(String.Format)),
                    x => x.MatchNewobj(out _),
                    x => x.MatchThrow()
                ))
                {
                    c.RemoveRange(12);
                }
                else
                {
                    Log.LogError(il.Method.Name + " IL Hook 2 failed!");
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
        }

        private static void GeneratedNetworkCode__ReadPhysForceInfo_None(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchStfld<PhysForceInfo>(nameof(PhysForceInfo.force))
                ))
            {
                c.Emit(OpCodes.Ldloca, 0);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(Pissbox);
                void Pissbox(ref PhysForceInfo physForceInfo, NetworkReader networkReader)
                {
                    physForceInfo.ignoreGroundStick = networkReader.ReadBoolean();
                    physForceInfo.disableAirControlUntilCollision = networkReader.ReadBoolean();
                }
                //c.Emit(OpCodes.Ldarg_0);
                //c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(NetworkReader), nameof(NetworkReader.ReadBoolean)));
                //c.Emit(OpCodes.Call, AccessTools.Field(typeof(PhysForceInfo), nameof(PhysForceInfo.ignoreGroundStick)));
                //c.Emit(OpCodes.Nop);
                //c.Emit(OpCodes.Ldarg_0);
                //c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(NetworkReader), nameof(NetworkReader.ReadBoolean)));
                //c.Emit(OpCodes.Call, AccessTools.Field(typeof(PhysForceInfo), nameof(PhysForceInfo.disableAirControlUntilCollision)));
                //c.Emit(OpCodes.Nop);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void GeneratedNetworkCode__WritePhysForceInfo_None(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchCall<NetworkWriter>(nameof(NetworkWriter.Write))
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate(Hopoopoo);
                void Hopoopoo(NetworkWriter networkWriter, PhysForceInfo physForceInfo)
                {
                    networkWriter.Write(physForceInfo.ignoreGroundStick);
                    networkWriter.Write(physForceInfo.disableAirControlUntilCollision);
                }
                //c.Emit(OpCodes.Ldarg_0);
                //c.Emit(OpCodes.Ldarg_1);
                //c.Emit(OpCodes.Call, AccessTools.PropertyGetter(typeof(PhysForceInfo), nameof(PhysForceInfo.ignoreGroundStick)));
                //c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(NetworkWriter), nameof(NetworkWriter.Write), [typeof(bool)]));
                //c.Emit(OpCodes.Nop);
                //c.Emit(OpCodes.Ldarg_0);
                //c.Emit(OpCodes.Ldarg_1);
                //c.Emit(OpCodes.Call, AccessTools.PropertyGetter(typeof(PhysForceInfo), nameof(PhysForceInfo.disableAirControlUntilCollision)));
                //c.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(NetworkWriter), nameof(NetworkWriter.Write), [typeof(bool)]));
                //c.Emit(OpCodes.Nop);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        /*private void HealthComponent_Heal(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdflda<HealthComponent>(nameof(HealthComponent.itemCounts)),
                    x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.barrierOnOverHeal))
                ))
            {
                //c.Index++;
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate(SetSection);
                int SetSection(ProcChainMask procChainMask)
                {
                    return procChainMask.HasModdedProc(Assets.Overheal) ? 1 : 0;
                }
                c.Emit(OpCodes.Add);
            }
        }*/

        private static void LoadoutPanelController_Rebuild(On.RoR2.UI.LoadoutPanelController.orig_Rebuild orig, LoadoutPanelController self)
        {
            orig(self);
            int rowsCount = Section.sections.Count;
            if (rowsCount <= 0) return;
            if (rowsCount == 1)
            {
                ClearSections();
                return;
            }
            else
            {
                LayoutElement layoutElement = loadoutSectionHolder ? loadoutSectionHolder.GetComponent<LayoutElement>() : null;
                if (layoutElement != null)
                {
                    layoutElement.minHeight = 32f;
                    layoutElement.preferredHeight = 48f;
                    layoutElement.flexibleHeight = 0f;
                }
                Utils.SelectRowsSection(LoadoutMainSectionToken);
            }
        }

        private static void Row_FromSkin(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            //int newLocal = il.Body.Variables.Count;
            //il.Body.Variables.Add(new(il.Import(typeof(Transform))));
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchNewobj<LoadoutPanelController.Row>()
                ))
            {
                //c.Index++;
                c.Emit(OpCodes.Dup);
                c.EmitDelegate(SetSection);
                void SetSection(LoadoutPanelController.Row row)
                {
                    string section = LoadoutMainSectionToken;
                    CreateSection(row, section);
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
        private static void CreateSection(LoadoutPanelController.Row row, string sectionName)
        {
            if (Section.sectionByName.ContainsKey(sectionName))
            {
                Section section = Section.sectionByName[sectionName];
                section.rows.Add(row);
            }
            else
            {
                Section section = new Section(sectionName, row.primaryColor);
                section.rows.Add(row);
            }
        }
        private static void Row_FinishSetup(On.RoR2.UI.LoadoutPanelController.Row.orig_FinishSetup orig, LoadoutPanelController.Row self, bool addWIPIcons)
        {
            orig(self, addWIPIcons);
            //string section = self.GetSection();
            //if (rows.ContainsKey(section))
            //{
            //    rows[section].Add(self);
            //}
            //else
            //{
            //    List<LoadoutPanelController.Row> rows2 = new List<LoadoutPanelController.Row>();
            //    rows2.Add(self);
            //    rows.Add(section, rows2);
            //}
        }
        private static void ClearSections()
        {
            while (Section.sections.Count > 0)
            {
                Section.sections[0].Dispose();
            }
            LayoutElement layoutElement = loadoutSectionHolder ? loadoutSectionHolder.GetComponent<LayoutElement>() : null;
            if(layoutElement != null)
            {
                layoutElement.minHeight = 0f;
                layoutElement.preferredHeight = 0f;
            }
        }
        private static void LoadoutPanelController_DestroyRows(On.RoR2.UI.LoadoutPanelController.orig_DestroyRows orig, LoadoutPanelController self)
        {
            ClearSections();
            orig(self);
        }

        private static void Row_FromSkillSlot(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            //int newLocal = il.Body.Variables.Count;
            //il.Body.Variables.Add(new(il.Import(typeof(Transform))));
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchNewobj<LoadoutPanelController.Row>()
                ))
            {
                //c.Index++;
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Ldarg_3);
                c.EmitDelegate(SetSection);
                void SetSection(LoadoutPanelController.Row row, GenericSkill genericSkill)
                {
                    string section = genericSkill.GetSection();
                    if (section == null || section == "")
                    {
                        section = LoadoutMainSectionToken;
                    }
                    CreateSection(row, section);
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
        private static void LoadoutPanelController_Awake(On.RoR2.UI.LoadoutPanelController.orig_Awake orig, LoadoutPanelController self)
        {
            orig(self);
            if (loadoutSectionHolder == null)
            {
                loadoutSectionHolder = new GameObject("SectionHolder");
                Transform transform = loadoutSectionHolder.transform;
                loadoutSectionHolder.transform.SetParent(self.transform);
                transform.localScale = Vector3.one;
                transform.localRotation = Quaternion.identity;
                transform.localPosition = Vector3.zero;
                RectTransform rectTransform = loadoutSectionHolder.AddComponent<RectTransform>();
                CanvasRenderer canvasRenderer = loadoutSectionHolder.AddComponent<CanvasRenderer>();
                canvasRenderer.cullTransparentMesh = false;
                HorizontalLayoutGroup horizontalLayoutGroup = loadoutSectionHolder.AddComponent<HorizontalLayoutGroup>();
                RectOffset rectOffset = horizontalLayoutGroup.padding;
                rectOffset.right = 6;
                rectOffset.left = 6;
                rectOffset.top = 6;
                rectOffset.bottom = 6;
                horizontalLayoutGroup.childControlHeight = true;
                horizontalLayoutGroup.childControlWidth = true;
                LayoutElement layoutElement = loadoutSectionHolder.AddComponent<LayoutElement>();
                layoutElement.minHeight = 32f;
                layoutElement.preferredHeight = 48f;
                layoutElement.flexibleHeight = 0f;
            }

        }
        private static bool BulletAttack_ProcessHit(On.RoR2.BulletAttack.orig_ProcessHit orig, BulletAttack self, ref BulletAttack.BulletHit hitInfo)
        {
            if (hitInfo != null && hitInfo.hitHurtBox && hitInfo.hitHurtBox.healthComponent && self.GetIgnoreHitTargets())
            {
                List<object> targets = self.GetIgnoredHealthComponents();
                if (targets == null)
                {
                    self.SetIgnoredHealthComponents(new List<object>());
                    targets = self.GetIgnoredHealthComponents();
                }
                if (targets.Contains(hitInfo.hitHurtBox.healthComponent))
                {
                    return false;
                }
                else
                {
                    targets.Add(hitInfo.hitHurtBox.healthComponent);
                }
            }
            return orig(self, ref hitInfo);
        }
        private static void CharacterSelectController_OnEnable(On.RoR2.UI.CharacterSelectController.orig_OnEnable orig, RoR2.UI.CharacterSelectController self)
        {
            orig(self);
            SetConfigValues();
        }
        private static void GenericSkill_Awake(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall<GenericSkill>("get_skillFamily"),
                    x => x.MatchCallvirt<SkillFamily>("get_defaultSkillDef"),
                    x => x.MatchCall<GenericSkill>("set_defaultSkillDef")
                ))
            {
                c.RemoveRange(5);
                c.Emit(OpCodes.Ldarg_0);
                //c.Emit<GenericSkill>(OpCodes.Call, "get_skillFamily");
                c.EmitDelegate<Action<GenericSkill>>((cb) =>
                {
                    cb.defaultSkillDef = cb.skillFamily ? cb.skillFamily.defaultSkillDef : null;

                });
                //c.Emit(OpCodes.Brtrue_S, iLLabel);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void GenericCharacterMain_HandleMovements(On.EntityStates.GenericCharacterMain.orig_HandleMovements orig, EntityStates.GenericCharacterMain self)
        {
            if (self.characterBody && self.characterBody.HasModdedBodyFlag(Assets.SprintAllTime))
                self.sprintInputReceived = true;
            orig(self);
        }

        private static void CameraModePlayerBasic_CollectLookInputInternal(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (c.TryGotoNext(
                    x => x.MatchLdsfld<CameraRigController>(nameof(CameraRigController.enableSprintSensitivitySlowdown)),
                    x => x.MatchCallvirt<BoolConVar>("get_value"),
                    x => x.MatchBrfalse(out iLLabel)
                ))
            {
                c.Emit(OpCodes.Ldarg_2);
                c.Emit<CameraModeBase.CameraModeContext>(OpCodes.Ldflda, nameof(CameraModeBase.CameraModeContext.targetInfo));
                c.Emit<CameraModeBase.TargetInfo>(OpCodes.Ldfld, nameof(CameraModeBase.TargetInfo.body));
                c.EmitDelegate<Func<CharacterBody, bool>>((cb) =>
                {
                    return cb ? cb.HasModdedBodyFlag(Assets.SprintAllTime) : false;

                });
                c.Emit(OpCodes.Brtrue_S, iLLabel);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void SkillDef_OnExecute(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<SkillDef>("cancelSprintingOnActivation"),
                    x => x.MatchBrfalse(out iLLabel)
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<GenericSkill, bool>>((cb) =>
                {
                    return cb.characterBody.HasModdedBodyFlag(Assets.SprintAllTime);

                });
                c.Emit(OpCodes.Brtrue_S, iLLabel);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void SkillDef_OnFixedUpdate(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (c.TryGotoNext(
                    x => x.MatchLdarg(1),
                    x => x.MatchCallvirt<GenericSkill>("get_characterBody"),
                    x => x.MatchCallvirt<CharacterBody>("get_isSprinting"),
                    x => x.MatchBrfalse(out iLLabel)
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<GenericSkill, bool>>((cb) =>
                {
                    return cb && cb.characterBody ? cb.characterBody.HasModdedBodyFlag(Assets.SprintAllTime) : false;

                });
                c.Emit(OpCodes.Brtrue_S, iLLabel);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
        private static void CameraModePlayerBasic_UpdateInternal(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (
                c.TryGotoNext(
                    x => x.MatchLdarg(2),
                    x => x.MatchLdflda<CameraModeBase.CameraModeContext>("targetInfo"),
                    x => x.MatchLdfld<CameraModeBase.TargetInfo>("isSprinting"),
                    x => x.MatchBrfalse(out iLLabel)
                ))
            {
                //Debug.Log(c);
                //Debug.Log(iLLabel?.Target);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate(sus);
                bool sus(ref CameraModeBase.CameraModeContext cameraModeContext)
                {
                    if (cameraModeContext.targetInfo.body != null)
                    {
                        return cameraModeContext.targetInfo.body.HasModdedBodyFlag(Assets.SprintAllTime);
                    }
                    else
                    {
                        return true;
                    }

                }
                c.Emit(OpCodes.Brtrue_S, iLLabel);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
        private static void CrosshairManager_UpdateCrosshair1(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            ILLabel iLLabel2 = null;
            if (
                c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<CrosshairManager>("cameraRigController"),
                    x => x.MatchCallvirt<CameraRigController>("get_hasOverride"),
                    x => x.MatchBrtrue(out iLLabel)
                )
                &&
                c.TryGotoNext(
                    x => x.MatchLdarg(1),
                    x => x.MatchCallvirt<CharacterBody>("get_isSprinting"),
                    x => x.MatchBrfalse(out iLLabel2)
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<CharacterBody, bool>>((cb) =>
                {
                    return cb.HasModdedBodyFlag(Assets.SprintAllTime);
                });
                c.Emit(OpCodes.Brtrue_S, iLLabel);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
        private static void CameraRigController_SetCameraState1(On.RoR2.CameraRigController.orig_SetCameraState orig, CameraRigController self, CameraState cameraState)
        {
            orig(self, cameraState);
            if (worldCrosshairPositionOverride == Vector3.zero) return;
            self.transform.rotation = Quaternion.LookRotation(worldCrosshairPositionOverride - self.transform.position);
        }

        private void CameraRigController_SetCameraState(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Instruction instruction = null;
            if (c.TryGotoNext(MoveType.Before,
                    //x => x.MatchLdloc(5),
                    x => x.MatchLdfld<CameraState>(nameof(CameraState.rotation))
                    //x => x.MatchCall<CameraRigController>("set_crosshairWorldPosition")
                ))
            {
                c.Remove();
                c.EmitDelegate(SetOverrideWorldCrosshairPosition);
                Quaternion SetOverrideWorldCrosshairPosition(ref CameraState cameraState)
                {
                    return worldCrosshairPositionOverride == Vector3.zero ? cameraState.rotation : Quaternion.LookRotation(worldCrosshairPositionOverride - cameraState.position);
                }
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void CameraRigController_LateUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Instruction instruction = null;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdloc(5),
                    x => x.MatchLdfld<CameraModeBase.UpdateResult>(nameof(CameraModeBase.UpdateResult.crosshairWorldPosition)),
                    x => x.MatchCall<CameraRigController>("set_crosshairWorldPosition")
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SetOverrideWorldCrosshairPosition);
                void SetOverrideWorldCrosshairPosition(CameraRigController cameraRigController)
                {
                    if (worldCrosshairPositionOverride == Vector3.zero) return;
                    cameraRigController.crosshairWorldPosition = worldCrosshairPositionOverride;
                }
                //instruction = c.Next.Next.Next;
                //c.EmitDelegate(HasOverrideWorldCrosshairPosition);
                //bool HasOverrideWorldCrosshairPosition()
                //{
                //    return worldCrosshairPositionOverride == Vector3.zero ? false : true;
                //}
                //c.EmitDelegate(ReturnOverrideWorldCrosshairPosition);
                //Vector3 ReturnOverrideWorldCrosshairPosition(ref CameraModeBase.UpdateResult updateResult)
                //{
                //    return worldCrosshairPositionOverride;
                //}
                //c.Emit(OpCodes.Brtrue_S, instruction);
                //c.Emit(OpCodes.Pop);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void CameraModePlayerBasic_UpdateCrosshair(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Instruction instruction = null;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdcI4(-1),
                    x => x.MatchStloc(7)
                ))
            {
                instruction = c.Next;
                c.EmitDelegate(HasWorldCrosshairOverride);
                bool HasWorldCrosshairOverride()
                {
                    return worldCrosshairPositionOverride == Vector3.zero ? false : true;
                }
                c.Emit(OpCodes.Brfalse_S, instruction);
                c.Emit(OpCodes.Ldloc, 5);
                c.EmitDelegate(ReturnResults);
                void ReturnResults(RaycastHit[] raycastHits)
                {
                    HGPhysics.ReturnResults(raycastHits);
                }
                c.Emit(OpCodes.Ldarg, 5);
                c.EmitDelegate(GetWorldCrosshairOverride);
                Vector3 GetWorldCrosshairOverride()
                {
                    return worldCrosshairPositionOverride;
                }
                c.Emit(OpCodes.Stobj, typeof(Vector3));
                c.Emit(OpCodes.Ret);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        /*
private void GenericSkill_SetBonusStockFromBody(ILContext il)
{
   ILCursor c = new ILCursor(il);
   Instruction instruction = null;
   if (c.TryGotoNext(
           x => x.MatchLdarg(1)
           //x => x.MatchStfld<GenericSkill>(nameof(GenericSkill.bonusStockFromBody))
       ))
   {
       c.Remove();
       c.EmitDelegate();
       int SetBonusStock();
       //instruction = c.Next.Next;
       //c.EmitDelegate(HasSkillOverride);
       //int HasSkillOverride(GenericSkill genericSkill)
       //{
       //    GenericSkill linkedSKill = genericSkill.GetLinkedSkill();
       //    return linkedSKill ? linkedSKill.bonusStockFromBody : -1;
       //}
       //c.Emit(OpCodes.Ldc_I4, 0);
       //c.Emit(OpCodes.Blt_Un_S, instruction);
   }
   else
   {
       Debug.LogError(il.Method.Name + " IL Hook failed!");
   }
}*/
        /// <summary>
        /// A collection of modifiers for various stats. It will be passed down the event chain of GetStatCoefficients; add to the contained values to modify stats.
        /// </summary>
        public class ExtraStatHookEventArgs : EventArgs
        {
            #region wallJump
            /// <summary>Added to wall jump.</summary> <remarks>WALL_JUMP ~ (BASE_WALL_JUMP + wallJumpAdd) * wallJumpMult</remarks>
            public int wallJumpAdd = 0;
            /// <summary>Wall jump is multiplied by this number. Multiply this value by your multiplier.</summary> <inheritdoc cref="wallJumpAdd"/>
            public float wallJumpMult = 1f;
            #endregion
        }

        /// <summary>
        /// Used as the delegate type for the GetStatCoefficients event.
        /// </summary>
        /// <param name="sender">The CharacterBody which RecalculateStats is being called for.</param>
        /// <param name="args">An instance of StatHookEventArgs, passed to each subscriber to this event in turn for modification.</param>
        public delegate void ExtraStatHookEventHandler(CharacterBody sender, ExtraStatHookEventArgs args);

        private static event ExtraStatHookEventHandler _getExtraStatCoefficients;

        /// <summary>
        /// Subscribe to this event to modify one of the stat hooks which StatHookEventArgs covers. Fired during CharacterBody.RecalculateStats.
        /// </summary>
        public static event ExtraStatHookEventHandler GetExtraStatCoefficients
        {
            add
            {
                _getExtraStatCoefficients += value;
            }

            remove
            {
                _getExtraStatCoefficients -= value;
            }
        }

        private static ExtraStatHookEventArgs ExtraStatMods;
        private static void CharacterBody_RecalculateStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(ExtraStats);
            void ExtraStats(CharacterBody characterBody)
            {
                characterBody.SetMaxWallJumpCount(characterBody.GetBaseWallJumpCount());
                ExtraStatHookEventArgs extraStatHookEventArgs = new ExtraStatHookEventArgs();
                if (_getExtraStatCoefficients != null)
                {
                    foreach (ExtraStatHookEventHandler @event in _getExtraStatCoefficients.GetInvocationList())
                    {
                        try
                        {
                            @event(characterBody, extraStatHookEventArgs);
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            }
            c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcR4(0),
                    x => x.MatchCall<CharacterBody>("set_critHeal")
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(HasSkillOverride);
                void HasSkillOverride(CharacterBody characterBody)
                {
                    if (characterBody.skillLocator)
                    {
                        for (int i = 0; i < characterBody.skillLocator.allSkills.Length; i++)
                        {
                            GenericSkill genericSkill = characterBody.skillLocator.allSkills[i];
                            if (genericSkill == null) continue;
                            GenericSkill linkedSkill = genericSkill.GetLinkedSkill();
                            if (linkedSkill == null) continue;
                            genericSkill.cooldownScale = linkedSkill.cooldownScale;
                            genericSkill.finalRechargeInterval = linkedSkill.finalRechargeInterval;
                            genericSkill.SetBonusStockFromBody(linkedSkill.bonusStockFromBody);
                        }
                    }
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void CharacterMotor_OnLanded(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (
                c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchStfld<CharacterMotor>(nameof(CharacterMotor.jumpCount))
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SetWallJumpCount);
                void SetWallJumpCount(CharacterMotor characterMotor)
                {
                    characterMotor.SetWallJumpCount(0);
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
        private static void GenericCharacterMain_ProcessJump_bool(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Instruction instruction = null;
            Instruction instruction2 = null;
            Instruction instruction3 = null;
            Instruction instruction4 = null;
            ILLabel iLLabel = null;
            int newLocal = il.Body.Variables.Count;
            il.Body.Variables.Add(new(il.Import(typeof(bool))));
            if (
                c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BaseCharacterMain>(nameof(BaseCharacterMain.hasCharacterMotor)),
                x => x.MatchBrfalse(out var num)
                ))
            {
                c.Emit(OpCodes.Ldc_I4_0);
                c.Emit(OpCodes.Stloc, newLocal);
                c = new ILCursor(il);
                if (c.TryGotoNext(
                        x => x.MatchLdarg(0),
                        x => x.MatchCall<EntityStates.EntityState>("get_characterMotor"),
                        x => x.MatchLdfld<CharacterMotor>(nameof(CharacterMotor.jumpCount)),
                        x => x.MatchLdarg(0),
                        x => x.MatchCall<EntityStates.EntityState>("get_characterBody"),
                        x => x.MatchCallvirt<CharacterBody>("get_maxJumpCount"),
                        x => x.MatchClt(),
                        x => x.MatchBr(out iLLabel)
                    ))
                {
                    instruction = c.Next;
                    c.GotoLabel(iLLabel);
                    instruction2 = c.Prev;
                    c.Goto(instruction);
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate(WallJump);
                    bool WallJump(EntityState entityState)
                    {
                        CharacterMotor characterMotor = entityState.characterMotor;
                        KinematicCharacterMotor kinematicCharacterMotor = characterMotor.Motor;
                        if (kinematicCharacterMotor && !entityState.characterMotor.isGrounded && characterMotor.GetWallJumpCount() < entityState.characterBody.GetMaxWallJumpCount() && kinematicCharacterMotor.CharacterCollisionsOverlap(kinematicCharacterMotor.TransientPosition, kinematicCharacterMotor.TransientRotation, kinematicCharacterMotor._internalProbedColliders, 0.1f) > 0) return true;
                        return false;
                    }
                    instruction3 = c.Emit(OpCodes.Brtrue_S, instruction2).Prev;
                    c.Index--;
                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Brfalse_S, instruction3);
                    c.Emit(OpCodes.Ldc_I4_1);
                    c.Emit(OpCodes.Stloc, newLocal);
                    c = new ILCursor(il);
                    if (
                        c.TryGotoNext(MoveType.After,
                        x => x.MatchLdarg(0),
                        x => x.MatchCall<EntityStates.EntityState>("get_characterMotor"),
                        x => x.MatchDup(),
                        x => x.MatchLdfld<CharacterMotor>(nameof(CharacterMotor.jumpCount)),
                        x => x.MatchLdcI4(1),
                        x => x.MatchAdd(),
                        x => x.MatchStfld<CharacterMotor>(nameof(CharacterMotor.jumpCount))
                        ))
                    {
                        //instruction4 = c.Next;
                        //c.Index -= 8;
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldloc, newLocal);
                        c.EmitDelegate(CountWallJump);
                        void CountWallJump(EntityState entityState, bool isWallJump)
                        {
                            if (isWallJump)
                            {
                                CharacterMotor characterMotor = entityState.characterMotor;
                                characterMotor.SetWallJumpCount(characterMotor.GetWallJumpCount() + 1);
                                characterMotor.jumpCount--;
                            }
                        }
                        //c.Emit(OpCodes.Ldloc, newLocal);
                        //c.Emit(OpCodes.Brtrue_S, instruction4);
                    }
                    else
                    {
                        Log.LogError(il.Method.Name + " IL Hook 3 failed!");
                    }
                }
                else
                {
                    Log.LogError(il.Method.Name + " IL Hook 2 failed!");
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
            
        }

        private static void HealthComponent_TakeDamageProcess(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            int thatStructId = 0;
            Mono.Cecil.FieldReference damageInfoField = null;
            Mono.Cecil.FieldReference attackerBodyField = null;
            int i = 6;
            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(out thatStructId),
                    x => x.MatchLdfld(out damageInfoField),
                    x => x.MatchLdcI4(0),
                    x => x.MatchLdcI4(0),
                    x => x.MatchCall<HealthComponent>(nameof(HealthComponent.TakeDamageForce))
                ))
            {
                c.Index += 2;
                c.RemoveRange(2);
                c.Emit(OpCodes.Ldloc, thatStructId);
                c.Emit(OpCodes.Ldfld, damageInfoField);
                c.EmitDelegate(SetAlwaysApply);
                bool SetAlwaysApply(DamageInfo damageInfo) => damageInfo.GetForceAlwaysApply();
                c.Emit(OpCodes.Ldloc, thatStructId);
                c.Emit(OpCodes.Ldfld, damageInfoField);
                c.EmitDelegate(SetDisableAirControl);
                bool SetDisableAirControl(DamageInfo damageInfo) => damageInfo.GetForceDisableAirControlUntilCollision();
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
            c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloca(out var num),
                    x => x.MatchLdloc(out thatStructId),
                    x => x.MatchLdfld(out damageInfoField),
                    x => x.MatchLdfld<DamageInfo>(nameof(DamageInfo.attacker)),
                    x => x.MatchCallvirt<GameObject>(nameof(GameObject.GetComponent)),
                    x => x.MatchStfld(out attackerBodyField)
                ))
            {
                /*if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloc(7),
                    x => x.MatchStloc(8)
                ))
                {
                    c.Index++;
                    c.Emit(OpCodes.Ldloc, thatStructId);
                    c.Emit(OpCodes.Ldfld, damageInfoField);
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate(CheckForAnyFlags);
                    bool CheckForAnyFlags(DamageInfo damageInfo, HealthComponent healthComponent)
                    {
                        CharacterBody characterBody = healthComponent.body;
                        if(characterBody == null) return false;
                        bool isChampion = characterBody.isChampion;
                        return isChampion ? (damageInfo.HasModdedDamageType(InstakillChampion) || damageInfo.HasModdedDamageType(BruiseChampion)) : (damageInfo.HasModdedDamageType(InstakillNoChampion) || damageInfo.HasModdedDamageType(BruiseNoChampion));
                    }
                    c.Emit(OpCodes.Brfalse_S, c.Next);
                    c.Emit(OpCodes.Pop);
                    c.Emit(OpCodes.Ldloc, thatStructId);
                    c.Emit(OpCodes.Ldfld, damageInfoField);
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate(ReplaceDamageNumber);
                    float ReplaceDamageNumber(DamageInfo damageInfo, HealthComponent healthComponent)
                    {
                        float num = (damageInfo.HasModdedDamageType(InstakillNoChampion) || damageInfo.HasModdedDamageType(InstakillChampion)) ? healthComponent.fullCombinedHealth : healthComponent.fullCombinedHealth / 4f;
                        return num;
                    }
                }
                else
                {
                    Log.LogError(il.Method.Name + " IL Hook 3 failed!");
                }*/
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 2 failed!");
            }
        }

        private static bool GenericSkill_CanApplyAmmoPack(On.RoR2.GenericSkill.orig_CanApplyAmmoPack orig, GenericSkill self)
        {
            if (self.skillDef)
                self.skillDef.CanApplyAmmoPack();
            return orig(self);
        }

        internal static void OnSniperHurtboxAdded(GameObject sniperHurtboxTracker)
        {
            onSniperHurtboxAdded?.Invoke(sniperHurtboxTracker);
        }
        internal static void OnSniperHurtboxRemoved(GameObject sniperHurtboxTracker)
        {
            onSniperHurtboxRemoved?.Invoke(sniperHurtboxTracker);
        }
        private static void HurtBox_OnDisable(On.RoR2.HurtBox.orig_OnDisable orig, HurtBox self)
        {
            orig(self);
            Transform transform = self.transform.Find("SniperHurtboxTrackerObject");
            if (transform) Destroy(transform.gameObject);
        }

        private static void HurtBox_OnEnable(On.RoR2.HurtBox.orig_OnEnable orig, HurtBox self)
        {
            orig(self);
            if (!self.isSniperTarget) return;
            GameObject gameObject = new GameObject("SniperHurtboxTrackerObject");
            SniperHurtboxTracker sniperHurtboxTracker = gameObject.AddComponent<SniperHurtboxTracker>();
            sniperHurtboxTracker.hurtBox = self;
            sniperHurtboxTracker.transform.SetParent(self.transform, false);
        }

        private static void FogDamageController_MyFixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Instruction instruction = null;
            int i = 6;
            if (c.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<HealthComponent>(nameof(HealthComponent.TakeDamage))
                ))
            {
                instruction = c.Next;
                //c = new ILCursor(il);
                if (c.TryGotoPrev(
                x => x.MatchLdloc(out i),
                    x => x.MatchCallvirt<CharacterBody>("get_healthComponent")
                ))
                {
                    c.Index++;
                    c.Emit(OpCodes.Ldloc, i);
                    c.EmitDelegate(CheckForBodyFlog);
                    bool CheckForBodyFlog(CharacterBody characterBody)
                    {
                        return characterBody.HasModdedBodyFlag(Assets.ImmuneToVoidFog);
                    }
                    c.Emit(OpCodes.Brtrue_S, instruction);
                }
                else
                {
                    Log.LogError(il.Method.Name + " IL Hook 2 failed!");
                }
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            }
        }

        private static void GlobalEventManager_OnCharacterHitGroundServer(On.RoR2.GlobalEventManager.orig_OnCharacterHitGroundServer orig, GlobalEventManager self, CharacterBody characterBody, CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            CharacterMotor characterMotor = characterBody.characterMotor;
            if (characterMotor != null && onHitGroundServerDictionary.ContainsKey(characterMotor))
            {
                List<OnHitGroundServerDelegate> onHitGroundDelegates = onHitGroundServerDictionary[characterMotor];
                for (int i = 0; i < onHitGroundDelegates.Count; i++)
                {
                    OnHitGroundServerDelegate onHitGroundDelegate = onHitGroundDelegates[i];
                    onHitGroundDelegate?.Invoke(characterMotor, ref hitGroundInfo);
                }
            }
            orig(self, characterBody, hitGroundInfo);
            IOnGroundHitServer[] onGroundHitServers = characterBody.gameObject.GetComponents<IOnGroundHitServer>();
            if (onGroundHitServers != null) foreach (IOnGroundHitServer onGroundHitServer in onGroundHitServers) onGroundHitServer.OnGroundHitServer(characterBody, hitGroundInfo);
        }

        private static void GenericCharacterMain_ApplyJumpVelocity(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (c.TryGotoNext(
                    x => x.MatchStloc(2)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Func<CharacterMotor, Vector3, CharacterBody, float, float>>((cm, vector, cb, horizontalBonus) =>
                {
                    bool canBunnyHop = cm.GetBunnyHop();
                    float multiplier = 1f;
                    if (canBunnyHop)
                    {
                        Vector3 vector3 = new(cm.velocity.x, 0f, cm.velocity.z);
                        float num = vector.sqrMagnitude * horizontalBonus * horizontalBonus;
                        multiplier = num != 0 ? MathF.Max(num - vector3.sqrMagnitude, 0f) / num : 1f;
                    }
                    return multiplier;
                });
                c.Emit(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Multiply", new Type[] {typeof(Vector3), typeof(float)}));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<CharacterMotor, Vector3>>((cm) =>
                {
                    Vector3 vector3 = Vector3.zero;
                    bool canBunnyHop = cm.GetBunnyHop();
                    if (canBunnyHop) vector3 = cm.velocity;
                    return vector3;
                });
                c.Emit(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Addition"));
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider.Invoke(new ContentPacks());
        }
        /*
private void BulletAttack_Fire(ILContext il)
{
   ILCursor c = new ILCursor(il);
   ILLabel iLLabel = null;
   if (c.TryGotoNext(
           x => x.MatchLdarg(0),
           x => x.MatchLdfld<BulletAttack>(nameof(BulletAttack.weapon)),
           x => x.MatchCall<UnityEngine.Object>("op_Implicit"),
           x => x.MatchBrfalse(out iLLabel)
       ))
   {
       c.Index += 4;
       c.Emit(OpCodes.Ldarg_0);
       c.EmitDelegate<Func<BulletAttack, int>>((ba) =>
       {
           GameObject gameObject = ba.GetWeaponOverride();
           int i = -1;
           if (gameObject != null)
           {
               ChildLocator childLocator = gameObject.GetComponentInChildren<ChildLocator>();
               if(childLocator != null)
               {
                   ba.weapon = childLocator.gameObject;
                   i = childLocator.FindChildIndex(ba.muzzleName);
               }
           }
           return i;
       });
       c.Emit(OpCodes.Stloc_1);
       c.Emit(OpCodes.Ldarg_0);
       c.EmitDelegate<Func<BulletAttack, bool>>((ba) =>
       {
           GameObject gameObject = ba.GetWeaponOverride();
           if (gameObject != null)
           {
               return true;
           }
           else
           {
               return false;
           }
       });
       c.Emit(OpCodes.Brtrue_S, iLLabel);
   }
   else
   {
       Debug.LogError(il.Method.Name + " IL Hook failed!");
   }
}
*/
        private static void ProjectileExplosion_DetonateServer(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (c.TryGotoNext(
                    x => x.MatchCallvirt<ProjectileExplosion>(nameof(ProjectileExplosion.OnBlastAttackResult))
                ))
            {
                c.Index += 1;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Ldloc_2);
                c.EmitDelegate<Action<ProjectileExplosion, BlastAttack, BlastAttack.Result>>((pe, ba, bar) =>
                {
                    IOnProjectileExplosionDetonate[] onProjectileExplosionDetonateInterfaces = pe.GetComponents<IOnProjectileExplosionDetonate>();
                    foreach (IOnProjectileExplosionDetonate onProjectileExplosionDetonate in onProjectileExplosionDetonateInterfaces)
                    {
                        onProjectileExplosionDetonate.OnProjectileExplosionDetonate(ba, bar);
                    }
                });
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }
        public static float strafeMultiplier = 15f;
        private static void CharacterMotor_PreMove(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (c.TryGotoNext(
                    x => x.MatchStfld<CharacterMotor>(nameof(CharacterMotor.velocity))
                ))
            {
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Action<CharacterMotor, Vector3, float>>(delegate (CharacterMotor cb, Vector3 wishDirection, float deltaTime)
                {
                    if (cb.GetStrafe())
                    {
                        var currentVelocity = new Vector3(cb.velocity.x, 0, cb.velocity.z);
                        var dotProduct = Vector3.Dot(currentVelocity.normalized, wishDirection);
                        if (dotProduct < 0.10)
                            cb.velocity += new Vector3(wishDirection.x * cb.walkSpeed * strafeMultiplier, 0, wishDirection.z * cb.walkSpeed * strafeMultiplier) * deltaTime;
                    }
                    Vector3 velocityOverride = cb.GetVelocityOverride();
                    bool flag2 = velocityOverride != Vector3.zero;
                    if (flag2)
                    {
                        cb.velocity = velocityOverride;
                    }
                });
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 1 failed!");
            } 
            c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchStloc(2)
                ))
            {
                c.Index++;
                //c.Emit(OpCodes.Ldloc_2);
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Ldloc_2);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(sus);
                Vector3 sus(Vector3 wishDirection, Vector3 vector31, CharacterMotor characterMotor)
                {
                    Vector3 velocity = characterMotor.velocity;
                    if (!characterMotor.isGrounded && characterMotor.GetKeepVelocityOnMoving())
                    {
                        if (characterMotor.isFlying)
                        {
                            return (characterMotor.velocity + vector31).normalized * MathF.Max(characterMotor.velocity.magnitude, vector31.magnitude);
                        }
                        else
                        {
                            var currentVelocity = new Vector3(characterMotor.velocity.x, 0, characterMotor.velocity.z);
                            return (currentVelocity + vector31).normalized * MathF.Max(currentVelocity.magnitude, vector31.magnitude);
                        }
                        
                    }
                    return vector31;
                }
                //c.Emit(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Addition"));
                c.Emit(OpCodes.Stloc_2);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 2 failed!");
            }
            c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchStloc(0)
                ))
            {
                c.Index++;
                //f.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(amogus);
                float amogus(float originalAirControl, CharacterMotor characterMotor)
                {
                    float overrideAirControl = characterMotor.GetConsistentAcceleration();
                    return overrideAirControl != 0 ? overrideAirControl * characterMotor.walkSpeed : originalAirControl;
                }
                c.Emit(OpCodes.Stloc_0);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 3 failed!");
            }
            c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchLdarg(1),
                    x => x.MatchMul()
                ))
            {
                c.Index++;
                //f.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(amogus);
                float amogus(Vector3 wishDirection, CharacterMotor characterMotor)
                {
                    if (characterMotor.GetFluidMaxDistanceDelta())
                    {
                        return 1;
                    }
                    return 1;
                }
                c.Emit(OpCodes.Mul);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 4 failed!");
            }
            c = new ILCursor(il);
            if (c.TryGotoNext(
                    x => x.MatchMul(),
                    x => x.MatchStloc(0)
                ))
            {
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(amogus);
                float amogus(CharacterMotor characterMotor)
                {
                    float magnitude = characterMotor.GetAirControlFromVelocityAdd();
                    magnitude += characterMotor.body ? characterMotor.body.GetBuffCount(Assets.AirControlFromVelocityAddBuff) : 0;
                    if (magnitude == 0) return 0f;
                    Vector3 vector3 = characterMotor.velocity;
                    if (!characterMotor.isFlying) vector3.y = 0f;
                    magnitude *= vector3.magnitude;
                    return magnitude;
                }
                c.Emit(OpCodes.Add);
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook 5 failed!");
            }
        }

        private static void CharacterBody_RecalculateStats1(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (!self.skillLocator) return;
            List<GenericSkill> allSkills = new List<GenericSkill>();
            if (self.skillLocator.allSkills != null)
                allSkills.AddRange(self.skillLocator.allSkills);
            //List<GenericSkill> list = self.skillLocator.GetBonusSkills();
            //if (list != null)
            //    allSkills.AddRange(list);
            foreach (GenericSkill skill in allSkills)
            {
                if (skill != null)
                {
                    GenericSkill linkedSkill = skill.GetLinkedSkill();
                    if (linkedSkill != null)
                    {
                        skill.cooldownScale = linkedSkill.cooldownScale;
                        skill.flatCooldownReduction = linkedSkill.flatCooldownReduction;
                        skill.bonusStockFromBody = linkedSkill.bonusStockFromBody;
                        skill.RecalculateValues();
                    };
                    //List<GenericSkill> extraSkills = skill.GetExtraSkills();
                    //if (extraSkills != null && extraSkills.Count > 0)
                    //    foreach (GenericSkill extraSkill in extraSkills)
                    //    {
                    //        extraSkill.cooldownScale = skill.cooldownScale;
                    //        extraSkill.flatCooldownReduction = skill.flatCooldownReduction;
                    //        extraSkill.bonusStockFromBody = skill.bonusStockFromBody;
                    //        extraSkill.RecalculateValues();
                    //    }
                }

            }
        }

        private static float GenericSkill_CalculateFinalRechargeInterval1(On.RoR2.GenericSkill.orig_CalculateFinalRechargeInterval orig, GenericSkill self)
        {
            List<GenericSkill> genericSkills = self.GetExtraSkills();
            if (genericSkills != null && genericSkills.Count > 0)
                foreach (GenericSkill skill in genericSkills)
                {
                    if (skill == self) continue;
                    skill.cooldownScale = self.cooldownScale;
                    skill.flatCooldownReduction = self.flatCooldownReduction;
                    skill.temporaryCooldownPenalty = self.temporaryCooldownPenalty;
                    skill.CalculateFinalRechargeInterval();
                }
            return orig(self);
        }

        private static void GenericSkill_CalculateFinalRechargeInterval(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            //Instruction instruction = null;
            //Instruction instruction2 = null;
            if (c.TryGotoNext(
                    x => x.MatchCall<GenericSkill>("get_cooldownScale")
                ))
            {
                //instruction = c.Next;
                //instruction2 = c.Next.Next;
                c.Remove();
                c.EmitDelegate<Func<GenericSkill, float>>((cb) =>
                {
                    GenericSkill genericSkill = cb.GetLinkedSkill();
                    if (genericSkill != null)
                    {
                        return genericSkill.cooldownScale;
                    }
                    else
                    {
                        return cb.cooldownScale;
                    }

                });
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
            if (c.TryGotoNext(
                    x => x.MatchCall<GenericSkill>("get_flatCooldownReduction")
                ))
            {
                c.Remove();
                c.EmitDelegate<Func<GenericSkill, float>>((cb) =>
                {
                    GenericSkill genericSkill = cb.GetLinkedSkill();
                    if (genericSkill != null)
                    {
                        return genericSkill.flatCooldownReduction;
                    }
                    else
                    {
                        return cb.flatCooldownReduction;
                    }

                });
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
            if (c.TryGotoNext(
                    x => x.MatchCall<GenericSkill>("get_temporaryCooldownPenalty")
                ))
            {
                c.Remove();
                c.EmitDelegate<Func<GenericSkill, float>>((cb) =>
                {
                    GenericSkill genericSkill = cb.GetLinkedSkill();
                    if (genericSkill != null)
                    {
                        return genericSkill.temporaryCooldownPenalty;
                    }
                    else
                    {
                        return cb.temporaryCooldownPenalty;
                    }

                });
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }


        private static void GenericSkill_RecalculateMaxStock1(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel iLLabel = null;
            if (c.TryGotoNext(
                    x => x.MatchLdfld<GenericSkill>(nameof(GenericSkill.bonusStockFromBody))
                ))
            {
                c.Remove();
                c.EmitDelegate<Func<GenericSkill, int>>((cb) =>
                {
                    GenericSkill genericSkill = cb.GetLinkedSkill();
                    if (genericSkill != null)
                    {
                        return genericSkill.bonusStockFromBody;
                    }
                    else
                    {
                        return cb.bonusStockFromBody;
                    }

                });
            }
            else
            {
                Log.LogError(il.Method.Name + " IL Hook failed!");
            }
        }

        private static void GenericSkill_RecalculateMaxStock(On.RoR2.GenericSkill.orig_RecalculateMaxStock orig, GenericSkill self)
        {
            orig(self);
            List<GenericSkill> genericSkills = self.GetExtraSkills();
            if (genericSkills != null && genericSkills.Count > 0)
            foreach (GenericSkill skill in genericSkills)
            {
                if(skill == self) continue;
                skill.bonusStockFromBody = self.bonusStockFromBody;
                skill.RecalculateMaxStock();
            }
        }
        #endregion Hooks
        public struct ModMetaData
        {
            public string Guid;

            public string Name;
        }
        public delegate void OnConfigApplied(int configId, INetworkConfig networkConfig);
        private System.Collections.IEnumerator RoR2Application_OnLoad(On.RoR2.RoR2Application.orig_OnLoad orig, RoR2Application self)
        {
            SetConfigValues();
            return orig(self);
        }
        public static void CreateResetToDefaultButtonInRiskOfOptionsConfigMenu(string name, string category, string description, string buttonText)
        {
            ModMetaData modMetaData = Assembly.GetCallingAssembly().GetModMetaData();
            ModSettingsManager.AddOption(new GenericButtonOption(name, category, description, buttonText, OnButtonPressed), modMetaData.Guid, modMetaData.Name);
            void OnButtonPressed()
            {
                if (modConfigs.ContainsKey(modMetaData.Guid))
                {
                    foreach (var config in modConfigs[modMetaData.Guid])
                    {
                        if (config.parameterType == typeof(float))
                        {
                            NetworkConfig<float> config2 = (NetworkConfig<float>)config;
                            config2.configEntry.Value = config2.DefaultValue;
                            config2.Value = config2.DefaultValue;
                        }
                        if (config.parameterType == typeof(int))
                        {
                            NetworkConfig<int> config2 = (NetworkConfig<int>)config;
                            config2.configEntry.Value = config2.DefaultValue;
                            config2.Value = config2.DefaultValue;
                        }
                        if (config.parameterType == typeof(bool))
                        {
                            NetworkConfig<bool> config2 = (NetworkConfig<bool>)config;
                            config2.configEntry.Value = config2.DefaultValue;
                            config2.Value = config2.DefaultValue;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Create new NetworkConfig.
        /// </summary>
        public static NetworkConfig<T> CreateConfig<T>(ConfigFile configFile, string section, string key, T defaultValue, string description, OnConfigApplied onConfigApplied = null, NetworkConfig<bool> enableConfig = null, bool generateRiskOfOptionsOption = true)
        {
            ConfigEntry<T> configEntry = configFile.Bind<T>(section, key, defaultValue, description);
            return CreateConfig(configEntry, onConfigApplied, enableConfig, generateRiskOfOptionsOption, Assembly.GetCallingAssembly());
        }
        /// <summary>
        /// Create NetworkConfig from already existing config. Keep in mind that you still need to use NetworkConfig.
        /// </summary>
        public static NetworkConfig<T> CreateConfig<T>(ConfigEntry<T> configEntry, OnConfigApplied onConfigApplied = null, NetworkConfig<bool> enableConfig = null, bool generateRiskOfOptionsOption = true, Assembly assembly = null)
        {
            NetworkConfig<T> config = new NetworkConfig<T>();
            config.id = networkConfigs.Count;
            config.enableConfig = enableConfig;
            config.OnConfigApplied = onConfigApplied;
            config.configEntry = configEntry;
            config.configEntry.SettingChanged += ConfigEntry_SettingChanged;
            networkConfigs.Add(config);
            ModMetaData modMetaData = assembly == null ? Assembly.GetCallingAssembly().GetModMetaData() : assembly.GetModMetaData();
            if (modConfigs.ContainsKey(modMetaData.Guid))
            {
                modConfigs[modMetaData.Guid].Add(config);
            }
            else
            {
                List<INetworkConfig> networkConfigs = new List<INetworkConfig>();
                networkConfigs.Add(config);
                modConfigs.Add(modMetaData.Guid, networkConfigs);
            }
            if (riskOfOptionsLoaded && generateRiskOfOptionsOption)
            {
                if (configEntry is ConfigEntry<float>)
                {
                    ModSettingsManager.AddOption(new FloatFieldOption(config.configEntry as ConfigEntry<float>), modMetaData.Guid, modMetaData.Name);
                }
                if (configEntry is ConfigEntry<int>)
                {
                    ModSettingsManager.AddOption(new IntFieldOption(config.configEntry as ConfigEntry<int>), modMetaData.Guid, modMetaData.Name);
                }
                if (configEntry is ConfigEntry<bool>)
                {
                    ModSettingsManager.AddOption(new CheckBoxOption(config.configEntry as ConfigEntry<bool>), modMetaData.Guid, modMetaData.Name);
                }
            }
            return config;
        }
        private static void ConfigEntry_SettingChanged(object sender, EventArgs e)
        {
            SetConfigValues();
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                SetConfigValues();
            }
            else
            {
                new RequestSyncConfigsNetMessage().Send(NetworkDestination.Server);
            }

        }
        public static List<INetworkConfig> networkConfigs = new List<INetworkConfig>();

        public interface INetworkConfig
        {
            int id { get; set; }
            OnConfigApplied OnConfigApplied { get; set; }
            Type parameterType { get; }
            object Value {  get; set; }
        }
        public class NetworkConfig<T> : INetworkConfig
        {
            public NetworkConfig<bool> enableConfig;
            public ConfigEntry<T> configEntry;
            private T configValue;
            private OnConfigApplied onConfigApplied;
            public int configId;
            public T Value
            {
                get
                {
                    if (enableConfig != null && enableConfig.Value == false)
                    {
                        return DefaultValue;
                    }
                    if (takeServerConfigValues)
                    {
                        return configValue;
                    }
                    else
                    {
                        return configEntry.Value;
                    }
                }
                set
                {
                    configValue = value;
                    //configEntry.OnSettingChanged(configEntry);
                }
            }
            public T DefaultValue
            {
                get
                {
                    return (T)configEntry.DefaultValue;
                }
            }
            public Type parameterType
            {
                get
                {
                    return typeof(T);
                }
            }
            public OnConfigApplied OnConfigApplied
            {
                get
                {
                    return onConfigApplied;
                }
                set
                {
                    onConfigApplied = value;
                }
            }
            public int id
            {
                get
                {
                    return configId;
                }
                set
                {
                    configId = value;
                }
            }
            object INetworkConfig.Value { get => Value; set => Value = (T)value; }
        }
        /// <summary>
        /// Updates confings using server values.
        /// </summary>
        public static void SetConfigValues()
        {
            if (NetworkServer.active)
            {
                foreach (INetworkConfig networkConfig in networkConfigs)
                {
                    if (networkConfig.parameterType == typeof(float))
                    {
                        new SyncConfigsNetMessage(networkConfigs.IndexOf(networkConfig), (networkConfig as NetworkConfig<float>).configEntry.Value.ToString()).Send(NetworkDestination.Clients);
                    }
                    if (networkConfig.parameterType == typeof(int))
                    {
                        new SyncConfigsNetMessage(networkConfigs.IndexOf(networkConfig), (networkConfig as NetworkConfig<int>).configEntry.Value.ToString()).Send(NetworkDestination.Clients);
                    }
                    if (networkConfig.parameterType == typeof(bool))
                    {
                        new SyncConfigsNetMessage(networkConfigs.IndexOf(networkConfig), (networkConfig as NetworkConfig<bool>).configEntry.Value.ToString()).Send(NetworkDestination.Clients);
                    }

                }
            }

        }
        public class RequestSyncConfigsNetMessage : INetMessage
        {
            public RequestSyncConfigsNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
            }

            public void OnReceived()
            {
                SetConfigValues();
            }

            public void Serialize(NetworkWriter writer)
            {
            }
        }
        public class SyncConfigsNetMessage : INetMessage
        {
            int configId;
            string input;
            public SyncConfigsNetMessage(int id, string input)
            {
                configId = id;
                this.input = input;
            }
            public SyncConfigsNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
                configId = reader.ReadInt32();
                input = reader.ReadString();
            }

            public void OnReceived()
            {
                INetworkConfig networkConfig = networkConfigs[configId];
                if (networkConfig.parameterType == typeof(float))
                {
                    NetworkConfig<float> networkConfig1 = networkConfig as NetworkConfig<float>;
                    networkConfig1.Value = float.Parse(input);
                    if (!NetworkServer.active)
                    {
                        //networkConfig1.configEntry.OnSettingChanged(networkConfig1.configEntry);
                        networkConfig1.configEntry.Value += 1f;
                        networkConfig1.configEntry.Value -= 1f;
                    }

                }
                if (networkConfig.parameterType == typeof(int))
                {
                    NetworkConfig<int> networkConfig1 = networkConfig as NetworkConfig<int>;
                    networkConfig1.Value = int.Parse(input);
                    if (!NetworkServer.active)
                    {
                        networkConfig1.configEntry.Value += 1;
                        networkConfig1.configEntry.Value -= 1;
                    }
                }
                if (networkConfig.parameterType == typeof(bool))
                {
                    NetworkConfig<bool> networkConfig1 = networkConfig as NetworkConfig<bool>;
                    networkConfig1.Value = bool.Parse(input);
                    if (!NetworkServer.active)
                    {
                        networkConfig1.configEntry.Value = !networkConfig1.configEntry.Value;
                        networkConfig1.configEntry.Value = !networkConfig1.configEntry.Value;
                    }
                }
                networkConfig.OnConfigApplied?.Invoke(configId, networkConfig);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(configId);
                writer.Write(input);
            }
        }
    }
    public class AddBuffNetMessage : INetMessage
    {
        NetworkInstanceId instanceId;
        int buffIndex;
        int amount;
        float buffTime;
        public AddBuffNetMessage(NetworkInstanceId networkInstanceId, int buffIndex, int amount, float buffTime)
        {
            this.instanceId = networkInstanceId;
            this.buffIndex = buffIndex;
            this.amount = amount;
            this.buffTime = buffTime;
        }
        public AddBuffNetMessage(NetworkInstanceId networkInstanceId, BuffIndex buffIndex, int amount, float buffTime)
        {
            this.instanceId = networkInstanceId;
            this.buffIndex = (int)buffIndex;
            this.amount = amount;
            this.buffTime = buffTime;
        }
        public AddBuffNetMessage(NetworkInstanceId networkInstanceId, BuffDef buffDef, int amount, float buffTime)
        {
            this.instanceId = networkInstanceId;
            this.buffIndex = (int)buffDef.buffIndex;
            this.amount = amount;
            this.buffTime = buffTime;
        }
        public AddBuffNetMessage()
        {

        }
        public void Deserialize(NetworkReader reader)
        {
            instanceId = reader.ReadNetworkId();
            buffIndex = reader.ReadInt32();
            amount = reader.ReadInt32();
            buffTime = reader.ReadSingle();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active) return;
            GameObject gameObject = Util.FindNetworkObject(instanceId);
            if (gameObject == null) return;
            CharacterBody characterBody = gameObject.GetComponent<CharacterBody>();
            if (characterBody == null) return;
            bool isRemoving = amount <= 0;
            if (isRemoving) amount *= -1;
            for (int i = 0; i < amount; i++)
            {
                if (isRemoving)
                {
                    characterBody.RemoveBuff((BuffIndex)buffIndex);
                }
                else
                {
                    if (buffTime > 0)
                    {
                        characterBody.AddTimedBuff((BuffIndex)buffIndex, buffTime);
                    }
                    else
                    {
                        characterBody.AddBuff((BuffIndex)buffIndex);
                    }
                }
            }

        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(instanceId);
            writer.Write(buffIndex);
            writer.Write(amount);
            writer.Write(buffTime);
        }
    }
    public class TimeScaleChangeNetMessage : INetMessage
    {
        public float timeScaleChangeAmount;
        public TimeScaleChangeNetMessage(float value)
        {
            timeScaleChangeAmount = value;
        }
        public TimeScaleChangeNetMessage()
        {

        }
        public void Deserialize(NetworkReader reader)
        {
            timeScaleChangeAmount = reader.ReadSingle();
        }

        public void OnReceived()
        {
            Time.timeScale = timeScaleChangeAmount;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(timeScaleChangeAmount);
        }
    }
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class SerializeProperty : PropertyAttribute
    {
        public string PropertyName { get; private set; }

        public SerializeProperty(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
    public static class Assets
    {
        /// <summary>
        /// Body will sprint all time. All sprinting negative effects will be nullified.
        /// </summary>
        public static CharacterBodyAPI.ModdedBodyFlag SprintAllTime = CharacterBodyAPI.ReserveBodyFlag();
        /// <summary>
        /// Body will not receive damage from Fog. Fog bonus damage still increases.
        /// </summary>
        public static CharacterBodyAPI.ModdedBodyFlag ImmuneToVoidFog = CharacterBodyAPI.ReserveBodyFlag();
        /// <summary>
        /// Jumping will not reset horizontal velocity.
        /// </summary>
        public static BuffDef BunnyHopBuff;
        /// <summary>
        /// Perpendicullar to horizontal velocity movement will make a sharp turn.
        /// </summary>
        public static BuffDef StrafeBuff;
        /// <summary>
        /// KeepVelocityBuff.
        /// </summary>
        public static BuffDef KeepVelocityBuff;
        /// <summary>
        /// KeepVelocityBuff.
        /// </summary>
        public static BuffDef AirControlFromVelocityAddBuff;
        /// <summary>
        /// Set this damage type to set taked damage to targets max health. Works only on non champion enemies
        /// </summary>
        public static DamageAPI.ModdedDamageType InstakillNoChampion = DamageAPI.ReserveDamageType();
        /// /// <summary>
        /// Set this damage type to set taked damage to targets max health. Works only on champion enemies
        /// </summary>
        public static DamageAPI.ModdedDamageType InstakillChampion = DamageAPI.ReserveDamageType();
        /// /// <summary>
        /// Set this damage type to set taked damage to 1/4 of targets max health. Works only on non champion enemies
        /// </summary>
        public static DamageAPI.ModdedDamageType BruiseNoChampion = DamageAPI.ReserveDamageType();
        /// /// <summary>
        /// Set this damage type to set taked damage to 1/4 of targets max health. Works only on champion enemies
        /// </summary>
        public static DamageAPI.ModdedDamageType BruiseChampion = DamageAPI.ReserveDamageType();
        /*/// /// <summary>
        /// Set this proc type to heal to make it overheal
        /// </summary>
        public static ModdedProcType Overheal = ProcTypeAPI.ReserveProcType();*/
        public struct EntityStateMachineAdditionInfo
        {
            public string entityStateMachineName;
            public Type initialStateType;
            public Type mainStateType;
        }
    }
    public class LanguageTokensToAddOnLoad : IDisposable
    {
        public static List<LanguageTokensToAddOnLoad> languageTokensToAddOnLoad = new List<LanguageTokensToAddOnLoad>();
        public string language;
        public string token;
        public string output;
        public LanguageTokensToAddOnLoad(string token, string output, string language)
        {
            this.language = language;
            this.token = token;
            this.output = output;
            languageTokensToAddOnLoad.Add(this);
        }
        public void Dispose()
        {
            languageTokensToAddOnLoad.Remove(this);
            Utils.AddLanguageToken(token, output, language);
        }
    }
    public class Section : IDisposable
    {
        public static List<Section> sections = new List<Section>();
        public static Dictionary<string, Section> sectionByName = new Dictionary<string, Section>();
        public string name;
        public Color color;
        public HGButton button;
        public List<LoadoutPanelController.Row> rows = new List<LoadoutPanelController.Row>();
        public Section(string sectionName, Color color)
        {
            name = sectionName;
            this.color = color;
            sections.Add(this);
            sectionByName.Add(name, this);
            button = GameObject.Instantiate(loadoutSectionButton, loadoutSectionHolder.transform);
            LanguageTextMeshController languageTextMeshController = button.gameObject.GetComponent<LanguageTextMeshController>();
            languageTextMeshController.token = name;
            button.onClick.AddListener(SelectSection);
            button.requiredTopLayer = null;
            void SelectSection()
            {
                Utils.SelectRowsSection(name);
            }
            ColorBlock colorBlock = button.colors;
            color.a = 0.2f;
            colorBlock.normalColor = color;
            colorBlock.highlightedColor = color;
            color.a = 0.5f;
            colorBlock.selectedColor = color;
            button.colors = colorBlock;
        }
        public void Dispose()
        {
            if (sections.Contains(this)) sections.Remove(this);
            if (sectionByName.ContainsKey(name)) sectionByName.Remove(name);
            if (button != null) GameObject.Destroy(button.gameObject);
        }
    }
    public class ProjectileOnMessage : MessageBase
    {
        public override void Serialize(NetworkWriter writer)
        {
        }
        public override void Deserialize(NetworkReader reader)
        {
        }
    }
    public interface IProjectileOnFireMessage
    {
        public void Deserialize(NetworkReader reader)
        {
        }

        public void OnReceived(ProjectileController projectileController)
        {
        }

        public void Serialize(NetworkWriter writer)
        {
        }
    }
    /// <summary>
    /// Gives a method that runs on ProjectileExplosion Detonate method.
    /// </summary>
    public interface IOnProjectileExplosionDetonate
    {
        public void OnProjectileExplosionDetonate(BlastAttack blastAttack, BlastAttack.Result result);
    }
    public interface IProjectileMasterSummon
    {
        public ProjectileController projectileController { get; }
        public GameObject master { get; }
        public delegate void OnMasterSummon(IProjectileMasterSummon projectileMasterSummon, CharacterMaster characterMaster);
        public DelegateHolder<OnMasterSummon> onMasterSummon { get; }
        public void Spawn(Vector3 position)
        {
            MasterSummon masterSummon = new MasterSummon
            {
                ignoreTeamMemberLimit = true,
                masterPrefab = master,
                position = position,
                rotation = Quaternion.identity,
                summonerBodyObject = projectileController?.owner,
                useAmbientLevel = true,
            };
            CharacterMaster characterMaster = masterSummon.Perform();
            onMasterSummon?.InvokeAll(this, characterMaster);
        }
    }
    public interface IOnGroundHitServer
    {
        public void OnGroundHitServer(CharacterBody characterBody, CharacterMotor.HitGroundInfo hitGroundInfo);
    }
    public class RemoveBuffsOnGroundHitServer : MonoBehaviour, IOnGroundHitServer
    {
        public List<BuffDef> buffDefs = [];
        public void OnGroundHitServer(CharacterBody characterBody, CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            Destroy(this);
            if (buffDefs == null || !characterBody) return;
            foreach (BuffDef buffDef in buffDefs) characterBody.RemoveBuff(buffDef);
        }
    }
    [RequireComponent(typeof(ProjectileController))]
    public class ProjectileExplosionMasterSummon : MonoBehaviour, IOnProjectileExplosionDetonate, IProjectileMasterSummon
    {
        public ProjectileController controller;
        public ProjectileController projectileController => controller;
        public CharacterMaster characterMaster;
        public GameObject master => characterMaster.gameObject;
        public DelegateHolder<IProjectileMasterSummon.OnMasterSummon> onMasterSummonEvent;
        public DelegateHolder<IProjectileMasterSummon.OnMasterSummon> onMasterSummon => onMasterSummonEvent;

        public void OnProjectileExplosionDetonate(BlastAttack blastAttack, BlastAttack.Result result)
        {
            (this as IProjectileMasterSummon).Spawn(blastAttack.position);
        }
    }
    /// <summary>
    /// Applies velocity to filtered targets on projectile explosion.
    /// </summary>
    [RequireComponent(typeof(ProjectileExplosion))]
    public class RocketJumpComponent : MonoBehaviour, IOnProjectileExplosionDetonate
    {
        public float force = 3000f;
        public AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);
        public AnimationCurve verticalForceReduction = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float radiusMultiplier = 1.25f;
        public bool alwaysApply = true;
        public bool disableAirControl = true;
        public List<BuffDef> buffsWhileMidair = new List<BuffDef>();
        public RocketJumpFiltering rocketJumpFiltering;
        public delegate void OnRocketJumpApplied(RocketJumpComponent rocketJumpComponent, CharacterBody characterBody, Vector3 force);
        public DelegateHolder<OnRocketJumpApplied> onRocketJumpApplied;
        public void OnProjectileExplosionDetonate(BlastAttack blastAttack, BlastAttack.Result result)
        {
            Collider[] colliders = Physics.OverlapSphere(blastAttack.position, blastAttack.radius * radiusMultiplier, LayerIndex.CommonMasks.characterBodiesOrDefault, QueryTriggerInteraction.UseGlobal);
            List<CharacterBody> li = new List<CharacterBody>();
            foreach (Collider collider in colliders)
            {
                CharacterBody body = collider.GetComponent<CharacterBody>();
                if (!body || li.Contains(body)) continue;
                li.Add(body);
                bool flag = false;
                switch (rocketJumpFiltering)
                {
                    case RocketJumpFiltering.Everyone:
                        flag = true;
                        break;
                    case RocketJumpFiltering.OnlyTeammates:
                        if (body.teamComponent && body.teamComponent.teamIndex == blastAttack.teamIndex) flag = true;
                        break;
                    case RocketJumpFiltering.OnlySelf:
                        if (blastAttack.attacker && body.gameObject == blastAttack.attacker) flag = true;
                        break;
                }
                if(!flag) continue;
                Vector3 vector3 = (body.corePosition - blastAttack.position);
                float falloffIntensity = falloff.Evaluate(vector3.sqrMagnitude / (radiusMultiplier * radiusMultiplier));
                vector3.Normalize();
                Vector3 vector31 = new Vector3(vector3.x, 0f, vector3.z);
                float angle = Vector3.Angle(vector3, vector31);
                float angleIntensity = angle / 90f;
                vector3.y *= verticalForceReduction.Evaluate(angleIntensity);
                vector3 = vector3.normalized * force * falloffIntensity;
                if (body.characterMotor)
                {
                    if (body.characterMotor.velocity.y < 0f)
                    {
                        PhysForceInfo physForceInfo = new PhysForceInfo
                        {
                            force = new Vector3(0f, -body.characterMotor.velocity.y, 0f),
                            disableAirControlUntilCollision = false,
                            ignoreGroundStick = true,
                            massIsOne = true,
                        };
                        body.characterMotor.ApplyForceImpulse(physForceInfo);
                    }
                    body.characterMotor.ApplyForce(vector3, alwaysApply, disableAirControl);
                }
                else if (body.rigidbody)
                {
                    if (body.rigidbody.velocity.y < 0f)
                        body.rigidbody.velocity = new Vector3(body.rigidbody.velocity.x, 0f, body.rigidbody.velocity.y);
                    body.rigidbody.AddForce(vector3, ForceMode.Impulse);
                }
                if (body.characterMotor)
                {
                    RemoveBuffsOnGroundHitServer removeBuffsOnGroundHitServer = body.gameObject.GetOrAddComponent<RemoveBuffsOnGroundHitServer>();
                    removeBuffsOnGroundHitServer.buffDefs.AddRange(buffsWhileMidair);
                    foreach (BuffDef buffDef in buffsWhileMidair) body.AddBuff(buffDef);
                }
                onRocketJumpApplied?.InvokeAll(this, body, vector3);
            }
        }
        public enum RocketJumpFiltering
        {
            OnlySelf,
            OnlyTeammates,
            Everyone
        }
    }
    /// <summary>
    /// Projectile rotates towards owner point of view or view direction if there is no point.
    /// </summary>
    [RequireComponent(typeof(ProjectileController))]
    [RequireComponent(typeof(Rigidbody))]
    public class GuidedProjectile : MonoBehaviour
    {
        public Rigidbody rigidbody;
        public ProjectileController projectileController;
        public float guidingPower = 15f;
        [HideInInspector] public InputBankTest inputBankTest;
        public void Awake()
        {
            if(rigidbody == null) rigidbody = GetComponent<Rigidbody>();
            if(projectileController == null) projectileController = GetComponent<ProjectileController>();
        }
        public void Start()
        {
            inputBankTest = projectileController?.owner.GetComponent<InputBankTest>();
        }
        public void FixedUpdate()
        {
            if (!inputBankTest || !rigidbody) return;
            Vector3 vector3 = inputBankTest.aimDirection;
            if (inputBankTest.GetAimRaycast(1024, out var hit))
            {
                vector3 = hit.point - transform.position;
            }
            rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, vector3, 15f / 57f, 0f);
        }
    }
    [CreateAssetMenu(menuName = "BrynzaAPI/DelegateDef/RocketJumpDelegateDef")]
    public class RocketJumpDelegateDef : DelegateDef<RocketJumpComponent.OnRocketJumpApplied>
    {

    }
    [CreateAssetMenu(menuName = "BrynzaAPI/DelegateDef/OnMasterSummonToGiveItemsDelegateDef")]
    public class OnMasterSummonToGiveItemsDelegateDef : OnMasterSummonDelegateDef
    {
        public OnMasterSummonToGiveItemsDelegateDef() => @delegate = GiveItems;
        public GiveItemsDelegateDef giveItemsDelegateDef;
        public void GiveItems(IProjectileMasterSummon projectileMasterSummon, CharacterMaster characterMaster)
        {
            if (giveItemsDelegateDef == null) return;
            Inventory inventory = characterMaster.inventory;
            if (inventory == null) return;
            giveItemsDelegateDef.@delegate?.Invoke(inventory);
        }
    }
    [CreateAssetMenu(menuName = "BrynzaAPI/DelegateDef/OnMasterSummonDelegateDef")]
    public class OnMasterSummonDelegateDef : DelegateDef<IProjectileMasterSummon.OnMasterSummon>
    {
    }
    [CreateAssetMenu(menuName = "BrynzaAPI/DelegateDef/GiveItemsDelegateDef")]
    public class GiveItemsDelegateDef : DelegateDef<GiveItems>
    {
        public GiveItemsDelegateDef() => @delegate = GiveItemsMethod;
        public delegate void GiveItems(Inventory inventory);
        public Item[] items;
        [Serializable]
        public struct Item
        {
            public AddressReferencedItemDef itemDef;
            public int amount;
        }
        public void GiveItemsMethod(Inventory inventory)
        {
            for (int i = 0; i < items.Length; i++)
            {
                ref Item item = ref items[i];
                inventory.GiveItem(item.itemDef.Asset, item.amount);
            }
        }
    }
    public class DelegateDef<T> : ScriptableObject where T : Delegate
    {
        public T @delegate;
    }
    [Serializable]
    public class DelegateHolder<T> where T : Delegate
    {
        [SerializeField]
        public List<DelegateDef<T>> delegateDefs = [];
        public List<T> delegates = [];
        //public T @delegate;
        public DelegateHolder()
        {
        }
        public void InvokeAll(params object[] parameters)
        {
            foreach (DelegateDef<T> delegateDef in delegateDefs) if(delegateDef != null && delegateDef.@delegate != null) delegateDef.@delegate.DynamicInvoke(parameters);
            foreach (T @delegate in delegates) @delegate?.DynamicInvoke(parameters);
        }
        /*
        public object Invoke()
        {
            return @delegate.Method.Invoke(@delegate.Target, null);
        }
        //public static void CreateDynamicMethod(string name, DelegateHolder<T> delegateHolder)
        //{
        //    T @delegate = delegateHolder.@delegate;
        //    List<Type> typesList = [];
        //    foreach (ParameterInfo parameterInfo in @delegate.Method.GetParameters())
        //    {
        //        typesList.Add(parameterInfo.ParameterType);
        //    }
        //    Type[] types = [.. typesList];
        //    System.Reflection.Emit.DynamicMethod dynamicMethod = new System.Reflection.Emit.DynamicMethod(name, @delegate.Method.ReturnParameter.ParameterType, types, typeof(T).Module);
        //    System.Reflection.Emit.ILGenerator il = dynamicMethod.GetILGenerator();
        //    il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        //    il.Emit(System.Reflection.Emit.OpCodes.Conv_I8);
        //    il.Emit(System.Reflection.Emit.OpCodes.Dup);
        //    il.Emit(System.Reflection.Emit.OpCodes.Mul);
        //    il.Emit(System.Reflection.Emit.OpCodes.Ret);
        //}
        public static void CreateDelegate(DelegateHolder<T> delegateHolder)
        {
            T @delegate = delegateHolder.@delegate;
                List<Type> typesList = [];
            MethodInfo methodInfo = @delegate.Method;
            foreach (ParameterInfo parameterInfo in methodInfo.GetParameters())
            {
                typesList.Add(parameterInfo.ParameterType);
            }
            Type[] types = [.. typesList];
            System.Reflection.Emit.DynamicMethod dynamicMethod = new System.Reflection.Emit.DynamicMethod("InvokeAll", methodInfo.ReturnParameter.ParameterType, types, typeof(T).Module);
            System.Reflection.Emit.ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Conv_I8);
            il.Emit(System.Reflection.Emit.OpCodes.Dup);
            il.Emit(System.Reflection.Emit.OpCodes.Mul);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
        }*/
        public static DelegateHolder<T> operator +(DelegateHolder<T> delegateHolder, DelegateDef<T> delegateDef)
        {
            delegateHolder.delegateDefs.Add(delegateDef);
            return delegateHolder;
        }
        public static DelegateHolder<T> operator -(DelegateHolder<T> delegateHolder, DelegateDef<T> delegateDef)
        {
            delegateHolder.delegateDefs.Remove(delegateDef);
            return delegateHolder;
        }
        public static DelegateHolder<T> operator +(DelegateHolder<T> delegateHolder, T @delegate)
        {
            delegateHolder.delegates.Add(@delegate);
            return delegateHolder;
        }
        public static DelegateHolder<T> operator -(DelegateHolder<T> delegateHolder, T @delegate)
        {
            delegateHolder.delegates.Remove(@delegate);
            return delegateHolder;
        }
    }
    public class DestroyOnParticleEndAndNoParticles : MonoBehaviour
    {
        public void Start()
        {
            if (this.trackedParticleSystem == null)
            {
                this.trackedParticleSystem = base.GetComponentInChildren<ParticleSystem>();
            }
            if (!this.efh)
            {
                this.efh = base.GetComponent<EffectManagerHelper>();
            }
        }
        public void Update()
        {
            if (this.trackedParticleSystem && !this.trackedParticleSystem.IsAlive() && trackedParticleSystem.particleCount <= 0)
            {
                if (this.efh && this.efh.OwningPool != null)
                {
                    this.efh.OwningPool.ReturnObject(this.efh);
                    return;
                }
                UnityEngine.Object.Destroy(base.gameObject);
            }
        }
        [SerializeField]
        [Tooltip("Set this if you want to specify which particle system is tracked. Otherwise it will automatically grab the first one it finds.")]
        private ParticleSystem trackedParticleSystem;
        private EffectManagerHelper efh;
    }
    public class SniperHurtboxTracker : MonoBehaviour
    {
        [HideInInspector] public HurtBox hurtBox;
        [HideInInspector] public NetworkUser networkUser;
        [HideInInspector] public GameObject currentTrackerPrefab;
        public void Awake()
        {
            if (!Run.instance) return;
            if(NetworkUser.readOnlyInstancesList != null && NetworkUser.readOnlyInstancesList.Count > 0) networkUser = NetworkUser.readOnlyInstancesList[0];
        }
        public void OnEnable()
        {
            onSniperHurtboxAdded += UpdateTracker;
            onSniperHurtboxRemoved += UpdateTracker;
        }
        public void UpdateTracker(GameObject obj)
        {
            if (currentTrackerPrefab) Destroy(currentTrackerPrefab);
            GameObject sniperHurtboxTrackerDef = null;
            if(activeSniperHurtboxTrackers.Count > 0)
            foreach (GameObject sniperHurtboxTrackerDef1 in activeSniperHurtboxTrackers)
                {
                    sniperHurtboxTrackerDef = sniperHurtboxTrackerDef1;
                }
            if (sniperHurtboxTrackerDef != null)
            {
                CameraRigController cameraRigController = networkUser?.cameraRigController;
                if (cameraRigController != null)
                {
                    GameObject mainContainer = cameraRigController.hud.mainContainer;
                    currentTrackerPrefab = Instantiate(sniperHurtboxTrackerDef, mainContainer.transform);
                }
            }
        }
        public void Update()
        {
            if (hurtBox == null) return;
            //transform.position = hurtBox.transform.position;
            if (currentTrackerPrefab == null || networkUser == null) return;
            CameraRigController cameraRigController = networkUser.cameraRigController;
            Camera camera = cameraRigController.sceneCam;
            Camera UIcamera = cameraRigController.uiCam;
            if (camera == null || UIcamera == null) return;
            Vector3 worldToViewportPointVector = camera.WorldToViewportPoint(transform.position);
            Vector3 viewportToScreenPointVector = UIcamera.ViewportToScreenPoint(worldToViewportPointVector);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cameraRigController.hud.mainContainer.transform as RectTransform, viewportToScreenPointVector, UIcamera, out Vector2 localPoint);
            currentTrackerPrefab.transform.localPosition = localPoint;
        }
        public void OnDisable()
        {
            onSniperHurtboxAdded -= UpdateTracker;
            onSniperHurtboxRemoved -= UpdateTracker;
        }
    }
    public static class Utils
    {
        public static void AddLanguageToken(string token, string output, string language = "en")
        {
            if (RoR2Application.loadFinished)
            {
                Language language1 = Language.GetOrCreateLanguage(language);
                if (language1 == null) return;
                if (language1.stringsByToken.ContainsKey(token))
                {
                    language1.stringsByToken[token] = output;
                }
                else
                {
                    language1.stringsByToken.Add(token, output);
                }
                return;
            }
            LanguageTokensToAddOnLoad languageTokensToAddOnLoad = new LanguageTokensToAddOnLoad(token, output, language);
        }
        public static void SelectRowsSection(string sectionName)
        {
            foreach (Section section in Section.sections)
            {
                if (section == null) continue;
                List<LoadoutPanelController.Row> rows = section.rows;
                if (rows == null) continue;
                foreach (LoadoutPanelController.Row row in rows)
                {
                    GameObject rowObject = row != null && row.rowPanelTransform ? row.rowPanelTransform.gameObject : null;
                    if (rowObject == null) continue;
                    if (sectionName == section.name)
                    {
                        rowObject.SetActive(true);
                    }
                    else
                    {
                        rowObject.SetActive(false);
                    }
                }
            }
            Section section2 = Section.sectionByName[sectionName];
            int count2 = section2.rows.Count;
            for (int j = 0; j < count2; j++)
            {
                LoadoutPanelController.Row row = section2.rows[j];
                int count3 = row.rowData.Count;
                for (int k = 0; k < count3; k++)
                {
                    MPButton button = row.rowData[k].button;
                    Navigation navigation = button.navigation;
                    navigation.mode = Navigation.Mode.Explicit;
                    navigation.selectOnLeft = null;
                    navigation.selectOnRight = null;
                    navigation.selectOnUp = section2.button;
                    navigation.selectOnDown = null;
                    if (count3 > 1)
                    {
                        int index = (k + count3 - 1) % count3;
                        navigation.selectOnLeft = section2.rows[j].rowData[index].button;
                        int index2 = (k + count3 + 1) % count3;
                        navigation.selectOnRight = section2.rows[j].rowData[index2].button;
                    }
                    if (count2 > 1)
                    {
                        if (j != 0)
                        {
                            int num2 = section2.rows[j - 1].rowData.Count - 1;
                            int index3 = (num2 < k) ? num2 : k;
                            navigation.selectOnUp = section2.rows[j - 1].rowData[index3].button;
                        }
                        if (j != count2 - 1)
                        {
                            int num3 = section2.rows[j + 1].rowData.Count - 1;
                            int index4 = (num3 < k) ? num3 : k;
                            navigation.selectOnDown = section2.rows[j + 1].rowData[index4].button;
                        }
                    }
                    button.navigation = navigation;
                }
            }
            int count = Section.sections.Count;
            MPButton buttonToSelect = section2.rows[0].rowData[0].button;
            for (int i = 0; i < count; i++)
            {
                Section section = Section.sections[i];
                HGButton hGButton = section.button;
                Navigation navigation = hGButton.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = i - 1 < 0 ? Section.sections[count - 1].button : Section.sections[i - 1].button;
                navigation.selectOnRight = i + 1 < count ? Section.sections[i + 1].button : Section.sections[0].button;
                navigation.selectOnDown = buttonToSelect;
                hGButton.navigation = navigation;
            }
            buttonToSelect.Select();
        }
        public static BuffDef CreateBuff(string name, Sprite icon, Color color, bool canStack, bool isDebuff, bool isCooldown, bool isHidden, bool ignoreGrowthNectar)
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.name = name;
            buffDef.buffColor = color;
            buffDef.canStack = canStack;
            buffDef.isDebuff = isDebuff;
            buffDef.ignoreGrowthNectar = ignoreGrowthNectar;
            buffDef.iconSprite = icon;
            buffDef.isHidden = isHidden;
            buffDef.isCooldown = isCooldown;
            buffs.Add(buffDef);
            return buffDef;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SuperRoll(float chance)
        {
            int rolls = (int)MathF.Floor(chance / 100);
            if (Util.CheckRoll(chance - (rolls * 100))) rolls++;
            return rolls;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ConvertAmplificationPercentageIntoReductionPercentage(float amplificationPercentage, float maxChance)
        {
            return (1f - maxChance / (maxChance + amplificationPercentage)) * maxChance;
        }
        /// <summary>
        /// Add new EntityStateMachine component to the body and wire everything needed.
        /// </summary>
        public static EntityStateMachine AddEntityStateMachine(CharacterBody characterBody, EntityStateMachineAdditionInfo entityStateMachineAdditionInfo)
        {
            NetworkStateMachine networkStateMachine = characterBody.GetComponent<NetworkStateMachine>();
            List<EntityStateMachine> entityStateMachines = networkStateMachine ? networkStateMachine.stateMachines.ToList() : null;
            EntityStateMachine entityStateMachine = characterBody.gameObject.AddComponent<EntityStateMachine>();
            EntityStates.SerializableEntityStateType serializableEntityStateType = new EntityStates.SerializableEntityStateType(entityStateMachineAdditionInfo.mainStateType ?? typeof(EntityStates.Idle));
            EntityStates.SerializableEntityStateType serializableEntityStateType2 = new EntityStates.SerializableEntityStateType(entityStateMachineAdditionInfo.initialStateType ?? typeof(EntityStates.Idle));
            entityStateMachine.mainStateType = serializableEntityStateType;
            entityStateMachine.initialStateType = serializableEntityStateType2;
            entityStateMachine.customName = entityStateMachineAdditionInfo.entityStateMachineName;
            if (networkStateMachine)
            {
                entityStateMachines.Add(entityStateMachine);
                entityStateMachine.networkIndex = networkStateMachine.stateMachines.Length;
                entityStateMachine.networker = networkStateMachine;
                entityStateMachine.networkIdentity = networkStateMachine.networkIdentity;
            }
            if (networkStateMachine)
                networkStateMachine.stateMachines = [.. entityStateMachines];
            return entityStateMachine;
        }
        /// <summary>
        /// Add new EntityStateMachine components to the body and wire everything needed.
        /// </summary>
        public static List<EntityStateMachine> AddEntityStateMachines(CharacterBody characterBody, List<EntityStateMachineAdditionInfo> entityStateMachineAdditionInfos)
        {
            NetworkStateMachine networkStateMachine = characterBody.GetComponent<NetworkStateMachine>();
            List<EntityStateMachine> entityStateMachines = networkStateMachine ? networkStateMachine.stateMachines.ToList() : null;
            List<EntityStateMachine> newEntityStateMachines = new List<EntityStateMachine>();
            foreach (var entityStateMachineAddition in entityStateMachineAdditionInfos)
            {
                EntityStateMachine entityStateMachine = characterBody.gameObject.AddComponent<EntityStateMachine>();
                EntityStates.SerializableEntityStateType serializableEntityStateType = new EntityStates.SerializableEntityStateType(entityStateMachineAddition.mainStateType ?? typeof(EntityStates.Idle));
                EntityStates.SerializableEntityStateType serializableEntityStateType2 = new EntityStates.SerializableEntityStateType(entityStateMachineAddition.initialStateType ?? typeof(EntityStates.Idle));
                entityStateMachine.mainStateType = serializableEntityStateType;
                entityStateMachine.initialStateType = serializableEntityStateType2;
                entityStateMachine.customName = entityStateMachineAddition.entityStateMachineName;
                newEntityStateMachines.Add(entityStateMachine);
                if (networkStateMachine)
                {
                    entityStateMachines.Add(entityStateMachine);
                    entityStateMachine.networkIndex = networkStateMachine.stateMachines.Length;
                    entityStateMachine.networker = networkStateMachine;
                    entityStateMachine.networkIdentity = networkStateMachine.networkIdentity;
                }
            }
            if(networkStateMachine)
            networkStateMachine.stateMachines = entityStateMachines.ToArray();
            return newEntityStateMachines;
        }
        /// <summary>
        /// Change timescale with a desired value for all clients.
        /// </summary>
        public static void ChangeTimescaleForAllClients(float value)
        {
            new TimeScaleChangeNetMessage(value).Send(NetworkDestination.Clients);
        }
        public static void AddSniperHurtboxTracker(GameObject sniperHurtboxTracker)
        {
            //if (activeSniperHurtboxTrackers.Contains(sniperHurtboxTrackerParams)) return;
            activeSniperHurtboxTrackers.Add(sniperHurtboxTracker);
            OnSniperHurtboxAdded(sniperHurtboxTracker);
        }
        public static void RemoveSniperHurtboxTracker(GameObject sniperHurtboxTracker)
        {
            //if (!activeSniperHurtboxTrackers.Contains(sniperHurtboxTrackerParams)) return;
            activeSniperHurtboxTrackers.Remove(sniperHurtboxTracker);
            OnSniperHurtboxRemoved(sniperHurtboxTracker);
        }
    }
    public class ContentPacks : IContentPackProvider
    {
        internal ContentPack contentPack = new ContentPack();
        public string identifier => BrynzaAPI.ModGuid + ".ContentProvider";
        public static List<GameObject> bodies = new List<GameObject>();
        public static List<BuffDef> buffs = new List<BuffDef>();
        public static List<SkillDef> skills = new List<SkillDef>();
        public static List<SkillFamily> skillFamilies = new List<SkillFamily>();
        public static List<GameObject> projectiles = new List<GameObject>();
        public static List<GameObject> networkPrefabs = new List<GameObject>();
        public static List<SurvivorDef> survivors = new List<SurvivorDef>();
        public static List<Type> states = new List<Type>();
        public static List<NetworkSoundEventDef> sounds = new List<NetworkSoundEventDef>();
        public static List<UnlockableDef> unlockableDefs = new List<UnlockableDef>();
        public static List<GameObject> masters = new List<GameObject>();
        public static List<ItemDef> items = new List<ItemDef>();
        public static List<EquipmentDef> equipments = new List<EquipmentDef>();
        public static List<EliteDef> elites = new List<EliteDef>();
        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(this.contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            this.contentPack.identifier = this.identifier;
            contentPack.skillDefs.Add(skills.ToArray());
            contentPack.skillFamilies.Add(skillFamilies.ToArray());
            contentPack.bodyPrefabs.Add(bodies.ToArray());
            contentPack.buffDefs.Add(buffs.ToArray());
            contentPack.projectilePrefabs.Add(projectiles.ToArray());
            contentPack.survivorDefs.Add(survivors.ToArray());
            contentPack.entityStateTypes.Add(states.ToArray());
            contentPack.networkSoundEventDefs.Add(sounds.ToArray());
            contentPack.networkedObjectPrefabs.Add(networkPrefabs.ToArray());
            contentPack.unlockableDefs.Add(unlockableDefs.ToArray());
            contentPack.masterPrefabs.Add(masters.ToArray());
            contentPack.itemDefs.Add(items.ToArray());
            contentPack.equipmentDefs.Add(equipments.ToArray());
            contentPack.eliteDefs.Add(elites.ToArray());
            yield break;
        }
    }
    public static class Extensions
    {
        public static void AddExtraSkill(this GenericSkill skill, GenericSkill extraSkill) => BrynzaInterop.AddExtraSkill(skill, extraSkill);
        public static List<GenericSkill> GetExtraSkills(this GenericSkill skill) => BrynzaInterop.GetExtraSkills(skill);
        public static void RemoveExtraSkill(this GenericSkill skill, GenericSkill extraSkill) => BrynzaInterop.RemoveExtraSkill(skill, extraSkill);
        /// <summary>
        /// Links Generic Skill to take its cooldownScale, bonusStockFromBody and flatCooldownReduction values.
        /// </summary>
        public static void LinkSkill(this GenericSkill genericSkill, GenericSkill linkSKill) => BrynzaInterop.LinkSkill(genericSkill, linkSKill);
        /// <summary>
        /// Gets linked skill.
        /// </summary>
        public static GenericSkill GetLinkedSkill(this GenericSkill genericSkill) => BrynzaInterop.GetLinkedSkill(genericSkill);
        public static void AddBonusSkill(this SkillLocator skillLocator, GenericSkill bonusSkill) => BrynzaInterop.AddBonusSkill(skillLocator, bonusSkill);
        public static void RemoveBonusSkill(this SkillLocator skillLocator, GenericSkill bonusSkill) => BrynzaInterop.RemoveBonusSkill(skillLocator, bonusSkill);
        public static List<GenericSkill> GetBonusSkills(this SkillLocator skillLocator) => BrynzaInterop.GetBonusSkills(skillLocator);
        /// <summary>
        /// Set Character Motor to always move by this Vector if it's not zero.
        /// </summary>
        public static void SetVelocityOverride(this CharacterMotor characterMotor, Vector3 vector3) => BrynzaInterop.SetVelocityOverride(characterMotor, vector3);
        public static Vector3 GetVelocityOverride(this CharacterMotor characterMotor) => BrynzaInterop.GetVelocityOverride(characterMotor);
        /// <summary>
        /// Set Character Motor movement to take current velocity.
        /// </summary>
        public static void SetKeepVelocityOnMoving(this CharacterMotor characterMotor, bool flag) => BrynzaInterop.SetKeepVelocityOnMoving(characterMotor, flag);
        public static bool GetKeepVelocityOnMoving(this CharacterMotor characterMotor) => BrynzaInterop.GetKeepVelocityOnMoving(characterMotor) || (characterMotor && characterMotor.body && characterMotor.body.GetBuffCount(Assets.KeepVelocityBuff) > 0);
        /// <summary>
        /// Set Character Motor to have consistent air acceleration.
        /// </summary>
        public static void SetConsistentAcceleration(this CharacterMotor characterMotor, float value) => BrynzaInterop.SetConsistentAcceleration(characterMotor, value);
        public static float GetConsistentAcceleration(this CharacterMotor characterMotor) => BrynzaInterop.GetConsistentAcceleration(characterMotor);
        public static float GetAirControlFromVelocityAdd(this CharacterMotor characterMotor) => BrynzaInterop.GetAirControlFromVelocityAdd(characterMotor);
        public static void SetAirControlFromVelocityAdd(this CharacterMotor characterMotor, float value) => BrynzaInterop.SetAirControlFromVelocityAdd(characterMotor, value);
        [Obsolete("Doesn't work as intended", true)]
        public static void SetFluidMaxDistanceDelta(this CharacterMotor characterMotor, bool flag) => BrynzaInterop.SetFluidMaxDistanceDelta(characterMotor, flag);
        public static bool GetFluidMaxDistanceDelta(this CharacterMotor characterMotor) => BrynzaInterop.GetFluidMaxDistanceDelta(characterMotor);
        public static void SetStrafe(this CharacterMotor characterMotor, bool flag) => BrynzaInterop.SetStrafe(characterMotor, flag);
        public static bool GetStrafe(this CharacterMotor characterMotor) => (BrynzaInterop.GetStrafe(characterMotor)) || (characterMotor && characterMotor.body && characterMotor.body.GetBuffCount(Assets.StrafeBuff) > 0);
        public static void SetBunnyHop(this CharacterMotor characterMotor, bool flag) => BrynzaInterop.SetBunnyHop(characterMotor, flag);
        public static bool GetBunnyHop(this CharacterMotor characterMotor) => (BrynzaInterop.GetBunnyHop(characterMotor)) || (characterMotor && characterMotor.body && characterMotor.body.GetBuffCount(Assets.BunnyHopBuff) > 0);
        public static void SetBaseWallJumpCount(this CharacterBody characterBody, int count) => BrynzaInterop.SetBaseWallJumpCount(characterBody, count);
        public static int GetBaseWallJumpCount(this CharacterBody characterBody) => BrynzaInterop.GetBaseWallJumpCount(characterBody);
        public static void SetMaxWallJumpCount(this CharacterBody characterBody, int count) => BrynzaInterop.SetMaxWallJumpCount(characterBody, count);
        public static int GetMaxWallJumpCount(this CharacterBody characterBody) => BrynzaInterop.GetMaxWallJumpCount(characterBody);
        public static void SetWallJumpCount(this CharacterMotor characterMotor, int count) => BrynzaInterop.SetWallJumpCount(characterMotor, count);
        public static int GetWallJumpCount(this CharacterMotor characterMotor) => BrynzaInterop.GetWallJumpCount(characterMotor);
        public static bool CanApplyAmmoPack(this SkillDef skillDef) => BrynzaInterop.CanApplyAmmoPack(skillDef);
        public static List<object> GetIgnoredHealthComponents(this BulletAttack bulletAttack) => BrynzaInterop.GetIgnoredHealthComponents(bulletAttack);
        public static void SetIgnoredHealthComponents(this BulletAttack bulletAttack, List<object> list) => BrynzaInterop.SetIgnoredHealthComponents(bulletAttack, list);
        public static bool GetIgnoreHitTargets(this BulletAttack bulletAttack) => BrynzaInterop.GetIgnoreHitTargets(bulletAttack);
        public static void SetIgnoreHitTargets(this BulletAttack bulletAttack, bool flag) => BrynzaInterop.SetIgnoreHitTargets(bulletAttack, flag);
        public static string GetSection(this LoadoutPanelController.Row row) => BrynzaInterop.GetSection(row);
        public static void SetSection(this LoadoutPanelController.Row row, string section) => BrynzaInterop.SetSection(row, section);
        public static string GetSection(this GenericSkill genericSkill) => BrynzaInterop.GetSection(genericSkill);
        public static void SetSection(this GenericSkill genericSkill, string section) => BrynzaInterop.SetSection(genericSkill, section);
        public static List<string> GetSections(this LoadoutPanelController loadoutPanelController) => BrynzaInterop.GetSections(loadoutPanelController);
        public static void SetSections(this LoadoutPanelController loadoutPanelController, List<string> sections) => BrynzaInterop.SetSections(loadoutPanelController, sections);
        public static bool GetDontFadeWhenNearCamera(this ref CharacterModel.RendererInfo rendererInfo) => BrynzaInterop.GetDontFadeWhenNearCamera(ref rendererInfo);
        public static void SetDontFadeWhenNearCamera(this ref CharacterModel.RendererInfo rendererInfo, bool flag) => BrynzaInterop.SetDontFadeWhenNearCamera(ref rendererInfo, flag);
        public static void SetForceMassIsOne(this BulletAttack bulletAttack, bool flag) => BrynzaInterop.SetForceMassIsOne(bulletAttack, flag);
        public static bool GetForceMassIsOne(this BulletAttack bulletAttack) => BrynzaInterop.GetForceMassIsOne(bulletAttack);
        public static void SetForceMassIsOne(this BlastAttack blastAttack, bool flag) => BrynzaInterop.SetForceMassIsOne(blastAttack, flag);
        public static bool GetForceMassIsOne(this BlastAttack blastAttack) => BrynzaInterop.GetForceMassIsOne(blastAttack);
        public static void SetForceMassIsOne(this ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo, bool flag) => BrynzaInterop.SetForceMassIsOne(ref blastAttackDamageInfo, flag);
        public static bool GetForceMassIsOne(this ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo) => BrynzaInterop.GetForceMassIsOne(ref blastAttackDamageInfo);
        public static void SetForceMassIsOne(this DamageInfo damageInfo, bool flag) => BrynzaInterop.SetForceMassIsOne(damageInfo, flag);
        public static bool GetForceMassIsOne(this DamageInfo damageInfo) => BrynzaInterop.GetForceMassIsOne(damageInfo);
        public static void SetForceAlwaysApply(this BulletAttack bulletAttack, bool value) => BrynzaInterop.SetForceAlwaysApply(bulletAttack, value);
        public static bool GetForceAlwaysApply(this BulletAttack bulletAttack) => BrynzaInterop.GetForceAlwaysApply(bulletAttack);
        public static void SetForceAlwaysApply(this BlastAttack blastAttack, bool value) => BrynzaInterop.SetForceAlwaysApply(blastAttack, value);
        public static bool GetForceAlwaysApply(this BlastAttack blastAttack) => BrynzaInterop.GetForceAlwaysApply(blastAttack);
        public static void SetForceAlwaysApply(this ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo, bool value) => BrynzaInterop.SetForceAlwaysApply(ref blastAttackDamageInfo, value);
        public static bool GetForceAlwaysApply(this ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo) => BrynzaInterop.GetForceAlwaysApply(ref blastAttackDamageInfo);
        public static void SetForceAlwaysApply(this DamageInfo damageInfo, bool value) => BrynzaInterop.SetForceAlwaysApply(damageInfo, value);
        public static bool GetForceAlwaysApply(this DamageInfo damageInfo) => BrynzaInterop.GetForceAlwaysApply(damageInfo);
        public static void SetForceDisableAirControlUntilCollision(this BulletAttack bulletAttack, bool value) => BrynzaInterop.SetForceDisableAirControlUntilCollision(bulletAttack, value);
        public static bool GetForceDisableAirControlUntilCollision(this BulletAttack bulletAttack) => BrynzaInterop.GetForceDisableAirControlUntilCollision(bulletAttack);
        public static void SetForceDisableAirControlUntilCollision(this BlastAttack blastAttack, bool value) => BrynzaInterop.SetForceDisableAirControlUntilCollision(blastAttack, value);
        public static bool GetForceDisableAirControlUntilCollision(this BlastAttack blastAttack) => BrynzaInterop.GetForceDisableAirControlUntilCollision(blastAttack);
        public static void SetForceDisableAirControlUntilCollision(this ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo, bool value) => BrynzaInterop.SetForceDisableAirControlUntilCollision(ref blastAttackDamageInfo, value);
        public static bool GetForceDisableAirControlUntilCollision(this ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo) => BrynzaInterop.GetForceDisableAirControlUntilCollision(ref blastAttackDamageInfo);
        public static void SetForceDisableAirControlUntilCollision(this DamageInfo damageInfo, bool value) => BrynzaInterop.SetForceDisableAirControlUntilCollision(damageInfo, value);
        public static bool GetForceDisableAirControlUntilCollision(this DamageInfo damageInfo) => BrynzaInterop.GetForceAlwaysApply(damageInfo);
        public static void SetBonusForce(this BulletAttack bulletAttack, Vector3 bonusForce) => BrynzaInterop.SetBonusForce(bulletAttack, bonusForce);
        public static Vector3 GetBonusForce(this BulletAttack bulletAttack) => BrynzaInterop.GetBonusForce(bulletAttack);
        public static void SetNoWeaponIfOwner(this BulletAttack bulletAttack, bool value) => BrynzaInterop.SetNoWeaponIfOwner(bulletAttack, value);
        public static bool GetNoWeaponIfOwner(this BulletAttack bulletAttack) => BrynzaInterop.GetNoWeaponIfOwner(bulletAttack);
        public static float GetYawClipCycleStart(this AimAnimator aimAnimator) => BrynzaInterop.GetYawClipCycleStart(aimAnimator);
        public static void SetYawClipCycleStart(this AimAnimator aimAnimator, float value) => BrynzaInterop.SetYawClipCycleStart(aimAnimator, value);
        public static float GetPitchClipCycleStart(this AimAnimator aimAnimator) => BrynzaInterop.GetPitchClipCycleStart(aimAnimator);
        public static void SetPitchClipCycleStart(this AimAnimator aimAnimator, float value) => BrynzaInterop.SetPitchClipCycleStart(aimAnimator, value);
        public static Run.FixedTimeStamp GetLastJumpTime(this CharacterBody characterBody) => BrynzaInterop.GetLastJumpTime(characterBody);
        public static void SetLastJumpTime(this CharacterBody characterBody, Run.FixedTimeStamp value) => BrynzaInterop.SetLastJumpTime(characterBody, value);
        public static void ResetIgnoredHealthComponents(this BulletAttack bulletAttack)
        {
            if(bulletAttack.GetIgnoredHealthComponents() != null) bulletAttack.GetIgnoredHealthComponents().Clear();
        }
        public static void SetStateToMain(this EntityStateMachine entityStateMachine)
        {
            entityStateMachine.SetState(EntityStateCatalog.InstantiateState(entityStateMachine.mainStateType.stateType));
        }
        /// <summary>
        /// Add onHitGroundServer event.
        /// </summary>
        public static void AddOnHitGroundServerDelegate(this CharacterMotor characterMotor, OnHitGroundServerDelegate hitGroundServerDelegate)
        {
            if (onHitGroundServerDictionary.ContainsKey(characterMotor))
            {
                onHitGroundServerDictionary[characterMotor].Add(hitGroundServerDelegate);
            }
            else
            {
                List<OnHitGroundServerDelegate> list = [hitGroundServerDelegate];
                onHitGroundServerDictionary.Add(characterMotor, list);
            }
        }
        /// <summary>
        /// Remove onHitGroundServer event.
        /// </summary>
        public static void RemoveOnHitGroundServerDelegate(this CharacterMotor characterMotor, OnHitGroundServerDelegate hitGroundServerDelegate)
        {
            if (onHitGroundServerDictionary.ContainsKey(characterMotor))
            {
                onHitGroundServerDictionary[characterMotor].Remove(hitGroundServerDelegate);
                if (onHitGroundServerDictionary[characterMotor].Count <= 0)
                {
                    onHitGroundServerDictionary.Remove(characterMotor);
                }
            }
        }
        //public static void SetWeaponOverride(this BulletAttack bulletAttack, GameObject gameObject) => BrynzaInterop.SetWeaponOverride(bulletAttack, gameObject);
        //public static GameObject GetWeaponOverride(this BulletAttack bulletAttack) => BrynzaInterop.GetWeaponOverride(bulletAttack);
        //public static void SetOnProjectileExplosion(this ProjectileExplosion projectileExplosion, UnityEvent<BlastAttack, BlastAttack.Result> unityEvent) => BrynzaInterop.SetOnProjectileExplode(projectileExplosion, unityEvent);
        //public static UnityEvent<BlastAttack, BlastAttack.Result> GetOnProjectileExplosion(this ProjectileExplosion projectileExplosion) => BrynzaInterop.GetOnProjectileExplode(projectileExplosion);
        public static T CopyComponent<T>(this GameObject destination, T original) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }
        public static T GetOrAddComponent<T>(this Transform transform) where T : Component
        {
            return transform.gameObject.GetOrAddComponent<T>();
        }
        public static void AddBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef, int amount)
        {
            characterBody.AddBuffAuthotiry(buffDef.buffIndex, amount);
        }
        public static void AddBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex, int amount)
        {
            characterBody.AddOrRemoveBuffAuthotiry(buffIndex, amount);
        }
        public static void RemoveBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef, int amount)
        {
            characterBody.RemoveBuffAuthotiry(buffDef.buffIndex, amount);
        }
        public static void RemoveBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex, int amount)
        {
            characterBody.AddOrRemoveBuffAuthotiry(buffIndex, -amount);
        }
        public static void AddBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef)
        {
            characterBody.AddBuffAuthotiry(buffDef.buffIndex);
        }
        public static void AddBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex)
        {
            characterBody.AddOrRemoveBuffAuthotiry(buffIndex, 1);
        }
        public static void RemoveBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef)
        {
            characterBody.RemoveBuffAuthotiry(buffDef.buffIndex);
        }
        public static void RemoveBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex)
        {
            characterBody.AddOrRemoveBuffAuthotiry(buffIndex, -1);
        }
        public static void AddOrRemoveBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef, int amount)
        {
            characterBody.AddOrRemoveBuffAuthotiry(buffDef.buffIndex, amount);
        }
        public static void AddOrRemoveBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex, int amount)
        {
            new AddBuffNetMessage(characterBody.netId, buffIndex, amount, -1f).Send(NetworkDestination.Server);
        }
        public static void AddTimedBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef, int amount, float duration)
        {
            characterBody.AddTimedBuffAuthotiry(buffDef.buffIndex, amount, duration);
        }
        public static void AddTimedBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex, int amount, float duration)
        {
            new AddBuffNetMessage(characterBody.netId, buffIndex, amount, duration).Send(NetworkDestination.Server);
        }
        public static void ChangeTimescaleForAllClients(this Time time, float value)
        {
            Utils.ChangeTimescaleForAllClients(value);
        }
        public static bool MatchStfldOut<T>(this Instruction instr, string name, out ILLabel iLLabel)
        {
            if (instr.MatchStfld(out var value))
            {
                iLLabel = (ILLabel)instr.Operand;
                return value.Is(typeof(T), name);
            }
            iLLabel = null;
            return false;
        }
        public static void AddValueToListInDictionary<T1, T2>(this Dictionary<T1, List<T2>> keyValuePairs, T1 t1, T2 t2)
        {
            if (keyValuePairs.ContainsKey(t1))
            {
                keyValuePairs[t1].Add(t2);
            }
            else
            {
                List<T2> list = [t2];
                keyValuePairs.Add(t1, list);
            }
        }
        public static void RemoveValueFromListInDictionary<T1, T2>(this Dictionary<T1, List<T2>> keyValuePairs, T1 t1, T2 t2)
        {
            if (keyValuePairs.ContainsKey(t1))
            {
                keyValuePairs[t1].Remove(t2);
                if (keyValuePairs[t1].Count <= 0)
                {
                    keyValuePairs.Remove(t1);
                }
            }
        }
        internal static ModMetaData GetModMetaData(this Assembly assembly)
        {
            ModMetaData modMetaData = default;

            Type[] types = assembly.GetExportedTypes();

            foreach (var item in types)
            {
                BepInPlugin bepInPlugin = item.GetCustomAttribute<BepInPlugin>();

                if (bepInPlugin == null) continue;

                modMetaData.Guid = bepInPlugin.GUID;
                modMetaData.Name = bepInPlugin.Name;
            }

            return modMetaData;
        }
    }
}