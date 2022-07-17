using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageActionKillProjectile", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageAction/ Kill Projectile")]
    public class ProjectileBehaviorActionKillProjectile : ProjectileBehaviorAction
    {
        public override void Tick(ActiveProjectile proj)
        {
            //proj.CurrentTimeAlive = proj.MaxTimeAlive;
            proj.Alive = false;
        }
    }
}
