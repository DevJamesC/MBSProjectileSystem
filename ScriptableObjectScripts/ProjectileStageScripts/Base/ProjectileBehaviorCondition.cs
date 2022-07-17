using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [Serializable]
    //[CreateAssetMenu(fileName = "NewProjectileStageCondition", menuName = "MBSTools/Scriptable Objects/ Projectiles/ New Projectile Stage Condition")]
    public class ProjectileBehaviorCondition : ProjectileBehavior
    {

        public virtual bool Evaluate(ActiveProjectile proj)
        {
            
            return false;
        }

        protected bool ShouldReturnEvaluated()
        {
            return false;
        }
    }
}
