using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MBS.ProjectileSystem
{
    [CustomEditor(typeof(ProjectileTag))]
    public class ProjectileTagCustomInspector : Editor
    {
        SerializedProperty TagValue;
        protected void OnEnable()
        {
            TagValue = serializedObject.FindProperty("Tag");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (TagValue.stringValue == "")
                TagValue.stringValue = serializedObject.targetObject.name;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(TagValue);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
