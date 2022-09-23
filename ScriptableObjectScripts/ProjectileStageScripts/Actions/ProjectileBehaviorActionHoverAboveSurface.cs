using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageActionHoverAboveSurface", menuName = "MBS/Projectile System/Scriptable Objects/ Projectiles/ StageAction/ Hover Above Surface")]
    public class ProjectileBehaviorActionHoverAboveSurface : ProjectileBehaviorAction
    {
        public LayerMask SurfaceLayers;
        public float HoverDistance;

        public override void Tick(ActiveProjectile proj)
        {
            if(Physics.Raycast(new Ray(proj.Position, Vector3.down), out RaycastHit hit, HoverDistance + .01f, SurfaceLayers))
            {
                proj.Position.y = hit.point.y + HoverDistance;
            }
        }
    }
}
