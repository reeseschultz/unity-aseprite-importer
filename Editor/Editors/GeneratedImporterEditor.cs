using UnityEditor;
using UnityEngine;

namespace AsepriteImporter.Editors
{
    public class GeneratedImporterEditor : SpriteImporterEditor
    {
        readonly string[] spritePivotOptions =
        {
            "Center",
            "Top Left",
            "Top",
            "Top Right",
            "Left", "Right",
            "Bottom Left",
            "Bottom",
            "Bottom Right",
            "Custom"
        };

        bool customSpritePivot = default;

        protected override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Texture Options", EditorStyles.boldLabel);
            {
                ++EditorGUI.indentLevel;
                var transparencyMode = SerializedObject.FindProperty(SettingsPath + "transparencyMode");
                var transparentColor = SerializedObject.FindProperty(SettingsPath + "transparentColor");

                EditorGUILayout.PropertyField(transparencyMode);
                if (transparencyMode.intValue == (int)TransparencyMode.Mask)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(transparentColor);

                    if (GUILayout.Button("Reset")) transparentColor.colorValue = Color.magenta;

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.PropertyField(SerializedObject.FindProperty(SettingsPath + "pixelsPerUnit"));

                if (ImportType == AseFileImportType.Sprite)
                {
                    PivotPopup("Pivot");

                    EditorGUILayout.PropertyField(SerializedObject.FindProperty(SettingsPath + "tagDelimiter"));

                    // TODO: switch below to TextureSettings

                    EditorGUILayout.PropertyField(SerializedObject.FindProperty(TextureSettingsPath + "readable"));

                    var splitLayers = SerializedObject.FindProperty(SettingsPath + "splitLayers");

                    EditorGUILayout.PropertyField(splitLayers);

                    if (splitLayers.boolValue)
                    {
                        EditorGUILayout.PropertyField(SerializedObject.FindProperty(SettingsPath + "layerMergeOptions"));
                    }
                }

                --EditorGUI.indentLevel;
            }

            EditorGUILayout.Space();

            if (ImportType == AseFileImportType.Sprite)
            {
                EditorGUILayout.LabelField("Animation Options", EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;
                var bindTypeProperty = SerializedObject.FindProperty(SettingsPath + "bindType");
                var bindType = (AseAnimationBindType)bindTypeProperty.intValue;

                EditorGUI.BeginChangeCheck();
                bindType = (AseAnimationBindType)EditorGUILayout.EnumPopup("Bind Type", bindType);

                var animTypeProperty = SerializedObject.FindProperty(SettingsPath + "animType");
                var animType = (AseAnimatorType)animTypeProperty.intValue;
                animType = (AseAnimatorType)EditorGUILayout.EnumPopup("Animator Type", animType);

                if (animType == AseAnimatorType.AnimatorOverrideController)
                    EditorGUILayout.PropertyField(SerializedObject.FindProperty(SettingsPath + "baseAnimator"));

                EditorGUILayout.PropertyField(SerializedObject.FindProperty(SettingsPath + "buildAtlas"));

                if (EditorGUI.EndChangeCheck())
                {
                    bindTypeProperty.intValue = (int)bindType;
                    animTypeProperty.intValue = (int)animType;
                }

                --EditorGUI.indentLevel;
            }

            if (ImportType == AseFileImportType.Tileset)
            {
                EditorGUILayout.LabelField("Tileset Options", EditorStyles.boldLabel);
                {
                    ++EditorGUI.indentLevel;

                    EditorGUILayout.PropertyField(SerializedObject.FindProperty(SettingsPath + "tileSize"));
                    PivotPopup("Tile Pivot");
                    EditorGUILayout.PropertyField(SerializedObject.FindProperty(SettingsPath + "tileEmpty"),
                        new GUIContent("Empty Tile Behaviour",
                            "Behavior for empty tiles:\nKeep - Keep empty tiles\nIndex - Remove empty tiles, but still index them\nRemove - Remove empty tiles completely"));

                    // tileNameType
                    var tileNameTypeProperty = SerializedObject.FindProperty(SettingsPath + "tileNameType");
                    var tileNameType = (TileNameType)tileNameTypeProperty.enumValueIndex;

                    EditorGUI.BeginChangeCheck();
                    tileNameType = (TileNameType)EditorGUILayout.EnumPopup("TileNameType", tileNameType);

                    if (EditorGUI.EndChangeCheck())
                        tileNameTypeProperty.enumValueIndex = (int)tileNameType;

                    --EditorGUI.indentLevel;
                }
            }
        }

        void PivotPopup(string label)
        {
            var alignmentProperty = SerializedObject.FindProperty(SettingsPath + "spriteAlignment");
            var pivotProperty = SerializedObject.FindProperty(SettingsPath + "spritePivot");
            var pivot = pivotProperty.vector2Value;
            var alignment = alignmentProperty.intValue;

            EditorGUI.BeginChangeCheck();
            alignment = EditorGUILayout.Popup(label, alignment, spritePivotOptions);
            switch (alignment)
            {
                case 0:
                    customSpritePivot = false;
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case 1:
                    customSpritePivot = false;
                    pivot = new Vector2(0f, 1f);
                    break;
                case 2:
                    customSpritePivot = false;
                    pivot = new Vector2(0.5f, 1f);
                    break;
                case 3:
                    customSpritePivot = false;
                    pivot = new Vector2(1f, 1f);
                    break;
                case 4:
                    customSpritePivot = false;
                    pivot = new Vector2(0f, 0.5f);
                    break;
                case 5:
                    customSpritePivot = false;
                    pivot = new Vector2(1f, 0.5f);
                    break;
                case 6:
                    customSpritePivot = false;
                    pivot = new Vector2(0f, 0f);
                    break;
                case 7:
                    customSpritePivot = false;
                    pivot = new Vector2(0.5f, 0f);
                    break;
                case 8:
                    customSpritePivot = false;
                    pivot = new Vector2(1f, 0f);
                    break;
                default:
                    customSpritePivot = true;
                    break;
            }

            alignmentProperty.intValue = alignment;

            if (customSpritePivot)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(SerializedObject.FindProperty(SettingsPath + "spritePivot"),
                    new GUIContent(label));
                --EditorGUI.indentLevel;
            }
            else if (EditorGUI.EndChangeCheck() && !customSpritePivot)
            {
                pivotProperty.vector2Value = pivot;
            }
        }
    }
}
