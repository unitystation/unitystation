using UI.DIMGUI;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using Logs;
using SecureStuff;
using UnityEngine;
using Util;

namespace UI.Admin.DIMGUI.Mapping.MapEditorGamemode
{
    public class LoadMapsWindow : IBlueprintDimgui
    {
	    private GameObject father;
        private List<string> availableMapNames;      // Stores only the map names
        private List<string> availableMapPaths;      // Stores the corresponding paths
        private int selectedMapIndex = -1;           // -1 means no selection
        private string selectedMapName = "";         // Name of the selected map
        private string selectedMapPath = "";         // Full path of the selected map
        private SceneType sceneType = SceneType.AdditionalScenes;

        public LoadMapsWindow(GameObject fatherLinkedTo)
        {
	        father = fatherLinkedTo;
            availableMapNames = new List<string>();
            availableMapPaths = new List<string>();

            // Retrieve directories containing maps
            var directories = new List<string>(AccessFile.DirectoriesOrFilesIn("", FolderType.Maps, false, false));
            foreach (var folder in directories)
            {
                var maps = new List<string>(AccessFile.DirectoriesOrFilesIn(folder, FolderType.Maps, false, true));

                foreach (var map in maps)
                {
                    availableMapPaths.Add(folder + "/" + map);
                    availableMapNames.Add(map);
                }
            }

            Loggy.Info($"{availableMapNames.Count} maps found. \n{string.Join("\n", availableMapPaths)}");
        }

        public void OnLayout(UImGui.UImGui obj)
        {
            if (ImGui.Begin("Load Maps", ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (availableMapNames == null || availableMapNames.Count == 0)
                {
                    ImGui.Text("No maps found.");
                }
                else
                {
                    DrawAvailableMaps();

                    if (selectedMapIndex != -1)
                    {
                        ImGui.Separator();
                        IMGUIHelper.DrawEnumField(ref sceneType, "Map Type");
                        ImGui.Text($"Selected Map: {selectedMapName}");
                        ImGui.Text($"Path: {selectedMapPath}");  // Show full path

                        if (ImGui.Button("Load Selected Map"))
                        {
                            LoadMap(selectedMapPath);  // Pass full path for loading
                        }
                    }
                }
                ImGui.End();
            }
        }

        private void DrawAvailableMaps()
        {
            ImGui.Text("Available Maps:");
            for (int i = 0; i < availableMapNames.Count; i++)
            {
                bool isSelected = i == selectedMapIndex;
                if (ImGui.Selectable(availableMapNames[i], isSelected))
                {
                    selectedMapIndex = i;
                    selectedMapName = availableMapNames[i];
                    selectedMapPath = availableMapPaths[i]; // Update path for display
                }
            }
        }

        private void LoadMap(string mapPath)
        {
	        if (sceneType == SceneType.MainStation)
	        {
		        var orginalPos = Vector3.zero;
		        if (PlayerManager.LocalPlayerObject != null)
		        {
			        PlayerManager.LocalPlayerScript.playerMove.AppearAtWorldPositionServer(new Vector3(9000000,9000000));
		        }
		        foreach (var matrix in MatrixManager.Instance.ActiveMatricesList)
		        {
			        if (matrix.Name == "Space") continue;
			        Loggy.Info($"deleting {matrix.Name}");
			        Despawn.ServerSingle(matrix.GameObject);
		        }
		        CleanupUtil.SoftCleanup();
		        if (PlayerManager.LocalPlayerObject != null)
		        {
			        PlayerManager.LocalPlayerScript.playerMove.AppearAtWorldPositionServer(orginalPos);
		        }
	        }
	        father.GetComponent<Toolbar>().StartCoroutine(SubSceneManager.Instance.LoadSubScene(mapPath, default, default, sceneType));
	        Loggy.Info("loading map: " + mapPath + "... - " + sceneType);
        }

        public void OnCreateLayout(UImGui.UImGui obj)
        {
            // No initialization needed currently.
        }

        public void OnStopLayout(UImGui.UImGui obj)
        {
            availableMapNames = null;
            availableMapPaths = null;
            selectedMapIndex = -1;
            selectedMapName = "";
            selectedMapPath = "";
        }
    }
}
