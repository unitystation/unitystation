//-----------------------------------------------------------------
// This script handles the export window for turning an UPAImage into .jpg or .png format.
// It hosts functions for exporting & also draws the proper editor GUI.
// TODO: Add ExportImage method (Kind of crucial don't you think?).
//-----------------------------------------------------------------

using UnityEngine;
using UnityEditor;

public class UPAExportWindow : EditorWindow {
	
	public static UPAExportWindow window;
	
	public UPAImage exportImg;
	
	private TextureType texType = TextureType.sprite;
	private TextureExtension texExtension = TextureExtension.PNG;
	
	public static void Init (UPAImage img) {
		// Get existing open window or if none, make new one
		window = (UPAExportWindow)EditorWindow.GetWindow (typeof (UPAExportWindow));
		#if UNITY_4_3
		window.title = "Export Image";
		#elif UNITY_4_6
		window.title = "Export Image";
		#else
		window.titleContent = new GUIContent ("Export Image");
		#endif
		
		
		window.position = new Rect(Screen.width/2 + 260/2f,Screen.height/2 - 80, 260, 170);
		window.ShowPopup();
		
		window.exportImg = img;
	}
	
	void OnGUI () {
		GUILayout.Label ("Image to Export", EditorStyles.boldLabel);
		exportImg = (UPAImage)EditorGUILayout.ObjectField (exportImg, typeof(UPAImage), false);
		
		GUILayout.Label ("Export Settings", EditorStyles.boldLabel);
		texExtension = (TextureExtension)EditorGUILayout.EnumPopup("Save As:", texExtension);
		if (texExtension == TextureExtension.JPG) {
			#if UNITY_4_2
			GUILayout.Label ("Error: Export to JPG requires Unity 4.5+");
			#elif UNITY_4_3
			GUILayout.Label ("Error: Export to JPG requires Unity 4.5+");
			#endif
			
			GUILayout.Label ("Warning: JPG files will lose transparency.");
		}
		texType = (TextureType)EditorGUILayout.EnumPopup("Texture Type:", texType);
		
		EditorGUILayout.Space ();
		
		if ( GUILayout.Button ("Export", GUILayout.Height(30)) ) {
			
			if (exportImg == null) {
				EditorUtility.DisplayDialog(
					"Select Image",
					"You Must Select an Image first!",
					"Ok");
				return;
			}	
			
			bool succes = UPASession.ExportImage ( exportImg, texType, texExtension );
			if (succes)
				this.Close();
			UPAEditorWindow.window.Repaint();
		}
	}
}