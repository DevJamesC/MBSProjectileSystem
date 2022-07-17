using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionOnDistanceToTarget", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageCondition/ On Distance to Target")]
    public class ProjectileBehaviorConditionOnDistanceToTarget : ProjectileBehaviorCondition
    {
        [Tooltip("The distance between the projectile and the target before this condition evaluates true")]
        public float Distance = 0f;
        [Tooltip("Will this condition evaluate True when we exceed or equal the distance, or will we evalutate True when we are under or equal to the distance?")]
        public bool TrueWhenExceedsDistance = true;

        public override bool Evaluate(ActiveProjectile proj)
        {

            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.NoSeek)
                return false;

            if (proj.SeekData.TargetTransform==null)
                return false;

            bool eval;
            Vector3 targPoint = proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform ? proj.SeekData.TargetTransform.position : proj.SeekData.TargetPoint;
            float distance= Vector3.Distance(proj.Position, targPoint);

            if (TrueWhenExceedsDistance)
                eval = distance >= Distance;
            else
                eval = distance <= Distance;


            return eval;
        }
    }
}
