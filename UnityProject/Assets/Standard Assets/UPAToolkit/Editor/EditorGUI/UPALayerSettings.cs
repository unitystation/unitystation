//-----------------------------------------------------------------
// This script handles the layer settings window.
// At the moment all changes are aplied instantly. Maybe add a
// preview and make changes cancelable
//-----------------------------------------------------------------

using UnityEngine;
using UnityEditor;

public class UPALayerSettings : EditorWindow {
	
	public static UPALayerSettings window;
	
	public UPALayer layer;
	
	private new string name;
	
	public static void Init (UPALayer layer) {
		// Get existing open window or if none, make new one
		window = (UPALayerSettings)EditorWindow.GetWindow (typeof (UPALayerSettings));
		#if UNITY_4_3
		window.title = layer.name + " - Settings";
		#elif UNITY_4_6
		window.title = layer.name + " - Settings";
		#else
		window.titleContent = new GUIContent (layer.name + " - Settings");
		#endif
		
		window.position = new Rect(Screen.width/2 + 260/2f,Screen.height/2 - 80, 360, 170);
		window.ShowPopup();
		
		window.layer = layer;
	}
	
	void OnGUI () {
		// Edit name and visibility
		GUILayout.Label ("General", EditorStyles.boldLabel);
		layer.name = EditorGUILayout.TextField ("Name: ", layer.name);
		layer.enabled = EditorGUILayout.Toggle ("Enabled: ", layer.enabled);
		//exportImg = (UPAImage)EditorGUILayout.ObjectField (exportImg, typeof(UPAImage), false);
		
		// Edit blend mode and opacity
		GUILayout.Label ("Blending", EditorStyles.boldLabel);
		layer.mode = (UPALayer.BlendMode) EditorGUILayout.EnumPopup ("Mode: ", layer.mode);
		if (layer.mode != UPALayer.BlendMode.NORMAL)
		{
			GUILayout.Label("Some blend modes are still in testing and might not produce\nentirely accurate results.");
		}
		layer.opacity = EditorGUILayout.IntSlider ("Opacity: ", Mathf.RoundToInt(layer.opacity * 100), 0, 100) / 100f;
	}
}
