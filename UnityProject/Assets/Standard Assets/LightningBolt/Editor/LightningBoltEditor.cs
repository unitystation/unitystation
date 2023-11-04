using System;

using UnityEngine;
#if UNITY_EDITOR // Preprocessor line added (and corresponding #endif at the end): non-standard change.
using UnityEditor;

namespace DigitalRuby.LightningBolt
{
    [CustomEditor(typeof(LightningBoltScript))]
    public class LightningBoltEditor : Editor
    {
        private Texture2D logo;

        public override void OnInspectorGUI()
        {
            if (logo == null)
            {
                string[] guids = AssetDatabase.FindAssets("LightningBoltLogo");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    logo = AssetDatabase.LoadMainAssetAtPath(path) as Texture2D;
                    if (logo != null)
                    {
                        break;
                    }
                }
            }
            if (logo != null)
            {
                const float maxLogoWidth = 430.0f;
                EditorGUILayout.Separator();
                float w = EditorGUIUtility.currentViewWidth;
                Rect r = new Rect();
                r.width = Math.Min(w - 40.0f, maxLogoWidth);
                r.height = r.width / 2.7f;
                Rect r2 = GUILayoutUtility.GetRect(r.width, r.height);
                r.x = ((EditorGUIUtility.currentViewWidth - r.width) * 0.5f) - 4.0f;
                r.y = r2.y;
                GUI.DrawTexture(r, logo, ScaleMode.StretchToFill);
                EditorGUILayout.Separator();
            }

            DrawDefaultInspector();
        }
    }
}
#endif