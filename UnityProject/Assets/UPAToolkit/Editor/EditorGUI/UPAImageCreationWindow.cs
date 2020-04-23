//-----------------------------------------------------------------
// This script handles the Image Creation Window where you can add new UPAImages.
// It draws the proper editor GUI and hosts methods for instantiating images which can be edited
// in the UPAEditorWindow. The images created here can also be exported using the UPAExportWindow.
//-----------------------------------------------------------------

using UnityEngine;
using UnityEditor;

public class UPAImageCreationWindow : EditorWindow {
	
	public static UPAImageCreationWindow window;
	
	private int xRes = 32, yRes = 32;
	
	[MenuItem ("Assets/Create/UPA Image")]
	public static void Init () {
		// Get existing open window or if none, make new one
		window = (UPAImageCreationWindow)EditorWindow.GetWindow (typeof (UPAImageCreationWindow));
		#if UNITY_4_3
		window.title = "New Image";
		#elif UNITY_4_6
		window.title = "New Image";
		#else
		window.titleContent = new GUIContent ("New Image");
		#endif
		window.position = new Rect(Screen.width/2 + 250/2f,Screen.height/2 - 80, 250, 105);
		window.ShowPopup();
	}
	
	void OnGUI () {
		if (window == null)
			Init ();
		
		GUILayout.Label ("UPA Image Settings", EditorStyles.boldLabel);
		
		xRes = Mathf.Clamp (EditorGUILayout.IntField ("Width: ", xRes), 1, 256 );
		yRes = Mathf.Clamp (EditorGUILayout.IntField ("Height: ", yRes), 1, 256 );
		
		EditorGUILayout.Space ();
		
		if ( GUILayout.Button ("Create", GUILayout.Height (30))) {
			this.Close();
			UPASession.CreateImage (xRes, yRes);
		}
	}
}