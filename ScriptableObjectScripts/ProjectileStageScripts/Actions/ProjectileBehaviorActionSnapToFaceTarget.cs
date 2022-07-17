using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem {
    [CreateAssetMenu(fileName = "StageActionSnapToFaceTarget", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageAction/ Snap To Face Target")]
    public class ProjectileBehaviorActionSnapToFaceTarget : ProjectileBehaviorAction
    {
        [Range(1,180),Tooltip("The amount that the projectile can turn, in degrees, when this actions triggers")]
        public float maxSnapAmount = 90;

        public override void Tick(ActiveProjectile proj)
        {
            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.NoSeek)
                return;

            Vector3 forward = proj.VelocityNormal;
            Vector3 toOther = Vector3.zero;

            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform)
                toOther = proj.SeekData.TargetTransform.position;
            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekVectorPoint)
                toOther = proj.SeekData.TargetPoint;

            toOther -= proj.Position;

            proj.Velocity = Vector3.RotateTowards(forward, toOther, maxSnapAmount * Mathf.Deg2Rad, 0)*proj.Velocity.magnitude;
            proj.VelocityNormal = proj.Velocity.normalized;

        }
    }
}
