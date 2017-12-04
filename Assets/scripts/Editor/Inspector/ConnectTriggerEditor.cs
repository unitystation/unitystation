using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MatrixOld
{

    [CustomEditor(typeof(ConnectTrigger))]
    public class ConnectTriggerEditor : Editor
    {
        private static string[] connectTypeNames;

        static ConnectTriggerEditor()
        {
            List<string> list = new List<string>();
            foreach (var tileConnectType in ConnectType.List)
            {
                list.Add(tileConnectType.Name);
            }
            connectTypeNames = list.ToArray();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var connectTrigger = target as ConnectTrigger;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Connect Type");
            connectTrigger.connectTypeIndex = EditorGUILayout.Popup(connectTrigger.connectTypeIndex, connectTypeNames);
            EditorGUILayout.EndHorizontal();
        }
    }
}