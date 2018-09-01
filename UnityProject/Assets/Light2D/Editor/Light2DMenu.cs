using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Light2D
{
    public static class Light2DMenu
    {
        [MenuItem("GameObject/Light2D/Lighting System", false, 6)]
        public static void CreateLightingSystem()
        {
            LightingSystemCreationWindow.CreateWindow();
        }



        [MenuItem("GameObject/Light2D/Light Source", false, 6)]
        public static void CreateLightSource()
        {
            var obj = new GameObject("Light");
            if (LightingSystem.Instance != null)
                obj.layer = LightingSystem.Instance.LightSourcesLayer;
            var light = obj.AddComponent<LightSprite>();
            light.Material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Light2D/Materials/Light60Points.mat");
            light.Sprite = Resources.Load<Sprite>("DefaultLight");
            light.Color = new Color(1, 1, 1, 0.5f);
            Selection.activeObject = obj;
        }

        [MenuItem("GameObject/Light2D/Enable 2DTK Support", false, 6)]
        public static void Enable2DToolkitSupport()
        {
            var targets = (BuildTargetGroup[]) Enum.GetValues(typeof (BuildTargetGroup));
            foreach (var target in targets)
                DefineSymbol("LIGHT2D_2DTK", target);
        }

        [MenuItem("GameObject/Light2D/Disable 2DTK Support", false, 6)]
        public static void Disable2DToolkitSupport()
        {
            var targets = (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup));
            foreach (var target in targets)
                UndefineSymbol("LIGHT2D_2DTK", target);
        }

        public static void DefineSymbol(string symbol, BuildTargetGroup target)
        {
            UndefineSymbol(symbol, target);

            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            if (!defines.EndsWith(";"))
                defines += ";";
            defines += symbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
        }

        public static void UndefineSymbol(string symbol, BuildTargetGroup target)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            defines = defines.Replace(symbol + ";", "");
            defines = defines.Replace(";" + symbol, "");
            defines = defines.Replace(symbol, "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
        }
    }
}
