using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RoR2
{
    public class BulletAttack
    {
        public GameObject bapi_weaponOverride;
        public List<object> bapi_ignoredHealthComponentList;
        public bool bapi_ignoreHitTargets;
        public bool bapi_forceMassIsOne;
        public bool bapi_forceAlwaysApply;
        public bool bapi_forceDisableAirControlUntilCollision;
        public Vector3 bapi_bonusForce;
        public bool bapi_noWeaponIfOwner;
    }
}
