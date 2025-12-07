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
    }
}
