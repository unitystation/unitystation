using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapEditor
{

    public class OptionsView : AbstractView
    {

        public OptionsView()
        {
            PreviewObject.ShowPreview = true;
            BuildControl.CheckTileFit = true;
        }

        public override void OnGUI()
        {
            InputControl.AllowMouse = EditorGUILayout.Toggle("Create On Mouse Click", InputControl.AllowMouse);
            PreviewObject.ShowPreview = EditorGUILayout.Toggle("Enable Preview", PreviewObject.ShowPreview);
            BuildControl.CheckTileFit = EditorGUILayout.Toggle("Check Tile Fit", BuildControl.CheckTileFit);

            InputControl.RotateOptA = EditorGUILayout.Toggle("Use Rotate Keys: z and x", InputControl.RotateOptA);
            InputControl.RotateOptB = EditorGUILayout.Toggle("Use Rotate Keys: < and >", InputControl.RotateOptB);

        }
    }
}