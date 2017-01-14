using UnityEngine;
using UnityEditor;
using Matrix;
using System.Linq;
using UI;

public class MapEditorWindow: EditorWindow {
    private GameObject currentPrefab;
    private GameObject mapObj;
    private int xCount = 4;
    private int sectionIndex = 0;

    private bool enableEdit = false;
    private int tabIndex = 0;
    private int[] gridIndices;
    private Vector2[] scrollPositions;

    private string[] prefabFolders = new string[] { "Walls", "Floors", "Doors", "Tables" };

    [MenuItem("Window/Map Editor")]
    public static void ShowWindow() {
        //Show existing window instance. If one doesn't exist, make one.
        GetWindow<MapEditorWindow>("Map Editor");
    }

    public void OnEnable() {
        SceneView.onSceneGUIDelegate += BuildUpdate;
        Init();
    }

    public void OnDisable() {
        SceneView.onSceneGUIDelegate -= BuildUpdate;
    }

    void Init() {
        MapEditorData.Clear();
        foreach(var s in prefabFolders) {
            MapEditorData.Load(s);
        }

        if(gridIndices == null) {
            gridIndices = new int[prefabFolders.Length];
            for(int i = 0; i < gridIndices.Length; i++) {
                gridIndices[i] = -1;
            }
            scrollPositions = new Vector2[prefabFolders.Length];
        }
        if(mapObj == null) {
            mapObj = GameObject.FindGameObjectWithTag("Map");
            MapEditorMap.SetMap(mapObj);
        }
    }

    void OnGUI() {
        if(mapObj == null) {
            mapObj = GameObject.FindGameObjectWithTag("Map");
            MapEditorMap.SetMap(mapObj);
        }
        enableEdit = EditorGUILayout.BeginToggleGroup("Map Editor Mode", enableEdit);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CurrentMap", GUILayout.Width(100));
        EditorGUILayout.LabelField(MapEditorMap.mapName);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Section", GUILayout.Width(100));
        string[] options = new string[MapEditorMap.mapSections.Count];
        foreach(GameObject section in MapEditorMap.mapSections) {
            options[MapEditorMap.mapSections.IndexOf(section)] = section.name;
        }
        sectionIndex = EditorGUILayout.Popup(sectionIndex, options);
        EditorGUILayout.EndHorizontal();

        if(GUILayout.Button("Refresh Data")) {
            Init();
        }

        tabIndex = GUILayout.Toolbar(tabIndex, prefabFolders);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 75, fixedWidth = 75 };

        if(Event.current.type == EventType.Repaint)
            xCount = (int) ((GUILayoutUtility.GetLastRect().width + 20) / 75);

        EditorGUILayout.BeginHorizontal();
        scrollPositions[tabIndex] = EditorGUILayout.BeginScrollView(scrollPositions[tabIndex]);

        gridIndices[tabIndex] = GUILayout.SelectionGrid(gridIndices[tabIndex], MapEditorData.Textures[prefabFolders[tabIndex]], xCount, buttonStyle);

        if(gridIndices[tabIndex] >= 0) {
            currentPrefab = MapEditorData.Prefabs[prefabFolders[tabIndex]][gridIndices[tabIndex]];
        } else {
            currentPrefab = null;
        }

        EditorGUILayout.EndScrollView();
        GUILayout.Space(5);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndToggleGroup();
    }

    private bool clickStarted;

    private bool Build(Event e) {
        Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

        int x = Mathf.RoundToInt(r.origin.x);
        int y = Mathf.RoundToInt(r.origin.y);

        var registerTile = currentPrefab.GetComponent<RegisterTile>();
        if(registerTile) { // it's something constructable
            if(Matrix.Matrix.IsPassableAt(x, y) && registerTile.tileType > Matrix.Matrix.GetTypeAt(x, y)) {

                GameObject gameObj = (GameObject) PrefabUtility.InstantiatePrefab(currentPrefab);
                gameObj.transform.position = r.origin;
                AddSectionToParent(gameObj);
                Undo.RegisterCreatedObjectUndo(gameObj, "Create " + gameObj.name);
                return true;
            }
        } else {
            var itemAttributes = currentPrefab.GetComponent<ItemAttributes>();
            if(itemAttributes) { // it's an item
                if(registerTile.tileType > Matrix.Matrix.GetTypeAt(x, y)) {
                    // TODO
                    return true;
                }
            }
        }
        return false;
    }

    int oldID;

    void BuildUpdate(SceneView sceneview) {
        if(!enableEdit)
            return;

        Event e = Event.current;

        int controlID = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

        switch(e.GetTypeForControl(controlID)) {
            case EventType.MouseDown:
                if(e.button == 0) {
                    oldID = GUIUtility.hotControl;
                    GUIUtility.hotControl = controlID;
                    clickStarted = true;
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if(Selection.activeGameObject != null) {
                    Selection.activeGameObject = null;
                    GUIUtility.hotControl = oldID;
                    e.Use();
                } else {
                    if(clickStarted && e.button == 0) {
                        if(currentPrefab && Build(e)) {
                            e.Use();
                        }
                        GUIUtility.hotControl = 0;
                    }
                }
                clickStarted = false;

                break;
            case EventType.MouseDrag:
                if(clickStarted) {
                    GUIUtility.hotControl = oldID;
                    oldID = 0;
                    clickStarted = false;
                    e.Use();
                }
                Debug.Log(GUIUtility.hotControl);
                break;
            case EventType.KeyDown:
                // TODO any key combinations? 
                break;
        }
    }

    void AddSectionToParent(GameObject newObj) {
        if(MapEditorMap.mapSections.Count > sectionIndex) {
            Transform subSection = MapEditorMap.mapSections[sectionIndex].transform.FindChild(prefabFolders[tabIndex]);
            if(subSection == null) {
                GameObject newSubSection = new GameObject(prefabFolders[tabIndex]);
                newSubSection.transform.parent = MapEditorMap.mapSections[sectionIndex].transform;
                newSubSection.transform.localPosition = Vector3.zero;
                subSection = newSubSection.transform;
            }
            newObj.transform.parent = subSection;
        }
    }
}