//-----------------------------------------------------------------
// This script handles the main Pixel Art Editor.
// It selects tools, finds the right pixels to color, handles input events & draws the toolbar gui.
// TODO: Tidy things up. Split functionality into smaller code portions. Make even a bit of optimization?
//-----------------------------------------------------------------

using UnityEngine;
using UnityEditor;

public class UPAEditorWindow : EditorWindow {
	
	public static UPAEditorWindow window;	// The static instance of the window
	
	public static UPAImage CurrentImg;		// The img currently being edited
	
	
	// HELPFUL GETTERS AND SETTERS
	
	private float gridSpacing {
		get { return CurrentImg.gridSpacing; }
		set { CurrentImg.gridSpacing = value; }
	}
	private float gridOffsetX {
		get { return CurrentImg.gridOffsetX; }
		set { CurrentImg.gridOffsetX = value; }
	}
	private float gridOffsetY {
		get { return CurrentImg.gridOffsetY; }
		set { CurrentImg.gridOffsetY = value; }
	}
	
	private UPATool tool {
		get { return CurrentImg.tool; }
		set { CurrentImg.tool = value; }
	}
	private Color32 selectedColor {
		get { return CurrentImg.selectedColor; }
		set { CurrentImg.selectedColor = value; }
	}
	private int gridBGIndex {
		get { return CurrentImg.gridBGIndex; }
		set { CurrentImg.gridBGIndex = value; }
	}
	
	
	// MISC TEMP VARIABLES
	
	// Stores the previous tool when temporarily switching
	private static UPATool lastTool = UPATool.Empty;
	
	
	// INITIALIZATION
	
	[MenuItem ("Window/Pixel Art Editor %#p")]
	public static void Init () {
		// Get existing open window or if none, make new one
		window = (UPAEditorWindow)EditorWindow.GetWindow (typeof (UPAEditorWindow));
		#if UNITY_4_3
		window.title = "Pixel Art Editor";
		#elif UNITY_4_6
		window.title = "Pixel Art Editor";
		#else
		window.titleContent = new GUIContent ("Pixel Art Editor");
		#endif
		
		string path = EditorPrefs.GetString ("currentImgPath", "");
		
		if (path.Length != 0)
			CurrentImg = UPASession.OpenImageAtPath (path);
	}
	
	
	// Draw the Pixel Art Editor.
	// This includes both toolbar and painting area.
	// TODO: Add comments
	void OnGUI () {
		if (window == null)
			Init ();
		
		if (CurrentImg == null) { 
			
			string curImgPath = EditorPrefs.GetString ("currentImgPath", "");
			
			if (curImgPath.Length != 0) {
				CurrentImg = UPASession.OpenImageAtPath (curImgPath);
				return;
			}
			
			if ( GUI.Button (new Rect (window.position.width / 2f - 140, window.position.height /2f - 25, 130, 50), "New Image") ) {
				UPAImageCreationWindow.Init ();
			}
			if ( GUI.Button (new Rect (window.position.width / 2f + 10, window.position.height /2f - 25, 130, 50), "Open Image") ) {
				CurrentImg = UPASession.OpenImage ();
				return;
			}
			
			return;
		}
		
		// Init the textures correctly, won't cost performance if nothing to load
		CurrentImg.LoadAllTexsFromMaps();
		
		EditorGUI.DrawRect (window.position, new Color32 (30,30,30,255));
		
		
		#region Event handling
		Event e = Event.current;	//Init event handler
		
		//Capture mouse position
		Vector2 mousePos = e.mousePosition;
		
		// If key is pressed
		if (e.button == 0) {
			
			// Mouse buttons
			if (e.isMouse && mousePos.y > 40 && e.type != EventType.MouseUp) {
				if (!UPADrawer.GetLayerPanelRect (window.position).Contains (mousePos)) {
					
					if (tool == UPATool.Eraser)
						CurrentImg.SetPixelByPos (Color.clear, mousePos, CurrentImg.selectedLayer);
					else if (tool == UPATool.PaintBrush)
						CurrentImg.SetPixelByPos (selectedColor, mousePos, CurrentImg.selectedLayer);
					else if (tool == UPATool.BoxBrush)
						Debug.Log ("TODO: Add Box Brush tool.");
					else if (tool == UPATool.ColorPicker){
						Vector2 pCoord = CurrentImg.GetPixelCoordinate (mousePos);
						Color? newColor = CurrentImg.GetBlendedPixel( (int)pCoord.x, (int)pCoord.y );
						if (newColor != null && newColor != Color.clear){
							selectedColor = (Color)newColor;
						}
						tool = lastTool;
					}
					
				}
			}
			
			// Key down
			if (e.type == EventType.KeyDown) {
				if (e.keyCode == KeyCode.W) {
					gridOffsetY += 20f;
				}
				if (e.keyCode == KeyCode.S) {
					gridOffsetY -= 20f;
				}
				if (e.keyCode == KeyCode.A) {
					gridOffsetX += 20f;
				}
				if (e.keyCode == KeyCode.D) {
					gridOffsetX -= 20f;
				}
				
				if (e.keyCode == KeyCode.Alpha1) {
					tool = UPATool.PaintBrush;
				}
				if (e.keyCode == KeyCode.Alpha2) {
					tool = UPATool.Eraser;
				}
				if (e.keyCode == KeyCode.P) {
					lastTool = tool;
					tool = UPATool.ColorPicker;
				}
				
				if (e.keyCode == KeyCode.UpArrow) {
					gridSpacing *= 1.2f;
				}
				if (e.keyCode == KeyCode.DownArrow) {
					gridSpacing *= 0.8f;
					gridSpacing -= 2;
				}
				
			}
			
			if (e.control) {
				if (lastTool == UPATool.Empty) {
					lastTool = tool;
					tool = UPATool.Eraser;
				}
			} else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.LeftControl) {
				if (lastTool != UPATool.Empty) {
					tool = lastTool;
					lastTool = UPATool.Empty;
				}
			}
		}
		
		// TODO: Better way of doing this?
		// Why does it behave so weirdly with my mac tablet.
		if (e.type == EventType.ScrollWheel) {
			gridSpacing -= e.delta.y;
		}
		#endregion
		
		// DRAW IMAGE
		UPADrawer.DrawImage ( CurrentImg );
		
		UPADrawer.DrawToolbar (window.position, mousePos);
		
		UPADrawer.DrawLayerPanel ( window.position );
		
		e.Use();	// Release event handler
	}
}