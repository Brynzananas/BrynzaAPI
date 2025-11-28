using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

[assembly: InternalsVisibleTo("BrynzaAPI")]
namespace BrynzaAPI.Interop;

public static class BrynzaInterop
{
    public static List<GenericSkill> GetExtraSkills(GenericSkill genericSkill) => genericSkill.bapi_extraSkills != null ? (Enumerable.Range(0, genericSkill.bapi_extraSkills.Count)
                             .Select(i => (genericSkill.bapi_extraSkills[i] is GenericSkill ? genericSkill.bapi_extraSkills[i] as GenericSkill : null))
                             .ToList()) : null;
    public static void SetExtraSkills(GenericSkill genericSkill, List<object> list) => genericSkill.bapi_extraSkills = list;
    public static void AddExtraSkill(GenericSkill genericSkill, GenericSkill obj)
    {
        if (genericSkill.bapi_extraSkills != null)
        {
            genericSkill.bapi_extraSkills.Add(obj);
        }
        else
        {
            genericSkill.bapi_extraSkills = new List<object>();
            genericSkill.bapi_extraSkills.Add(obj);
        }
    }
    public static void RemoveExtraSkill(GenericSkill genericSkill, GenericSkill obj)
    {
        if (genericSkill.bapi_extraSkills != null && genericSkill.bapi_extraSkills.Contains(obj)) genericSkill.bapi_extraSkills.Remove(obj);
    }
    public static void LinkSkill(GenericSkill genericSkil, object obj) => genericSkil.bapi_linkedSkill = obj;
    public static GenericSkill GetLinkedSkill(GenericSkill genericSkil) => genericSkil.bapi_linkedSkill != null && genericSkil.bapi_linkedSkill is GenericSkill ? genericSkil.bapi_linkedSkill as GenericSkill : null;
    public static void AddBonusSkill(SkillLocator skillLocator, GenericSkill genericSkill)
    {
        if (skillLocator.bapi_bonusSkills != null)
        {
            skillLocator.bapi_bonusSkills.Add(genericSkill);
        }
        else
        {
            skillLocator.bapi_bonusSkills = new List<object>();
            skillLocator.bapi_bonusSkills.Add(genericSkill);
        }
    }
    public static void RemoveBonusSkill(SkillLocator skillLocator, GenericSkill genericSkill)
    {
        if (skillLocator.bapi_bonusSkills != null && skillLocator.bapi_bonusSkills.Contains(genericSkill)) skillLocator.bapi_bonusSkills.Remove(genericSkill);
    }
    public static List<GenericSkill> GetBonusSkills(SkillLocator skillLocator) => skillLocator.bapi_bonusSkills != null ? (Enumerable.Range(0, skillLocator.bapi_bonusSkills.Count)
                             .Select(i => (skillLocator.bapi_bonusSkills[i] is GenericSkill ? skillLocator.bapi_bonusSkills[i] as GenericSkill : null))
                             .ToList()) : null;
    public static Vector3 GetVelocityOverride(CharacterMotor characterMotor) => characterMotor.bapi_velocityOverride;
    public static void SetVelocityOverride(CharacterMotor characterMotor, Vector3 value) => characterMotor.bapi_velocityOverride = value;
    public static bool GetKeepVelocityOnMoving(CharacterMotor characterMotor) => characterMotor.bapi_keepVelocityOnMoving;
    public static void SetKeepVelocityOnMoving(CharacterMotor characterMotor, bool value) => characterMotor.bapi_keepVelocityOnMoving = value;
    public static float GetConsistentAcceleration(CharacterMotor characterMotor) => characterMotor.bapi_consistentAcceleration;
    public static void SetConsistentAcceleration(CharacterMotor characterMotor, float value) => characterMotor.bapi_consistentAcceleration = value;
    public static float GetAirControlFromVelocityAdd(CharacterMotor characterMotor) => characterMotor.bapi_airControlFromVelocityAdd;
    public static void SetAirControlFromVelocityAdd(CharacterMotor characterMotor, float value) => characterMotor.bapi_airControlFromVelocityAdd = value;
    public static bool GetFluidMaxDistanceDelta(CharacterMotor characterMotor) => characterMotor.bapi_fluidMaxDistanceDelta;
    public static void SetFluidMaxDistanceDelta(CharacterMotor characterMotor, bool value) => characterMotor.bapi_fluidMaxDistanceDelta = value;
    public static bool GetStrafe(CharacterMotor characterMotor) => characterMotor.bapi_strafe;
    public static void SetStrafe(CharacterMotor characterMotor, bool value) => characterMotor.bapi_strafe = value;
    public static bool GetBunnyHop(CharacterMotor characterMotor) => characterMotor.bapi_bunnyHop;
    public static void SetBunnyHop(CharacterMotor characterMotor, bool value) => characterMotor.bapi_bunnyHop = value;
    public static int GetBaseWallJumpCount(CharacterBody characterBody) => characterBody.bapi_baseWallJumpCount;
    public static void SetBaseWallJumpCount(CharacterBody characterBody, int value) => characterBody.bapi_baseWallJumpCount = value;
    public static int GetMaxWallJumpCount(CharacterBody characterBody) => characterBody.bapi_maxWallJumpCount;
    public static void SetMaxWallJumpCount(CharacterBody characterBody, int value) => characterBody.bapi_maxWallJumpCount = value;
    public static int GetWallJumpCount(CharacterMotor characterMotor) => characterMotor.bapi_wallJumpCount;
    public static void SetWallJumpCount(CharacterMotor characterMotor, int value) => characterMotor.bapi_wallJumpCount = value;
    public static bool CanApplyAmmoPack(SkillDef skillDef) => skillDef.bapi_CanApplyAmmoPack();
    public static void SetStateToMain(EntityStateMachine entityStateMachine) => entityStateMachine.bapi_SetStateToMain();
    public static List<object> GetIgnoredHealthComponents(BulletAttack bulletAttack) => bulletAttack.bapi_ignoredHealthComponentList;
    public static void SetIgnoredHealthComponents(BulletAttack bulletAttack, List<object> value) => bulletAttack.bapi_ignoredHealthComponentList = value;
    public static bool GetIgnoreHitTargets(BulletAttack bulletAttack) => bulletAttack.bapi_ignoreHitTargets;
    public static void SetForceMassIsOne(BulletAttack bulletAttack, bool value) => bulletAttack.bapi_forceMassIsOne = value;
    public static bool GetForceMassIsOne(BulletAttack bulletAttack) => bulletAttack.bapi_forceMassIsOne;
    public static void SetForceMassIsOne(BlastAttack blastAttack, bool value) => blastAttack.bapi_forceMassIsOne = value;
    public static bool GetForceMassIsOne(BlastAttack blastAttack) => blastAttack.bapi_forceMassIsOne;
    public static void SetForceMassIsOne(ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo, bool value) => blastAttackDamageInfo.bapi_forceMassIsOne = value;
    public static bool GetForceMassIsOne(ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo) => blastAttackDamageInfo.bapi_forceMassIsOne;
    public static void SetForceMassIsOne(DamageInfo damageInfo, bool value) => damageInfo.bapi_forceMassIsOne = value;
    public static bool GetForceMassIsOne(DamageInfo damageInfo) => damageInfo.bapi_forceMassIsOne;
    public static void SetForceAlwaysApply(BulletAttack bulletAttack, bool value) => bulletAttack.bapi_forceAlwaysApply = value;
    public static bool GetForceAlwaysApply(BulletAttack bulletAttack) => bulletAttack.bapi_forceAlwaysApply;
    public static void SetForceAlwaysApply(BlastAttack blastAttack, bool value) => blastAttack.bapi_forceAlwaysApply = value;
    public static bool GetForceAlwaysApply(BlastAttack blastAttack) => blastAttack.bapi_forceAlwaysApply;
    public static void SetForceAlwaysApply(ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo, bool value) => blastAttackDamageInfo.bapi_forceAlwaysApply = value;
    public static bool GetForceAlwaysApply(ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo) => blastAttackDamageInfo.bapi_forceAlwaysApply;
    public static void SetForceAlwaysApply(DamageInfo damageInfo, bool value) => damageInfo.bapi_forceAlwaysApply = value;
    public static bool GetForceAlwaysApply(DamageInfo damageInfo) => damageInfo.bapi_forceAlwaysApply;
    public static void SetForceDisableAirControlUntilCollision(BulletAttack bulletAttack, bool value) => bulletAttack.bapi_forceDisableAirControlUntilCollision = value;
    public static bool GetForceDisableAirControlUntilCollision(BulletAttack bulletAttack) => bulletAttack.bapi_forceDisableAirControlUntilCollision;
    public static void SetForceDisableAirControlUntilCollision(BlastAttack blastAttack, bool value) => blastAttack.bapi_forceDisableAirControlUntilCollision = value;
    public static bool GetForceDisableAirControlUntilCollision(BlastAttack blastAttack) => blastAttack.bapi_forceDisableAirControlUntilCollision;
    public static void SetForceDisableAirControlUntilCollision(ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo, bool value) => blastAttackDamageInfo.bapi_forceDisableAirControlUntilCollision = value;
    public static bool GetForceDisableAirControlUntilCollision(ref BlastAttack.BlastAttackDamageInfo blastAttackDamageInfo) => blastAttackDamageInfo.bapi_forceDisableAirControlUntilCollision;
    public static void SetForceDisableAirControlUntilCollision(DamageInfo damageInfo, bool value) => damageInfo.bapi_forceDisableAirControlUntilCollision = value;
    public static bool GetForceDisableAirControlUntilCollision(DamageInfo damageInfo) => damageInfo.bapi_forceDisableAirControlUntilCollision;
    public static void SetBonusForce(BulletAttack bulletAttack, Vector3 value) => bulletAttack.bapi_bonusForce = value;
    public static Vector3 GetBonusForce(BulletAttack bulletAttack) => bulletAttack.bapi_bonusForce;
    public static void SetIgnoreHitTargets(BulletAttack bulletAttack, bool value) => bulletAttack.bapi_ignoreHitTargets = value;
    public static string GetSection(LoadoutPanelController.Row row) => row.bapi_section;
    public static void SetSection(LoadoutPanelController.Row row, string value) => row.bapi_section = value;
    public static string GetSection(GenericSkill genericSkill) => genericSkill.bapi_section;
    public static void SetSection(GenericSkill genericSkill, string value) => genericSkill.bapi_section = value;
    public static List<string> GetSections(LoadoutPanelController loadoutPanelController) => loadoutPanelController.bapi_sections;
    public static void SetSections(LoadoutPanelController loadoutPanelController, List<string> value) => loadoutPanelController.bapi_sections = value;
    public static bool GetDontFadeWhenNearCamera(ref CharacterModel.RendererInfo rendererInfo) => rendererInfo.bapi_dontFadeCloseOn;
    public static void SetDontFadeWhenNearCamera(ref CharacterModel.RendererInfo rendererInfo, bool value) => rendererInfo.bapi_dontFadeCloseOn = value;
    public static void SetNoWeaponIfOwner(BulletAttack bulletAttack, bool value) => bulletAttack.bapi_noWeaponIfOwner = value;
    public static bool GetNoWeaponIfOwner(BulletAttack bulletAttack) => bulletAttack.bapi_noWeaponIfOwner;
    //public static List<HealthComponent> GetIgnoredHealthComponents(BulletAttack bulletAttack) => bulletAttack.bapi_ignoredHealthComponentList != null ? (Enumerable.Range(0, bulletAttack.bapi_ignoredHealthComponentList.Count)
    //                         .Select(i => (bulletAttack.bapi_ignoredHealthComponentList[i] is HealthComponent ? bulletAttack.bapi_ignoredHealthComponentList[i] as HealthComponent : null))
    //                         .ToList()) : null;
    //public static GameObject GetWeaponOverride(BulletAttack bulletAttack) => bulletAttack.bapi_weaponOverride;
    //public static void SetWeaponOverride(BulletAttack bulletAttack, GameObject value) => bulletAttack.bapi_weaponOverride = value;
    //public static UnityEvent<BlastAttack, BlastAttack.Result> GetOnProjectileExplode(ProjectileExplosion projectileExplosion)
    //{
    //    object obj = projectileExplosion.bapi_onProjectileExplosion;
    //    if (obj != null && obj is UnityEvent<BlastAttack, BlastAttack.Result>)
    //    {
    //        return obj as UnityEvent<BlastAttack, BlastAttack.Result>;
    //    }
    //    else
    //    {
    //        UnityEvent<BlastAttack, BlastAttack.Result> unityEvent = new UnityEvent<BlastAttack, BlastAttack.Result>();
    //        SetOnProjectileExplode(projectileExplosion, unityEvent);
    //        return obj as UnityEvent<BlastAttack, BlastAttack.Result>;
    //    }
    //}
    //public static void SetOnProjectileExplode(ProjectileExplosion projectileExplosion, UnityEvent<BlastAttack, BlastAttack.Result> value) => projectileExplosion.bapi_onProjectileExplosion = value;
}