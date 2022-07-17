using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionOnDie", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageCondition/ On Die")]
    public class ProjectileBehaviorConditionOnDie : ProjectileBehaviorCondition
    {

        public override bool Evaluate(ActiveProjectile proj)
        {

            return !proj.Alive;
        }
    }
}
