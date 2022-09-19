using MBS.LocalGravity;
using MBS.LocalTimescale;
using MBS.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.ParticleSystem;
using Random = UnityEngine.Random;

namespace MBS.ProjectileSystem
{
    public enum ProjectileEventTypes
    {
        OnHit,
        OnHitTrigger,
        OnDie,
        OnRicochet,
        OnPenetration,
        OnPenetrationExit
    }
    public struct ProjectileEvent
    {
        public ProjectileEventTypes EventType;
        public ActiveProjectile Projectile;
        public RaycastHit RaycastHit;
        public ProjectileEmitter Emitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MBS.Tools.MBSGameEvent"/> struct.
        /// </summary>
        /// <param name="eventType">Event type.</param>
        public ProjectileEvent(ProjectileEventTypes eventType, ActiveProjectile proj, RaycastHit rayHit, ProjectileEmitter emitter)
        {
            EventType = eventType;
            Projectile = proj;
            RaycastHit = rayHit;
            Emitter = emitter;
        }

        static ProjectileEvent e;
        public static void Trigger(ProjectileEventTypes eventType, ActiveProjectile proj, RaycastHit rayHit, ProjectileEmitter emitter)
        {
            e.EventType = eventType;
            e.Projectile = proj;
            e.RaycastHit = rayHit;
            e.Emitter = emitter;
            MBSEventManager.TriggerEvent(e);
        }

        public bool HitThisCollider(Collider collider)
        {
            if (RaycastHit.collider == null)
                return collider == null ? true : false;



            return RaycastHit.collider == collider;
        }
    }


    public class ActiveProjectile : ILocalTimeScale, ILocalGravity
    {
        public Projectile ProjectileBlueprint; //The SO from which this projectile is based
        public Projectile.ProjectileInstanceData ProjectileBlueprintInstance;
        public ProjectileEmitter Emitter; //The emitter which launched the projectile
        public Atmosphere AtmosphereData;
        public bool Alive; //weather the projectile is alive or not
        public bool DryRun; //weather this projectile is on a "DryRun", or fake-fire. This bool is looked at by health systems and whatnot, so they know not to take damage during a DryRun. Good for trajectory drawing

        public LayerMask TargetLayers; //The layers which will be targeted by the raycast.
        public Vector3 Position; //The position of the projectile
        public float EvaluatedSpeed; //The speed we evaluated from ProjectileBlueprint. This is used to control changes in velocity magnitude through an animation curve
        public Vector3 Velocity; //The velocity of the projectile
        public Vector3 VelocityNormal; //The cached value of Velocity.normalized, becuase it is a costly (square root) action

        //ILocalTimeScale values
        protected float _localTimeScale;
        public float LocalTimescaleValue { get => _localTimeScale; set => _localTimeScale = value; }

        //ILocalGravity values

        protected Vector3 _localGravity;
        public Vector3 LocalGravityValue { get => _localGravity; set => _localGravity = value; }


        public Quaternion FirstSampleQuaternionDir; //used to rotate any paths by the direction we are facing

        public List<Vector3> LastFramePositions; //The list of position that the projectile inhabited last frame. Used to draw the most recent frame debug lines
        public List<Vector3> LifetimePositions; //The list of position that the projectile inhabited over all time. May be used for trajectory prediction on a dry run
        public float LifetimeDistance; //The distance the projectile has traveled.
        public float CurrentTimeAlive; //The lifetime that the projectile has lived
        public float MaxTimeAlive; //The max time that the projectile can live. This may be increased by stage changes

        public float LifetimeStageOffset;//the offset required to calculate current time alive in the current stage
        public float DistanceStageOffset;//the offset required to calculate current distance traveled in the current stage

        public ProjectileSeekData SeekData; //Seekdata used for projectile seeking

        public float CurrentDistancePenetrated; //The current distance that the projectile has penetrated
        public float CurrenNumofPenetrations;
        public float MaxPenetrationDistance;
        public float MaxNumOfPenetrations;
        public RaycastHit LastRaycastHit; //The most recent rayhit. This is used during recursion to see if we penetrated the object
        public List<RaycastHit> ContainedWithinRaycastHits; //A list of all the trigger colliders that we have entered, but not yet exited. Useful for projectile physics in water, or other substances or atmospheres
        public List<MaterialToughness> ContainedWithinMaterialToughness;

        public int RicochetsRemaining;
        public int RicochetsRemainingTillNextRicochetSeek;

        public Particle[] MainParticle; //Particle data.
        public int MainParticleIndex; //Index location of our particle in the particle system
        public bool RotateParticleIn3DSpace; //True if the particle is a mesh, false if the particle is a sprite
        public GameObject TrailingGameobject; //This is a reference to a trailing gameobject. Useful for represeting the projectile with a gameobject.
        public ParticleSystem TrailingParticleSystem; //This is a reference to a trailing particle system. Useful for smoke, or other advanced particles.
        public Animator TrailingAnimator; //This is a reference to a trailing animator. Useful for large and very complicated projectiles, such as multi-compartments spinning drills or whatnot
        public TrailRenderer trailingTrailRenderer;//This is a reference to a trailRenderer. Useful for many projectiles, and needs to cleared on death, so it can be repositioned when pooled.

        public List<Projectile.ProjectileStage> Stages; //A list of the stages this projectile may change to

        public bool RunDebugCode;

        protected int RecommendedSamples;

        #region Constructors
        public ActiveProjectile(Projectile _projectileScriptableObject, ProjectileEmitter _emitter, Vector3 _position, Vector3 _direction, ProjectileSeekData _seekData = null, bool _dryRun = false, bool _noGraphics = false)
        {
            ProjectileBlueprint = _projectileScriptableObject;
            ProjectileBlueprintInstance = new Projectile.ProjectileInstanceData(_projectileScriptableObject);
            Emitter = _emitter;
            AtmosphereData = Emitter.AtmosphereData;
            Alive = true;
            DryRun = _dryRun;

            TargetLayers = _emitter.TargetLayers;
            Position = _position;
            EvaluatedSpeed = ProjectileBlueprint.speed.Evaluate(0);
            Velocity = _direction * EvaluatedSpeed;
            VelocityNormal = _direction;

            LocalTimescaleValue = _emitter.LocalTimescaleValue;
            LocalGravityValue = _emitter.LocalGravityValue;

            FirstSampleQuaternionDir = Quaternion.identity;

            LastFramePositions = new List<Vector3>();
            LifetimePositions = new List<Vector3>();
            LifetimePositions.Add(_position);
            LifetimeDistance = 0;
            CurrentTimeAlive = 0;
            MaxTimeAlive = ProjectileBlueprint.lifetime;

            LifetimeStageOffset = 0;
            DistanceStageOffset = 0;

            SeekData = _seekData;

            CurrentDistancePenetrated = 0;
            CurrenNumofPenetrations = 0;
            MaxPenetrationDistance = ProjectileBlueprint.penetration;
            MaxNumOfPenetrations = ProjectileBlueprint.maxNumPenetrationObjects;
            if (MaxPenetrationDistance == 0 && MaxNumOfPenetrations != 0)
                MaxPenetrationDistance = int.MaxValue;
            if (MaxNumOfPenetrations == 0 && MaxPenetrationDistance != 0)
                MaxNumOfPenetrations = int.MaxValue;
            ContainedWithinRaycastHits = new List<RaycastHit>();
            ContainedWithinMaterialToughness = new List<MaterialToughness>();

            if (Emitter.ContainedByCollider != null)
            {
                ContainedWithinMaterialToughness.Add(Emitter.ContainedByCollider.GetComponentInChildren<MaterialToughness>());
                RaycastHit hit;
                Emitter.ContainedByCollider.Raycast(new Ray(Position + (VelocityNormal * 100000), -VelocityNormal), out hit, 100000);
                ContainedWithinRaycastHits.Add(hit);

            }

            RicochetsRemaining = ProjectileBlueprint.ricochets;
            RicochetsRemainingTillNextRicochetSeek = ProjectileBlueprint.firstBounceSeeks ? 0 : ProjectileBlueprint.ricochetSeekInterval;

            MainParticle = new Particle[1];
            MainParticleIndex = -1;
            RotateParticleIn3DSpace = ProjectileBlueprint.rotateParticleIn3DSpace;
            if (!_noGraphics)
            {
                //Instanciate projectile trail (using object pooling)
                if (ProjectileBlueprint.projectileTrailParticleSystemPrefab != null)
                {
                    MBSSimpleAddressablePooler pooler = MBSSimpleAddressablePooler.GetInstanceOrInstanciate(ProjectileBlueprint.projectileTrailParticleSystemPrefab,Emitter.FXScene);
                    if (pooler != null)
                    {
                        pooler.GetPooledGameObject((gameObject) => 
                        {
                            SetUpTrailingGameObject(gameObject, _position);
                        });

                        //if (!trail.IsEmpty())
                        //{
                        //    if (trail.Object != null)
                        //    {
                        //        SetUpTrailingGameObject(trail.Object, _position);
                        //    }
                        //    else if (trail.IsLoadingHandle())
                        //    {

                        //        trail.Handle.Completed += (asyncOperationHandle) =>
                        //        {
                        //            if (this == null)
                        //                return;
                        //            GameObjectOrHandle<GameObject> trailObj = pooler.GetPooledGameObject();
                        //            if (!trailObj.IsEmpty())
                        //            {
                        //                if (trailObj.Object != null)
                        //                {
                        //                    SetUpTrailingGameObject(trailObj.Object, Position);
                        //                }
                        //                else
                        //                {
                        //                    trailObj.Handle.Completed += (asyncOpHandle) =>
                        //                    {
                        //                        SetUpTrailingGameObject(asyncOpHandle.Result as GameObject, Position);
                        //                    };
                        //                }
                        //            }

                        //        };
                        //    }
                        //    else
                        //    {
                        //        trail.Handle.Completed += (asyncOperationHandle) =>
                        //        {
                        //            SetUpTrailingGameObject(asyncOperationHandle.Result as GameObject, Position);
                        //        };
                        //    }
                        //}
                    }


                }
            }

            Stages = new List<Projectile.ProjectileStage>();//create shallow clones of each stage, so we don't reference the Scriptable Object
            for (int i = 0; i < ProjectileBlueprint.projectileStages.Count; i++)
            {
                Projectile.ProjectileStage stage = new Projectile.ProjectileStage(ProjectileBlueprint.projectileStages[i]);
                stage.Triggered = false;
                Stages.Add(stage);
            }

            RecommendedSamples = ProjectileBlueprint.recommendedSamples;
        }

        public ActiveProjectile(ActiveProjectile proj)
        {
            ProjectileBlueprint = proj.ProjectileBlueprint;
            ProjectileBlueprintInstance=proj.ProjectileBlueprintInstance;
            Emitter = proj.Emitter;
            AtmosphereData = proj.AtmosphereData;
            Alive = proj.Alive;
            DryRun = proj.DryRun;

            TargetLayers = proj.TargetLayers;
            Position = proj.Position;
            EvaluatedSpeed = proj.EvaluatedSpeed;
            Velocity = proj.Velocity;
            VelocityNormal = proj.VelocityNormal;
            LocalTimescaleValue = proj.LocalTimescaleValue;

            FirstSampleQuaternionDir = proj.FirstSampleQuaternionDir;

            LastFramePositions = proj.LastFramePositions.Clone();
            LifetimePositions = proj.LifetimePositions.Clone();
            LifetimeDistance = proj.LifetimeDistance;
            CurrentTimeAlive = proj.CurrentTimeAlive;
            MaxTimeAlive = proj.MaxTimeAlive;

            LifetimeStageOffset = proj.LifetimeStageOffset;
            DistanceStageOffset = proj.DistanceStageOffset;

            SeekData = proj.SeekData;

            CurrentDistancePenetrated = proj.CurrentDistancePenetrated;
            CurrenNumofPenetrations = proj.CurrenNumofPenetrations;
            MaxPenetrationDistance = proj.MaxPenetrationDistance;
            MaxNumOfPenetrations = proj.MaxNumOfPenetrations;
            LastRaycastHit = proj.LastRaycastHit;
            ContainedWithinRaycastHits = proj.ContainedWithinRaycastHits.Clone();
            ContainedWithinMaterialToughness = proj.ContainedWithinMaterialToughness.Clone();

            MainParticle = proj.MainParticle;
            MainParticleIndex = proj.MainParticleIndex;
            RotateParticleIn3DSpace = proj.RotateParticleIn3DSpace;
            TrailingGameobject = proj.TrailingGameobject;
            TrailingParticleSystem = proj.TrailingParticleSystem;
            TrailingAnimator = proj.TrailingAnimator;
            trailingTrailRenderer = proj.trailingTrailRenderer;

            Stages = proj.Stages;

            RecommendedSamples = proj.RecommendedSamples;

        }
        #endregion


        #region Methods
        /// <summary>
        /// Moves the projectile. 
        /// </summary>
        public List<ActiveProjectile> Move(bool outputRecursionList = false, MoveRecursionData recursionData = null)
        {

            //Check if we need to end recursion
            if (VelocityNormal == Vector3.zero)
                Alive = false;

            if (!Alive)
            {
                if (outputRecursionList && recursionData != null)
                    return recursionData.RecursionList;
                return null;
            }


            List<ActiveProjectile> recursionListForDebug = outputRecursionList ? new List<ActiveProjectile>() : null;

            if (recursionData != null)
            {
                recursionListForDebug = recursionData.RecursionList;
            }

            //cache our local deltatime so we don't have to reference LocalTimeScale to retrieve it
            float initalDeltaTime = LocalTimeScale.LocalFixedDeltaTime(LocalTimescaleValue);
            float deltaTime = initalDeltaTime;
            float percentTimeAlive = Mathf.Clamp(1 - ((((deltaTime / RecommendedSamples) + CurrentTimeAlive) - MaxTimeAlive) / (deltaTime / RecommendedSamples)), .01f, 1);//Gives us the percent of deltaTime that we are alive during            
            float initalPercentTimeAlive = percentTimeAlive;
            deltaTime *= percentTimeAlive;
            if (recursionData != null)//Handles assigning to deltaTime when we are penetrating something on our last sample before our lifetime expires
            {
                initalPercentTimeAlive = recursionData.PercentTravelRemaining;
                percentTimeAlive = recursionData.PercentTravelRemaining;
                deltaTime = initalDeltaTime * percentTimeAlive;
            }

            //Get any speed change determined by the Projectile SO 
            float speedEval = ProjectileBlueprintInstance.Speed.Evaluate(ProjectileBlueprintInstance.EvaluateSpeedWithDistance ? LifetimeDistance-DistanceStageOffset : CurrentTimeAlive-LifetimeStageOffset);
            float magnitude = (Velocity.magnitude + (speedEval - EvaluatedSpeed));
            float initalMagnitude = magnitude;
            magnitude *= percentTimeAlive;
            EvaluatedSpeed = speedEval;
            Vector3 workingVelocityNormal = VelocityNormal;
            Vector3 workingVelocity = workingVelocityNormal * magnitude;
            Vector3 velocityPathsDiff = Vector3.zero;

            //Apply atmospheric or in-substance slowdown 
            float slowdown = magnitude;
            MaterialToughness material = ContainedWithinMaterialToughness.Count > 0 ? ContainedWithinMaterialToughness[ContainedWithinMaterialToughness.Count - 1] : null;
            ApplySlowdown(deltaTime, material, ref workingVelocity, initalMagnitude * Mathf.Lerp(.7f, 1, percentTimeAlive));
            slowdown -= workingVelocity.magnitude;
            magnitude -= slowdown;

            //If we will not be using our paths for gravity, apply paths
            if (ProjectileBlueprintInstance.GravityOption == Projectile.ProjectileGravityMode.NoGravity || ProjectileBlueprintInstance.GravityOption == Projectile.ProjectileGravityMode.Gravity)
                velocityPathsDiff = AddPaths(magnitude, ref workingVelocity, ref workingVelocityNormal, recursionData);

            //Add gravity
            Vector3 gravDiff = AddGravity(deltaTime, ref workingVelocity, ref workingVelocityNormal, recursionData);

            //Add seeking
            //if necessary, calculate seek path
            Vector3 seekDiff = Vector3.zero;
            if (SeekData != null)
            {
                if (SeekData.CurrentSeekMode != ProjectileSeekData.Seekmode.NoSeek)
                    seekDiff = AddSeeking(deltaTime, velocityPathsDiff, ref workingVelocity, ref workingVelocityNormal, recursionData);
            }

            //Get our working velocity for this frame (because Velocity is distance in seconds, not distance in frame)
            Vector3 FrameVelocity = (workingVelocity * initalDeltaTime) / RecommendedSamples;
            //Get our expected end position
            Vector3 expectedEndPosition = Position + FrameVelocity;
            float frameMag = FrameVelocity.magnitude;

            RaycastHit hit;
            RaycastHit NewContainedInHit = new RaycastHit();
            MaterialToughness NewContainedInMat = null;
            bool recurse = false;

            List<int> containedInToRemoveIndex = new List<int>();
            float distancePenetrated = 0;
            float percentDistanceTraveled = 1;
            float initalPercentTImeAlive = percentTimeAlive;
            Vector3 initalExpectedEndPosition = expectedEndPosition;
            bool checkPenetrationVals = false;

            //check if we will be exiting any objects which we are contained in
            if (ContainedWithinRaycastHits.Count > 0)
            {
                float distanceToCheck = FrameVelocity.magnitude;
                //First, check if the thing we are currently in is a non-trigger gameobject. If so, check how far we can penetrate it before exceeding our maxPenetration
                if (ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider != null)
                {
                    if (!ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider.isTrigger &&
                            ProjectileBlueprintInstance.PenetrateableLayers.ContainsLayer(ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider.gameObject.layer))
                    {

                        //if the drag threshold is low enough, then don't incur penetration cost
                        if (ContainedWithinMaterialToughness[ContainedWithinMaterialToughness.Count - 1] != null)
                        {
                            if (ContainedWithinMaterialToughness[ContainedWithinMaterialToughness.Count - 1].Drag > ProjectileBlueprintInstance.DragPenetrationThreshold)
                            {
                                distancePenetrated = distanceToCheck;
                                if (CurrentDistancePenetrated + distanceToCheck > MaxPenetrationDistance)//if our expected End Position exceeds the distance we can penetrate...
                                {//...then adjust the distance we are testing against

                                    distanceToCheck = MaxPenetrationDistance - CurrentDistancePenetrated;
                                    distancePenetrated = distanceToCheck;
                                    expectedEndPosition = Position + (workingVelocityNormal * distanceToCheck);
                                }
                            }

                        }

                    }
                }

                //check if we can exit the thing we are currently contained in
                if (ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider != null)
                {
                    if (ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider.Raycast(new Ray(expectedEndPosition, -workingVelocity), out hit, distanceToCheck))
                    {

                        distanceToCheck = Vector3.Distance(Position, hit.point);
                        percentDistanceTraveled = distanceToCheck / Vector3.Distance(Position, initalExpectedEndPosition);
                        expectedEndPosition = hit.point + (workingVelocityNormal * .0001f);
                        percentTimeAlive = initalPercentTImeAlive * percentDistanceTraveled;
                        deltaTime = initalDeltaTime * percentTimeAlive;
                        containedInToRemoveIndex.Add(ContainedWithinRaycastHits.Count - 1);
                        recurse = true;

                        workingVelocity += velocityPathsDiff + seekDiff - gravDiff;
                        velocityPathsDiff = Vector3.zero;
                        workingVelocityNormal = workingVelocity.normalized;

                        workingVelocity = workingVelocityNormal * (workingVelocity.magnitude + slowdown);
                        slowdown = workingVelocity.magnitude;
                        ApplySlowdown(deltaTime, material, ref workingVelocity, initalMagnitude * Mathf.Lerp(.7f, 1, percentTimeAlive));
                        magnitude = workingVelocity.magnitude;
                        slowdown -= magnitude;
                        

                        //then add back gravity, paths and seeking
                        //If we will not be using our paths for gravity, apply paths
                        //if (ProjectileBlueprintInstance.gravityOption == Projectile.ProjectileGravityMode.NoGravity || ProjectileBlueprintInstance.gravityOption == Projectile.ProjectileGravityMode.Gravity)
                            //velocityPathsDiff = AddPaths(magnitude, ref workingVelocity, ref workingVelocityNormal, recursionData);
                        //Add gravity
                        gravDiff = AddGravity(deltaTime, ref workingVelocity, ref workingVelocityNormal, recursionData);
                        //Add seeking
                        //if necessary, calculate seek path
                        if (SeekData != null)
                        {
                            if (SeekData.CurrentSeekMode != ProjectileSeekData.Seekmode.NoSeek)
                                seekDiff = AddSeeking(deltaTime, velocityPathsDiff, ref workingVelocity, ref workingVelocityNormal, recursionData);
                        }
                        //If we are inside a proper collider, reduce our penetration distance
                        if (!ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider.isTrigger &&
                            ProjectileBlueprintInstance.PenetrateableLayers.ContainsLayer(ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider.gameObject.layer))
                        {
                            distancePenetrated = distanceToCheck;
                            //if the drag threshold is low enough, then don't incur penetration cost
                            if (ContainedWithinMaterialToughness[ContainedWithinMaterialToughness.Count - 1] != null)
                            {
                                if (ContainedWithinMaterialToughness[ContainedWithinMaterialToughness.Count - 1].Drag <= ProjectileBlueprintInstance.DragPenetrationThreshold)
                                {
                                    distancePenetrated = 0;
                                }
                            }
                        }
                        ProjectileEvent.Trigger(ProjectileEventTypes.OnPenetrationExit, this, hit, Emitter);

                    }
                }

                FrameVelocity = FrameVelocity.normalized * distanceToCheck;
                frameMag = FrameVelocity.magnitude;
            }

            //If we hit something
            if (Physics.Raycast(Position, workingVelocityNormal, out hit, frameMag, TargetLayers))
            {
                //check that we are not raycasting against something we just hit and entered (this can be a problem if we enter something at the very very end of a sample that will cause endless recursion)
                bool safetyCheck = true;
                for (int i = 0; i < ContainedWithinRaycastHits.Count; i++)
                {
                    if (ContainedWithinRaycastHits[i].collider == hit.collider)
                    {
                        safetyCheck = false;
                        break;
                    }
                }
                if (!safetyCheck)
                {
                    safetyCheck = Physics.Raycast(Position + (workingVelocityNormal * .0001f), workingVelocityNormal, out hit, frameMag - .0001f, TargetLayers);
                }

                bool notContainedInNew = false;
                if (safetyCheck)
                {
                    float distanceToCheck = Vector3.Distance(Position, hit.point);
                    percentDistanceTraveled = distanceToCheck / Vector3.Distance(Position, initalExpectedEndPosition);
                    expectedEndPosition = hit.point;
                    percentTimeAlive = initalPercentTImeAlive * percentDistanceTraveled;
                    deltaTime = initalDeltaTime * percentTimeAlive;
                    containedInToRemoveIndex.Clear();

                    workingVelocity += velocityPathsDiff + seekDiff - gravDiff;
                    velocityPathsDiff = Vector3.zero;
                    workingVelocityNormal = workingVelocity.normalized;

                    workingVelocity = workingVelocityNormal * (workingVelocity.magnitude + slowdown);
                    slowdown = workingVelocity.magnitude;
                    ApplySlowdown(deltaTime, material, ref workingVelocity, initalMagnitude);
                    magnitude = workingVelocity.magnitude;
                    slowdown -= magnitude;                   
                    //then add back gravity, paths and seeking
                    //If we will not be using our paths for gravity, apply paths
                    //if (ProjectileBlueprintInstance.gravityOption == Projectile.ProjectileGravityMode.NoGravity || ProjectileBlueprintInstance.gravityOption == Projectile.ProjectileGravityMode.Gravity)
                        //velocityPathsDiff = AddPaths(magnitude, ref workingVelocity, ref workingVelocityNormal, recursionData);
                    //Add gravity
                    gravDiff = AddGravity(deltaTime, ref workingVelocity, ref workingVelocityNormal, recursionData);
                    //Add seeking
                    //if necessary, calculate seek path
                    if (SeekData != null)
                    {
                        if (SeekData.CurrentSeekMode != ProjectileSeekData.Seekmode.NoSeek)
                            seekDiff = AddSeeking(deltaTime, velocityPathsDiff, ref workingVelocity, ref workingVelocityNormal, recursionData);
                    }
                    //If we hit a non-trigger collider
                    if (!hit.collider.isTrigger)
                    {
                        LastRaycastHit = hit;
                        ProjectileEvent.Trigger(ProjectileEventTypes.OnHit, this, hit, Emitter);

                        if (ProjectileBlueprintInstance.RicochetableLayers.ContainsLayer(hit.collider.gameObject.layer))
                        {
                            //check if we need to ricochet. If so, do not add the hit as a new ContainedIn
                            notContainedInNew = Ricochet(frameMag, ref FrameVelocity, ref workingVelocity, ref workingVelocityNormal);
                            magnitude = workingVelocity.magnitude;
                        }
                        else if (ProjectileBlueprintInstance.PenetrateableLayers.ContainsLayer(hit.collider.gameObject.layer))
                        {
                            //Handle penetration
                            CurrenNumofPenetrations++;
                            checkPenetrationVals = true;

                            MaterialToughness matT = hit.collider.gameObject.GetComponent<MaterialToughness>();
                            if (matT != null)
                            {
                                if (matT.Drag <= ProjectileBlueprintInstance.DragPenetrationThreshold)
                                {
                                    CurrenNumofPenetrations--;
                                    checkPenetrationVals = false;
                                }
                            }
                            ProjectileEvent.Trigger(ProjectileEventTypes.OnPenetration, this, hit, Emitter);

                        }
                        else// else we have just hit something that we can neither penetrate nor ricochet off of. In this case, the projectile is dead
                        {
                            Alive = false;
                            notContainedInNew = true;
                        }

                    }
                    else
                    {
                        ProjectileEvent.Trigger(ProjectileEventTypes.OnHitTrigger, this, hit, Emitter);
                    }

                    //adjust the penetration cost through the material we are currently contained in, if any
                    if (ContainedWithinRaycastHits.Count > 0)
                    {
                        if (ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider != null)
                        {
                            if (!ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider.isTrigger &&
                                ProjectileBlueprintInstance.PenetrateableLayers.ContainsLayer(ContainedWithinRaycastHits[ContainedWithinRaycastHits.Count - 1].collider.gameObject.layer))
                            {
                                distancePenetrated = distanceToCheck;
                                //if the drag threshold is low enough, then don't incur penetration cost
                                if (ContainedWithinMaterialToughness[ContainedWithinMaterialToughness.Count - 1] != null)
                                {
                                    if (ContainedWithinMaterialToughness[ContainedWithinMaterialToughness.Count - 1].Drag <= ProjectileBlueprintInstance.DragPenetrationThreshold)
                                    {
                                        distancePenetrated = 0;
                                    }
                                }
                            }
                        }
                    }

                    if (!notContainedInNew)
                    {
                        //Add the trigger to our list of "contained within" objects.
                        NewContainedInHit = hit;
                        NewContainedInMat = hit.collider.gameObject.GetComponentInParent<MaterialToughness>();
                        float slow = magnitude;
                        ApplySlowdown(0, NewContainedInMat, ref workingVelocity, initalMagnitude - slowdown, true);
                        slow -= workingVelocity.magnitude;
                        slowdown += slow;
                    }
                    FrameVelocity = FrameVelocity.normalized * distanceToCheck;
                    //slowdown will be applied as per normal slowdown. We will recurse afterwards to complete our travel
                    if (Alive)
                        recurse = true;

                }
            }


            //check if we exit any other containedIn objects
            for (int i = 0; i < ContainedWithinRaycastHits.Count - 1; i++)
            {
                if (ContainedWithinRaycastHits[i].collider == null)
                    continue;

                if (ContainedWithinRaycastHits[i].collider.Raycast(new Ray(expectedEndPosition, -workingVelocity), out hit, frameMag))
                {
                    recurse = true;
                    containedInToRemoveIndex.Add(i);
                }
                else if (!ContainedWithinRaycastHits[i].collider.bounds.Contains(expectedEndPosition))//This allows us to exit an object if it moves, even if we are going very slow
                {
                    containedInToRemoveIndex.Add(i);
                }
            }



            //take stock of the distance traveled, position, and lifetime
            float distance = Vector3.Distance(Position, expectedEndPosition);

            LifetimeDistance += distance;
            CurrentDistancePenetrated += distancePenetrated;
            Position = expectedEndPosition;
            LastFramePositions.Add(Position);

            if (distancePenetrated > 0 || checkPenetrationVals)
            {
                if (CurrentDistancePenetrated + .0001f > MaxPenetrationDistance || CurrenNumofPenetrations > MaxNumOfPenetrations)
                    Alive = false;
            }

            //If our paths are not relative, then we subtract the velocity we added from paths, giving us a net 0 change from paths after all calculations
            if (!ProjectileBlueprintInstance.PathsAreRelative)
            {
                workingVelocity += velocityPathsDiff;
                workingVelocityNormal = workingVelocity.normalized;
            }

            if (recursionData != null)
            {
                magnitude = workingVelocity.magnitude + slowdown;
                workingVelocity = workingVelocityNormal * (magnitude);
                workingVelocity = workingVelocityNormal * (magnitude / recursionData.PercentTravelRemaining);
                magnitude /= recursionData.PercentTravelRemaining;
                workingVelocity = workingVelocityNormal * (magnitude - slowdown);
            }

            Velocity = workingVelocity;
            VelocityNormal = workingVelocityNormal;
            CurrentTimeAlive += ((deltaTime + .0001f) / RecommendedSamples);

            //check if we are still alive
            if (Alive)
                Alive = DeterminIfAlive(recursionData);

            //check if any stage conditions have been met
            HandleStages();

            if (outputRecursionList)
                recursionListForDebug.Add(new ActiveProjectile(this));

            //If we have exited any objects, remove them from our containedwithin lists
            if (containedInToRemoveIndex.Count > 0)
            {
                for (int i = 0; i < containedInToRemoveIndex.Count; i++)
                {
                    ContainedWithinRaycastHits.RemoveAt(containedInToRemoveIndex[i]);
                    ContainedWithinMaterialToughness.RemoveAt(containedInToRemoveIndex[i]);

                    //shift downwards all our other exiting indexs above the one we just removed 
                    for (int j = i; j < containedInToRemoveIndex.Count; j++)
                    {
                        if (containedInToRemoveIndex[j] > containedInToRemoveIndex[i])
                            containedInToRemoveIndex[j]--;
                    }
                }
            }

            if (NewContainedInHit.collider != null)
            {
                ContainedWithinRaycastHits.Add(NewContainedInHit);
                ContainedWithinMaterialToughness.Add(NewContainedInMat);
            }

            if (percentDistanceTraveled >= 1)
                recurse = false;

            //recurse if we need to
            if (recurse && Alive)
            {
                //Vector3 remainingVelocity = Velocity * (1 - percentDistanceTraveled);
                if (recursionData == null)
                {

                    recursionData = new MoveRecursionData(1 - percentDistanceTraveled, recursionListForDebug, LastFramePositions[0]);
                }
                else
                {
                    recursionData = new MoveRecursionData(1 - percentDistanceTraveled, recursionListForDebug, recursionData.FrameStartingPosition);
                }

                Move(outputRecursionList, recursionData);
            }

            if (!Alive)
                ProjectileEvent.Trigger(ProjectileEventTypes.OnDie, this, hit, Emitter);

            return recursionListForDebug;
        }

        /// <summary>
        /// Updates the projectile particle to reflect the state of the projectile
        /// </summary>
        public void MirrorParticle()
        {//NOTE: Death is handled in Emitter, so it can have that "last one frame after death" functionality using a coroutine (coroutines don't work on non monobehavior scripts)

            Quaternion rotation = Quaternion.identity;
            float rotationAngle = 0f;

            //Handle rotation mode
            switch (ProjectileBlueprintInstance.ParticleRotationMode)
            {
                case Projectile.ProjectileParticleRotationMode.InitalRotationOnly:
                    rotation = Quaternion.Euler(MainParticle[0].rotation3D);
                    rotationAngle = MainParticle[0].rotation;
                    break;

                case Projectile.ProjectileParticleRotationMode.VelocityRotation:
                    rotation = Quaternion.LookRotation(VelocityNormal, Vector3.up);
                    rotationAngle = Vector3.Angle(Vector3.forward, VelocityNormal);
                    break;

                case Projectile.ProjectileParticleRotationMode.PositionDeltaRotation:
                    Vector3 dir = Vector3Extensions.Direction(LifetimePositions[LifetimePositions.Count - 2], Position);
                    rotation = Quaternion.LookRotation(dir, Vector3.up);
                    rotationAngle = Vector3.Angle(Vector3.forward, dir);
                    break;
            }

            //Handle main particle
            if (MainParticleIndex >= 0)
            {
                //Set position
                MainParticle[0].position = Position;
                //Set velocity. May need to set it to something like Velocity*.2f to prevent stuttering? may not be an issue between fixedupdates?
                MainParticle[0].velocity = Vector3.zero;
                //Set rotation angle
                if (ProjectileBlueprintInstance.ParticleRotationMode != Projectile.ProjectileParticleRotationMode.InitalRotationOnly)
                {
                    Vector3 eulerRotation = rotation.eulerAngles;
                    eulerRotation += ProjectileBlueprintInstance.ParticleSystemRotationOffset;
                    if (RotateParticleIn3DSpace)
                        MainParticle[0].rotation3D = eulerRotation;
                    else
                        MainParticle[0].rotation = rotationAngle;
                }
            }


            //handle particle trail object if any
            if (TrailingGameobject != null)
            {
                //position
                TrailingGameobject.transform.position = Position;
                //rotation
                if (ProjectileBlueprintInstance.ParticleRotationMode != Projectile.ProjectileParticleRotationMode.InitalRotationOnly)
                    TrailingGameobject.transform.rotation = rotation;

                //if (ProjectileBlueprintInstance.name == "RocketStage3")
                //  Debug.Log("test");
            }

            //handle particle trail system if any
            if (TrailingParticleSystem != null)
            {
                //timescale
                MainModule main = TrailingParticleSystem.main;
                main.simulationSpeed = LocalTimescaleValue;

            }
            //handle particle trail animator if any
            if (TrailingAnimator != null)
            {
                TrailingAnimator.speed = LocalTimescaleValue;
            }
        }

        public void HandleStages()
        {
            for (int i = 0; i < Stages.Count; i++)
            {
                Stages[i].HandleStage(this);

            }
        }

        /// <summary>
        /// Used by Move() to handle checking if the projectile should expire
        /// </summary>
        /// <returns></returns>
        private bool DeterminIfAlive(MoveRecursionData recursionData)
        {
            //check if our lifetime expired
            if (CurrentTimeAlive >= MaxTimeAlive)
                return false;

            //check if we are out of bounds
            Vector3 upper = ProjectileBlueprintInstance.PhysicsLimitUpper;
            Vector3 lower = ProjectileBlueprintInstance.PhysicsLimitLower;
            if (Position.x > upper.x || Position.x < lower.x
            || Position.y > upper.y || Position.y < lower.y
            || Position.z > upper.z || Position.z < lower.z)
                return false;

            //check if our speed is too slow
            float mult = Mathf.Clamp01(recursionData == null ? 1 : recursionData.PercentTravelRemaining);
            if (Velocity.magnitude <= ProjectileBlueprintInstance.MinimumSpeed * mult)
                return false;

            //else we are not dead yet, and return alive=true
            return true;
        }

        /// <summary>
        /// Add pathing pull to the velocity. It returns the change in velocity and takes in the magnitude just so it doesn't have to do the calculation itself
        /// </summary>
        /// <param name="magnitude"></param>
        /// <returns></returns>
        private Vector3 AddPaths(float magnitude, ref Vector3 workingVelocity, ref Vector3 workingVelocityNormal, MoveRecursionData recursionData = null)
        {
            //Get pathing data
            Vector3 paths = ProjectileBlueprintInstance.EvaluateXYPath(ProjectileBlueprintInstance.EvaluteCurvesWithDistance ? LifetimeDistance-DistanceStageOffset : CurrentTimeAlive-LifetimeStageOffset);

            if (recursionData != null)
                paths *= recursionData.PercentTravelRemaining;

            paths = FirstSampleQuaternionDir * paths;
            Vector3 velocityDiff = workingVelocity;
            //add paths
            workingVelocity += paths;
            //get the new normal
            workingVelocityNormal = workingVelocity.normalized;
            //clamp the magnitude so adding paths doesn't actually increase speed
            workingVelocity = workingVelocityNormal * magnitude;
            //get the difference in our velocity (will be used if we need to restore our velocity to the original)
            return velocityDiff - workingVelocity;
        }

        /// <summary>
        /// Add gravity pull to the velocity
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="magnitude"></param>
        /// <returns></returns>
        private Vector3 AddGravity(float deltaTime, ref Vector3 workingVelocity, ref Vector3 workingVelocityNormal, MoveRecursionData recursionData = null, float recalcPercent = 1)
        {
            Vector3 gravMod = Vector3.zero;

            switch (ProjectileBlueprintInstance.GravityOption)
            {
                case Projectile.ProjectileGravityMode.Gravity:
                    //get gravity
                    gravMod = LocalGravityValue;
                    //apply gravity mass, deltaTime, and our samplecount
                    gravMod *= ProjectileBlueprintInstance.GravityMass * (deltaTime / RecommendedSamples);
                    //do not apply gravity if we have reached our terminal velocity
                    if (Mathf.Sign(workingVelocity.y) == Mathf.Sign(gravMod.y) && Mathf.Abs(workingVelocity.y) >= ProjectileBlueprintInstance.TerminalVelociy)
                        gravMod.y = 0;
                    if (Mathf.Sign(workingVelocity.x) == Mathf.Sign(gravMod.x) && Mathf.Abs(workingVelocity.x) >= ProjectileBlueprintInstance.TerminalVelociy)
                        gravMod.x = 0;
                    break;

                case Projectile.ProjectileGravityMode.UsePathAsGravity:
                    //get gravity
                    gravMod = ProjectileBlueprintInstance.EvaluateXYPath(ProjectileBlueprintInstance.EvaluteCurvesWithDistance ? LifetimeDistance - DistanceStageOffset : CurrentTimeAlive - LifetimeStageOffset);
                    //apply gravity mass, deltaTime, and our samplecount
                    gravMod *= ProjectileBlueprintInstance.GravityMass * (deltaTime / RecommendedSamples);
                    //do not apply gravity if we have reached our terminal velocity
                    if (Mathf.Abs(workingVelocity.y) >= ProjectileBlueprintInstance.TerminalVelociy)
                        gravMod.y = 0;
                    if (Mathf.Abs(workingVelocity.x) >= ProjectileBlueprintInstance.TerminalVelociy)
                        gravMod.x = 0;
                    break;

                case Projectile.ProjectileGravityMode.UsePathAsGravityMultiplier:
                    //get gravity
                    gravMod = Vector3Extensions.Multiply(LocalGravityValue, ProjectileBlueprintInstance.EvaluateXYPath(ProjectileBlueprintInstance.EvaluteCurvesWithDistance ? LifetimeDistance - DistanceStageOffset : CurrentTimeAlive - LifetimeStageOffset));
                    //apply gravity mass, deltaTime, and our samplecount
                    gravMod *= ProjectileBlueprintInstance.GravityMass * (deltaTime / RecommendedSamples);
                    //do not apply gravity if we have reached our terminal velocity
                    if (Mathf.Sign(workingVelocity.y) == Mathf.Sign(LocalGravityValue.y) && Mathf.Abs(workingVelocity.y) >= ProjectileBlueprintInstance.TerminalVelociy)
                        gravMod.y = 0;
                    if (Mathf.Sign(workingVelocity.x) == Mathf.Sign(LocalGravityValue.y) && Mathf.Abs(workingVelocity.x) >= ProjectileBlueprintInstance.TerminalVelociy)
                        gravMod.x = 0;
                    break;
            }

            if (recursionData != null)
                gravMod *= recursionData.PercentTravelRemaining;

            //add gravmod
            workingVelocity += gravMod * recalcPercent;
            workingVelocityNormal = workingVelocity.normalized;
            return gravMod * recalcPercent;
        }

        /// <summary>
        /// Add seeking pull to the velocity
        /// </summary>
        /// <param name="deltaTime"></param>
        private Vector3 AddSeeking(float deltaTime, Vector3 velocityDiff, ref Vector3 workingVelocity, ref Vector3 workingVelocityNormal, MoveRecursionData recursionData = null)
        {
            //If we need to seek a transform, but we don't have target transform stored, then return
            if (SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform && !SeekData.TargetTransform)
                return Vector3.zero;

            //If seeking overrides any pathing, set our paths to 0
            if (ProjectileBlueprintInstance.SeekingOverridesPaths)
            {
                workingVelocity -= velocityDiff;
                workingVelocityNormal = workingVelocity.normalized;
            }
            //Set the seek angle
            Vector3 point = SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform ? SeekData.TargetTransform.position : SeekData.TargetPoint;
            Vector3 targDir = point - Position;
            float seekAmount = ProjectileBlueprintInstance.SeekTurnAmount.Evaluate(ProjectileBlueprintInstance.EvaluteSeekCurveWithDistance ? LifetimeDistance - DistanceStageOffset : CurrentTimeAlive - LifetimeStageOffset);
            float stepSize = (seekAmount / RecommendedSamples / 60) * deltaTime;
            if (recursionData != null)
                stepSize *= recursionData.PercentTravelRemaining;

            Vector3 diff = workingVelocity;
            workingVelocityNormal = Vector3.RotateTowards(workingVelocityNormal, targDir, stepSize, 0f);
            workingVelocity = workingVelocityNormal * workingVelocity.magnitude;
            return diff - workingVelocity;

        }

        /// <summary>
        /// Handles Ricochet
        /// </summary>
        /// <returns></returns>
        private bool Ricochet(float frameVelocityMagnitude, ref Vector3 frameVelocity, ref Vector3 workingVelocity, ref Vector3 workingVelocityNormal)
        {
            if (RicochetsRemaining > 0)
            {
                RicochetsRemaining--;
                //change the velocity based on how much speed we are supposed to lose each ricochet
                float magnitude = workingVelocity.magnitude;
                float speedLossPercent = ProjectileBlueprintInstance.RicochetSpeedLossPercent;
                //Adjust speed loss based on angle of hit (if applicable)
                if (ProjectileBlueprintInstance.RicochetSpeedLossBasedOnHitAngle)
                    speedLossPercent *= Vector3.Angle(workingVelocityNormal, LastRaycastHit.normal) / 180;
                //Clamp our speedLossPercent between our min and max
                speedLossPercent = Mathf.Clamp(speedLossPercent, ProjectileBlueprintInstance.RicochetMinimumSpeedLossPercent, ProjectileBlueprintInstance.RicochetMaximumSpeedLossPercent);

                //Reflect our velocity
                workingVelocityNormal = Vector3.Reflect(workingVelocityNormal, LastRaycastHit.normal);
                workingVelocity = workingVelocityNormal * magnitude;

                Vector3 remainingWorkingVelociyThisSample = Vector3.Lerp(frameVelocity, Vector3.zero, Vector3.Distance(Position, LastRaycastHit.point) / frameVelocityMagnitude);
                //Fire the OnProjectileRicochet method on the emitter, so it can do whatever it needs to on a ricochet
                ProjectileEvent.Trigger(ProjectileEventTypes.OnRicochet, this, LastRaycastHit, Emitter);

                //Check if we need to seek
                bool seekBounce = RicochetsRemainingTillNextRicochetSeek == 0 ? true : false;
                RicochetsRemainingTillNextRicochetSeek = seekBounce ? ProjectileBlueprintInstance.RicochetSeekInterval : RicochetsRemainingTillNextRicochetSeek - 1;

                //Adjust our magnitude by the speedLossPercent
                magnitude *= 1 - speedLossPercent;
                workingVelocity = workingVelocityNormal * magnitude;
                remainingWorkingVelociyThisSample = workingVelocityNormal * (remainingWorkingVelociyThisSample.magnitude * 1 - speedLossPercent);

                //if necessary, calculate seek angle for ricochet
                if (SeekData.CurrentSeekMode != ProjectileSeekData.Seekmode.NoSeek && seekBounce)
                {
                    //Set the seek angle
                    Vector3 point = SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform ? SeekData.TargetTransform.position : SeekData.TargetPoint;
                    Vector3 targDir = point - Position;
                    //Recalculate our velocity based on the new direction
                    float stepSize = ProjectileBlueprintInstance.RicochetSeekTurnAmount * Mathf.Deg2Rad;
                    Vector3 velocityNormalBeforeSeek = workingVelocityNormal; //used to reset our normal if we are ricocheting off our target.
                    workingVelocityNormal = Vector3.RotateTowards(workingVelocityNormal, targDir, stepSize, 0f);
                    workingVelocity = workingVelocityNormal * workingVelocity.magnitude;
                    remainingWorkingVelociyThisSample = workingVelocityNormal * remainingWorkingVelociyThisSample.magnitude;

                    //take a step back and raycast against our collider. If we hit it, then we are trying to shoot through it, which is not allowed and we should just ricochet normally
                    RaycastHit ricHit;
                    LastRaycastHit.collider.Raycast(new Ray(LastRaycastHit.point - (workingVelocityNormal * .1f), workingVelocity), out ricHit, 1);

                    if (ricHit.collider)
                    {
                        //check if we are ricoceting off our target. If so, then ricocet normally
                        if (ricHit.collider.gameObject.transform == SeekData.TargetTransform && SeekData.CurrentSeekMode == ProjectileSeekData.Seekmode.SeekTransform)
                        {
                            workingVelocityNormal = velocityNormalBeforeSeek;
                            workingVelocity = workingVelocityNormal * workingVelocity.magnitude;
                            remainingWorkingVelociyThisSample = workingVelocityNormal * remainingWorkingVelociyThisSample.magnitude;
                        }
                        else//otherwise, ricochet as far as we can realistically turn
                        {
                            //project the velocity onto the surface normal as a plane. This sets the velocity parallel to the surface.
                            workingVelocityNormal = Vector3.ProjectOnPlane(workingVelocityNormal, ricHit.normal).normalized;
                            //rotate the velocity by 5 degrees towards the surface normal, making it so it is not perfectly parallel
                            workingVelocityNormal = Vector3.RotateTowards(workingVelocityNormal, ricHit.normal, 5 * Mathf.Deg2Rad, 0f);
                            workingVelocity = workingVelocityNormal * workingVelocity.magnitude;
                            remainingWorkingVelociyThisSample = workingVelocityNormal * remainingWorkingVelociyThisSample.magnitude;
                        }

                    }


                }
                else//if necessary, calculate random ricochet amount
                {
                    float percent = Random.Range(0, 1f);
                    float x = Mathf.Lerp(0, ProjectileBlueprintInstance.RicochetAngleVariability, percent) * Mathf.Sign(Random.Range(-1, 1));
                    percent = Random.Range(0, 1 - percent);
                    float y = Mathf.Lerp(0, ProjectileBlueprintInstance.RicochetAngleVariability, percent) * Mathf.Sign(Random.Range(-1, 1));

                    Quaternion randomRot = Quaternion.Euler(x, y, 0);
                    workingVelocity = randomRot * workingVelocity;
                    workingVelocityNormal = workingVelocity.normalized;
                    remainingWorkingVelociyThisSample = workingVelocityNormal * remainingWorkingVelociyThisSample.magnitude;
                }

                return true;
            }
            else
            {
                Alive = false;
                frameVelocity = workingVelocityNormal * frameVelocityMagnitude;

                return false;
            }



        }

        /// <summary>
        /// Applies slowdown to velocity based on contained substance density
        /// </summary>
        private void ApplySlowdown(float time, MaterialToughness material, ref Vector3 workingVelocity, float realMagnitude, bool applyFirstContactSlowdown = false)
        {

            float dragValue = ((material == null ? AtmosphereData.Drag : material.Drag) / 100) / ProjectileBlueprintInstance.DragMass;
            float slowdown = 0;

            if (applyFirstContactSlowdown && material != null)
            {
                slowdown = realMagnitude * Mathf.Clamp01((material.PecentSlowdownOnImpact.Evaluate(realMagnitude) / 100) / ProjectileBlueprintInstance.FirstContactDragMass);

            }
            else
            {
                float measure = time / 10f;
                int iterations = 10;

                float speed = realMagnitude;
                for (int i = 0; i < iterations; i++)
                {
                    speed -= (speed * dragValue) * measure;
                }
                slowdown += realMagnitude - speed;

                //Debug.Log("DragValue: " + dragValue + "  Time: " + time +"  DragVal*Time: "+time*dragValue+"  InitalSpeed: "+ realMagnitude + "  Slowdown:  " + slowdown);
            }
            if (workingVelocity.magnitude - slowdown == 0)
                slowdown = workingVelocity.magnitude - .1f;

            workingVelocity = (workingVelocity.magnitude - slowdown) * VelocityNormal;
            return;
        }

        public void SetUpTrailingGameObject(GameObject trailObj, Vector3 pos)
        {
            trailObj.SetActive(false);
            TrailingGameobject = trailObj;
            TrailingParticleSystem = TrailingGameobject.GetComponentInChildren<ParticleSystem>();
            TrailingAnimator = TrailingGameobject.GetComponentInChildren<Animator>();
            trailingTrailRenderer=TrailingGameobject.GetComponentInChildren<TrailRenderer>();

            if (TrailingParticleSystem!=null)
            {
                TrailingParticleSystem.gameObject.transform.position = pos;
                MainModule main = TrailingParticleSystem.main;
                main.simulationSpeed = LocalTimescaleValue;
            }

            if (TrailingAnimator!=null)
            {
                TrailingAnimator.speed = LocalTimescaleValue;
            }

            if (TrailingGameobject!=null)
            {
                TrailingGameobject.transform.position = pos;
                TrailingGameobject.transform.rotation = Quaternion.Euler(TrailingGameobject.transform.rotation.eulerAngles + ProjectileBlueprintInstance.ParticleSystemRotationOffset);
                TrailingGameobject.SetActive(true);
            }
            
        }

        public void KillProjectile(bool AllowOnDeathStages)
        {
            Alive = false;
            if (AllowOnDeathStages)
                HandleStages();
        }
        #endregion



        public class MoveRecursionData
        {
            //public Vector3 RemainingVelocity;
            public float PercentTravelRemaining;
            public List<ActiveProjectile> RecursionList;
            public Vector3 FrameStartingPosition;
            public MoveRecursionData(float percentRemaining, List<ActiveProjectile> _recursionList, Vector3 _frameStartingPosition)
            {
                //RemainingVelocity = _remainingVelocity;
                PercentTravelRemaining = percentRemaining;

                RecursionList = _recursionList;
                FrameStartingPosition = _frameStartingPosition;
            }
        }

    }

    public class ProjectileSeekData
    {
        public Vector3 TargetPoint;
        public Transform TargetTransform;
        public Seekmode CurrentSeekMode;

        public ProjectileSeekData(Vector3 _targetPoint = new Vector3(), Transform _targetTransform = null, Seekmode _seekmode = Seekmode.NoSeek)
        {
            TargetPoint = _targetPoint;
            TargetTransform = _targetTransform;
            CurrentSeekMode = _seekmode;
        }

        [Serializable]
        public enum Seekmode
        {
            NoSeek,
            SeekTransform,
            SeekVectorPoint
        }
    }


}
