using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionHoverAboveSurface", menuName = "MBS/Projectile System/Scriptable Objects/ Projectiles/ StageCondition/ Hover Above Surface")]
    public class ProjectileBehaviorConditionHoverAboveSurface : ProjectileBehaviorCondition
    {
        public LayerMask SurfaceLayers;
        public float HoverDistance;
        public float DistanceToCheck;
        public float SnapToSpeed;

        public override bool Evaluate(ActiveProjectile proj)
        {
            if (Physics.Raycast(new Ray(proj.Position, Vector3.down), out RaycastHit hit, DistanceToCheck, SurfaceLayers))
            {
                float targetHeight= hit.point.y + HoverDistance;
                proj.Position.y = Mathf.MoveTowards(proj.Position.y, targetHeight, SnapToSpeed*Time.fixedDeltaTime);
            }
            return false;
        }
    }
}
