using Matrix;
using System;
using UI;
using UnityEditor;
using UnityEngine;

public class MapEditorControl {
    public static GameObject CurrentPrefab {
        get {
            return currenPrefab;
        }
        set {
            currenPrefab = value;
            if(previewObject)
                Undo.DestroyObjectImmediate(previewObject);
        }
    }

    public static bool EnableEdit { get; set; }
    public static bool MouseControl { get; set; }
    public static bool EnablePreview { get; set; }
    public static int HashCode { get; set; }

    private static GameObject currenPrefab { get; set; }
    private static GameObject previewObject { get; set; }
    private static bool keyDown = false;
    private static bool mouseDown = false;
    private static int oldID;

    static MapEditorControl() {
        EnableEdit = true;
        EnablePreview = true;
    }

    public static void BuildUpdate(SceneView sceneview) {
        if(!EnableEdit) {
            if(previewObject) {
                Undo.DestroyObjectImmediate(previewObject);
            }
            return;
        }

        Event e = Event.current;

        if(CurrentPrefab && EnablePreview) {
            Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

            int x = Mathf.RoundToInt(r.origin.x);
            int y = Mathf.RoundToInt(r.origin.y);

            if(!previewObject) {
                previewObject = (GameObject) PrefabUtility.InstantiatePrefab(CurrentPrefab);
                previewObject.name = "Preview";
                foreach(var renderer in previewObject.GetComponentsInChildren<Renderer>(true)) {
                    var m = new Material(renderer.sharedMaterial);
                    var c = m.color;
                    c.a = 0.7f;
                    m.color = c;
                    renderer.sharedMaterial = m;
                }

                foreach(var script in Array.FindAll(previewObject.GetComponentsInChildren<MonoBehaviour>(true), o => !(o is EditModeControl))) {
                    Undo.DestroyObjectImmediate(script);
                }
            }
            previewObject.transform.position = new Vector3(x, y, 0);

            if(Selection.Contains(previewObject)) {
                Selection.objects = Array.FindAll(Selection.objects, o => (o != previewObject));
            }
        } else if(previewObject) {
            Undo.DestroyObjectImmediate(previewObject);
        }

        if(MouseControl) {
            CheckMouseControls(e);
        }
        CheckKeyControls(e);
    }

    private static void CheckMouseControls(Event e) {
        if(!e.isMouse)
            return;

        int controlID = GUIUtility.GetControlID(HashCode, FocusType.Passive);

        switch(e.GetTypeForControl(controlID)) {
            case EventType.MouseDown:
                if(e.button == 0) {
                    oldID = GUIUtility.hotControl;
                    GUIUtility.hotControl = controlID;
                    mouseDown = true;
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if(mouseDown && e.button == 0) {
                    if(Selection.activeGameObject != null) {
                        SelectObject(e);
                        GUIUtility.hotControl = oldID;
                    } else {
                        if(!Build(e)) {
                            SelectObject(e);
                        }
                        GUIUtility.hotControl = 0;
                    }
                    mouseDown = false;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if(mouseDown) {
                    GUIUtility.hotControl = oldID;
                    oldID = 0;
                    e.Use();
                }
                break;
        }
        if(previewObject)
            previewObject.SetActive(GUIUtility.hotControl == 0 || !mouseDown);
    }

    private static void CheckKeyControls(Event e) {
        if(e.isKey) {
            if(!keyDown && e.type == EventType.KeyDown) {
                keyDown = true;
                switch(e.character) {
                    case 'a':
                        Build(e);
                        e.Use();
                        break;
                    case 'd':
                        foreach(GameObject obj in Selection.gameObjects)
                            Undo.DestroyObjectImmediate(obj);
                        e.Use();
                        break;
                    case 'y':
                        //var editModeControl = previewObject.GetComponent<EditModeControl>();
                        //if(editModeControl && editModeControl.allowRotate) {
                        //    var spriteTransform = previewObject.transform.FindChild("Sprite");
                        //    spriteTransform.Rotate(Vector3.forward * 90);
                        //}
                        //e.Use();
                        break;
                    case 'x':
                        break;
                    default:
                        keyDown = false;
                        break;
                }
            } else if(e.type == EventType.KeyUp) {
                keyDown = false;
            }
        }
    }

    private static bool Build(Event e) {
        if(!CurrentPrefab)
            return false;

        Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

        int x = Mathf.RoundToInt(r.origin.x);
        int y = Mathf.RoundToInt(r.origin.y);

        var registerTile = CurrentPrefab.GetComponent<RegisterTile>();
        if(registerTile) { // it's something constructable
            if(Matrix.Matrix.IsPassableAt(x, y) && registerTile.tileType > Matrix.Matrix.GetTypeAt(x, y)) {

                GameObject gameObject = (GameObject) PrefabUtility.InstantiatePrefab(CurrentPrefab);
                gameObject.transform.position = r.origin;
                gameObject.transform.parent = MapEditorMap.CurrentSubSection;

                Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
                return true;
            }
        } else {
            var itemAttributes = CurrentPrefab.GetComponent<ItemAttributes>();
            if(itemAttributes) { // it's an item
                // TODO
                return true;
            }
        }
        return false;
    }

    private static void SelectObject(Event e) {
        var mousePosition = Camera.current.ScreenToWorldPoint(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));
        var collider = Physics2D.OverlapPoint(mousePosition);
        GameObject gameObject = null;
        if(collider)
            gameObject = Physics2D.OverlapPoint(mousePosition).gameObject;

        Selection.activeGameObject = gameObject;
    }
}
