using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    public class VLSLayerList
    {
        private static GUIContent
            moveUpBtn = new GUIContent("\u25B2", "Moves Layer Up"),
            moveDnBtn = new GUIContent("\u25BC", "Moves Layer Down"),
            dupeBtn = new GUIContent("+", "Duplicates Layer"),
            deleteBtn = new GUIContent("\u2717", "Deletes Layer");
            //addBtn = new GUIContent("New", "Add New Pass");

        public static void Show(SerializedProperty list, string _layerTitle)
        {
            //EditorGUILayout.PropertyField(list);

            EditorGUILayout.PrefixLabel(_layerTitle);

            EditorGUI.indentLevel++;
            {
                if (list.isExpanded)
                {
                    if (list.arraySize == 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Create Layer"))
                                AddNewArrayElement(list, 0);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < list.arraySize; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.Space(16);
                                
                                GUILayout.Label(list.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);

                                if (ShowButtons(list, i))
                                    break;
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
                        }
                        EditorGUI.indentLevel--;
                    }

                    //for (int i = 0; i < list.arraySize; i++)
                    //{
                    //    EditorGUILayout.BeginHorizontal();
                    //    {
                    //        EditorGUILayout.PrefixLabel(string.Concat("Layer ", (i + 1).ToString()));

                    //        GUILayout.FlexibleSpace();

                    //        if (ShowButtons(list, i))
                    //            break;
                    //    }
                    //    EditorGUILayout.EndHorizontal();

                    //    GUILayout.Space(-18);
                    //    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);

                    //    EditorGUILayout.Space();
                    //}
                }
            }
            EditorGUI.indentLevel--;
        }

        private static bool ShowButtons(SerializedProperty _list, int _index)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(moveUpBtn, EditorStyles.miniButton))
                {
                    _list.MoveArrayElement(_index, _index - 1);
                    return true;
                }

                if (GUILayout.Button(moveDnBtn, EditorStyles.miniButton))
                {
                    _list.MoveArrayElement(_index, _index + 1);
                    return true;
                }

                if (GUILayout.Button(deleteBtn, EditorStyles.miniButton))
                {
                    _list.DeleteArrayElementAtIndex(_index);
                    return true;
                }

                if (GUILayout.Button(dupeBtn, EditorStyles.miniButton))
                {
                    AddNewArrayElement(_list, _index);
                    return true;
                }
            }
            EditorGUILayout.EndHorizontal();

            return false;
        }

        private static void AddNewArrayElement(SerializedProperty _list, int _index)
        {
            _list.InsertArrayElementAtIndex(_index);

            SerializedProperty item = _list.GetArrayElementAtIndex((_index + 1) % _list.arraySize);

            SerializedProperty name = item.FindPropertyRelative("name");
            name.stringValue = string.Concat("Layer ", _list.arraySize.ToString());

            if (item.FindPropertyRelative("lightLayerMask") != null)
                item.FindPropertyRelative("lightLayerMask").intValue = 1;

            if (item.FindPropertyRelative("lightIntensity") != null)
                item.FindPropertyRelative("lightIntensity").floatValue = 1;

            if(item.FindPropertyRelative("ambientColor") != null)
                item.FindPropertyRelative("ambientColor").colorValue = new Color(0.2f, 0.2f, 0.235f, 1f);

            if (item.FindPropertyRelative("clearFlag") != null)
                item.FindPropertyRelative("clearFlag").intValue = (int)CameraClearFlags.Color;

            if (item.FindPropertyRelative("backgroundColor") != null)
                item.FindPropertyRelative("backgroundColor").colorValue = new Color(0.5f, 0.5f, 0.5f, 0f);

            if (item.FindPropertyRelative("lightsEnabled") != null)
                item.FindPropertyRelative("lightsEnabled").boolValue = true;
        }
    }
}