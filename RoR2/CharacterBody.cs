using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RoR2
{
    public class CharacterBody
    {
        public int bapi_maxWallJumpCount;
        public int bapi_baseWallJumpCount;
        public int[] bapi_clientBuffs;
        public Run.FixedTimeStamp bapi_lastJumpTime;
        public Vector3 bapi_positionDelta;
        public Vector3 bapi_previousPosition;
        public int bapi_bulletCount;
        public float bapi_bulletCountGraceDuration;
        public float bapi_bulletCountResetTimer;
    }
}
