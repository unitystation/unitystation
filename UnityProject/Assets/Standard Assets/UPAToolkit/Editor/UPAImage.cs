//-----------------------------------------------------------------
// This class stores all information about the image.
// It has a full pixel map, width & height properties and some private project data.
// It also hosts functions for calculating how the pixels should be visualized in the editor.
//-----------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class UPAImage : ScriptableObject {

	// HELPER GETTERS
	private Rect window {
		get { return UPAEditorWindow.window.position; }
	}
	

	// IMAGE DATA

	public int width;
	public int height;

	public List<UPALayer> layers;
	public int layerCount
	{
		get { return layers.Count; }
	}

	public Texture2D finalImg;

	// VIEW & NAVIGATION SETTINGS

	[SerializeField]
	private float _gridSpacing = 20f;
	public float gridSpacing {
		get { return _gridSpacing + 1f; }
		set { _gridSpacing = Mathf.Clamp (value, 0, 140f); }
	}

	public float gridOffsetY = 0;
	public float gridOffsetX = 0;

	//Make sure we always get a valid layer
	private int _selectedLayer = 0;
	public int selectedLayer {
		get {
			return Mathf.Clamp(_selectedLayer, 0, layerCount);
		}
		set { _selectedLayer = value; }
	}
	
	
	// PAINTING SETTINGS

	public Color selectedColor = new Color (1,0,0,1);
	public UPATool tool = UPATool.PaintBrush;
	public int gridBGIndex = 0;


	//MISC VARIABLES

	public bool dirty = false;		// Used for determining if data has changed
	
	
	// Class constructor
	public UPAImage () {
		// do nothing so far
	}

	// This is not called in constructor to have more control
	public void Init (int w, int h) {
		width = w;
		height = h;

		layers = new List<UPALayer>();
		UPALayer newLayer = new UPALayer (this);
		layers.Add ( newLayer );
		
		EditorUtility.SetDirty (this);
		dirty = true;
	}

	// Color a certain pixel by position in window in a certain layer
	public void SetPixelByPos (Color color, Vector2 pos, int layer) {
		Vector2 pixelCoordinate = GetPixelCoordinate (pos);

		if (pixelCoordinate == new Vector2 (-1, -1))
			return;

		Undo.RecordObject (layers[layer].tex, "ColorPixel");

		layers[layer].SetPixel ((int)pixelCoordinate.x, (int)pixelCoordinate.y, color);
		
		EditorUtility.SetDirty (this);
		dirty = true;
	}

	// Return a certain pixel by position in window
	public Color GetPixelByPos (Vector2 pos, int layer) {
		Vector2 pixelCoordinate = GetPixelCoordinate (pos);

		if (pixelCoordinate == new Vector2 (-1, -1)) {
			return Color.clear;
		} else {
			return layers[layer].GetPixel ((int)pixelCoordinate.x, (int)pixelCoordinate.y);
		}
	}

	public Color GetBlendedPixel (int x, int y) {
		Color color = Color.clear;

		for (int i = 0; i < layers.Count; i++) {
			if (!layers[i].enabled)
				continue;

			Color pixel = layers[i].tex.GetPixel(x,y);

			// This is a blend between two methods of calculating color blending; Alpha blending and premultiplied alpha blending
			// I have no clue why this actually works but it's very accurate :D
			float newR = Mathf.Lerp (1f * pixel.r + (1f - pixel.a) * color.r, pixel.a * pixel.r + (1f - pixel.a) * color.r, color.a);
			float newG = Mathf.Lerp (1f * pixel.g + (1f - pixel.a) * color.g, pixel.a * pixel.g + (1f - pixel.a) * color.g, color.a);
			float newB = Mathf.Lerp (1f * pixel.b + (1f - pixel.a) * color.b, pixel.a * pixel.b + (1f - pixel.a) * color.b, color.a);

			float newA = pixel.a + color.a * (1 - pixel.a);

			color = new Color (newR, newG, newB, newA);
		}

		return color;
	}
	
	public void ChangeLayerPosition (int from, int to) {
		if (from >= layers.Count || to >= layers.Count || from < 0 || to < 0) {
			Debug.LogError ("Cannot ChangeLayerPosition, out of range.");
			return;
		}
		
		UPALayer layer = layers[from];
		layers.RemoveAt(from);
		layers.Insert(to, layer);

		dirty = true;
	}

	// Get the rect of the image as displayed in the editor
	public Rect GetImgRect () {
		float ratio = (float)height / (float)width;
		float w = gridSpacing * 30;
		float h = ratio * gridSpacing * 30;
		
		float xPos = window.width / 2f - w/2f + gridOffsetX;
		float yPos = window.height / 2f - h/2f + 20 + gridOffsetY;

		return new Rect (xPos,yPos, w, h);
	}

	public Vector2 GetPixelCoordinate (Vector2 pos) {
		Rect texPos = GetImgRect();
			
		if (!texPos.Contains (pos)) {
			return new Vector2(-1f,-1f);
		}

		float relX = (pos.x - texPos.x) / texPos.width;
		float relY = (texPos.y - pos.y) / texPos.height;
		
		int pixelX = (int)( width * relX );
		int pixelY = (int)( height * relY ) - 1;

		return new Vector2(pixelX, pixelY);
	}
	
	public Vector2 GetReadablePixelCoordinate (Vector2 pos) {
		Vector2 coord = GetPixelCoordinate (pos);
		
		if (coord.x == -1)
			return coord;
		
		coord.x += 1;
		coord.y *= -1;
		return coord;
	}

	public Texture2D GetFinalImage (bool update) {

		if (!dirty && finalImg != null || !update && finalImg != null)
			return finalImg;

		finalImg = UPADrawer.CalculateBlendedTex(layers);
		finalImg.filterMode = FilterMode.Point;
		finalImg.Apply();

		dirty = false;
		return finalImg;
	}

	public void LoadAllTexsFromMaps () {
		for (int i = 0; i < layers.Count; i++) {
			if (layers[i].tex == null)
				layers[i].LoadTexFromMap();
		}
	}

	public void AddLayer () {
		Undo.RecordObject (this, "AddLayer");
		EditorUtility.SetDirty (this);
		this.dirty = true;

		UPALayer newLayer = new UPALayer (this);
		layers.Add(newLayer);
	}

	public void RemoveLayerAt (int index) {
		Undo.RecordObject (this, "RemoveLayer");
		EditorUtility.SetDirty (this);
		this.dirty = true;

		layers.RemoveAt (index);
		if (selectedLayer == index) {
			selectedLayer = index - 1;
		}
	}
}
