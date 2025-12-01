#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyStats))]
public class EnemyStatsEditor : Editor
{
    SerializedProperty groupProp;
    SerializedProperty typeProp;

    private void OnEnable()
    {
        groupProp = serializedObject.FindProperty("GroupType");
        typeProp = serializedObject.FindProperty("Type");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(groupProp);
        EnemyGroupType group = (EnemyGroupType)groupProp.enumValueIndex;

        EnemyType[] allowed = GetAllowedTypes(group);
        string[] optionNames = new string[allowed.Length];
        int currentIndex = 0;
        for (int i = 0; i < allowed.Length; i++)
        {
            optionNames[i] = allowed[i].ToString();
            if ((EnemyType)typeProp.enumValueIndex == allowed[i])
            {
                currentIndex = i;
            }
        }

        int newIndex = EditorGUILayout.Popup("Type", currentIndex, optionNames);
        EnemyType newType = allowed[newIndex];
        typeProp.enumValueIndex = (int)newType;

        DrawDefaultInspectorExcept(new string[] { "GroupType", "Type" });

        serializedObject.ApplyModifiedProperties();
    }

    private EnemyType[] GetAllowedTypes(EnemyGroupType group)
    {
        switch (group)
        {
            case EnemyGroupType.Ground:
                return new EnemyType[] { EnemyType.Skeleton, EnemyType.Imp, EnemyType.Custom };
            case EnemyGroupType.Air:
                return new EnemyType[] { EnemyType.Dragon, EnemyType.Custom };
            default:
                return new EnemyType[] { EnemyType.Custom };
        }
    }

    private void DrawDefaultInspectorExcept(string[] exclude)
    {
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            bool skip = false;
            for (int i = 0; i < exclude.Length; i++)
            {
                if (prop.name == exclude[i])
                {
                    skip = true;
                    break;
                }
            }
            if (!skip)
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }
    }
}
#endif
