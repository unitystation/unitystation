using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    [CustomEditor(typeof(VLSBehaviour))]
    public class VLSBehaviourEditor : Editor
    {
        private static float guiScale = 0.07f;

        private SerializedProperty vertices;
        private SerializedProperty edges;
        private List<int> selectedVertices = new List<int>();

        protected virtual void OnEnable()
        {
            vertices = serializedObject.FindProperty("localVertices");
            edges = serializedObject.FindProperty("edges");

            VLSBehaviour.SHOW_NORMALS = EditorPrefs.GetBool("SHOW_NORMALS", false);
        }

        protected virtual void OnDisable()
        {
            EditorPrefs.SetBool("SHOW_NORMALS", VLSBehaviour.SHOW_NORMALS);
			Tools.hidden = true;
        }

        protected void ShowEdgeInspector()
        {
            serializedObject.Update();
            {
                EditorGUILayout.Space();

                GUILayout.Label("Instructions", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Hold 'control' and click edge to add a new point.\nHold 'shift' to select multiple points.\nSelect point & press 'backspace' to delete.", MessageType.Info);

                EditorGUILayout.Space();

                GUILayout.Label("Stats", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(VLSObstructor.SHOW_NORMALS ? "Show Normals: True" : "Show Normals: False " + "\nEdges: " + serializedObject.FindProperty("edges").arraySize, MessageType.None);

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button((VLSObstructor.SHOW_NORMALS) ? "Hide Norms" : "Show Norms", EditorStyles.miniButtonLeft))
                    {
                        VLSObstructor.SHOW_NORMALS = !VLSObstructor.SHOW_NORMALS;
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    }

                    if (GUILayout.Button("Inv. Norms", EditorStyles.miniButtonLeft))
                    {
                        (serializedObject.targetObject as VLSBehaviour).ReverseNormals();
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    }                    
                }
                GUILayout.EndHorizontal();
            }
            serializedObject.ApplyModifiedProperties();
        }

        protected void DrawEdgeEditor()
        {
            Vector3 positionAverage = Vector2.zero;
            VLSBehaviour obst = (serializedObject.targetObject as VLSBehaviour);
            Transform transform = obst.gameObject.transform;
            float guiSize = HandleUtility.GetHandleSize(transform.position) * guiScale;

            Event e = Event.current;
            Vector3 mousePosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;

            Handles.DrawSolidDisc(obst.transform.position, -Vector3.forward, guiSize);
            if (Handles.Button(obst.transform.position, Quaternion.identity, guiSize * 1.5f, guiSize * 2, Handles.CircleCap))
            {
                selectedVertices.Clear();
                Tools.hidden = false;
                //Tools.current = prevTool;
            }

            serializedObject.Update();
            {
                if (e.control)
                {
                    Vector3 vPos = new Vector3();
                    int index = GetClosestEdgePointToPosition(mousePosition, guiSize * 2, ref vPos);
                    if (index >= 0)
                    {
                        if (Handles.Button(vPos, Quaternion.identity, guiSize, guiSize * 2, Handles.CircleCap))
                        {
                            obst.InsertLocalVertex(index, obst.transform.InverseTransformPoint(vPos));

                            selectedVertices.Clear();
                            selectedVertices.Add(index);

                            GUI.changed = true;
                            Tools.hidden = true;
                        }
                    }
                }

                for (int i = 0; i < vertices.arraySize; i++)
                {
                    Vector3 position = vertices.GetArrayElementAtIndex(i).vector3Value;
                    bool isSelected = selectedVertices.Contains(i);

                    Handles.color = (isSelected) ? Color.green : Color.gray;
                    Handles.DrawSolidDisc(transform.TransformPoint(position), Vector3.forward, guiSize);

                    if (isSelected)
                    {
                        positionAverage += obst.LocalVertex(i);
                    }

                    if (Handles.Button(transform.TransformPoint(position), Quaternion.identity, guiSize, guiSize, Handles.CircleCap))
                    {
                        if (e.shift)
                        {
                            if (selectedVertices.Contains(i))
                                selectedVertices.Remove(i);
                            else
                                selectedVertices.Add(i);
                        }
                        else
                        {
                            selectedVertices.Clear();
                            selectedVertices.Add(i);
                        }

                        Tools.hidden = true;
                    }
                }

                if (selectedVertices.Count > 0)
                {
                    positionAverage /= selectedVertices.Count;

                    Vector3 tPos = transform.TransformPoint(positionAverage);
                    Vector3 delta = Handles.PositionHandle(tPos, (Tools.pivotRotation == PivotRotation.Global) ? Quaternion.identity : obst.transform.rotation) - tPos;

                    for (int i = 0; i < selectedVertices.Count; i++)
                        obst.LocalVertex(selectedVertices[i], obst.LocalVertex(selectedVertices[i]) + transform.InverseTransformVector(delta));

                    if (e.isKey && (e.keyCode == KeyCode.Backspace))
                    {
                        for (int i = 0; i < selectedVertices.Count; i++)
                            obst.RemoveLocalVertex(selectedVertices[i]);

                        selectedVertices.Clear();
                        GUI.changed = true;
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        int GetClosestEdgePointToPosition(Vector3 _position, float _pickSize, ref Vector3 _outVector)
        {
            Vector3 pnt = new Vector3();
            float pDist = Mathf.Infinity;
            int index = -1;

            for (int i = 0; i < edges.arraySize; i++)
            {
                Vector2 pntA = edges.GetArrayElementAtIndex(i).FindPropertyRelative("pointA").FindPropertyRelative("position").vector3Value;
                Vector2 pntB = edges.GetArrayElementAtIndex(i).FindPropertyRelative("pointB").FindPropertyRelative("position").vector3Value;
                Vector2 norm = edges.GetArrayElementAtIndex(i).FindPropertyRelative("normal").vector3Value;

                if (VLSUtility.LineIntersects(_position - (Vector3)(norm * _pickSize), _position + (Vector3)(norm * _pickSize), pntA, pntB, ref pnt))
                {
                    if (Vector3.SqrMagnitude(pnt - _position) < pDist)
                    {
                        pDist = Vector3.SqrMagnitude(pnt - _position);
                        _outVector = pnt;
                        index = i;
                    }
                }
            }

            return (index + 1) % edges.arraySize;
        }
    }
}