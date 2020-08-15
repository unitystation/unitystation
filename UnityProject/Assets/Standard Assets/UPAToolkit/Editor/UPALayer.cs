using UnityEngine;
using UnityEditor;

[System.Serializable]
public class UPALayer {
	public enum BlendMode {
		NORMAL, MULTIPLY, SCREEN
	};

	public string name;
	public Color[] map;
	public Texture2D tex;
	public bool enabled;
	public float opacity;
	public BlendMode mode;
	public bool locked;
	
	public UPAImage parentImg;
	
	// Constructor
	public UPALayer (UPAImage img) {
		name = "Layer " + (img.layers.Count + 1);
		opacity = 1;
		mode = BlendMode.NORMAL;
		
		map = new Color[img.width * img.height];
		tex = new Texture2D (img.width, img.height);
		
		for (int x = 0; x < img.width; x++) {
			for (int y = 0; y < img.height; y++) {
				map[x + y * img.width] = Color.clear;
				tex.SetPixel (x,y, Color.clear);
			}
		}
		
		tex.filterMode = FilterMode.Point;
		tex.Apply ();
		
		enabled = true;
		locked = false;
		parentImg = img;
		
		// Because Unity won't record map (Color[]) as an undo,
		// we instead register a callback to LoadMapFromTex since undoing textures works fine
		Undo.undoRedoPerformed += LoadMapFromTex; // subscribe to the undo event
	}

	// Create clone of other UPALayer
	public UPALayer(UPALayer original) {
		name = original.name + " - Clone";
		opacity = 1;
		mode = original.mode;

		map = (Color[]) original.map.Clone();
		tex = new Texture2D (original.parentImg.width, original.parentImg.height);
		tex.SetPixels (original.tex.GetPixels ());

		tex.filterMode = FilterMode.Point;
		tex.Apply ();
		
		enabled = true;
		locked = original.locked;
		parentImg = original.parentImg;

		// Because Unity won't record map (Color[]) as an undo,
		// we instead register a callback to LoadMapFromTex since undoing textures works fine
		Undo.undoRedoPerformed += LoadMapFromTex; // subscribe to the undo event
	}
	
	void LoadMapFromTex() {
	
		for (int x = 0; x < parentImg.width; x++) {
			for (int y = 0; y < parentImg.height; y++) {
				map[x + y * parentImg.width] = tex.GetPixel (x, parentImg.height - y - 1);
			}
		}

	}
	
	public Color GetPixel (int x, int y) {
		return tex.GetPixel (x, y);
	}
	
	public void SetPixel (int x, int y, Color color) {
		if (!locked) {
			tex.SetPixel (x, y, color);
			tex.Apply ();
		
			map [x + y * - 1 * parentImg.width - parentImg.height] = color;
		}
	}
	
	public void LoadTexFromMap () {
		tex = new Texture2D (parentImg.width, parentImg.height);

		for (int x = 0; x < parentImg.width; x++) {
			for (int y = 0; y < parentImg.height; y++) {
				tex.SetPixel (x, parentImg.height - y - 1, map[x + y * parentImg.width]);
			}
		}
		
		tex.filterMode = FilterMode.Point;
		tex.Apply();
	}
	
	public int GetOrder () {
		return parentImg.layers.IndexOf (this);
	}

}
