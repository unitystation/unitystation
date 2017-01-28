using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Matrix;

//[CustomEditor(typeof(RegisterTile))]
//[CanEditMultipleObjects]
public class RegisterTileEditor: Editor {

    //private string[] choices;
    //private int choiceIndex = 0;

    //public override void OnInspectorGUI() {
    //    if(choices == null) {
    //        choices = new string[TileType.Order.Count];
    //        for(int i = 0; i < choices.Length; i++) {
    //            choices[i] = TileType.Order[i].Name;
    //        }
    //    }

    //    DrawDefaultInspector();

    //    choiceIndex = EditorGUILayout.Popup(choiceIndex, choices);

    //    var registerTile = (RegisterTile) target;
    //    registerTile.tileType = TileType.Order[choiceIndex];

    //    EditorUtility.SetDirty(target);
    //}
}