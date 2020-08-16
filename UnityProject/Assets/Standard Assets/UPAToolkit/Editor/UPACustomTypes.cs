using UnityEngine;

// Used for switching tools
public enum UPATool {
	PaintBrush,
	Eraser,
	BoxBrush, // TODO: Add BoxBrush
	Empty, // Used as null
	ColorPicker,
}

// Used for selecting texture export type
public enum TextureType {
	sprite = 0,
	texture = 1,
}

// Used for selecting texture export exstension
public enum TextureExtension {
	PNG = 0,
	JPG = 1,
}