using System;
using System.Collections.Generic;
using ImGuiNET;
using SecureStuff;
using UI.DIMGUI;
using UImGui;
using UnityEngine;

namespace UI.Admin.DIMGUI.Mapping.MapEditorGamemode
{
	public class Toolbar : MonoBehaviour
	{

		private List<IBlueprintDimgui> blueprints = new List<IBlueprintDimgui>();

		private void Start()
		{
			UImGuiUtility.Layout += OnLayout;
			UImGuiUtility.OnInitialize += OnInitialize;
			UImGuiUtility.OnDeinitialize += OnDeinitialize;
		}

		private void OnDestroy()
		{
			ClearActiveBlueprints();
			UImGuiUtility.Layout -= OnLayout;
			UImGuiUtility.OnInitialize -= OnInitialize;
			UImGuiUtility.OnDeinitialize -= OnDeinitialize;
		}

		private void OnDeinitialize(UImGui.UImGui obj)
		{
			ClearActiveBlueprints();
		}

		private void OnInitialize(UImGui.UImGui obj)
		{

		}

		private void OnLayout(UImGui.UImGui obj)
		{
			if (ImGui.BeginMainMenuBar())
			{
				DrawMenuBar();
				ImGui.EndMenu();
				ImGui.EndMainMenuBar();
			}
		}

		private void ClearActiveBlueprints()
		{
			foreach (var print in blueprints)
			{
				print.Kill();
			}
		}

		private void DrawMenuBar()
		{
			if (ImGui.BeginMenu("File"))
			{
				ImGui.MenuItem("New");
				ImGui.SeparatorText("Loading");
				if (ImGui.MenuItem("Load Map"))
				{
					LoadMapsWindow window = new LoadMapsWindow(gameObject);
					SetupWindow(window);
				}
				ImGui.MenuItem("Load Blueprint");
				ImGui.SeparatorText("Saving");
				ImGui.MenuItem("Save Current Loaded Map");
				if (ImGui.BeginMenu("Save As"))
				{
					ImGui.MenuItem("Map");
					ImGui.MenuItem("Blueprint");
					ImGui.EndMenu();
				}
				ImGui.EndMenu();
			}
			if (ImGui.BeginMenu("Tools"))
			{
				ImGui.MenuItem("History");
				ImGui.MenuItem("Action/Event Editor");
				ImGui.MenuItem("Light Editor");
				ImGui.EndMenu();
			}
			if (ImGui.BeginMenu("Tests"))
			{
				ImGui.MenuItem("Spawns");
				ImGui.MenuItem("Shuttle Navigation");
				ImGui.EndMenu();
			}

			DrawInfoMenu();
		}

		private void SetupWindow(object win)
		{
			if (win is not IBlueprintDimgui blueprint) return;
			blueprint.Setup();
			blueprints.Add(blueprint);
		}

		private void DrawInfoMenu()
		{
			if (ImGui.BeginMenu("Info"))
			{
				ImGui.SeparatorText("Current Matricies");
				foreach (var matrix in MatrixManager.Instance.ActiveMatrices.Values)
				{
					ImGui.BulletText($"{matrix.Name}");
				}
				ImGui.EndMenu();
			}
		}
	}
}
