using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Matrix
{

    [CustomEditor(typeof(RegisterTile))]
    [CanEditMultipleObjects]
    public class RegisterTileEditor : Editor
    {

        private static string[] tileTypeNames;

        static RegisterTileEditor()
        {
            List<string> list = new List<string>();
            foreach (var tileType in TileType.List)
            {
                list.Add(tileType.Name);
            }
            tileTypeNames = list.ToArray();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var registerTile = target as RegisterTile;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Tile Type");
            registerTile.tileTypeIndex = EditorGUILayout.Popup(registerTile.tileTypeIndex, tileTypeNames);
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Object obj in targets)
                {
                    ((RegisterTile)obj).tileTypeIndex = registerTile.tileTypeIndex;
                }
            }
        }
    }
}