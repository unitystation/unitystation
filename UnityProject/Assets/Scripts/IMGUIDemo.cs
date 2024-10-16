using ImGuiNET;
using UImGui;
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
				UImGuiUtility.Layout += OnLayout;
				UImGuiUtility.OnInitialize += OnInitialize;
				UImGuiUtility.OnDeinitialize += OnDeinitialize;
			}
			else
			{
				UImGuiUtility.Layout -= OnLayout;
				UImGuiUtility.OnInitialize -= OnInitialize;
				UImGuiUtility.OnDeinitialize -= OnDeinitialize;
			}
		}
	}

	private void Start()
	{
		if (showDemo)
		{
			UImGuiUtility.Layout += OnLayout;
			UImGuiUtility.OnInitialize += OnInitialize;
			UImGuiUtility.OnDeinitialize += OnDeinitialize;
		}
	}

	private void OnInitialize(UImGui.UImGui obj)
	{
		// runs after UImGui.OnEnable();
	}

	private void OnDeinitialize(UImGui.UImGui obj)
	{
		// runs after UImGui.OnDisable();
	}

	private void OnDisable()
	{
		UImGuiUtility.Layout -= OnLayout;
		UImGuiUtility.OnInitialize -= OnInitialize;
		UImGuiUtility.OnDeinitialize -= OnDeinitialize;
	}

	private void OnLayout(UImGui.UImGui obj)
	{
		ImGui.ShowDemoWindow();
	}
}
