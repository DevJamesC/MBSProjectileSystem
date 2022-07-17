using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MBS.Tools;
using System;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;

namespace MBS.ProjectileSystem
{

    [CreateAssetMenu(fileName = "NewProjectile", menuName = "MBSTools/Scriptable Objects/ Projectiles/ New Projectile")]
    public class Projectile : ScriptableObject
    {
        //[Header("GRAPHICS")]
        [SerializeField, Tooltip("The particle system which will be used to visualize projectiles. This will be instanciated as a child gameobject under the launcher")]
        protected AssetReference ProjectileParticleSystemPrefab;
        [SerializeField, Tooltip("InitalRotationOnly: The particle will not have its rotation influenced after aligning itself to the direction of the shooter.\n\n" +
            " VelocityRotation: The particle will rotate to match it's velocity heading. If paths are used, and are not 'relative', then velocity is not actually changing, and nor will rotation. \n\n" +
            " PositionDeltaRotation: The particle will rotate to match the angle between it's current and last position. This is good if paths are used, and are not set to 'relative'. ")]
        protected ProjectileParticleRotationMode ParticleRotationMode;
        [SerializeField, MBSReadOnly, Tooltip("If the projectile graphic is a mesh, it should be rotated in 3D space. Otherwise it should be rotated relative to the camera view. This value is set automatically.")]
        protected bool RotateParticleIn3DSpace;
        [SerializeField, Tooltip("Adjust this for 3d mesh projectiles if the model needs an offset to be 'heading' correctly according to velocity. This does not take into account center of gravity. Make sure that the" +
            "projectile system Renderer is set to 'world' and not 'view', otherwise the rotation will be rotated according to the angle you are looking at the projectile.")]
        protected Vector3 ParticleSystemRotationOffset;
        [SerializeField, Tooltip("The particle system which will be used to visualize any projectile trail. Usually this can be done with the 'Trail' module in the ProjectileParticlePrefab." +
            "Set this for more complicated trails, such as smoke from a rocket. This will be instaciated as a child gameobject under the launcher. For complicated projectiles which need an animateable gameobject graphic, " +
            "use this feild.")]
        protected AssetReference ProjectileTrailParticleSystemPrefab;
        [SerializeField, Tooltip("Enable this for the trail object to persist after the projectile has been removed from the simulation. This will work for an enemy that launches enemies, or such other maddness." +
            " Add component MBSSimpleObjectPooler to the prefab to add a lifetime expiration counter to the perminently spawned projectiles")]
        protected bool TrailObjectPersistsAfterProjectileDies;
        [SerializeField, Tooltip("The tag which will be used to figure out what hit sounds and effects this projectile causes. Leave it blank to have no hit sound or particle effect.")]
        protected ProjectileTag Tag;
        [SerializeField, Tooltip("The dictonary to use when looking up what effects and sounds to play when shooting from within, or hitting certain materials.")]
        protected ProjectileMaterialEffectDictionary EffectDictionary;

        //[Header("SPEED, DRAG, AND GRAVITY")]
        [SerializeField, Tooltip("Speed, in Meters per Second. Since speed is modified by other things (such as drag and gravity), this will basically be another modifier.")]
        protected MBSAnimationCurve Speed= MBSAnimationCurve.Constant(50,100);
        [SerializeField, Tooltip("Speed is evaluated with time by default. To evaluate with distance instead, enable this.")]
        protected bool EvaluateSpeedWithDistance = false;
        [SerializeField, Tooltip("Starting direction is usually the transform.forward of the projectile origin. Use this field to change the offset of the starting direction against gravity.")]
        protected float UpwardsOffset = 0;
        [SerializeField, Tooltip("This is used when determining how much a projectile slows down when passing through matter. Lower values represent more surface area and less inerta, basically. For less slowdown effect, set this higher. This does not affect Force of Impacts against targets.")]
        protected float DragMass = 1;
        [SerializeField, Tooltip("This is used when determining how much a projectile slows down when passing through matter when first hitting it. Lower values represent more surface area, while higher values represent smoother 'cutting', or less veloctiy loss. ")]
        protected float FirstContactDragMass = 1;
        [SerializeField, Tooltip("The 'weight' of the projectile. This is used when determining how much a projectile is pulled by gravity. 1 is default, 0 is ignores gravity, more than 1 is this projectile is affected by gravity stronger than other objects. This simulates weight. This does not affect Force of Impacts against targets.")]
        protected float GravityMass = 1;
        [SerializeField, Tooltip("No Gravity: Gravity does not affect this projectile\n\n" +
            "Gravity: The planetary gravity will effect this projectile\n\n" +
            "UsePathAsGravity: The X and Y paths of the projectile will be used to apply gravity, instead of the planet's gravity. This is good for projectiles which are fast and straight\n\n" +
            "UsePathAsGravityMultiplier: The X and Y paths of the projectile will be used as a multiplier against the planet's gravity. This is like 'UsePathAsGravity', but will conform to gravity differences.")]
        protected ProjectileGravityMode GravityOption;
        [SerializeField, Tooltip("This has no effect if gravity is not enabled. This clamps the maximum acceleration able to be exerted by gravity on the projectile. Set this low (50 or lower) for long lived projectiles, so they don't end up gaining a silly" +
            "amount of speed and breaking Unity due to float position values being too big. For more accurate arcs or distance (for a mortar or whatnot) or for very heavy projectiles, set this higher.")]
        protected float TerminalVelociy = 50f;

        //[Header("PROJECTILE PATHING")]
        [SerializeField, Tooltip("The change in path that the bullet has over distance. This can be used (usually with PathYIsRelative=true) to make bullet Drop")]
        protected MBSAnimationCurve ProjectilePathY = MBSAnimationCurve.Constant(1, 0);
        [SerializeField]
        protected MBSAnimationCurve ProjectilePathX = MBSAnimationCurve.Constant(1, 0);
        [SerializeField, Tooltip("Do the paths represent the hard/ sticky flight path (False) or do the paths represent a force added relative to the bullets position (True)?")]
        protected bool PathsAreRelative;
        [SerializeField, Tooltip("If this is true, the XY curves will be evaulated using the projectile's total distance traveled. If it is false, it will evaluate using the projectile's total time alive." +
                 "Using total lifetime is better if a projectile is prone to slowing down due to collisions and the like. Using distance will result in more consistant projectile behavior.")]
        protected bool EvaluteCurvesWithDistance;

        //[Header("PARTICLE COLLISION AND SAMPLES")]
        [SerializeField, Tooltip("Some slower projectiles may end up passing through other moving physics objects. In such cases, set this to True to allow those objects to detect if they hit the particle, in addition to the raycasts")]
        protected bool UseParticleCollider;
        [SerializeField, Tooltip("The Recommended number of Samples. More curvy things and faster things may need more samples. Less samples means better performance.")]
        [Range(1f, 50f)]
        protected int RecommendedSamples = 1;

        //[Header("PENETRATION")]
        [SerializeField, Tooltip("The layers that can be penetrated. Anything not selected will stop the projectile as if it had 0 penetration distance. NOTE: PenetrateableLayers should not contain any RicochetableLayers")]
        protected LayerMask PenetrateableLayers;
        [SerializeField, Tooltip("Penetration, in Meters. Penetration is X penetration at Y speed, so a loss in speed will reduce effective penetration. If speed is somehow gained, penetration will be increased.")]
        protected float Penetration;
        [SerializeField, Tooltip("The value indicates at what drag value this projectile gets 'free' penetration. This only affects non-trigger (trigger-colliders already do not incure penetration loss). A use case is " +
            "'A tank shell should be able to punch through a lot of low density drywall, even if it has zero or very low actual penetration'. Generally this is set to 0 for normal drag interactions.")]
        protected float DragPenetrationThreshold = 0;
        [SerializeField, Tooltip("The maximum number objects which can be penetrated. 1 means that the bullet will peirce 1 object only, even if it has more penetration. 0 is infinite objects.")]
        protected int MaxNumPenetrationObjects;

        //[Header("LIFETIME AND PHYSICS BOUNDS")]
        [SerializeField, Tooltip("The time till this projectile will delete itself")]
        protected float Lifetime;
        [SerializeField, Tooltip("If the projectile speed ever drops lower than this, it will be considered dead. This is useful for when penetration or ricochet cause a projectile to drop to very low speeds. The default is set" +
            " to be about as low as it can go before the projectile starts passing through surfaces. If you have issues, bump it up to .5")]
        protected float MinimumSpeed = .4f;
        [SerializeField, Tooltip("This is the limit at which projectiles will be removed from the simulation. If a transform gets too far away from the world origin, physics will being to break. Depending on use case, you may want to change the 'bounds'.")]
        protected Vector3 PhysicsLimitUpper = new Vector3(5000, 5000, 5000);
        [SerializeField, Tooltip("This is the limit at which projectiles will be removed from the simulation. If a transform gets too far away from the world origin, physics will being to break. Depending on use case, you may want to change the 'bounds'.")]
        protected Vector3 PhysicsLimitLower = new Vector3(-5000, -5000, -5000);

        //[Header("SEEKING")]
        [SerializeField, Tooltip("If true, paths will be ignored if there is a target. If false, paths will add to or take away from the seek turn amount (basically seek is appied, then paths are applied)")]
        protected bool SeekingOverridesPaths;
        [SerializeField, Tooltip("The degrees per second the projectile can turn to orient towards a target")]
        protected MBSAnimationCurve SeekTurnAmount = MBSAnimationCurve.Constant(1, 0);
        [SerializeField, Tooltip("If true, seek curve will be evaluated with distance traveled. Otherwise seek curve will be evaluated with time alive")]
        protected bool EvaluteSeekCurveWithDistance;

        //[Header("RICOCHET")]
        [SerializeField, Tooltip("The layers that can be ricocheted against. Anything not selected will stop the projectile as if it had 0 ricochets. NOTE: RicochetableLayers should not contain any PenetrateableLayers")]
        protected LayerMask RicochetableLayers;
        [SerializeField, Tooltip("The number of times the projectile can ricochet. Generally projectiles do not have ricochet and penetration. If one has both, then ricochet will be ignored.")]
        protected int Ricochets;
        [SerializeField, Range(0, .99f), Tooltip("The percent speed lost when the projectile ricochets off something. 0 is no speed lost, while 1 is a dead stop")]
        protected float RicochetSpeedLossPercent;
        [SerializeField, Tooltip("Enable this for the speed loss percent be modified by the angle of the hit. A glancing ricochet will not slow down the projectile as much as a head-on hit.")]
        protected bool RicochetSpeedLossBasedOnHitAngle;
        [SerializeField, Range(0, .99f), Tooltip("When using RicochetSpeedLossBasedOnHitAngle, this will cap the lower bound, ensureing that the projectile will always lose at least this percent.")]
        protected float RicochetMinimumSpeedLossPercent = 0;
        [SerializeField, Range(0, .99f), Tooltip("When using RicochetSpeedLossBasedOnHitAngle, this will cap the upper bound, ensureing that the projectile will never lose more than this percent.")]
        protected float RicochetMaximumSpeedLossPercent = 99f;
        [SerializeField, Range(0, 45f), Tooltip("The amount that the ricochet is randomized. If set to 0, it will perfectly ricochet, like light bouncing off a mirror. Otherwise the bounce angle will have some randomness.")]
        protected float RicochetAngleVariability;
        [SerializeField, Range(0, 180), Tooltip("The degrees which a ricochet projectile can angle itself towards a target on bounce. If the projectile has seeking active, then this will override AngleVariability.")]
        protected float RicochetSeekTurnAmount;
        [SerializeField, Tooltip("How many bounces until the ricochet uses seeking behavior. 0 is every ricochet. 1 is every other ricochet, so on and so forth. Set this to 1 or higher for a less aggressive ricochet.")]
        protected int RicochetSeekInterval;
        [SerializeField, Tooltip("Set this true for the first bounce to start the seek intervel. Set this false for the Nth shot to seek, as determined by RicochetSeekIntervel, even if that shot is many ricochets after the first.")]
        protected bool FirstBounceSeeks = true;

        //[Header("STAGES")]
        [SerializeField, Tooltip("Every Fixedupdate, these stages will be checked to see if conditions are met for them to trigger. They do not have any particular order. To control flow, use Conditions to make sure one stage will" +
            "trigger before others.")]
        protected List<ProjectileStage> ProjectileStages = new List<ProjectileStage>();

        #region Getters

        public AssetReference projectileParticleSystemPrefab { get => ProjectileParticleSystemPrefab; }
        public ProjectileParticleRotationMode particleRotationMode { get => ParticleRotationMode; }
        public bool rotateParticleIn3DSpace { get => RotateParticleIn3DSpace; }
        public Vector3 particleSystemRotationOffset { get => ParticleSystemRotationOffset; }
        public AssetReference projectileTrailParticleSystemPrefab { get => ProjectileTrailParticleSystemPrefab; }
        public bool trailObjectPersistsAfterProjectileDies { get => TrailObjectPersistsAfterProjectileDies; }
        public ProjectileTag tag { get => Tag; }

        public ProjectileMaterialEffectDictionary effectDictionary { get => EffectDictionary; }
        public MBSAnimationCurve speed { get => Speed; }
        public bool evaluateSpeedWithDistance { get => EvaluateSpeedWithDistance; }
        public float upwardsOffset { get => UpwardsOffset; }
        public float dragMass { get => DragMass; }
        public float firstContactDragMass { get => FirstContactDragMass; }
        public float gravityMass { get => GravityMass; }
        public ProjectileGravityMode gravityOption { get => GravityOption; }
        public float terminalVelociy { get => TerminalVelociy; }
        public MBSAnimationCurve projectilePathY { get => ProjectilePathY; }
        public MBSAnimationCurve projectilePathX { get => ProjectilePathX; }
        public bool pathsAreRelative { get => PathsAreRelative; }
        public bool evaluteCurvesWithDistance { get => EvaluteCurvesWithDistance; }
        public bool useParticleCollider { get => UseParticleCollider; }
        public int recommendedSamples { get => RecommendedSamples; }

        public LayerMask penetrateableLayers { get => PenetrateableLayers; }
        public float penetration { get => Penetration; }
        public float dragPenetrationThreshold { get => DragPenetrationThreshold; }
        public int maxNumPenetrationObjects { get => MaxNumPenetrationObjects; }
        public float lifetime { get => Lifetime; }
        public float minimumSpeed { get => MinimumSpeed; }
        public Vector3 physicsLimitUpper { get => PhysicsLimitUpper; }
        public Vector3 physicsLimitLower { get => PhysicsLimitLower; }
        public bool seekingOverridesPaths { get => SeekingOverridesPaths; }
        public MBSAnimationCurve seekTurnAmount { get => SeekTurnAmount; }

        public bool evaluteSeekCurveWithDistance { get => EvaluteSeekCurveWithDistance; }
        public LayerMask ricochetableLayers { get => RicochetableLayers; }
        public int ricochets { get => Ricochets; }
        public float ricochetSpeedLossPercent { get => RicochetSpeedLossPercent; }
        public bool ricochetSpeedLossBasedOnHitAngle { get => RicochetSpeedLossBasedOnHitAngle; }
        public float ricochetMinimumSpeedLossPercent { get => RicochetMinimumSpeedLossPercent; }
        public float ricochetMaximumSpeedLossPercent { get => RicochetMaximumSpeedLossPercent; }
        public float ricochetAngleVariability { get => RicochetAngleVariability; }
        public float ricochetSeekTurnAmount { get => RicochetSeekTurnAmount; }
        public int ricochetSeekInterval { get => RicochetSeekInterval; }
        public bool firstBounceSeeks { get => FirstBounceSeeks; }
        public List<ProjectileStage> projectileStages { get => ProjectileStages; }

        #endregion


        public ProjectileMaterialEffectDictonaryLinkageItem ProjMatDictLookup(ProjectileTag projTag, MaterialTag matTag)
        {
            if (EffectDictionary == null)
                return null;

            return EffectDictionary.Lookup(projTag, matTag);
        }

        public Vector3 EvaluateXYPath(float evalTime)
        {
            Vector3 v = Vector3.zero;
            v.x = projectilePathX.Evaluate(evalTime);
            v.y = projectilePathY.Evaluate(evalTime);
            return v;
        }

        public enum ProjectileGravityMode
        {
            NoGravity,
            Gravity,
            UsePathAsGravity,
            UsePathAsGravityMultiplier
        }
        public enum ProjectileParticleRotationMode
        {
            InitalRotationOnly,
            VelocityRotation,
            PositionDeltaRotation
        }

        public List<ProjectileBehaviorAction> GetAllAssignedActionsOfType<T>()
        {
            List<ProjectileBehaviorAction> returnVal = new List<ProjectileBehaviorAction>();

            foreach (var stage in ProjectileStages)
            {
                foreach (var block in stage.Blocks)
                {
                    foreach (var action in block.Actions)
                    {
                        if (action.GetType() == typeof(T))
                            returnVal.Add(action);
                    }
                }
            }

            return returnVal;
        }

        [Serializable]
        public class ProjectileStage //When creating a projectile, need to create a clone of each condition and action so it does not write evaluated to the scriptable object
        {
            [HideInInspector]
            public string name = "Stage";
            [SerializeField, Tooltip("Place logic blocks here to determine when this stage should trigger, and what actions will be taken when it does.")]
            public List<ProjectileStageFlow> Blocks;
            public bool Triggered { get; set; }
            public ProjectileStage(ProjectileStage stage)
            {
                Blocks = stage.Blocks;
                Triggered = stage.Triggered;
            }


            public void HandleStage(ActiveProjectile proj)
            {
                if (Triggered)
                    return;

                if (Blocks == null)
                {
                    Debug.Log("A projectile (" + proj.Emitter.gameObject.name + ") has a stage with a no Condition-Action Blocks!");
                    return;
                }
                if (Blocks.Count == 0)
                {
                    Debug.Log("A projectile (" + proj.Emitter.gameObject.name + ") has a stage with a no Condition-Action Blocks!");
                    return;
                }

                for (int i = 0; i < Blocks.Count; i++)
                {
                    if (Blocks[i].Evaluate(proj))
                    {
                        Triggered = true;
                        Blocks[i].TriggerActions(proj);
                    }
                }
            }
        }

        [Serializable]
        public class ProjectileStageFlow
        {
            [HideInInspector]
            public string name = "block";
            public StageFlowType BlockType;
            [Tooltip("Every fixedupdate, all the scripts placed here will be called to check if the projectile trigger has been met. If any of the conditions are met, this stage will trigger.")]
            public List<ProjectileBehaviorCondition> Conditions;
            [Tooltip("When this stage starts, every script placed here will execute.")]
            public List<ProjectileBehaviorAction> Actions;

            public bool Evaluate(ActiveProjectile proj)
            {
                bool eval;

                if (Conditions == null)
                {
                    Debug.LogWarning("A projectile from (" + proj.Emitter.gameObject.name + ") has a stage that contains no conditions!");
                    return false;
                }
                if (Conditions.Count == 0)
                {
                    Debug.LogWarning("A projectile from (" + proj.Emitter.gameObject.name + ") has a stage that contains no conditions!");
                    return false;
                }

                int numOfTrueConditions = 0;
                for (int i = 0; i < Conditions.Count; i++)
                {
                    if (Conditions[i].Evaluate(proj))
                    {
                        if (BlockType == StageFlowType.Or)
                        {
                            return true;
                        }
                        numOfTrueConditions++;

                    }
                }
                eval = numOfTrueConditions == Conditions.Count;

                return eval;
            }

            public void TriggerActions(ActiveProjectile proj)
            {
                for (int i = 0; i < Actions.Count; i++)
                {
                    Actions[i].Tick(proj);
                }
            }

            [Serializable]
            public enum StageFlowType
            {
                And,
                Or
            }
        }


        protected void OnValidate()
        {
            //Dragmass cannot be 0 or lower
            if (DragMass <= 0)
            {
                DragMass = .001f;
            }
            //Set the render-rotation mode of any particle system automatically, based on the settings of the supplied particle system
            if (ProjectileParticleSystemPrefab.RuntimeKeyIsValid())
            {
#if UNITY_EDITOR
                GameObject prefab = projectileParticleSystemPrefab.editorAsset as GameObject;
                RotateParticleIn3DSpace = prefab.GetComponent<ParticleSystemRenderer>().renderMode == ParticleSystemRenderMode.Mesh;
#endif
            }
            //Make sure that ricochetMinimumSpeedLossPercent is not higher than the maximum
            if (RicochetMinimumSpeedLossPercent > RicochetMaximumSpeedLossPercent)
            {
                float average = (RicochetMinimumSpeedLossPercent + RicochetMaximumSpeedLossPercent) / 2;
                RicochetMaximumSpeedLossPercent = average + .1f;
                RicochetMinimumSpeedLossPercent = average - .1f;

                if (RicochetMaximumSpeedLossPercent > .99f)
                    RicochetMaximumSpeedLossPercent = .99f;

                if (RicochetMinimumSpeedLossPercent < 0)
                    RicochetMinimumSpeedLossPercent = 0;
            }
            //Seek intervel for ricochet cannot be below 0
            if (RicochetSeekInterval < 0)
                RicochetSeekInterval = 0;
            //Make sure that penetration and ricochet layers do not contain the same layer
            bool[] penLayers = PenetrateableLayers.ContainsLayers();
            bool[] ricLayers = RicochetableLayers.ContainsLayers();
            bool showWarningMessage = false;
            string warningMessage = "PenetrateableLayers and RicochetableLayers have overlapping layers. This is not allowed and the projectile will act as if it has neither Penetration nor Ricochet. \nOverlapping layers are:\n";
            for (int i = 0; i < penLayers.Length; i++)
            {
                if (penLayers[i] && ricLayers[i])
                {
                    showWarningMessage = true;
                    warningMessage += LayerMask.LayerToName(i) + "\n";
                }
            }
            if (showWarningMessage)
                Debug.LogWarning(warningMessage);
            //set the names of stages
            for (int i = 0; i < ProjectileStages.Count; i++)
            {
                projectileStages[i].name = "Stage " + i;
                for (int j = 0; j < projectileStages[i].Blocks.Count; j++)
                {
                    projectileStages[i].Blocks[j].name = "Logic Block " + j;
                }
            }

            //if (UseCurveEditerForSeek)
            //SeekTurnAmount = CurveEditer(SeekCurveEditerValues, LinkXYForSeekValues);

        }

        protected AnimationCurve CurveEditer(Vector3 vector, bool xyLinked)
        {
            if (xyLinked)
                vector.y = vector.x;

            return AnimationCurve.Linear(0, vector.x, vector.z, vector.y);
        }

        public class ProjectileInstanceData
        {
            //[Header("GRAPHICS")]
            public AssetReference ProjectileParticleSystemPrefab;
            public ProjectileParticleRotationMode ParticleRotationMode;
            public bool RotateParticleIn3DSpace;
            public Vector3 ParticleSystemRotationOffset;
            public AssetReference ProjectileTrailParticleSystemPrefab;
            public bool TrailObjectPersistsAfterProjectileDies;
            public ProjectileTag Tag;
            public ProjectileMaterialEffectDictionary EffectDictionary;

            //[Header("SPEED, DRAG, AND GRAVITY")]           
            public MBSAnimationCurve Speed;
            public bool EvaluateSpeedWithDistance;
            public float UpwardsOffset;
            public float DragMass;
            public float FirstContactDragMass;
            public float GravityMass;
            public ProjectileGravityMode GravityOption;
            public float TerminalVelociy;

            //[Header("PROJECTILE PATHING")]
            public MBSAnimationCurve ProjectilePathY;
            public MBSAnimationCurve ProjectilePathX;
            public bool PathsAreRelative;
            public bool EvaluteCurvesWithDistance;

            //[Header("PARTICLE COLLISION AND SAMPLES")]
            public bool UseParticleCollider;
            public int RecommendedSamples;

            //[Header("PENETRATION")]
            public LayerMask PenetrateableLayers;
            public float Penetration;
            public float DragPenetrationThreshold;
            public int MaxNumPenetrationObjects;

            //[Header("LIFETIME AND PHYSICS BOUNDS")]
            public float Lifetime;
            public float MinimumSpeed;
            public Vector3 PhysicsLimitUpper;
            public Vector3 PhysicsLimitLower;

            //[Header("SEEKING")]
            public bool SeekingOverridesPaths;
            public MBSAnimationCurve SeekTurnAmount;
            public bool EvaluteSeekCurveWithDistance;

            //[Header("RICOCHET")]
            public LayerMask RicochetableLayers;
            public int Ricochets;
            public float RicochetSpeedLossPercent;
            public bool RicochetSpeedLossBasedOnHitAngle;
            public float RicochetMinimumSpeedLossPercent;
            public float RicochetMaximumSpeedLossPercent;
            public float RicochetAngleVariability;
            public float RicochetSeekTurnAmount;
            public int RicochetSeekInterval;
            public bool FirstBounceSeeks;

            //[Header("STAGES")]
            public List<ProjectileStage> ProjectileStages;

            public ProjectileInstanceData(Projectile projectile)
            {
                //[Header("GRAPHICS")]
                ProjectileParticleSystemPrefab = projectile.ProjectileParticleSystemPrefab;
                ParticleRotationMode = projectile.ParticleRotationMode;
                RotateParticleIn3DSpace = projectile.RotateParticleIn3DSpace;
                ParticleSystemRotationOffset = projectile.ParticleSystemRotationOffset;
                ProjectileTrailParticleSystemPrefab = projectile.ProjectileTrailParticleSystemPrefab;
                TrailObjectPersistsAfterProjectileDies = projectile.TrailObjectPersistsAfterProjectileDies;
                Tag = projectile.Tag;
                EffectDictionary = projectile.EffectDictionary;

                //[Header("SPEED, DRAG, AND GRAVITY")]           
                Speed = projectile.Speed;
                EvaluateSpeedWithDistance = projectile.EvaluateSpeedWithDistance;
                UpwardsOffset = projectile.UpwardsOffset;
                DragMass = projectile.DragMass;
                FirstContactDragMass = projectile.FirstContactDragMass;
                GravityMass = projectile.GravityMass;
                GravityOption = projectile.GravityOption;
                TerminalVelociy = projectile.TerminalVelociy;

                //[Header("PROJECTILE PATHING")]
                ProjectilePathY = projectile.ProjectilePathY;
                ProjectilePathX = projectile.ProjectilePathX;
                PathsAreRelative = projectile.PathsAreRelative;
                EvaluteCurvesWithDistance = projectile.EvaluteCurvesWithDistance;

                //[Header("PARTICLE COLLISION AND SAMPLES")]
                UseParticleCollider = projectile.UseParticleCollider;
                RecommendedSamples = projectile.RecommendedSamples;

                //[Header("PENETRATION")]
                PenetrateableLayers = projectile.PenetrateableLayers;
                Penetration = projectile.Penetration;
                DragPenetrationThreshold = projectile.DragPenetrationThreshold;
                MaxNumPenetrationObjects = projectile.MaxNumPenetrationObjects;

                //[Header("LIFETIME AND PHYSICS BOUNDS")]
                Lifetime = projectile.Lifetime;
                MinimumSpeed = projectile.MinimumSpeed;
                PhysicsLimitUpper = projectile.PhysicsLimitUpper;
                PhysicsLimitLower = projectile.PhysicsLimitLower;

                //[Header("SEEKING")]
                SeekingOverridesPaths = projectile.SeekingOverridesPaths;
                SeekTurnAmount = projectile.SeekTurnAmount;
                EvaluteSeekCurveWithDistance = projectile.EvaluteSeekCurveWithDistance;

                //[Header("RICOCHET")]
                RicochetableLayers = projectile.RicochetableLayers;
                Ricochets = projectile.Ricochets;
                RicochetSpeedLossPercent = projectile.RicochetSpeedLossPercent;
                RicochetSpeedLossBasedOnHitAngle = projectile.RicochetSpeedLossBasedOnHitAngle;
                RicochetMinimumSpeedLossPercent = projectile.RicochetMinimumSpeedLossPercent;
                RicochetMaximumSpeedLossPercent = projectile.RicochetMaximumSpeedLossPercent;
                RicochetAngleVariability = projectile.RicochetAngleVariability;
                RicochetSeekTurnAmount = projectile.RicochetSeekTurnAmount;
                RicochetSeekInterval = projectile.RicochetSeekInterval;
                FirstBounceSeeks = projectile.FirstBounceSeeks;

                //[Header("STAGES")]
                ProjectileStages = projectile.ProjectileStages;
            }

            public Vector3 EvaluateXYPath(float evalTime)
            {
                Vector3 v = Vector3.zero;
                v.x = ProjectilePathX.Evaluate(evalTime);
                v.y = ProjectilePathY.Evaluate(evalTime);
                return v;
            }
        }
    }
}

