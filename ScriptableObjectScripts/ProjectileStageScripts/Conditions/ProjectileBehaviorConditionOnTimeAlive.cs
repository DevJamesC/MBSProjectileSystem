using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionOnTimeAlive", menuName = "MBS/Projectile System/Scriptable Objects/ Projectiles/ StageCondition/ On Time Alive")]
    public class ProjectileBehaviorConditionOnTimeAlive : ProjectileBehaviorCondition
    {
        [Tooltip("The time alive before this condition evaluates true")]
        public float TimeAlive = 0f;
        [Tooltip("Will this condition evaluate True when we exceed or equal the time alive, or will we evalutate True when we are under or equal to the time alive?")]
        public bool trueWhenExceedsTime = true;

        public override bool Evaluate(ActiveProjectile proj)
        {

            bool eval;

            if (trueWhenExceedsTime)
                eval = proj.CurrentTimeAlive >= TimeAlive;
            else
                eval = proj.CurrentTimeAlive <= TimeAlive;


            return eval;
        }
    }
}
