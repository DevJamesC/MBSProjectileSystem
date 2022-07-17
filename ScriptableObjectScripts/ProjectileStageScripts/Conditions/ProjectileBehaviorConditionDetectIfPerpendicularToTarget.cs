using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionPerpedicularToTarget", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageCondition/ Perpedicular To Target")]
    public class ProjectileBehaviorConditionDetectIfPerpendicularToTarget : ProjectileBehaviorCondition
    {
        [Range(1,90),Tooltip("Since some projectiles move very fast, set this higher to allow the projectile to turn even if it has not reached/ has passed the target")]
        public float AngleLeniencey = 5f;

        public override bool Evaluate(ActiveProjectile proj)
        {

            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.NoSeek)
                return false;
          
            Vector3 forward = proj.Velocity.normalized;
            Vector3 toOther=Vector3.zero;

            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform)
                toOther = proj.SeekData.TargetTransform.position;
            if (proj.SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekVectorPoint)
                toOther = proj.SeekData.TargetPoint;

            toOther -= proj.Position;

            float dot = Vector3.Dot(forward, toOther);

            if (Mathf.Abs(dot) <= AngleLeniencey)
            {
                return true;
            }
                

            
            return false;
        }

    }
}
