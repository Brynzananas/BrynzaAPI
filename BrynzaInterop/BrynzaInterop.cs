using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RoR2;
using RoR2.Projectile;
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
    public static bool GetFluidMaxDistanceDelta(CharacterMotor characterMotor) => characterMotor.bapi_fluidMaxDistanceDelta;
    public static void SetFluidMaxDistanceDelta(CharacterMotor characterMotor, bool value) => characterMotor.bapi_fluidMaxDistanceDelta = value;
    public static bool GetStrafe(CharacterMotor characterMotor) => characterMotor.bapi_strafe;
    public static void SetStrafe(CharacterMotor characterMotor, bool value) => characterMotor.bapi_strafe = value;
    public static bool GetBunnyHop(CharacterMotor characterMotor) => characterMotor.bapi_bunnyHop;
    public static void SetBunnyHop(CharacterMotor characterMotor, bool value) => characterMotor.bapi_bunnyHop = value;
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