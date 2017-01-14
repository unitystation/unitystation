using UnityEngine;
using UnityEditor;
using Matrix;
using System.Linq;

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
    private GUIStyle buttonStyle;

    [MenuItem("Window/Map Editor")]
    public static void ShowWindow() {
        //Show existing window instance. If one doesn't exist, make one.
        GetWindow<MapEditorWindow>("Map Editor");
    }

    public void OnEnable() {
        SceneView.onSceneGUIDelegate += GridUpdate;
        mapObj = GameObject.FindGameObjectWithTag("Map");
        if (mapObj != null)
        {
            MapEditorMap.SetMap(mapObj);
        }
        else
        {
            Debug.Log("Failed to load map");
        }
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
        EditorGUILayout.LabelField("CurrentMap: " + MapEditorMap.mapName);
        EditorGUILayout.LabelField("Add tiles to which section:");
        string[] options = new string[MapEditorMap.mapSections.Count];
        foreach (GameObject section in MapEditorMap.mapSections)
        {
            options[MapEditorMap.mapSections.IndexOf(section)] = section.name;
        }
        sectionIndex = EditorGUILayout.Popup(sectionIndex, options);
        if (GUILayout.Button("Refresh Data"))
        {
            if(mapObj != null)
            MapEditorMap.SetMap(mapObj);
            Init();
        }
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

                        GameObject gameObj = (GameObject) PrefabUtility.InstantiatePrefab(currentPrefab);
                        gameObj.transform.position = r.origin;
                        AddSectionToParent(gameObj);
                        Undo.RegisterCreatedObjectUndo(gameObj, "Create " + gameObj.name);
                    }
                }
            }
            if(e.character == 'd') {
                foreach(GameObject obj in Selection.gameObjects)
                    Undo.DestroyObjectImmediate(obj);
            }
        }
    }

    void AddSectionToParent(GameObject newObj){
        Transform subSection = MapEditorMap.mapSections[sectionIndex].transform.FindChild(prefabFolders[tabIndex]);
        if (subSection != null)
        {
            newObj.transform.parent = subSection;
        }
        else
        {
            GameObject newSubSection = new GameObject();
            newSubSection.transform.parent = MapEditorMap.mapSections[sectionIndex].transform;
            newSubSection.name = prefabFolders[tabIndex];
            newSubSection.transform.localPosition = Vector3.zero;
            newObj.transform.parent = newSubSection.transform;
        }
    
    }
}