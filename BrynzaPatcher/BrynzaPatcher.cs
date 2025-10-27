using EntityStates;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace BrynzaAPI;

internal static class Ror2Patcher
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
        //PatchEntityStateMachine(assembly);
        //PatchSkillDef(assembly);
        PatchBulletAttack(assembly);
        PatchCharacterBody(assembly);
        PatchCharacterModel(assembly);
        PatchRow(assembly);
        PatchLoadoutPanelController(assembly);
        //PatchConfigEntry(ref assembly);
    }

    private static void PatchCharacterBody(AssemblyDefinition assembly)
    {
        TypeDefinition characterBody = assembly.MainModule.GetType("RoR2", "CharacterBody");
        if (characterBody != null)
        {
            characterBody.Fields.Add(new FieldDefinition("bapi_baseWallJumpCount", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(int))));
            characterBody.Fields.Add(new FieldDefinition("bapi_maxWallJumpCount", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(int))));
        }
    }
    private static void PatchCharacterModel(AssemblyDefinition assembly)
    {
        TypeDefinition rendererInfo = assembly.MainModule.GetType("RoR2.CharacterModel/RendererInfo");
        if (rendererInfo != null)
        {
            rendererInfo.Fields.Add(new FieldDefinition("bapi_dontFadeCloseOn", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
        }
    }
    private static void PatchEntityStateMachine(AssemblyDefinition assembly)
    {
        TypeDefinition entityStateMachine = assembly.MainModule.GetType("RoR2", "EntityStateMachine");
        entityStateMachine?.Methods.Add(new MethodDefinition("bapi_SetStateToMain", MethodAttributes.Public, assembly.MainModule.ImportReference(typeof(void))));
    }
    private static void PatchSkillDef(AssemblyDefinition assembly)
    {
        TypeDefinition genericSkill = assembly.MainModule.GetType("RoR2.Skills", "SkillDef");
        genericSkill?.Methods.Add(new MethodDefinition("bapi_CanApplyAmmoPack", MethodAttributes.Virtual, assembly.MainModule.ImportReference(typeof(bool))));
    }
    private static void PatchBulletAttack(AssemblyDefinition assembly)
    {
        TypeDefinition genericSkill = assembly.MainModule.GetType("RoR2", "BulletAttack");
        if (genericSkill != null)
        {
            genericSkill.Fields.Add(new FieldDefinition("bapi_ignoredHealthComponentList", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(List<object>))));
            genericSkill.Fields.Add(new FieldDefinition("bapi_ignoreHitTargets", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
            genericSkill.Fields.Add(new FieldDefinition("bapi_forceMassIsOne", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
        }
    }
    private static void PatchProjectileExplosion(AssemblyDefinition assembly)
    {
    }
    private static void PatchGenericSkill(AssemblyDefinition assembly)
    {
        TypeDefinition genericSkill = assembly.MainModule.GetType("RoR2", "GenericSkill");
        if (genericSkill != null)
        {
            genericSkill.Fields.Add(new FieldDefinition("bapi_extraSkills", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(List<object>))));
            genericSkill.Fields.Add(new FieldDefinition("bapi_linkedSkill", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(object))));
            genericSkill.Fields.Add(new FieldDefinition("bapi_section", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(string))));
        }
    }
    private static void PatchRow(AssemblyDefinition assembly)
    {
        TypeDefinition row = assembly.MainModule.GetType("RoR2.UI.LoadoutPanelController/Row");
        row?.Fields.Add(new FieldDefinition("bapi_section", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(string))));
    }
    private static void PatchLoadoutPanelController(AssemblyDefinition assembly)
    {
        TypeDefinition loadoutPanelController = assembly.MainModule.GetType("RoR2.UI", "LoadoutPanelController");
        loadoutPanelController?.Fields.Add(new FieldDefinition("bapi_sections", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(List<string>))));
    }
    private static void PatchSkillLocator(AssemblyDefinition assembly)
    {
        TypeDefinition skillLocator = assembly.MainModule.GetType("RoR2", "SkillLocator");
        skillLocator?.Fields.Add(new FieldDefinition("bapi_bonusSkills", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(List<object>))));
    }
    private static void PatchCharacterMotor(AssemblyDefinition assembly)
    {
        TypeDefinition characterMotor = assembly.MainModule.GetType("RoR2", "CharacterMotor");
        if (characterMotor != null)
        {
            characterMotor.Fields.Add(new FieldDefinition("bapi_velocityOverride", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(Vector3))));
            characterMotor.Fields.Add(new FieldDefinition("bapi_keepVelocityOnMoving", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
            characterMotor.Fields.Add(new FieldDefinition("bapi_consistentAcceleration", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(float))));
            characterMotor.Fields.Add(new FieldDefinition("bapi_fluidMaxDistanceDelta", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
            characterMotor.Fields.Add(new FieldDefinition("bapi_strafe", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
            characterMotor.Fields.Add(new FieldDefinition("bapi_bunnyHop", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
            characterMotor.Fields.Add(new FieldDefinition("bapi_wallJumpCount", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(int))));
        }
    }
    private static void PatchConfigEntry(ref AssemblyDefinition assembly)
    {
        TypeDefinition configEntry = assembly.MainModule.GetType("BepInEx.Configuration", "ConfigEntryBase");
        if (configEntry != null)
        {
            configEntry.Fields.Add(new FieldDefinition("bapi_isServerConfig", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
        }
    }
}

//internal static class BepinexPatcher
//{
//    public static TypeDefinition entry;
//    public static IEnumerable<string> TargetDLLs
//    {
//        get
//        {
//            yield return "BepInEx.dll";
//        }
//    }
//    public static void Patch(AssemblyDefinition assembly)
//    {
//        PatchConfigEntry(assembly);
//    }
//    private static void PatchConfigEntry(AssemblyDefinition assembly)
//    {
//        TypeDefinition configEntry = assembly.MainModule.GetType("BepInEx.Configuration", "ConfigEntryBase");
//        if (configEntry != null)
//        {
//            configEntry.Fields.Add(new FieldDefinition("bapi_isServerConfig", FieldAttributes.Public, assembly.MainModule.ImportReference(typeof(bool))));
//        }
//    }
//}
