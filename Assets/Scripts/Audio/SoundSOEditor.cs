#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditorInternal;

[CustomEditor(typeof(SoundSO))]
public class SoundSOEditor : Editor
{
    private ReorderableList _list;
    private SerializedProperty _soundsProp;
    private void OnEnable()
    {
        SoundSO so = (SoundSO)target;
        ref SoundList[] list = ref so.sounds;
        if (list == null)
        {
            return;
        }

        Dictionary<SoundType, SoundList> oldLookup = new Dictionary<SoundType, SoundList>();
        foreach (SoundList entry in list)
        {
            oldLookup[entry.type] = entry;
        }

        int enumCount = Enum.GetValues(typeof(SoundType)).Length;
        Array.Resize(ref list, enumCount);

        for (int i = 0; i < enumCount; i++)
        {
            SoundType key = (SoundType)i;
            if (oldLookup.TryGetValue(key, out var old))
            {
                list[i] = new SoundList
                {
                    type = key,
                    volume = old.volume,
                    sounds = old.sounds,
                    mixer = old.mixer
                };
            }
            else
            {
                list[i] = new SoundList
                {
                    type = key,
                    volume = 1f,
                    sounds = Array.Empty<AudioClip>(),
                    mixer = null
                };
            }
        }

        EditorUtility.SetDirty(target);

        _soundsProp = serializedObject.FindProperty("sounds");
        _list = new ReorderableList(
            serializedObject, _soundsProp,
            draggable: false,
            displayHeader: true,
            displayAddButton: false,
            displayRemoveButton: false
        );

        _list.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, "Sounds");

        _list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = _soundsProp.GetArrayElementAtIndex(index);
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float y = rect.y + 2;

            element.FindPropertyRelative("type").enumValueIndex = index;
            string label = ((SoundType)index).ToString();

            SerializedProperty isExpandedProp = element.FindPropertyRelative("_editorExpanded");

            EditorGUI.BeginProperty(rect, GUIContent.none, element);
            element.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, y, rect.width, lineHeight),
                element.isExpanded,
                new GUIContent(label),
                true
            );

            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;
                y += lineHeight + 2;
                SerializedProperty volumeProp = element.FindPropertyRelative("volume");
                SerializedProperty mixerProp = element.FindPropertyRelative("mixer");
                SerializedProperty clipsProp = element.FindPropertyRelative("sounds");

                EditorGUI.PropertyField(
                    new Rect(rect.x, y, rect.width, lineHeight),
                    volumeProp);
                y += lineHeight + 2;
                EditorGUI.PropertyField(
                    new Rect(rect.x, y, rect.width, lineHeight),
                    mixerProp);
                y += lineHeight + 2;
                EditorGUI.PropertyField(
                    new Rect(rect.x, y, rect.width, EditorGUI.GetPropertyHeight(clipsProp)),
                    clipsProp);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        };

        _list.elementHeightCallback = index =>
        {
            var element = _soundsProp.GetArrayElementAtIndex(index);
            float height = EditorGUIUtility.singleLineHeight + 4;
            if (element.isExpanded)
            {
                var clipsProp = element.FindPropertyRelative("sounds");
                height += (EditorGUIUtility.singleLineHeight + 2) * 2;
                height += EditorGUI.GetPropertyHeight(clipsProp) + 2;
            }
            return height;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (_list != null)
            _list.DoLayoutList();
        else
            EditorGUILayout.HelpBox("Sounds array not initialized.", MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif