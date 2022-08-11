using MBS.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CustomEditor(typeof(ProjectileEmitter), true)]
    [CanEditMultipleObjects]
    public class ProjectileEmitterCustomInspector : Editor
    {
        //General
        SerializedProperty TargetLayers;
        SerializedProperty AtmosphereData;
        SerializedProperty ProjectileSO;
        SerializedProperty ProjectileInheritsEmitterVelocity;
        SerializedProperty Origin;
        SerializedProperty interpolateProjectilePosition;
        SerializedProperty FXScene;
        SerializedProperty AudioScene;
        //Debug
        SerializedProperty DrawDebugLine;
        SerializedProperty DebugInAirColor;
        SerializedProperty DebugInObjectColor;
        //Local Timescale and Gravity
        SerializedProperty LocalTimescaleValue;
        SerializedProperty LocalGravityValue;

        protected static GUIStyle headerStyle;

        CustomEditorChildClassData childClassData;

        protected void OnEnable()
        {
            TargetLayers = serializedObject.FindProperty("TargetLayers");
            AtmosphereData = serializedObject.FindProperty("AtmosphereData");
            ProjectileSO = serializedObject.FindProperty("ProjectileSO");
            ProjectileInheritsEmitterVelocity = serializedObject.FindProperty("ProjectileInheritsEmitterVelocity");
            Origin = serializedObject.FindProperty("Origin");
            interpolateProjectilePosition = serializedObject.FindProperty("interpolateProjectilePosition");
            FXScene = serializedObject.FindProperty("FXScene");
            AudioScene = serializedObject.FindProperty("AudioScene");
            DrawDebugLine = serializedObject.FindProperty("DrawDebugLine");
            DebugInAirColor = serializedObject.FindProperty("DebugInAirColor");
            DebugInObjectColor = serializedObject.FindProperty("DebugInObjectColor");
            LocalTimescaleValue = serializedObject.FindProperty("_localTimeScale");
            LocalGravityValue = serializedObject.FindProperty("_localGravity");


            headerStyle = CustomEditorTools.QuickStyle(new GUIStyle(), 14, FontStyle.Bold, Color.grey);
            childClassData= CustomEditorTools.GetCustomEditorChildClassData(serializedObject, typeof(ProjectileEmitterCustomInspector), typeof(ProjectileEmitter), headerStyle);

        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("ProjectileEmitter", headerStyle);

            EditorGUILayout.PropertyField(ProjectileSO);
            EditorGUILayout.PropertyField(AtmosphereData);
            EditorGUILayout.PropertyField(TargetLayers);
            EditorGUILayout.PropertyField(Origin);
            EditorGUILayout.PropertyField(interpolateProjectilePosition, new GUIContent("Interpolate Position"));
            EditorGUILayout.PropertyField(ProjectileInheritsEmitterVelocity, new GUIContent("Inherit Emitter Velocity"));
            EditorGUILayout.PropertyField(LocalTimescaleValue);
            EditorGUILayout.PropertyField(LocalGravityValue);
            EditorGUILayout.PropertyField(FXScene);
            EditorGUILayout.PropertyField(AudioScene);

            EditorGUILayout.PropertyField(DrawDebugLine);
            if (DrawDebugLine.boolValue)
            {
                EditorGUILayout.PropertyField(DebugInAirColor);
                EditorGUILayout.PropertyField(DebugInObjectColor);
            }

            CustomEditorTools.DrawUILine(Color.gray);

            childClassData.Draw();

            serializedObject.ApplyModifiedProperties();
        }

        

    }
}
