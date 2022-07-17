using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionOnHitTarget", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageCondition/ On Hit Target")]
    public class ProjectileBehaviorConditionOnHitTarget : ProjectileBehaviorCondition
    {
        public override bool Evaluate(ActiveProjectile proj)
        {
            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform)
            {
                if (!proj.LastRaycastHit.collider)
                    return false;

                if (proj.LastRaycastHit.collider.transform == proj.SeekData.TargetTransform)
                {
                    return true;
                }
            }
            return false;

        }


    }
}
