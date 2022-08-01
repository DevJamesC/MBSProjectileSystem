using MBS.LocalGravity;
using MBS.LocalTimescale;
using MBS.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static MBS.ProjectileSystem.Projectile;
using static UnityEngine.ParticleSystem;

namespace MBS.ProjectileSystem
{
    public class ProjectileEmitter : MonoBehaviour, ILocalTimeScale, ILocalGravity, IMBSEventListener<ProjectileEvent>
    {
        [Tooltip("The layers which the emitted projectile can make contact with.")]
        public LayerMask TargetLayers;
        [Tooltip("The \"Atmosphere\" Scriptable Object which will determine airial drag when a projectile is not travelling through another material.")]
        public Atmosphere AtmosphereData;
        [Tooltip("The \"Projectile\" Scriptable Object which will determine what projectile is emitted.")]
        public Projectile ProjectileSO;
        [Tooltip("Weather emitted projectiles inherit the velocity of the Emitter. This uses the emitter position between last frame and current frame to get the emitter velocity")]
        public bool ProjectileInheritsEmitterVelocity;
        [Tooltip("The transform from which the projectile will be instanciated. Projectile spawn direction is the Local Z (blue) axis of the origin")]
        public Transform Origin;
        [Tooltip("Wiether projectiles interpolate their positions between FixedUpdates. Profiling resulted in a trivial performace cost... maybe. The cost is so small it might not exist.")]
        public bool interpolateProjectilePosition = true;
        [Tooltip("Enable this to draw a debug line every FixedUpdate from the last position to the new position for each projectile emitted from this emitter.")]
        public bool DrawDebugLine;
        [Tooltip("The Projectile debug line color while it is not penetrating an object.")]
        public Color DebugInAirColor = Color.yellow;
        [Tooltip("The Projectile debug line color while it is penetrating an object.")]
        public Color DebugInObjectColor = Color.red;

        //values set by MaterialToughness script if this object happens to be contained within another object
        [HideInInspector]
        public Collider ContainedByCollider;

        //ILocalTimeScale values
        [SerializeField, HideInInspector, Tooltip("The local timescale of this emitter. Any emitted projectiles will inherit the local timescale at the time of emission.")]
        protected float _localTimeScale = 1;
        /// <summary>
        /// The local timescale of this emitter. Any emitted projectiles will inherit the local timescale at the time of emission.
        /// </summary>
        public float LocalTimescaleValue { get => _localTimeScale; set => _localTimeScale = value; }

        //ILocalGravity values
        [SerializeField, HideInInspector, Tooltip("The local gravity of this emitter. Any emitted projectiles will inherit the local gravity at the time of emission.")]
        protected Vector3 _localGravity = Physics.gravity;
        /// <summary>
        /// The local gravity of this emitter. Any emitted projectiles will inherit the local gravity at the time of emission.
        /// </summary>
        public Vector3 LocalGravityValue { get => _localGravity; set => _localGravity = value; }


        //Protected values
        protected ParticleSystem ProjectileParticleSystem;
        protected ParticleSystem lastParticleSystem;
        protected ParticleSystem lastEmitEffectParticleSystem;
        protected List<ActiveProjectile> inFlightProjectiles;
        protected List<LaunchData> safeLaunchProjectiles;
        protected ProjectileMaterialEffectDictonaryLinkageItem lastEffectLinkItem;
        protected List<ParticleSystem> systemsToDestroy;


        private List<ActiveProjectile> _toRemove;
        private Particle[] _particlesToChange;
        private bool _firstProjectileSpawnFlag;
        private Projectile _lastProjectileSO; //used for OnValidate only (makes sure that if we switch projectiles in the inspector, the new particle system is instanciated properly)     
        private Vector3 velocity;
        private Vector3 lastFramePosition;
        private AsyncOperationHandle isSpawningProjectileGraphicHandle;
        private float framesPerFixedUpdate;
        private float[] lastFourFrames;
        private bool isFixedUpdateFrame;

        // Start is called before the first frame update
        protected virtual void Start()
        {
            if (inFlightProjectiles == null)
                inFlightProjectiles = new List<ActiveProjectile>();
            safeLaunchProjectiles = new List<LaunchData>();
            _toRemove = new List<ActiveProjectile>();
            systemsToDestroy = new List<ParticleSystem>();
            _firstProjectileSpawnFlag = true;
            lastFramePosition = transform.position;
            _particlesToChange = null;
            isFixedUpdateFrame = false;
            //Preload all graphics, spawns, and effects associated with this emitter
            if (ProjectileSO != null)
            {
                ProjectileOnHitParticleEmitter.PreloadHitAndEmitEffects(ProjectileSO.tag, ProjectileSO.effectDictionary);
                if (ProjectileSO.projectileParticleSystemPrefab.RuntimeKeyIsValid())
                    SpawnAddressable.LoadAsset(ProjectileSO.projectileParticleSystemPrefab);
                if (ProjectileSO.projectileTrailParticleSystemPrefab.RuntimeKeyIsValid())
                    SpawnAddressable.LoadAsset(ProjectileSO.projectileTrailParticleSystemPrefab);
                foreach (var action in ProjectileSO.GetAllAssignedActionsOfType<ProjectileBehaviorActionSwitchToDifferentProjectilePreset>())
                {
                    ProjectileBehaviorActionSwitchToDifferentProjectilePreset switchAction = action as ProjectileBehaviorActionSwitchToDifferentProjectilePreset;
                    if (switchAction.ProjectileBaseToChangeTo.projectileTrailParticleSystemPrefab.RuntimeKeyIsValid())
                        SpawnAddressable.LoadAsset(switchAction.ProjectileBaseToChangeTo.projectileTrailParticleSystemPrefab);
                }
                foreach (var action in ProjectileSO.GetAllAssignedActionsOfType<ProjectileBehaviorActionSpawnObject>())
                {
                    ProjectileBehaviorActionSpawnObject switchAction = action as ProjectileBehaviorActionSpawnObject;
                    if (switchAction.AddressableToSpawn.RuntimeKeyIsValid())
                        SpawnAddressable.LoadAsset(switchAction.AddressableToSpawn);
                }
            }
        }

        protected virtual void Awake()
        {
            if (inFlightProjectiles == null)
                inFlightProjectiles = new List<ActiveProjectile>();

            lastFourFrames = new float[4];
        }



        protected virtual void Update()
        {
            InterpolateProjectiles();
            RemoveDeadProjectiles();
            RemoveDeadParticleSystems();
            HandleLaunchSafe();
            isFixedUpdateFrame = false;
        }

        protected virtual void FixedUpdate()
        {
            isFixedUpdateFrame = true;
            UpdateProjectiles();
            UpdateParticles();
            if (ProjectileInheritsEmitterVelocity)
                TrackVelocity();
        }

        private void InterpolateProjectiles()
        {
            if (!interpolateProjectilePosition || ProjectileParticleSystem == null || isFixedUpdateFrame)
                return;

            lastFourFrames[0] = lastFourFrames[1];
            lastFourFrames[1] = lastFourFrames[2];
            lastFourFrames[2] = lastFourFrames[3];
            lastFourFrames[3] = Time.deltaTime;
            framesPerFixedUpdate = Time.fixedUnscaledDeltaTime / ((lastFourFrames[0] + lastFourFrames[1] + lastFourFrames[2] + lastFourFrames[3]) / 4);

            float distance;
            Particle[] particlesToChange = new Particle[inFlightProjectiles.Count];

            for (int i = 0; i < inFlightProjectiles.Count; i++)
            {
                ActiveProjectile proj = inFlightProjectiles[i];
                distance = Vector3.Distance(proj.LifetimePositions.Count > 1 ? proj.LifetimePositions[proj.LifetimePositions.Count - 2] : proj.Position, proj.Position) * .95f;
                Vector3 newPos = proj.VelocityNormal * (distance / framesPerFixedUpdate);
                Particle[] mainParticle = proj.MainParticle;
                mainParticle[0].position += newPos;
                particlesToChange[i] = mainParticle[0];
                // Debug.Log("Distance: "+distance+"  framesPerFixedUpdate: "+framesPerFixedUpdate+"  distance/frames = "+ distance / framesPerFixedUpdate);
                if (proj.TrailingGameobject != null)
                    proj.TrailingGameobject.transform.position = mainParticle[0].position;
            }

            ProjectileParticleSystem.SetParticles(particlesToChange);

        }


        /// <summary>
        /// Removes any dead projectiles from the simulation
        /// </summary>
        private void RemoveDeadProjectiles()
        {
            for (int i = 0; i < _toRemove.Count; i++)
            {
                inFlightProjectiles.Remove(_toRemove[i]);
            }

            _toRemove.Clear();
        }

        private void RemoveDeadParticleSystems()
        {
            List<ParticleSystem> toDestroy = new List<ParticleSystem>();
            for (int i = 0; i < systemsToDestroy.Count; i++)
            {
                if (systemsToDestroy[i].particleCount == 0)
                    toDestroy.Add(systemsToDestroy[i]);
            }

            foreach (ParticleSystem system in toDestroy)
            {
                systemsToDestroy.Remove(system);
                Destroy(system.gameObject);
            }
        }

        /// <summary>
        /// Updates any particles added to _particlesToChange
        /// </summary>
        private void UpdateParticles()
        {
            if (_particlesToChange == null)
                return;

            if (ProjectileParticleSystem != null)
                ProjectileParticleSystem.SetParticles(_particlesToChange);

            _particlesToChange = null;
        }

        /// <summary>
        /// Runs a check to see if we already have a _particlesToChange list (can happen if a slow moving particle is modified by a collider which 'teleported' inside of it between frames).
        /// </summary>
        private void InstanciateParticlesToChangeArray()
        {
            //If we added particles to change from outside sources, just expand the array
            if (_particlesToChange != null)
            {
                Particle[] temp = _particlesToChange;
                _particlesToChange = new Particle[_particlesToChange.Length + 1];
                for (int i = 0; i < temp.Length; i++)
                {
                    _particlesToChange[i] = temp[i];
                }
            }
            else//otherwise, create a new array
            {
                _particlesToChange = new Particle[inFlightProjectiles.Count];
            }
        }

        private void TrackVelocity()
        {
            velocity = (transform.position - lastFramePosition) / LocalTimeScale.LocalFixedUnscaledDeltaTime(_localTimeScale);
            lastFramePosition = transform.position;

        }

        /// <summary>
        /// Iterate through the inFlightProjectiles list and make them move
        /// </summary>
        protected void UpdateProjectiles()
        {
            InstanciateParticlesToChangeArray();


            for (int i = 0; i < inFlightProjectiles.Count; i++)
            {
                ActiveProjectile proj = inFlightProjectiles[i];

                proj.MirrorParticle();
                HandleMoveAndPositionTrackingManagement(proj, DrawDebugLine);
                if (!proj.Alive)
                    StartCoroutine(HandleParticleMirrorOnDeath(proj));

                _particlesToChange[i] = proj.MainParticle[0];

                if (!proj.Alive)
                    _toRemove.Add(proj);


            }
        }

        /// <summary>
        /// Handles cleaning up particles. This works to allow a particle to update to its death position before being destroyed next frame
        /// </summary>
        /// <param name="proj"></param>
        /// <returns></returns>
        protected IEnumerator HandleParticleMirrorOnDeath(ActiveProjectile proj, bool removeParticleImmidiate = false)
        {
            //Because other code can change the projectile references, cache the values we will need.
            GameObject trailingGameobject = proj.TrailingGameobject;
            ParticleSystem trailingParticleSystem = proj.TrailingParticleSystem;

            //handle death and particle (and trail gameobject) cleanup.
            if (trailingParticleSystem == null)
            {
                if (!proj.ProjectileBlueprint.trailObjectPersistsAfterProjectileDies)
                {
                    if (trailingGameobject)
                        trailingGameobject.SetActive(false);
                }
                else
                {
                    //Fire an event here to let the gameobject know that it has "landed" and can begin it's non-projectile functionality
                    //ProjectileEvent.Trigger(ProjectileEventTypes. ??? , proj, new RaycastHit(), this);
                }
            }

            if (!proj.Alive && trailingParticleSystem != null)
            {
                trailingParticleSystem.Stop(true, proj.ProjectileBlueprintInstance.ClearTrailParticlesOnProjectileDeath ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            }
            if (proj.trailingTrailRenderer != null)
            {
                proj.trailingTrailRenderer.Clear();
            }

            //Wait till next frame to actually remove the projectile particle
            if (!removeParticleImmidiate)
                yield return new WaitForEndOfFrame();

            proj.MainParticle[0].remainingLifetime = -1f;

        }

        /// <summary>
        /// handle LastFramePostiions, LifetimePosition and moving a projectile
        /// </summary>
        /// <param name="proj"></param>
        protected List<ActiveProjectile> HandleMoveAndPositionTrackingManagement(ActiveProjectile proj, bool drawDebugLines, bool outputList = false)
        {
            proj.LastFramePositions.Clear();
            proj.FirstSampleQuaternionDir = Quaternion.LookRotation(proj.VelocityNormal, Vector3.up);
            List<ActiveProjectile> debugOutputList = new List<ActiveProjectile>();

            for (int i = 0; i < proj.ProjectileBlueprint.recommendedSamples; i++)
            {
                if (outputList)
                {
                    List<ActiveProjectile> output = proj.Move(true);
                    if (output != null)
                        debugOutputList.AddRange(output);
                }
                else
                    proj.Move();
            }

            if (drawDebugLines)
                DrawProjectileDebugLines(proj);

            proj.LifetimePositions.AddRange(proj.LastFramePositions);

            if (!outputList)
                return null;

            return debugOutputList;

        }



        /// <summary>
        /// Instanciate a new projectile
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="isDryRun"></param>
        public ActiveProjectile Launch(Vector3 position, Vector3 direction, ProjectileSeekData seekdata = null, bool isDryRun = false, Projectile projectileSO = null, bool noGraphics = false, bool emitMuzzleflashAndAudio = true, bool muzzleFlashFXIsChildOfOrigin = true)
        {
            if (!projectileSO)
                projectileSO = ProjectileSO;

            if (projectileSO == null)
            {
                Debug.LogWarning(gameObject.name + " tried to emit a projectile, but no projectile data has been assign!");
                return null;
            }

            ActiveProjectile proj = new ActiveProjectile(projectileSO, this, position, direction, seekdata, isDryRun, noGraphics);
            inFlightProjectiles.Add(proj);

            if (ProjectileInheritsEmitterVelocity)
            {
                proj.Velocity += velocity;
                proj.VelocityNormal = proj.Velocity.normalized;
            }

            //Check to see if this projectile has a particle system and if we have already instanciated this particle system.
            InstanciateProjectileParticleSystem();
            _firstProjectileSpawnFlag = false;

            //If we have a particle system attached to this projectile, we will instanciate a new particle to reperesent the projectile. This will also play any "flash" or "launch" effect
            InstanciateProjectileParticle(proj, emitMuzzleflashAndAudio, muzzleFlashFXIsChildOfOrigin);

            proj.RunDebugCode = DrawDebugLine;
            return proj;
        }

        public void Launch(LaunchData data)
        {
            Launch(data.Position, data.Direction, data.Seekdata, data.IsDryRun, data.ProjectileSO);
        }

        /// <summary>
        /// Used by some stages. This makes it so a projectile is not launched mid-execution of the main loop in ProjectileEmitter. This queues the launch till next frame
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="seekdata"></param>
        /// <param name="isDryRun"></param>
        /// <param name="projectileSO"></param>
        public void LaunchSafe(Vector3 position, Vector3 direction, ProjectileSeekData seekdata = null, bool isDryRun = false, Projectile projectileSO = null)
        {
            safeLaunchProjectiles.Add(new LaunchData(position, direction, seekdata, isDryRun, projectileSO));
        }
        protected void HandleLaunchSafe()
        {
            for (int i = 0; i < safeLaunchProjectiles.Count; i++)
            {
                Launch(safeLaunchProjectiles[i]);
            }
            safeLaunchProjectiles.Clear();
        }

        /// <summary>
        /// Used by Launch() to check if we have a particle system instanciated, and if not, then to try and instanciate one
        /// </summary>
        private void InstanciateProjectileParticleSystem()
        {
            //check if we need to instanciate the Projectile particle System
            if (ProjectileParticleSystem != lastParticleSystem || _firstProjectileSpawnFlag)
            {
                //if we are switching to a new projectile system, tell the current system to stop emitting and queue it for destruction when all particles have dissapeared
                if (!_firstProjectileSpawnFlag && lastParticleSystem != null)
                {
                    lastParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    systemsToDestroy.Add(lastParticleSystem);
                }


                //Set up the projectile particle system
                if (ProjectileSO.projectileParticleSystemPrefab.RuntimeKeyIsValid())
                {
                    if (SpawnAddressable.AssetIsLoaded(ProjectileSO.projectileParticleSystemPrefab))
                    {
                        isSpawningProjectileGraphicHandle = SpawnAddressable.Spawn(ProjectileSO.projectileParticleSystemPrefab, gameObject.transform);
                        isSpawningProjectileGraphicHandle.Completed += (asyncOperationHandle) =>
                        {
                            GameObject obj = asyncOperationHandle.Result as GameObject;
                            ProjectileParticleSystem = obj.GetComponent<ParticleSystem>();
                            lastParticleSystem = ProjectileParticleSystem;
                        };
                    }
                    else
                    {
                        AsyncOperationHandle handle = SpawnAddressable.LoadAsset(ProjectileSO.projectileParticleSystemPrefab);
                        handle.Completed += (asyncOperationHandle) =>
                        {
                            if (this != null)
                            {
                                AsyncOperationHandle h = SpawnAddressable.Spawn(ProjectileSO.projectileParticleSystemPrefab, gameObject.transform);
                                h.Completed += (asyncOpHandle) =>
                                {
                                    GameObject obj = asyncOpHandle.Result as GameObject;
                                    ProjectileParticleSystem = obj.GetComponent<ParticleSystem>();
                                    lastParticleSystem = ProjectileParticleSystem;
                                };
                            }

                        };
                    }


                }
                else
                {
                    ProjectileParticleSystem = null;
                    lastParticleSystem = ProjectileParticleSystem;
                }



            }


        }



        /// <summary>
        /// Used by Launch() to check if we have a particle system, and if so, then to instanciate a new particle for the projectile
        /// </summary>
        /// <param name="proj"></param>
        private void InstanciateProjectileParticle(ActiveProjectile proj, bool emitFlashAndAudio, bool muzzleFlashFXIsChildOfOrigin)
        {
            if (ProjectileParticleSystem != null)
            {
                ManipulateParticleGraphicOnInstanciate(proj);

            }
            else if (!SpawnAddressable.AssetIsLoaded(ProjectileSO.projectileParticleSystemPrefab) && ProjectileSO.projectileParticleSystemPrefab.RuntimeKeyIsValid())
            {
                AsyncOperationHandle handle = SpawnAddressable.LoadAsset(ProjectileSO.projectileParticleSystemPrefab);

                handle.Completed += (asyncOperationHandle) =>
                {
                    if (isSpawningProjectileGraphicHandle.IsValid())
                    {
                        isSpawningProjectileGraphicHandle.Completed += (asyncOperationHandle) =>
                        {
                            if (proj != null)
                            {
                                ManipulateParticleGraphicOnInstanciate(proj);
                            }
                        };
                    }
                };

            }
            else if (isSpawningProjectileGraphicHandle.IsValid())
            {
                isSpawningProjectileGraphicHandle.Completed += (asyncOperationHandle) =>
                {
                    if (proj != null)
                    {
                        ManipulateParticleGraphicOnInstanciate(proj);
                    }
                };

            }

            if (emitFlashAndAudio)
            {
                //emit a flash or launch effect
                ProjectileMaterialEffectDictonaryLinkageItem effectLinkItem = ProjectileSO.ProjMatDictLookup(ProjectileSO.tag,
                proj.ContainedWithinMaterialToughness.Count > 0 ? proj.ContainedWithinMaterialToughness.GetLast().MaterialTag : null);

                if (effectLinkItem == null)
                    return;
                //emit flash
                ProjectileOnHitParticleEmitter.SpawnEffect(effectLinkItem.OnEmitParticleEffectPrefab, Origin.position, proj.VelocityNormal, muzzleFlashFXIsChildOfOrigin ? Origin : null);
                //emit noise
                ProjectileOnHitParticleEmitter.SpawnEffect(effectLinkItem.OnEmitSoundEffectPrefab, Origin.position, proj.VelocityNormal, null, true);
                //if(effectLinkItem.OnEmitSoundEffectPrefab.RuntimeKeyIsValid())
                //Debug.Log(effectLinkItem.OnEmitParticleEffectPrefab.editorAsset.name);
            }
        }

        private void ManipulateParticleGraphicOnInstanciate(ActiveProjectile proj)
        {
            //set particle collision settings (this may need to be auto turned on if the timescale is prone to dropping to be very close to 0... Currently that is not implimented)
            CollisionModule collModule = ProjectileParticleSystem.collision;
            collModule.enabled = proj.ProjectileBlueprint.useParticleCollider;
            collModule.sendCollisionMessages = proj.ProjectileBlueprint.useParticleCollider;

            //set up particle parameters
            EmitParams particleParams = new EmitParams();
            particleParams.velocity = Vector3.zero;
            particleParams.position = proj.Position;
            particleParams.applyShapeToPosition = true;
            if (proj.ProjectileBlueprint.rotateParticleIn3DSpace)
            {
                particleParams.rotation3D = proj.VelocityNormal;
                particleParams.rotation3D += proj.ProjectileBlueprint.particleSystemRotationOffset;

            }


            //emit the particle
            ProjectileParticleSystem.Emit(particleParams, 1);

            proj.MainParticleIndex = ProjectileParticleSystem.particleCount - 1;

            //by passing in Proj.MainParticle, and the index, Proj.MainParticle is populated with the data of its newly instanciated particle
            ProjectileParticleSystem.GetParticles(proj.MainParticle, proj.MainParticle.Length, proj.MainParticleIndex);
        }

        public ActiveProjectile GetProjectileByIndex(int index)
        {

            if (inFlightProjectiles.Count <= index)
                return null;

            return inFlightProjectiles[index];
        }

        public void ManualHitByIndex(int index, Collider coll, bool destroyProjectile = true)
        {
            ActiveProjectile proj = GetProjectileByIndex(index);
            if (proj == null)
                return;

            Vector3 dir = proj.VelocityNormal;
            Vector3 origin = proj.Position + (-dir * 100);
            RaycastHit hit;

            if (coll.Raycast(new Ray(origin, dir), out hit, 1000))
            {
                ProjectileEvent.Trigger(ProjectileEventTypes.OnHit, proj, hit, this);
            }

            if (destroyProjectile)
            {
                proj.Alive = false;
                ProjectileEvent.Trigger(ProjectileEventTypes.OnDie, proj, new RaycastHit(), this);
                StartCoroutine(HandleParticleMirrorOnDeath(proj, true));
                _toRemove.Add(proj);
            }
        }

        /// <summary>
        /// Used internally by HandleMoveAndPositionTrackingManagement() to draw debug lines
        /// </summary>
        /// <param name="proj"></param>
        private void DrawProjectileDebugLines(ActiveProjectile proj)
        {
            if (proj.LastFramePositions.Count > 0)
            {
                Color color = proj.ContainedWithinRaycastHits.Count > 0 ? DebugInObjectColor : DebugInAirColor;
                DrawProjectileDebugLine(proj.LifetimePositions[proj.LifetimePositions.Count - 1], proj.LastFramePositions[0], color);
            }

            for (int j = 1; j < proj.LastFramePositions.Count; j++)
            {
                Color color = proj.ContainedWithinRaycastHits.Count > 0 ? DebugInObjectColor : DebugInAirColor;
                DrawProjectileDebugLine(proj.LastFramePositions[j - 1], proj.LastFramePositions[j], color);
            }
        }

        /// <summary>
        /// Used interally by DrawProjectileDebugLines to draw a single debug line
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="color"></param>
        private void DrawProjectileDebugLine(Vector3 from, Vector3 to, Color color)
        {
            Debug.DrawLine(from, to, color);

            float lineSize = .1f;
            Vector3 lineStart = to;
            Vector3 lineEnd = to;
            lineStart.x -= lineSize;
            lineEnd.x += lineSize;
            Debug.DrawLine(lineStart, lineEnd, color);
            lineStart = to;
            lineEnd = to;
            lineStart.y -= lineSize;
            lineEnd.y += lineSize;
            Debug.DrawLine(lineStart, lineEnd, color);
        }

        /// <summary>
        /// Returns the ActiveProjectile as it would look at death. From this you can run through the lifetime position list to get trajectory.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        protected ActiveProjectile GetTrajectory(Vector3 position, Vector3 direction, ProjectileSeekData seekdata = null, Projectile projectileSO = null, bool drawDebugTrajectory = false)
        {
            if (!projectileSO)
                projectileSO = ProjectileSO;

            ActiveProjectile proj = new ActiveProjectile(projectileSO, this, position, direction, seekdata, true, true);
            if (ProjectileInheritsEmitterVelocity)
            {
                proj.Velocity += velocity;
                proj.VelocityNormal = proj.Velocity.normalized;
            }

            proj.LocalTimescaleValue = 1;

            while (proj.Alive)
            {
                HandleMoveAndPositionTrackingManagement(proj, drawDebugTrajectory);
                if (proj.LocalTimescaleValue == 0)
                    proj.Alive = false;
            }

            return proj;
        }

        protected void DrawTrajectoryLines(ActiveProjectile proj)
        {
            for (int i = 0; i < proj.LifetimePositions.Count - 1; i++)
            {
                DrawProjectileDebugLine(proj.LifetimePositions[i], proj.LifetimePositions[i + 1], DebugInAirColor);
            }
        }

        /// <summary>
        /// Gets a list of shallow clones of the projectile at each step of movement. More detailed than GetTrajectory, but also more costly.
        /// If necessary, we can make a GetTrajectoryDeep to return a deep clone of the projectile at each step, but right now that is unnecessary.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public List<ActiveProjectile> GetTrajectoryFull(Vector3 position, Vector3 direction, ProjectileSeekData seekdata = null, Projectile projectileSO = null, bool drawDebugTrajectory = false)
        {

            if (!projectileSO)
                projectileSO = ProjectileSO;

            ActiveProjectile proj = new ActiveProjectile(projectileSO, this, position, direction, seekdata, true, true);
            if (ProjectileInheritsEmitterVelocity)
            {
                proj.Velocity += velocity;
                proj.VelocityNormal = proj.Velocity.normalized;
            }

            List<ActiveProjectile> snapShots = new List<ActiveProjectile>();
            proj.LocalTimescaleValue = 1;

            while (proj.Alive)
            {

                snapShots.AddRange(HandleMoveAndPositionTrackingManagement(proj, false, true));

                if (proj.LocalTimescaleValue == 0)
                    proj.Alive = false;
            }
            if (drawDebugTrajectory)
                DrawTrajectoryFull(snapShots);

            return snapShots;
        }

        protected void DrawTrajectoryFull(List<ActiveProjectile> snapshots)
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                Color color = snapshots[i].ContainedWithinRaycastHits.Count > 0 ? DebugInObjectColor : DebugInAirColor;
                if (snapshots[i].LastFramePositions.Count < 2)
                    DrawProjectileDebugLine(snapshots[i].LifetimePositions[snapshots[i].LifetimePositions.Count - 1], snapshots[i].LastFramePositions[snapshots[i].LastFramePositions.Count - 1], color);
                else
                    DrawProjectileDebugLine(snapshots[i].LastFramePositions[snapshots[i].LastFramePositions.Count - 2], snapshots[i].LastFramePositions[snapshots[i].LastFramePositions.Count - 1], color);
            }
        }

        public virtual void OnMBSEvent(ProjectileEvent mbsEvent)
        {
            //If the projectile in the event is not a projectile fired from this emitter, then return
            if (mbsEvent.Emitter != this)
            {
                return;
            }

            switch (mbsEvent.EventType)
            {
                case ProjectileEventTypes.OnHit:
                    OnHit(mbsEvent.RaycastHit, mbsEvent.Projectile);
                    break;
                case ProjectileEventTypes.OnHitTrigger:
                    OnHitTrigger(mbsEvent.RaycastHit, mbsEvent.Projectile);
                    break;
                case ProjectileEventTypes.OnDie:
                    OnProjectileDie(mbsEvent.RaycastHit, mbsEvent.Projectile);
                    break;
                case ProjectileEventTypes.OnRicochet:
                    OnProjectileRicochet(mbsEvent.RaycastHit, mbsEvent.Projectile);
                    break;
                case ProjectileEventTypes.OnPenetration:
                    OnProjectilePenetrationStart(mbsEvent.RaycastHit, mbsEvent.Projectile);
                    break;
                case ProjectileEventTypes.OnPenetrationExit:
                    OnProjectilePenetrationExit(mbsEvent.RaycastHit, mbsEvent.Projectile);
                    break;
            }
        }


        /// <summary>
        /// OnEnable, we start listening to events.
        /// </summary>
        protected virtual void OnEnable()
        {
            this.MBSEventStartListening<ProjectileEvent>();
        }

        /// <summary>
        /// OnDisable, we stop listening to events.
        /// </summary>
        protected virtual void OnDisable()
        {
            this.MBSEventStopListening<ProjectileEvent>();
        }

        public static void SpawnProjectileEffect(RaycastHit hit, ActiveProjectile proj, ProjectileEffectType type, Transform newParent = null, MaterialTag tag = null)
        {
            //apply any OnHit particle effects
            MaterialToughness matToughness = hit.collider.gameObject.GetComponentInParent<MaterialToughness>();
            if (tag == null)
                tag = matToughness != null ? matToughness.MaterialTag : null;
            ProjectileMaterialEffectDictonaryLinkageItem effectLinkItem = proj.ProjectileBlueprint.ProjMatDictLookup(proj.ProjectileBlueprint.tag, tag);
            if (effectLinkItem == null)
                return;

            Vector3 direction = Vector3.Lerp(hit.normal, proj.VelocityNormal, effectLinkItem.OnHitParticleSlantTowardsHitVelocity);
            AssetReference obj = null;
            bool isNonParticle = false;
            switch (type)
            {
                case ProjectileEffectType.OnHit:
                    obj = effectLinkItem.OnHitParticleEffectPrefab;
                    break;
                case ProjectileEffectType.BulletMark:
                    obj = effectLinkItem.BulletMarkParticleEffectPrefab;
                    direction = hit.normal;
                    break;
                case ProjectileEffectType.BulletHole:
                    obj = effectLinkItem.BulletHoleParticleEffectPrefab;
                    direction = hit.normal;
                    break;
                case ProjectileEffectType.BulletHolePenetration:
                    obj = effectLinkItem.BulletHolePenetrationParticleEffectPrefab;
                    direction = hit.normal;
                    break;
                case ProjectileEffectType.OnHitAudio:
                    obj = effectLinkItem.OnHitSoundEffectPrefab;
                    direction = hit.normal;
                    isNonParticle = true;
                    break;
                case ProjectileEffectType.NearMissAudio:
                    obj = effectLinkItem.OnNearMissSoundEffectPrefab;
                    direction = hit.normal;
                    isNonParticle = true;
                    break;
            }

            if (obj == null)
                return;

            ProjectileOnHitParticleEmitter.SpawnEffect(obj, hit.point, direction, newParent, isNonParticle);
        }

        public void RemoveProjectile(ActiveProjectile proj)
        {
            if (inFlightProjectiles.Contains(proj))
                inFlightProjectiles.Remove(proj);
        }
        /// <summary>
        /// This is triggered when a projectile fired from this emitter hits something
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="proj"></param>
        protected virtual void OnHit(RaycastHit hit, ActiveProjectile proj)
        {
            MaterialTag tag = MaterialTagComponent.GetMaterialTagByCollider(hit.collider);
            //apply any OnHit particle effects
            SpawnProjectileEffect(hit, proj, ProjectileEffectType.OnHit, null, tag);
            SpawnProjectileEffect(hit, proj, ProjectileEffectType.OnHitAudio, null, tag);
        }

        protected virtual void OnHitTrigger(RaycastHit hit, ActiveProjectile proj)
        {
            //apply any OnHit particle effects
            bool hitNearMiss = false;
            if (hit.collider != null)
            {
                ListenForProjectileNearMiss nearMiss = ListenForProjectileNearMiss.GetListenForProjectileNearMissByCollider(hit.collider);
                if (nearMiss != null)
                {
                    hitNearMiss = true;
                    SpawnProjectileEffect(hit, proj, ProjectileEffectType.NearMissAudio, null, nearMiss.materialTagComp.materialTag);
                }
            }

            if (!hitNearMiss)
            {
                MaterialTag tag = MaterialTagComponent.GetMaterialTagByCollider(hit.collider);
                SpawnProjectileEffect(hit, proj, ProjectileEffectType.OnHit, null, tag);
                SpawnProjectileEffect(hit, proj, ProjectileEffectType.OnHitAudio, null, tag);
            }

        }

        /// <summary>
        /// This is triggered when a projectile fired from this emitter expires
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="proj"></param>
        protected virtual void OnProjectileDie(RaycastHit hit, ActiveProjectile proj)
        {
            //This is already handled by Onhit.
            //if (hit.collider != null)
            //{
            //    //apply any OnHit particle effects
            //    SpawnProjectileEffect(hit, proj, ProjectileEffectType.OnHit);
            //}
        }

        /// <summary>
        /// This is triggered when a projectile fired from this emitter ricochets
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="proj"></param>
        protected virtual void OnProjectileRicochet(RaycastHit hit, ActiveProjectile proj)
        {
            //apply any OnHit particle effects
            SpawnProjectileEffect(hit, proj, ProjectileEffectType.BulletMark, hit.collider.transform);
        }

        protected virtual void OnProjectilePenetrationStart(RaycastHit hit, ActiveProjectile proj)
        {
            //apply any OnHit particle effects
            SpawnProjectileEffect(hit, proj, ProjectileEffectType.BulletHolePenetration, hit.collider.transform);
        }

        protected virtual void OnProjectilePenetrationExit(RaycastHit hit, ActiveProjectile proj)
        {
            //apply any OnHit particle effects
            SpawnProjectileEffect(hit, proj, ProjectileEffectType.BulletHolePenetration, hit.collider.transform);
            SpawnProjectileEffect(hit, proj, ProjectileEffectType.OnHit);
        }


        public class LaunchData
        {
            public Vector3 Position;
            public Vector3 Direction;
            public ProjectileSeekData Seekdata;
            public bool IsDryRun;
            public Projectile ProjectileSO;

            public LaunchData(Vector3 position, Vector3 direction, ProjectileSeekData seekdata = null, bool isDryRun = false, Projectile projectileSO = null)
            {
                Position = position;
                Direction = direction;
                Seekdata = seekdata;
                Seekdata = seekdata;
                IsDryRun = isDryRun;
                ProjectileSO = projectileSO;
            }

        }

    }
}
