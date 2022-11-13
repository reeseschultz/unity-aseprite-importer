using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace AsepriteImporter.Editors
{
    public class SpriteImporterEditor
    {
        protected const string SettingsPath = "settings.";
        protected const string TextureSettingsPath = "textureImporterSettings.";
        protected const string AnimationSettingsPath = "animationSettings.";

        AseFileImporterEditor baseEditor = default;

        protected readonly Dictionary<string, bool> foldoutStates = new();
        AseFileImporter importer = default;

        public AseFileImporter Importer => importer;
        protected AseFileImportType ImportType => baseEditor.ImportType;
        protected SerializedObject SerializedObject => baseEditor.serializedObject;

        internal void Enable(AseFileImporterEditor importerEditor)
        {
            foldoutStates.Clear();
            baseEditor = importerEditor;

            OnEnable();
        }

        internal void Disable()
            => OnDisable();

        internal void InspectorGUI()
        {
            importer = SerializedObject.targetObject as AseFileImporter;
            OnInspectorGUI();
        }

        protected void ApplyAndImport()
            => baseEditor.CallApplyAndImport();

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        protected virtual void OnInspectorGUI() { }

        protected bool CustomEnumPopup(string label, SerializedProperty property, Dictionary<int, string> mappings)
        {
            if (!mappings.ContainsKey(property.enumValueIndex))
            {
                Debug.LogWarning("AsepriteImporterEditor: Enum Mapping is missing key");
                property.enumValueIndex = 0;
            }

            var names = mappings.Values.ToArray();
            var indices = mappings.Keys.ToArray();

            var index = Array.IndexOf(indices, property.enumValueIndex);
            EditorGUI.BeginChangeCheck();

            var indexNew = EditorGUILayout.Popup(label, index, names);
            if (EditorGUI.EndChangeCheck())
            {
                property.enumValueIndex = indices[indexNew];
                return true;
            }

            return false;
        }
    }
}
