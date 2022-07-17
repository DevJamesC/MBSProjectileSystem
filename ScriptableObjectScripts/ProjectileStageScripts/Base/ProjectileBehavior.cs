using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    public class ProjectileBehavior:ScriptableObject
    {
        public virtual void Tick(ActiveProjectile  proj)
        {
            return;
        }
    }
}

