using Matrix;
using System;
using System.Collections.Generic;
using UI;
using UnityEditor;
using UnityEngine;

public class MapEditorControl
{
	private static GameObject currentPrefab;
	private static SceneView currentSceneView;

	public static GameObject CurrentPrefab {
		get { return currentPrefab; }
		set {
			currentPrefab = value;
			Preview.Prefab = currentPrefab;

			// set Focus to scene view
			if (currentSceneView)
				currentSceneView.Focus();
		}
	}

	public static bool EnableEdit { get; set; }

	public static bool MouseControl { get; set; }

	public static bool EnablePreview { get; set; }

	public static int HashCode { get; set; }

	private static PreviewObject preview;

	private static PreviewObject Preview {
		get {
			if (!preview) {
				var previewGameObject = GameObject.FindGameObjectWithTag("Preview");
				if (previewGameObject) {
					preview = previewGameObject.GetComponent<PreviewObject>();
				} else {
					var previewPrefab = Resources.Load<GameObject>("prefabs/Preview");
					var gameObject = (GameObject)PrefabUtility.InstantiatePrefab(previewPrefab);
					preview = gameObject.GetComponent<PreviewObject>();
				}
			}
			return preview;
		}
	}

	//to handle different keyboard types (i.e. German and US);
	private static bool _rotateOptA;
	private static bool _rotateOptB;

	public static bool RotateOptA {
		get{ return _rotateOptA; }
		set {
			_rotateOptA = value;
			if (!_rotateOptA && !_rotateOptB) {
				RotateOptB = true;
			}
			if (_rotateOptA && _rotateOptB) {
				RotateOptB = false;
			}
		}
	}

	public static bool RotateOptB {
		get{ return _rotateOptB; }
		set {
			_rotateOptB = value;
			if (!_rotateOptA && !_rotateOptB) {
				RotateOptA = true;
			}
			if (_rotateOptB && _rotateOptA) {
				RotateOptA = false;
			}
		}
	}

	private static bool keyDown = false;
	private static bool mouseDown = false;
	private static int oldID;

	static MapEditorControl()
	{
		EnablePreview = true;
	}

	public static void BuildUpdate(SceneView sceneView)
	{
		currentSceneView = sceneView;
		if (!EnableEdit) {
			Preview.SetActive(false);
			return;
		}

		Event e = Event.current;

		if (CurrentPrefab && EnablePreview) {
			Preview.SetActive(true);

			Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

			int x = Mathf.RoundToInt(r.origin.x);
			int y = Mathf.RoundToInt(r.origin.y);

			Preview.transform.position = new Vector3(x, y, 0);

			if (Selection.Contains(Preview.gameObject)) {
				Selection.objects = Array.FindAll(Selection.objects, o => (o != Preview.gameObject));
			}
		} else {
			Preview.SetActive(false);
		}

		if (MouseControl) {
			CheckMouseControls(e);
		}
		CheckKeyControls(e);
	}

	private static void CheckMouseControls(Event e)
	{
		if (!e.isMouse)
			return;

		int controlID = GUIUtility.GetControlID(HashCode, FocusType.Passive);

		switch (e.GetTypeForControl(controlID)) {
			case EventType.MouseDown:
				if (e.button == 0) {
					oldID = GUIUtility.hotControl;
					GUIUtility.hotControl = controlID;
					mouseDown = true;
					e.Use();
				}
				break;
			case EventType.MouseUp:
				if (mouseDown && e.button == 0) {
					if (Selection.activeGameObject != null) {
						SelectObject(e);
						GUIUtility.hotControl = oldID;
					} else {
						if (!Build(e)) {
							SelectObject(e);
						}
						GUIUtility.hotControl = 0;
					}
					mouseDown = false;
					e.Use();
				}
				break;
			case EventType.MouseDrag:
				if (mouseDown) {
					GUIUtility.hotControl = oldID;
					oldID = 0;
					e.Use();
				}
				break;
		}
		Preview.SetActive(GUIUtility.hotControl == 0 || !mouseDown);
	}

	private static void CheckKeyControls(Event e)
	{
		if (e.isKey) {
			if (!keyDown && e.type == EventType.KeyDown) {
				keyDown = true;
				switch (e.character) {
					case 'a':
						Build(e);
						e.Use();
						break;
					case 'd':
						foreach (GameObject obj in Selection.gameObjects)
							Undo.DestroyObjectImmediate(obj);
						e.Use();
						break;
					case 'z':
						RotateKeys(e);
						break;
					case 'x':
						RotateKeys(e);
						e.Use();
						break;
					case '<':
						RotateKeys(e);
						break;
					case '>':
						RotateKeys(e);
						e.Use();
						break;
					default:
						keyDown = false;
						break;
				}
			} else if (e.type == EventType.KeyUp) {
				keyDown = false;
			}
		}
	}

	//to handle different keyboard types (i.e. German and US);
	private static void RotateKeys(Event e)
	{
		if (_rotateOptA) {
			if (e.character == 'z' || e.character == 'x') {
				Preview.RotateBackwards();
				e.Use();
			}
		} else {
			if (e.character == '<' || e.character == '>') {
				Preview.RotateBackwards();
				e.Use();
			}
		}
	}

	public static Dictionary<TileType, int> TileTypeLevels = new Dictionary<TileType, int>() {
		{ TileType.Space, 0 },
		{ TileType.Floor, 1 },
		{ TileType.Table, 1 },
		{ TileType.Wall, 2 },
		{ TileType.Window, 2 },
		{ TileType.Door, 2 }
	};

	private static bool Build(Event e)
	{
		if (!CurrentPrefab)
			return false;

		Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

		int x = Mathf.RoundToInt(r.origin.x);
		int y = Mathf.RoundToInt(r.origin.y);

		var registerTile = CurrentPrefab.GetComponent<RegisterTile>();
		if (registerTile) { // it's something constructable
			if (!Matrix.Matrix.HasTypeAt(x, y, registerTile.tileType) && TileTypeLevels[registerTile.tileType] >= TileTypeLevels[Matrix.Matrix.GetTypeAt(x, y)]) { 

				GameObject gameObject = Preview.CreateGameObject(r.origin);
				gameObject.transform.parent = MapEditorMap.CurrentSubSection;

				Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
				return true;
			}
		} else {
			var itemAttributes = CurrentPrefab.GetComponent<ItemAttributes>();
			if (itemAttributes) { // it's an item
				// TODO
				return true;
			}
		}
		return false;
	}

	private static void SelectObject(Event e)
	{
		var mousePosition = Camera.current.ScreenToWorldPoint(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));
		var collider = Physics2D.OverlapPoint(mousePosition);
		GameObject gameObject = null;
		if (collider)
			gameObject = Physics2D.OverlapPoint(mousePosition).gameObject;

		Selection.activeGameObject = gameObject;
	}
}
