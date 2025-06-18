using EntityStates;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using HarmonyLib.Tools;
using HarmonyLib;
namespace R2API;

internal static class CharacterBodyPatcher
{
    public static IEnumerable<string> TargetDLLs
    {
        get
        {
            yield return "RoR2.dll";
        }
    }
    public static void Patch(AssemblyDefinition assembly)
    {
        PatchGenericSkill(assembly);
        //PatchSkillLocator(assembly);
        PatchCharacterMotor(assembly);
        PatchProjectileExplosion(assembly);
        //PatchBulletAttack(assembly);
    }

    private static void PatchBulletAttack(AssemblyDefinition assembly)
    {
        TypeDefinition genericSkill = assembly.MainModule.GetType("RoR2", "BulletAttack");
        genericSkill?.Fields.Add(new FieldDefinition("bapi_weaponOverride", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(GameObject))));
    }

    private static void PatchProjectileExplosion(AssemblyDefinition assembly)
    {
    }

    private static void PatchGenericSkill(AssemblyDefinition assembly)
    {
        TypeDefinition genericSkill = assembly.MainModule.GetType("RoR2", "GenericSkill");
        genericSkill?.Fields.Add(new FieldDefinition("bapi_extraSkills", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(List<object>))));
        genericSkill?.Fields.Add(new FieldDefinition("bapi_linkedSkill", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object))));
    }
    private static void PatchSkillLocator(AssemblyDefinition assembly)
    {
        TypeDefinition skillLocator = assembly.MainModule.GetType("RoR2", "SkillLocator");
        skillLocator?.Fields.Add(new FieldDefinition("bapi_bonusSkills", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(List<object>))));
    }
    private static void PatchCharacterMotor(AssemblyDefinition assembly)
    {
        TypeDefinition characterMotor = assembly.MainModule.GetType("RoR2", "CharacterMotor");
        characterMotor?.Fields.Add(new FieldDefinition("bapi_velocityOverride", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Vector3))));
        characterMotor?.Fields.Add(new FieldDefinition("bapi_keepVelocityOnMoving", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
        characterMotor?.Fields.Add(new FieldDefinition("bapi_consistentAcceleration", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(float))));
        characterMotor?.Fields.Add(new FieldDefinition("bapi_fluidMaxDistanceDelta", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
        characterMotor?.Fields.Add(new FieldDefinition("bapi_strafe", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
        characterMotor?.Fields.Add(new FieldDefinition("bapi_bunnyHop", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
    }
}
