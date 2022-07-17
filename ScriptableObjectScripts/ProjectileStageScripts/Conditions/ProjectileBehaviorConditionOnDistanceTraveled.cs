using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionOnDistanceTraveled", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageCondition/ On Distance Traveled")]
    public class ProjectileBehaviorConditionOnDistanceTraveled : ProjectileBehaviorCondition
    {
        [Tooltip("The distance traveled before this condition evaluates true")]
        public float Distance = 0f;
        [Tooltip("Will this condition evaluate True when we exceed or equal the distance, or will we evalutate True when we are under or equal to the distance?")]
        public bool TrueWhenExceedsDistance = true;

        public override bool Evaluate(ActiveProjectile proj)
        {

            bool eval;

            if (TrueWhenExceedsDistance)
                eval = proj.LifetimeDistance >= Distance;
            else
                eval = proj.LifetimeDistance <= Distance;


            return eval;
        }
    }
}
