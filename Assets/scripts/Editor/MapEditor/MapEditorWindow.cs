using UnityEngine;
using UnityEditor;
using Matrix;

public class MapEditorWindow: EditorWindow {
    private GameObject currentPrefab;
    private int xCount = 4;

    private bool enableEdit = false;
    private int tabIndex = 0;
    private int[] gridIndices;
    private Vector2[] scrollPositions;

    private string[] prefabFolders = new string[] { "Walls", "Floors", "Doors", "Tables" };

    private GUIStyle buttonStyle;

    [MenuItem("Window/Map Editor")]
    public static void ShowWindow() {
        //Show existing window instance. If one doesn't exist, make one.
        GetWindow<MapEditorWindow>("Map Editor");
    }

    public void OnEnable() {
        SceneView.onSceneGUIDelegate += GridUpdate;
        Init();
    }

    public void OnDisable() {
        SceneView.onSceneGUIDelegate -= GridUpdate;
    }

    void Init() {
        MapEditorData.Clear();
        foreach(var s in prefabFolders) {
            MapEditorData.Load(s);
        }
        gridIndices = new int[prefabFolders.Length];
        scrollPositions = new Vector2[prefabFolders.Length];
    }

    void OnDestroy() {
        enableEdit = false;
    }

    void OnGUI() {
        EditorGUILayout.LabelField("'a' to build. 'd' to delete.");
        enableEdit = EditorGUILayout.BeginToggleGroup("Map Editor Mode", enableEdit);

        tabIndex = GUILayout.Toolbar(tabIndex, prefabFolders);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 75, fixedWidth = 75 };

        if(Event.current.type == EventType.Repaint)
            xCount = (int) ((GUILayoutUtility.GetLastRect().width + 20) / 75);

        scrollPositions[tabIndex] = EditorGUILayout.BeginScrollView(scrollPositions[tabIndex]);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        gridIndices[tabIndex] = GUILayout.SelectionGrid(gridIndices[tabIndex], MapEditorData.Textures[prefabFolders[tabIndex]], xCount, buttonStyle);
        currentPrefab = MapEditorData.Prefabs[prefabFolders[tabIndex]][gridIndices[tabIndex]];
        
        // popup = EditorGUILayout.Popup(popup, new string[] { "Walls", "Floors", "Doors", "Machines" });
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndToggleGroup();
    }

    void GridUpdate(SceneView sceneview) {
        Event e = Event.current;

        if(!enableEdit) {
            return;
        }

        if(e.isKey && e.type == EventType.KeyDown) {
            if(e.character == 'a') {
                if(currentPrefab) {
                    // Find screen position of mouse
                    Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

                    int x = Mathf.RoundToInt(r.origin.x);
                    int y = Mathf.RoundToInt(r.origin.y);

                    if(currentPrefab.GetComponent<RegisterTile>().tileType > Matrix.Matrix.GetTypeAt(x, y) && Matrix.Matrix.IsPassableAt(x, y)) {

                        GameObject gameObject = (GameObject) PrefabUtility.InstantiatePrefab(currentPrefab);
                        gameObject.transform.position = r.origin;

                        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
                    }
                }
            }
            if(e.character == 'd') {
                foreach(GameObject obj in Selection.gameObjects)
                    Undo.DestroyObjectImmediate(obj);
            }
        }
    }
}