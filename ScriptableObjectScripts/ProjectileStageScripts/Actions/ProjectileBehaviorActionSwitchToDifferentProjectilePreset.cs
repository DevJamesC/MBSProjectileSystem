using MBS.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace MBS.ProjectileSystem
{
    [CreateAssetMenu(fileName = "StageActionSwitchProjectilePresets", menuName = "MBS/Projectile System/Scriptable Objects/ Projectiles/ StageAction/ Switch Projectile Presets")]
    public class ProjectileBehaviorActionSwitchToDifferentProjectilePreset : ProjectileBehaviorAction
    {
        [Tooltip("Use this to change the projectile values when the stage activates. If not changing the values, leave this null")]
        public Projectile ProjectileBaseToChangeTo;
        [Tooltip("This will do nothing if ProjectileBaseToChangeTo is null.")]
        public ProjectileStageChangeStageAddOptions ProjectileStageChangeStageAddOption;
        [Tooltip("True, value will be overridden, false, value will be added.")]
        public bool SetPenetrationOnBaseChange;
        [Tooltip("True, value will be overridden, false, value will be added.")]
        public bool SetPenetrationNumberOnBaseChange;
        [Tooltip("True, the distance and time evaulations will be evaulate at 0 when this stage starts, false, distance and time evaluations will be CurrentLife and CurrentDistance.")]
        public bool UpdateLifeAndDistanceOffsets = true;

        public override void Tick(ActiveProjectile proj)
        {
            if (ProjectileBaseToChangeTo != null)
            {
                //Apply changes to projectile
                if (UpdateLifeAndDistanceOffsets)
                {
                    proj.LifetimeStageOffset = proj.CurrentTimeAlive;
                    proj.DistanceStageOffset = proj.LifetimeDistance;
                }
                else
                {
                    proj.LifetimeStageOffset = 0;
                    proj.DistanceStageOffset = 0;
                }
                proj.MaxTimeAlive += ProjectileBaseToChangeTo.lifetime;
                proj.Velocity = proj.VelocityNormal * ProjectileBaseToChangeTo.speed.Evaluate(proj.ProjectileBlueprint.evaluateSpeedWithDistance ? proj.LifetimeDistance-proj.DistanceStageOffset : proj.CurrentTimeAlive-proj.LifetimeStageOffset);
                proj.MaxPenetrationDistance = SetPenetrationOnBaseChange ? ProjectileBaseToChangeTo.penetration : proj.MaxPenetrationDistance + ProjectileBaseToChangeTo.penetration;
                proj.MaxNumOfPenetrations = SetPenetrationNumberOnBaseChange ? ProjectileBaseToChangeTo.maxNumPenetrationObjects : proj.MaxNumOfPenetrations + ProjectileBaseToChangeTo.maxNumPenetrationObjects;

                //apply and upwards offset
                Vector3 dir = Physics.gravity.normalized * ProjectileBaseToChangeTo.upwardsOffset;
                proj.VelocityNormal = (proj.VelocityNormal + dir).normalized;
                proj.Velocity = proj.VelocityNormal * proj.Velocity.magnitude;

                if (!proj.DryRun)
                    UpdateParticleSystemAndTrail(proj);

                proj.ProjectileBlueprint = ProjectileBaseToChangeTo;
                proj.ProjectileBlueprintInstance = new Projectile.ProjectileInstanceData(ProjectileBaseToChangeTo);

                switch (ProjectileStageChangeStageAddOption)
                {
                    case ProjectileStageChangeStageAddOptions.AddNewStagesToStages:
                        List<Projectile.ProjectileStage> stages = new List<Projectile.ProjectileStage>(ProjectileBaseToChangeTo.projectileStages);

                        for (int i = 0; i < stages.Count; i++)
                        {
                            Projectile.ProjectileStage s = stages[i];
                            s.Triggered = false;
                            stages[i] = s;
                        }

                        proj.Stages.AddRange(stages);
                        break;
                    case ProjectileStageChangeStageAddOptions.ReplaceStagesWithNewStages:
                        stages = new List<Projectile.ProjectileStage>(ProjectileBaseToChangeTo.projectileStages);

                        for (int i = 0; i < stages.Count; i++)
                        {
                            Projectile.ProjectileStage s = stages[i];
                            s.Triggered = false;
                            stages[i] = s;
                        }
                        proj.Stages = stages;
                        break;
                    case ProjectileStageChangeStageAddOptions.DoNotAddNewStages:
                        break;
                }
            }
        }

        protected void UpdateParticleSystemAndTrail(ActiveProjectile proj)
        {
            //check if the trail particle system needs to change
            if (proj.TrailingParticleSystem != null)//If we have a trail
            {
                if (proj.ProjectileBlueprint.projectileTrailParticleSystemPrefab != ProjectileBaseToChangeTo.projectileTrailParticleSystemPrefab)//If our current trail and our new trail are not the same
                {
                    proj.TrailingParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }


            }
            //Instanciate a new particle system if necessary
            if (ProjectileBaseToChangeTo.projectileTrailParticleSystemPrefab.RuntimeKeyIsValid())//If we have a new trail
            {
                if (proj.ProjectileBlueprint.projectileTrailParticleSystemPrefab != ProjectileBaseToChangeTo.projectileTrailParticleSystemPrefab)//If our current trail and our new trail are not the same
                {
                    MBSSimpleAddressablePooler pooler = MBSSimpleAddressablePooler.GetInstanceOrInstanciate(ProjectileBaseToChangeTo.projectileTrailParticleSystemPrefab);
                    if (pooler != null)
                    {
                        GameObjectOrHandle<GameObject> trail = pooler.GetPooledGameObject();
                        if (!trail.IsEmpty())
                        {
                            if (trail.Object != null)//If we have a gameobject
                            {
                                proj.SetUpTrailingGameObject(trail.Object, proj.Position);
                            }
                            else if (trail.IsLoadingHandle)
                            {
                                trail.Handle.Completed += (asyncOperationHandle) =>
                                {
                                    if (proj == null)
                                        return;
                                    GameObjectOrHandle<GameObject> trailObj = pooler.GetPooledGameObject();
                                    if (!trailObj.IsEmpty())
                                    {
                                        if (trailObj.Object != null)
                                        {
                                            proj.SetUpTrailingGameObject(trailObj.Object, proj.Position);
                                        }
                                        else
                                        {
                                            trailObj.Handle.Completed += (asyncOpHandle) =>
                                            {
                                                proj.SetUpTrailingGameObject(asyncOpHandle.Result as GameObject, proj.Position);
                                            };
                                        }
                                    }
                                };
                            }
                            else
                            {
                                trail.Handle.Completed += (asyncOperationHandle) =>
                                {
                                    proj.SetUpTrailingGameObject(asyncOperationHandle.Result as GameObject, proj.Position);
                                };
                            }
                        }
                    }


                }

            }
            else if (proj.TrailingGameobject != null)//If we have a trailing particle system from last stage, but none this stage, then we need to shut it down
            {
                if (proj.TrailingParticleSystem)
                    proj.TrailingParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                if (proj.TrailingAnimator)
                {
                    proj.TrailingAnimator = null;
                }

                if (proj.TrailingGameobject && !proj.TrailingParticleSystem)//If we have a trailing gameobject, but no particle system with it, set the object inactive
                {
                    proj.TrailingGameobject.SetActive(false);
                }
                proj.TrailingGameobject = null;

                
            }
        }

        [Serializable]
        public enum ProjectileStageChangeStageAddOptions
        {
            AddNewStagesToStages,
            ReplaceStagesWithNewStages,
            DoNotAddNewStages
        }
    }
}
