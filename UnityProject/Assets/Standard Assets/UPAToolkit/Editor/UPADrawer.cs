using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using AssemblyCSharpEditor;
using System.IO;

public class UPADrawer : MonoBehaviour {
	
	private static UPAImage CurrentImg {
		get { return UPAEditorWindow.CurrentImg; }
		set { UPAEditorWindow.CurrentImg = value; }
	}


	// VISUAL SETTINGS
	
	private static Color32 toolbarColor = new Color32 (50, 50, 50, 255);
	
	private static string[] gridBGStrings = new string[] {"Black", "White", "None"};
	public static Color gridBGColor = Color.black;
	
	private static GUIStyle style = new GUIStyle();

	private static Texture2D removeLayerIcon;


	// GETTER METHODS
	public static Rect GetLayerPanelRect (Rect window) {
		return new Rect (12, window.height - CurrentImg.layers.Count * 36 - 30, 119, CurrentImg.layers.Count * 36 + 25);
	}


	// DRAWING METHODS

	// Draw an image inside the editor window
	public static void DrawImage (UPAImage img) {
		Rect texPos = img.GetImgRect();

		Texture2D bg = new Texture2D (1,1);
		bg.SetPixel (0,0, Color.clear);
		bg.Apply();
		EditorGUI.DrawTextureTransparent (texPos, bg);
		DestroyImmediate (bg);

		//Calculate the final image from the layers list
		Texture2D _result = CalculateBlendedTex(img.layers);

		//Draw the image
		_result.SetPixel(1, 1, Color.black);
		GUI.DrawTexture(texPos, _result);
	
		// Draw a grid above the image (y axis first)
		for (int x = 0; x <= img.width; x += 1) {
			float posX = texPos.xMin + ( (float)texPos.width / (float)img.width ) * x - 0.2f;
			EditorGUI.DrawRect (new Rect (posX, texPos.yMin, 1, texPos.height), gridBGColor);
		}
		// Then x axis
		for (int y = 0; y <= img.height; y += 1) {
			float posY = texPos.yMin + ( (float)texPos.height / (float)img.height ) * y - 0.2f;
			EditorGUI.DrawRect (new Rect (texPos.xMin, posY, texPos.width, 1), gridBGColor);
		}
	}

	// Calculates the blended image given a list of layers
	public static Texture2D CalculateBlendedTex(List<UPALayer> _layers)
	{
		if (_layers.Count > 0)
		{
			// Calculate blended image
			Texture2D _result = null;
			for (int i = 0; i < _layers.Count; i++)
			{
				if (!_layers[i].enabled)
					continue;

				if (_result == null)
				{
					_result = UPABlendModes.Blend(_layers[i].tex, _layers[i].opacity, _layers[i].tex, _layers[i].opacity, _layers[i].mode);
				}
				else
				{
					_result = UPABlendModes.Blend(_result, 1, _layers[i].tex, _layers[i].opacity, _layers[i].mode);
				}
			}
			if (_result == null)
			{
				Texture2D _empty = new Texture2D(1, 1);
				_empty.SetPixel(0, 0, Color.clear);
				_empty.Apply();
				return _empty;	
			}
			return _result;
		}
		else
		{
			Texture2D _empty = new Texture2D(1, 1);
			_empty.SetPixel(0, 0, Color.clear);
			_empty.Apply();
			return _empty;	
		}
	}

	// Draw the settings toolbar
	public static void DrawToolbar (Rect window, Vector2 mousePos) {

		// Draw toolbar bg
		EditorGUI.DrawRect ( new Rect (0,0, window.width, 40), toolbarColor );
		
		if ( GUI.Button (new Rect (5, 4, 50, 30), "New") ) {
			UPAImageCreationWindow.Init ();
		}
		if ( GUI.Button (new Rect (60, 4, 50, 30), "Open") ) {
			CurrentImg = UPASession.OpenImage ();
			if (CurrentImg == null)
				return;
		}
		if ( GUI.Button (new Rect (115, 4, 50, 30), "Export") ) {
			UPAExportWindow.Init(CurrentImg);
		}

		if (GUI.Button (new Rect (179, 6, 25, 25), "+")) {
			CurrentImg.gridSpacing *= 1.2f;
		}
		if (GUI.Button (new Rect (209, 6, 25, 25), "-")) {
			CurrentImg.gridSpacing *= 0.8f;
			CurrentImg.gridSpacing -= 2;
		}
		
		CurrentImg.selectedColor = EditorGUI.ColorField (new Rect (250, 7, 70, 25), CurrentImg.selectedColor);
		EditorGUI.DrawRect ( new Rect (303, 7, 20, 25), toolbarColor );
		//bgColor = EditorGUI.ColorField (new Rect (400, 4, 70, 25), bgColor);
		
		GUI.backgroundColor = Color.white;
		if (CurrentImg.tool == UPATool.PaintBrush)
			GUI.backgroundColor = new Color (0.7f, 0.7f, 0.7f);
		if (GUI.Button (new Rect (320, 4, 60, 30), "Paint")) {
			CurrentImg.tool = UPATool.PaintBrush;
		}
		GUI.backgroundColor = Color.white;
		if (CurrentImg.tool == UPATool.BoxBrush)
			GUI.backgroundColor = new Color (0.7f, 0.7f, 0.7f);
		if (GUI.Button (new Rect (450, 4, 60, 30), "Box Fill")) {
			EditorUtility.DisplayDialog(
				"In Development",
				"This feature is currently being developed.",
				"Get it done please");
			//tool = UPATool.BoxBrush;
		}
		GUI.backgroundColor = Color.white;
		if (CurrentImg.tool == UPATool.Eraser)
			GUI.backgroundColor = new Color (0.7f, 0.7f, 0.7f);
		if (GUI.Button (new Rect (385, 4, 60, 30), "Erase")) {
			CurrentImg.tool = UPATool.Eraser;
		}
		GUI.backgroundColor = Color.white;
		
		style.normal.textColor = new Color (0.7f, 0.7f, 0.7f);
		style.fontSize = 12;
		style.fontStyle = FontStyle.Normal;
		GUI.Label (new Rect (525, 11, 150, 30), "Use WASD to navigate.", style);
		
		if (GUI.Button (new Rect (684, 4, 55, 30), "Center")) {
			CurrentImg.gridOffsetX = 0;
			CurrentImg.gridOffsetY = 0;
		}
		
		CurrentImg.gridBGIndex = GUI.Toolbar (new Rect (743, 4, 130, 30), CurrentImg.gridBGIndex, gridBGStrings);
		
		if (CurrentImg.gridBGIndex == 0) {
			gridBGColor = Color.black;
		} else if (CurrentImg.gridBGIndex == 1) {
			gridBGColor = Color.white;
		} else {
			gridBGColor = Color.clear;
		}
		
		Vector2 pixelCoordinate = CurrentImg.GetReadablePixelCoordinate (mousePos);
		GUI.Label (new Rect (880, 11, 100, 30), "(" + (int)pixelCoordinate.x + "," + (int)pixelCoordinate.y + ")", style);

		if (CurrentImg.tool == UPATool.ColorPicker) {
			style.fontStyle = FontStyle.Bold;
			style.fontSize = 15;
			GUI.Label (new Rect (window.width/2f - 140, 60, 100, 30), "Click on a pixel to choose a color.", style);
		}
	}

	static void Callback (object obj) {
		Debug.Log ("Selected: " + obj);
	}
	static void Callback () {
		Debug.Log ("No object passed");
	}
	
	public static void DrawLayerPanel (Rect window) {
		
		style.imagePosition = ImagePosition.ImageAbove;
		
		int from = 0;
		int to = 0;

		if (CurrentImg == null)
			return;

		for (int i = 0; i < CurrentImg.layers.Count; i++) {
			GUI.backgroundColor = Color.white;
			if (i == CurrentImg.selectedLayer) {
				GUI.backgroundColor = new Color (0.7f, 0.7f, 0.7f);
			}

			UPALayer tempLayer = CurrentImg.layers[i];
			if (GUI.Button (new Rect (12, window.height - 60 - i * 36, 65, 33), "")) {
				CurrentImg.selectedLayer = i;
			}

			GUI.backgroundColor = Color.white;
			GUI.Label (new Rect (15, window.height - 52 - i * 36, 90, 30), tempLayer.name);

			bool layerEnabled = tempLayer.enabled;
			tempLayer.enabled = GUI.Toggle (new Rect (80, window.height - 61 - i * 36, 15, 15), tempLayer.enabled, "");
			if (tempLayer.enabled != layerEnabled)
				tempLayer.parentImg.dirty = true;

			if (removeLayerIcon == null)
				removeLayerIcon = (Texture2D)Resources.Load ("UI/CrossWhite");

			if (tempLayer.locked) {
				if (GUI.Button (new Rect (80, window.height - 43 - i * 36, 15, 15), Resources.Load("UI/locked") as Texture2D,style)) {
					tempLayer.locked = false;
				}
			} else {
				if (GUI.Button (new Rect (80, window.height - 43 - i * 36, 15, 15), Resources.Load("UI/unlocked") as Texture2D,style)) {
					tempLayer.locked = true;
				}
			}

			
			if (i + 1 < CurrentImg.layers.Count) {
				if (GUI.Button (new Rect (97, window.height - 60  - i * 36, 22, 16), "+")) {
					from = i;
					to = i + 1;
				}
			}
			
			if (i > 0) {
				if (GUI.Button (new Rect (97, window.height - 44 - i * 36, 22, 16), "-")) {
					from = i;
					to = i - 1;
				}
			}

			CurrentImg.layers[i] = tempLayer;
		}

		if (from != 0 || to != 0) {
			CurrentImg.ChangeLayerPosition (from, to);
		}

		GUIStyle smallButon = new GUIStyle();
		smallButon.fontSize = 8;
		smallButon.alignment = TextAnchor.MiddleCenter;
		smallButon.normal.background = Resources.Load ("Background") as Texture2D;

		if (GUI.Button (new Rect (12, window.height - 20, 18, 18), new GUIContent(Resources.Load("UI/add") as Texture, "Add Layer"), smallButon)) {
			CurrentImg.AddLayer ();
		}

		if (CurrentImg.layerCount == 1)
		{
			GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
		}
		if (GUI.Button(new Rect(12 + 20, window.height - 20, 18, 18), new GUIContent(Resources.Load("UI/delete") as Texture, "Delete Layer"), smallButon))
		{
			if (CurrentImg.layers.Count > 1) {
				bool delete = EditorUtility.DisplayDialog(
					"Delete Layer",
					"Do you want to remove " + CurrentImg.layers[CurrentImg.selectedLayer].name + "?",
					"Delete",
					"Cancel");
				
				if (delete) {
					CurrentImg.RemoveLayerAt(CurrentImg.selectedLayer);
				}
			}
		}
		GUI.contentColor = Color.white;

		if (GUI.Button(new Rect(12 + 20 * 2, window.height - 20, 18, 18), new GUIContent(Resources.Load("UI/import") as Texture, "Import Image"), smallButon))
		{
			string path = EditorUtility.OpenFilePanel(
				"Find an Image (.jpg | .png)",
				"/",
				"Image Files;*.jpg;*.png");
			
			if (path.Length != 0) {
				// Load Texture from file
				Texture2D tex = null;
				byte[] fileData;
				if (File.Exists(path))     {
					fileData = File.ReadAllBytes(path);
					tex = new Texture2D(2, 2);
					tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
				}
				// Create a new Image with textures dimensions
				CurrentImg.AddLayer();
				// Set pixel colors
				UPALayer layer = CurrentImg.layers[CurrentImg.layers.Count - 1];
				for (int x = 0; x < CurrentImg.width; x++) {
					for (int y = 0; y < CurrentImg.height; y++) {
						layer.map[x + y * CurrentImg.width] = tex.GetPixel(x, CurrentImg.height - 1 - y);
					}
				}
				layer.LoadTexFromMap();
			}
		}

		if (GUI.Button(new Rect(12 + 20 * 3, window.height - 20, 18, 18), new GUIContent(Resources.Load("UI/duplicate") as Texture, "Duplicate Layer"), smallButon))
		{
			CurrentImg.layers.Add(new UPALayer(CurrentImg.layers[CurrentImg.selectedLayer]));
		}

		if (CurrentImg.selectedLayer == 0)
		{
			GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
		}
		if (GUI.Button(new Rect(12 + 20 * 4, window.height - 20, 18, 18), new GUIContent(Resources.Load("UI/merge") as Texture, "Merge Layer Down"), smallButon))
		{
			if (CurrentImg.selectedLayer > 0)
			{
				UPALayer upper = CurrentImg.layers[CurrentImg.selectedLayer];
				UPALayer lower = CurrentImg.layers[CurrentImg.selectedLayer - 1];
				lower.tex = UPABlendModes.Blend(lower.tex, lower.opacity, upper.tex, upper.opacity, upper.mode);
				for (int x = 0; x < lower.tex.width; x++)
				{
					for (int y = 0; y < lower.tex.height; y++)
					{
						lower.map[x + y * lower.tex.width] = lower.tex.GetPixel(x, lower.tex.height - 1 - y);
					}
				}
				CurrentImg.RemoveLayerAt(CurrentImg.selectedLayer);
			}
		}
		GUI.contentColor = Color.white;

		//if (GUI.Button (new Rect (12 + 18 * 4, window.height - 18, 16, 16),  Resources.Load("UI/up") as Texture2D, style)) {
		//	CurrentImg.AddLayer ();
		//}

		//if (GUI.Button (new Rect (12 + 18 * 5, window.height - 18, 16, 16),  Resources.Load("UI/down") as Texture2D, style)) {
		//	CurrentImg.AddLayer ();
		//}

		if (GUI.Button(new Rect(12 + 20 * 5, window.height - 20, 18, 18), new GUIContent(Resources.Load("UI/edit") as Texture, "Layer Options"), smallButon))
		{
			UPALayerSettings.Init(CurrentImg.layers[CurrentImg.selectedLayer]);
		}

		// Draw layer button tooltips
		if (GUI.tooltip != "")
			GUI.Box(new Rect(12, window.height - 43, 120, 20), GUI.tooltip);

		//CurrentImg.selectedLayer = GUI.Toolbar (new Rect (4, window.height - 200, 90, 30), CurrentImg.selectedLayer, layerNames);
	}
	
}
