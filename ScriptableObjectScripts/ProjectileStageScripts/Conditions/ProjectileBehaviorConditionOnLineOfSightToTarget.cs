using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionOnLineOfSightToTarget", menuName = "MBS/Projectile System/Scriptable Objects/ Projectiles/ StageCondition/ On Line of Sight to Target")]
    public class ProjectileBehaviorConditionOnLineOfSightToTarget : ProjectileBehaviorCondition
    {
        [Tooltip("Will this condition evaluate True when we have No line of sight, or when we do have line of sight?")]
        public bool TrueWhenNoLineOfSight = true;

        public override bool Evaluate(ActiveProjectile proj)
        {

            if (proj.SeekData.CurrentSeekMode != ProjectileSeekData.Seekmode.NoSeek)
                return false;

            if (!proj.SeekData.TargetTransform)
                return false;

            bool eval=false;
            Vector3 targPoint = proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform ? proj.SeekData.TargetTransform.position : proj.SeekData.TargetPoint;
            Vector3 direction = proj.Position - targPoint;

            RaycastHit hit;
            if(Physics.Raycast(proj.Position, direction, out hit, proj.Emitter.TargetLayers))
            {
                if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekVectorPoint)//if we hit something, and our target is a vector point, then we are blocked.
                {
                    eval = false;
                }
                else if(proj.SeekData.TargetTransform==hit.transform)//else check if we hit our target. If so, then we have a clear line of sight
                {
                    eval = true;
                }
                    
            }
            else
            {
                if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekVectorPoint)//if we hit nothing, and our target is a vector point, then we have a clear line of sight.
                {
                    eval = true;
                }
                else //else we hit nothing, so we cannot have line of sight without acutally hiting the target we are trying to see. Maybe they are on different layers?
                {
                    eval = false;
                }
            }

            if (TrueWhenNoLineOfSight)
                eval = !eval;        


            return eval;
        }
    }
}
