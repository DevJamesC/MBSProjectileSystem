using MBS.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CustomEditor(typeof(Projectile), true)]
    [CanEditMultipleObjects]
    public class ProjectileSOCustomInspector : Editor
    {
        //Graphics
        SerializedProperty ProjectileParticleSystemPrefab;
        SerializedProperty ParticleRotationMode;
        SerializedProperty RotateParticleIn3DSpace;
        SerializedProperty ParticleSystemRotationOffset;
        SerializedProperty ProjectileTrailParticleSystemPrefab;
        SerializedProperty TrailObjectPersistsAfterProjectileDies;
        SerializedProperty ClearTrailParticlesOnProjectileDeath;
        SerializedProperty Tag;
        SerializedProperty EffectDictionary;
        //Speed, Drag, Gravity
        SerializedProperty Speed;
        SerializedProperty EvaluateSpeedWithDistance;
        SerializedProperty UpwardsOffset;
        SerializedProperty DragMass;
        SerializedProperty FirstContactDragMass;
        SerializedProperty GravityMass;
        SerializedProperty GravityOption;
        SerializedProperty TerminalVelociy;
        //Projectile Pathing
        SerializedProperty ProjectilePathY;
        SerializedProperty ProjectilePathX;
        SerializedProperty PathsAreRelative;
        SerializedProperty EvaluteCurvesWithDistance;
        //Projectile Samples and Particle Collision
        SerializedProperty UseParticleCollider;
        SerializedProperty RecommendedSamples;
        //Penetration
        SerializedProperty PenetrateableLayers;
        SerializedProperty Penetration;
        SerializedProperty DragPenetrationThreshold;
        SerializedProperty MaxNumPenetrationObjects;
        //Lifetime and physics bounds
        SerializedProperty Lifetime;
        SerializedProperty MinimumSpeed;
        SerializedProperty PhysicsLimitUpper;
        SerializedProperty PhysicsLimitLower;
        //Seeking
        SerializedProperty SeekingOverridesPaths;
        SerializedProperty SeekTurnAmount;
        SerializedProperty EvaluteSeekCurveWithDistance;
        //Ricochet
        SerializedProperty RicochetableLayers;
        SerializedProperty Ricochets;
        SerializedProperty RicochetSpeedLossPercent;
        SerializedProperty RicochetSpeedLossBasedOnHitAngle;
        SerializedProperty RicochetMinimumSpeedLossPercent;
        SerializedProperty RicochetMaximumSpeedLossPercent;
        SerializedProperty RicochetAngleVariability;
        SerializedProperty RicochetSeekTurnAmount;
        SerializedProperty RicochetSeekInterval;
        SerializedProperty FirstBounceSeeks;
        //Stages
        SerializedProperty ProjectileStages;

        protected static GUIStyle foldoutStyle;
        protected static bool showGraphicsSettings = false;
        protected static bool showPhysicsSettings = false;
        protected static bool showPathingSettings = false;
        protected static bool showPenetrationSettings = false;
        protected static bool showLifetimeAndBoundsSettings = false;
        protected static bool showSeekingSettings = false;
        protected static bool showRicochetSettings = false;
        protected static bool showStageSettings = false;

        protected Projectile targetScript;
        protected AnimationCurveOptions speedCurveOptions;

        private void OnEnable()
        {
            targetScript = target as Projectile;

            //Graphics
            ProjectileParticleSystemPrefab = serializedObject.FindProperty("ProjectileParticleSystemPrefab");
            ParticleRotationMode = serializedObject.FindProperty("ParticleRotationMode");
            RotateParticleIn3DSpace = serializedObject.FindProperty("RotateParticleIn3DSpace");
            ParticleSystemRotationOffset = serializedObject.FindProperty("ParticleSystemRotationOffset");
            ProjectileTrailParticleSystemPrefab = serializedObject.FindProperty("ProjectileTrailParticleSystemPrefab");
            TrailObjectPersistsAfterProjectileDies = serializedObject.FindProperty("TrailObjectPersistsAfterProjectileDies");
            ClearTrailParticlesOnProjectileDeath= serializedObject.FindProperty("ClearTrailParticlesOnProjectileDeath");
            Tag = serializedObject.FindProperty("Tag");
            EffectDictionary = serializedObject.FindProperty("EffectDictionary");
            //Speed, Drag, Gravity
            Speed = serializedObject.FindProperty("Speed");
            EvaluateSpeedWithDistance = serializedObject.FindProperty("EvaluateSpeedWithDistance");
            UpwardsOffset = serializedObject.FindProperty("UpwardsOffset");
            DragMass = serializedObject.FindProperty("DragMass");
            FirstContactDragMass = serializedObject.FindProperty("FirstContactDragMass");
            GravityMass = serializedObject.FindProperty("GravityMass");
            GravityOption = serializedObject.FindProperty("GravityOption");
            TerminalVelociy = serializedObject.FindProperty("TerminalVelociy");
            //Projectile Pathing
            ProjectilePathY = serializedObject.FindProperty("ProjectilePathY");
            ProjectilePathX = serializedObject.FindProperty("ProjectilePathX");
            PathsAreRelative = serializedObject.FindProperty("PathsAreRelative");
            EvaluteCurvesWithDistance = serializedObject.FindProperty("EvaluteCurvesWithDistance");
            //Projectile Samples and Particle Collision
            UseParticleCollider = serializedObject.FindProperty("UseParticleCollider");
            RecommendedSamples = serializedObject.FindProperty("RecommendedSamples");
            //Penetration
            PenetrateableLayers = serializedObject.FindProperty("PenetrateableLayers");
            Penetration = serializedObject.FindProperty("Penetration");
            DragPenetrationThreshold = serializedObject.FindProperty("DragPenetrationThreshold");
            MaxNumPenetrationObjects = serializedObject.FindProperty("MaxNumPenetrationObjects");
            //Lifetime and physics bounds
            Lifetime = serializedObject.FindProperty("Lifetime");
            MinimumSpeed = serializedObject.FindProperty("MinimumSpeed");
            PhysicsLimitUpper = serializedObject.FindProperty("PhysicsLimitUpper");
            PhysicsLimitLower = serializedObject.FindProperty("PhysicsLimitLower");
            //Seeking
            SeekingOverridesPaths = serializedObject.FindProperty("SeekingOverridesPaths");
            SeekTurnAmount = serializedObject.FindProperty("SeekTurnAmount");
            EvaluteSeekCurveWithDistance = serializedObject.FindProperty("EvaluteSeekCurveWithDistance");
            //Ricochet
            RicochetableLayers = serializedObject.FindProperty("RicochetableLayers");
            Ricochets = serializedObject.FindProperty("Ricochets");
            RicochetSpeedLossPercent = serializedObject.FindProperty("RicochetSpeedLossPercent");
            RicochetSpeedLossBasedOnHitAngle = serializedObject.FindProperty("RicochetSpeedLossBasedOnHitAngle");
            RicochetMinimumSpeedLossPercent = serializedObject.FindProperty("RicochetMinimumSpeedLossPercent");
            RicochetMaximumSpeedLossPercent = serializedObject.FindProperty("RicochetMaximumSpeedLossPercent");
            RicochetAngleVariability = serializedObject.FindProperty("RicochetAngleVariability");
            RicochetSeekTurnAmount = serializedObject.FindProperty("RicochetSeekTurnAmount");
            RicochetSeekInterval = serializedObject.FindProperty("RicochetSeekInterval");
            FirstBounceSeeks = serializedObject.FindProperty("FirstBounceSeeks");
            //Stages
            ProjectileStages = serializedObject.FindProperty("ProjectileStages");           

        }

        public override void OnInspectorGUI()
        {
            foldoutStyle = CustomEditorTools.QuickStyle(EditorStyles.foldout, 14, FontStyle.Bold, Color.grey);
            serializedObject.Update();

            //Graphics
            showGraphicsSettings = EditorGUILayout.Foldout(showGraphicsSettings, "Graphics Settings", true, foldoutStyle);
            if (showGraphicsSettings)
            {
                EditorGUILayout.PropertyField(ProjectileParticleSystemPrefab);
                EditorGUILayout.PropertyField(ParticleRotationMode);
                EditorGUILayout.PropertyField(RotateParticleIn3DSpace);
                EditorGUILayout.PropertyField(ParticleSystemRotationOffset);
                EditorGUILayout.PropertyField(ProjectileTrailParticleSystemPrefab);
                EditorGUILayout.PropertyField(TrailObjectPersistsAfterProjectileDies);
                EditorGUILayout.PropertyField(ClearTrailParticlesOnProjectileDeath);
                EditorGUILayout.PropertyField(Tag);
                EditorGUILayout.PropertyField(EffectDictionary);
                CustomEditorTools.DrawUILine(Color.gray);
            }

            //Speed, Drag, Gravity
            showPhysicsSettings = EditorGUILayout.Foldout(showPhysicsSettings, "Physics Settings", true, foldoutStyle);
            if (showPhysicsSettings)
            {

                MBSAnimationCurve initalSpeedVal = Speed.GetValue<MBSAnimationCurve>();
                MBSAnimationCurve speedval = CustomEditorTools.DrawMBSAnimationCurve("Speed", initalSpeedVal,serializedObject);
                Speed.SetValue(speedval);
                if (serializedObject.targetObjects.Length > 1)
                    EditorGUILayout.LabelField("Multi Editing animation curves is not supported! \n You are currently only editing the topmost selected object!", GUILayout.Height(30));

                EditorGUILayout.PropertyField(EvaluateSpeedWithDistance);
                EditorGUILayout.PropertyField(UpwardsOffset);
                EditorGUILayout.PropertyField(DragMass);
                EditorGUILayout.PropertyField(FirstContactDragMass);
                EditorGUILayout.PropertyField(GravityMass);
                EditorGUILayout.PropertyField(GravityOption);
                EditorGUILayout.PropertyField(TerminalVelociy);
                //Projectile Samples and Particle Collision
                EditorGUILayout.PropertyField(UseParticleCollider);
                EditorGUILayout.PropertyField(RecommendedSamples);
                CustomEditorTools.DrawUILine(Color.gray);
            }

            //Projectile Pathing
            showPathingSettings = EditorGUILayout.Foldout(showPathingSettings, "Pathing Settings", true, foldoutStyle);
            if (showPathingSettings)
            {
                MBSAnimationCurve initalPathYVal= ProjectilePathY.GetValue<MBSAnimationCurve>();
                MBSAnimationCurve pathYval = CustomEditorTools.DrawMBSAnimationCurve("Path Y", initalPathYVal, serializedObject, "The change in path that the bullet has over distance. This can be used (usually with PathsAreRelative=true) to make bullet Drop");
                ProjectilePathY.SetValue(pathYval);
                MBSAnimationCurve initalPathXVal = ProjectilePathX.GetValue<MBSAnimationCurve>();
                MBSAnimationCurve pathXval = CustomEditorTools.DrawMBSAnimationCurve("Path X", initalPathXVal, serializedObject);
                ProjectilePathX.SetValue(pathXval);
                if (serializedObject.targetObjects.Length > 1)
                    EditorGUILayout.LabelField("Multi Editing animation curves is not supported! \n You are currently only editing the topmost selected object!", GUILayout.Height(30));

                EditorGUILayout.PropertyField(PathsAreRelative);
                EditorGUILayout.PropertyField(EvaluteCurvesWithDistance);
                CustomEditorTools.DrawUILine(Color.gray);
            }

            //Penetration
            showPenetrationSettings = EditorGUILayout.Foldout(showPenetrationSettings, "Penetration Settings", true, foldoutStyle);
            if (showPenetrationSettings)
            {
                EditorGUILayout.PropertyField(PenetrateableLayers);
                EditorGUILayout.PropertyField(Penetration);
                EditorGUILayout.PropertyField(DragPenetrationThreshold);
                EditorGUILayout.PropertyField(MaxNumPenetrationObjects);
                CustomEditorTools.DrawUILine(Color.gray);
            }

            //Lifetime and physics bounds
            showLifetimeAndBoundsSettings = EditorGUILayout.Foldout(showLifetimeAndBoundsSettings, "Lifetime and Bounding Settings", true, foldoutStyle);
            if (showLifetimeAndBoundsSettings)
            {
                EditorGUILayout.PropertyField(Lifetime);
                EditorGUILayout.PropertyField(MinimumSpeed);
                EditorGUILayout.PropertyField(PhysicsLimitUpper);
                EditorGUILayout.PropertyField(PhysicsLimitLower);
                CustomEditorTools.DrawUILine(Color.gray);
            }

            //Seeking
            showSeekingSettings = EditorGUILayout.Foldout(showSeekingSettings, "Seeking Settings", true, foldoutStyle);
            if (showSeekingSettings)
            {
                MBSAnimationCurve initalSeekVal = SeekTurnAmount.GetValue<MBSAnimationCurve>();
                MBSAnimationCurve seekVal = CustomEditorTools.DrawMBSAnimationCurve("SeekTurnAmount", initalSeekVal, serializedObject, "The degrees per second the projectile can turn to orient towards a target");
                SeekTurnAmount.SetValue(seekVal);
                if (serializedObject.targetObjects.Length > 1)
                    EditorGUILayout.LabelField("Multi Editing animation curves is not supported! \n You are currently only editing the topmost selected object!", GUILayout.Height(30));

                EditorGUILayout.PropertyField(SeekingOverridesPaths);
                EditorGUILayout.PropertyField(EvaluteSeekCurveWithDistance);
                CustomEditorTools.DrawUILine(Color.gray);
            }

            //Ricochet
            showRicochetSettings = EditorGUILayout.Foldout(showRicochetSettings, "Ricochet Settings", true, foldoutStyle);
            if (showRicochetSettings)
            {
                EditorGUILayout.PropertyField(RicochetableLayers);
                EditorGUILayout.PropertyField(Ricochets);
                EditorGUILayout.PropertyField(RicochetSpeedLossPercent);
                EditorGUILayout.PropertyField(RicochetSpeedLossBasedOnHitAngle);
                EditorGUILayout.PropertyField(RicochetMinimumSpeedLossPercent);
                EditorGUILayout.PropertyField(RicochetMaximumSpeedLossPercent);
                EditorGUILayout.PropertyField(RicochetAngleVariability);
                EditorGUILayout.PropertyField(RicochetSeekTurnAmount);
                EditorGUILayout.PropertyField(RicochetSeekInterval);
                EditorGUILayout.PropertyField(FirstBounceSeeks);
                CustomEditorTools.DrawUILine(Color.gray);
            }

            //Stages
            showStageSettings = EditorGUILayout.Foldout(showStageSettings, "Stage Settings", true, foldoutStyle);
            if (showStageSettings)
            {
                EditorGUILayout.PropertyField(ProjectileStages);
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}
