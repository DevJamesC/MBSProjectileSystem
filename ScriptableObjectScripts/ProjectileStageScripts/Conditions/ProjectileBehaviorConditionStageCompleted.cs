using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionOnStageCompleted", menuName = "MBS/Projectile System/Scriptable Objects/ Projectiles/ StageCondition/ On Stage Completed")]
    public class ProjectileBehaviorConditionStageCompleted : ProjectileBehaviorCondition
    {
        public int EvaluatedStageIndex;
        public override bool Evaluate(ActiveProjectile proj)
        {
        
            if(EvaluatedStageIndex>proj.Stages.Count-1)
                return false;

            return proj.Stages[EvaluatedStageIndex].Triggered;
        }
    }
}
