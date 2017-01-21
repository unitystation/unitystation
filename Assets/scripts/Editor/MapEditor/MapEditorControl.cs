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
    public static int HashCode { get; set; }

    private static GameObject currenPrefab { get; set; }
    private static GameObject previewObject { get; set; }
    private static bool keyDown = false;
    private static bool mouseDown = false;
    private static int oldID;

    public static void BuildUpdate(SceneView sceneview) {
        if(!EnableEdit) {
            if(previewObject) {
                Undo.DestroyObjectImmediate(previewObject);
            }
            return;
        }

        Event e = Event.current;

        if(CurrentPrefab) {
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
                foreach(var script in previewObject.GetComponentsInChildren<MonoBehaviour>(true)) {
                    Undo.DestroyObjectImmediate(script);
                }
            }
            previewObject.transform.position = new Vector3(x, y, 0);

            if(Selection.Contains(previewObject)) {
                Selection.objects = Array.FindAll(Selection.objects, o => (o != previewObject));
            }
        }

        if(MouseControl) {
            CheckMouseControls(e);
        }
        CheckKeyControls(e);
    }

    private static void CheckMouseControls(Event e) {
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
                    mouseDown = false;
                    e.Use();
                }
                break;
        }
    }

    private static void CheckKeyControls(Event e) {
        if(e.isKey) {
            if(!keyDown && e.type == EventType.KeyDown) {
                switch(e.character) {
                    case 'a':
                        keyDown = true;
                        Build(e);
                        e.Use();
                        break;
                    case 'd':
                        keyDown = true;
                        foreach(GameObject obj in Selection.gameObjects)
                            Undo.DestroyObjectImmediate(obj);
                        e.Use();
                        break;
                    case 'q':
                        keyDown = true;
                        break;
                    case 'e':
                        keyDown = true;
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
