using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageConditionOnHit", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageCondition/ On Hit")]
    public class ProjectileBehaviorConditionOnHit : ProjectileBehaviorCondition
    {
        [Tooltip("will this evaluate true against colliders, triggers, or both")]
        public TriggerType BehaviorType=TriggerType.CollidersOnly;
        public override bool Evaluate(ActiveProjectile proj)
        {
            if (proj.LastRaycastHit.collider == null)
                return false;

            switch (BehaviorType)
            {
                case TriggerType.CollidersOnly:                  
                    return !proj.LastRaycastHit.collider.isTrigger;

                case TriggerType.TriggersOnly:
                    return proj.LastRaycastHit.collider.isTrigger;

                case TriggerType.CollidersAndTriggers:
                    return true;
            }

            return false;
        }

        public enum TriggerType
        {
            CollidersOnly,
            CollidersAndTriggers,
            TriggersOnly
        }
    }
}
