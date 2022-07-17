using MBS.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageActionEmitProjectile", menuName = "MBSTools/Scriptable Objects/ Projectiles/ StageAction/ Emit Projectile")]
    public class ProjectileBehaviorActionEmitProjectile : ProjectileBehaviorAction
    {
       
        public List <EmitData> emitData= new List<EmitData>();
        [Tooltip("If false, we'll use the emitter which this projectile was fired from. If true, we'll try to grab an emitter on this projectile's trail object, if one exists.")]
        public bool GetEmitterFromProjectileTrailObject;
        [Tooltip("If false, we'll use the direction of this projectile as the direction. If true, we will use the direction of the emitter as the direction.")]
        public bool UseEmitterDirection;
        [Tooltip("If false, we'll use the position of this projectile as the origin. If true, we will use the position of the emitter as the origin.")]
        public bool UseEmitterOrigin;
        [Tooltip("Since this code has no idea if it is part of a trajectory draw, or a proper dry run with graphics, we need to specify the dry run behavior.")]
        public DryRunMode DryRunBehavior;
        [Tooltip("The amount of launch particles. It will use the launch particle system from the origial emitter. If this is not desierable, the code will need to be refactored to support a different launch particle system for children")]
        public int LaunchParticles;
        public enum DryRunMode
        {
            NoAction,
            EmitNormal,
            DrawDebugTrajectory
        }


        protected ProjectileEmitter emitter;

        public override void Tick(ActiveProjectile proj)
        {
          if(emitter == null)
            {
                if (GetEmitterFromProjectileTrailObject && proj.TrailingGameobject)
                    emitter = proj.TrailingGameobject.GetComponentInChildren<ProjectileEmitter>();
                else if (!GetEmitterFromProjectileTrailObject)
                    emitter = proj.Emitter;
            }

            if (emitter == null)
                return;

            if (emitData == null)
                return;

            Vector3 pos = UseEmitterOrigin ? emitter.transform.position : proj.Position;
            Vector3 dir = UseEmitterDirection ? emitter.Origin.forward : proj.VelocityNormal;

            for (int i = 0; i < emitData.Count; i++)
            {               
                Vector3 adjustedPos =pos+ emitData[i].PositionOffset;
                //Get heading as a quaterion, then multiply offset*forward by the heading quaternion
                Vector3 adjustedDir =Quaternion.LookRotation(dir,Vector3.up)*(Quaternion.Euler(emitData[i].DirectionOffset) * Vector3.forward);
                //Debug.Log(dir);

                if (!proj.DryRun)
                {
                    emitter.LaunchSafe(adjustedPos, adjustedDir, proj.SeekData, proj.DryRun, emitData[i].ProjectileSO, LaunchParticles);
                }
                else
                {
                    switch (DryRunBehavior)
                    {
                        case DryRunMode.NoAction:
                            break;
                        case DryRunMode.EmitNormal:
                            emitter.LaunchSafe(adjustedPos, adjustedDir, proj.SeekData, proj.DryRun, emitData[i].ProjectileSO);
                            break;
                        case DryRunMode.DrawDebugTrajectory:
                            emitter.GetTrajectoryFull(adjustedPos, adjustedDir, proj.SeekData, emitData[i].ProjectileSO, true);
                            break;
                    }
                }
                    
            }
           

        }

        [Serializable]
        public class EmitData
        {
            [Tooltip("The projectile Scriptable Object which to emit. If left blank, will spawn the projectile cached in the emitter")]
            public Projectile ProjectileSO;
            [Tooltip("The direction in which the projectile spawns at. 0,0,0 is same heading, 90,0,0 is down, 0,90,0 is to the right, ect.")]
            public Vector3 DirectionOffset;
            [Tooltip("The position at which the projectile spaws at. 0,0,0 is same position, 1,0,0 is 1 meter to the right, 0,1,0 is 1 meter above, ect.")]
            public Vector3 PositionOffset;
        }

        
    }
}
