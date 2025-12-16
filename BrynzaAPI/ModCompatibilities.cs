using BepInEx.Configuration;
using HG.Coroutines;
using RoR2;
using System;
using System.Collections;
using System.Text;
using UnityEngine;

namespace BrynzaAPI
{
    public static class ModCompatibilities
    {
        public static class ProjectilesConfiguratorCompatibility
        {
            public const string GUID = "com.brynzananas.projectilesconfigurator";
            public static void Init()
            {
                ProjectilesConfigurator.ProjectilesConfiguratorPlugin.GetProjectileCustomConfigsAsync += ConfigureProjectiles;
            }
            private static void ConfigureProjectiles(ParallelCoroutine parallelCoroutine, GameObject projectile, string sectionName)
            {
                parallelCoroutine.Add(ConfigureProjectilesAsync(projectile, sectionName));
            }
            private static IEnumerator ConfigureProjectilesAsync(GameObject projectile, string sectionName)
            {
                RocketJumpComponent rocketJumpComponent = projectile.GetComponent<RocketJumpComponent>();
                ConfigEntry<float> rjForce = null;
                ConfigEntry<float> rjRadiusMultiplier = null;
                ConfigEntry<PhysForceFlags> rjPhysForceFlags = null;
                ConfigEntry<RocketJumpComponent.RocketJumpFiltering> rjRocketJumpFiltering = null;
                GuidedProjectile guidedProjectile = projectile.GetComponent<GuidedProjectile>();
                ConfigEntry<float> gGuidingPower = null;
                yield return null;
                if (rocketJumpComponent)
                {
                    rjForce = ProjectilesConfigurator.ProjectilesConfiguratorPlugin.CreateConfig(sectionName, "Rocket Jump Force", rocketJumpComponent.force, "");
                    rjForce.SettingChanged += OnSettingChanged;
                    yield return null;
                    rjRadiusMultiplier = ProjectilesConfigurator.ProjectilesConfiguratorPlugin.CreateConfig(sectionName, "Rocket Jump Radius Multiplier", rocketJumpComponent.radiusMultiplier, "");
                    rjRadiusMultiplier.SettingChanged += OnSettingChanged;
                    yield return null;
                    rjPhysForceFlags = ProjectilesConfigurator.ProjectilesConfiguratorPlugin.CreateConfig(sectionName, "Rocket Jump Phys Force Flags", rocketJumpComponent.physForceFlags, "", false);
                    rjPhysForceFlags.SettingChanged += OnSettingChanged;
                    yield return null;
                    rjRocketJumpFiltering = ProjectilesConfigurator.ProjectilesConfiguratorPlugin.CreateConfig(sectionName, "Rocket Jump Filtering", rocketJumpComponent.rocketJumpFiltering, "");
                    rjRocketJumpFiltering.SettingChanged += OnSettingChanged;
                    yield return null;
                }
                if (guidedProjectile)
                {
                    gGuidingPower = ProjectilesConfigurator.ProjectilesConfiguratorPlugin.CreateConfig(sectionName, "Guiding Power", guidedProjectile.guidingPower, "");
                    gGuidingPower.SettingChanged += OnSettingChanged;
                    yield return null;
                }
                void OnSettingChanged(object sender, EventArgs e) => UpdateProjectile();
                void UpdateProjectile()
                {
                    if (rocketJumpComponent)
                    {
                        rocketJumpComponent.force = rjForce.Value;
                        rocketJumpComponent.radiusMultiplier = rjRadiusMultiplier.Value;
                        rocketJumpComponent.physForceFlags = rjPhysForceFlags.Value;
                        rocketJumpComponent.rocketJumpFiltering = rjRocketJumpFiltering.Value;
                    }
                    if (guidedProjectile)
                    {
                        guidedProjectile.guidingPower = gGuidingPower.Value;
                    }
                }
                UpdateProjectile();
                yield break;
            }
        }
    }
}
