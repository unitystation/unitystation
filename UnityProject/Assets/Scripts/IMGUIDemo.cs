using System;
using ImGuiNET;
using ImGuiNET.Unity;
using UnityEngine;

public class IMGUIDemo : MonoBehaviour
{
	[SerializeField] private bool showDemo = false;
	private void Update()
	{
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Insert))
		{
			showDemo = !showDemo;
			if (showDemo)
			{
				ImGuiUn.Layout += OnLayout;
			}
			else
			{
				ImGuiUn.Layout -= OnLayout;
			}
		}
	}
	private void OnDisable()
	{
		ImGuiUn.Layout -= OnLayout;
	}

	private void OnLayout()
	{
		ImGui.ShowDemoWindow();
	}
}
