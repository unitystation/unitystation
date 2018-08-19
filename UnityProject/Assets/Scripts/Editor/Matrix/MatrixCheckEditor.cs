using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MatrixCheckEditor : EditorWindow
{
	private SceneView currentSceneView;

	private int tab = 0;

	private string[] tabHeaders = { "Matrix", "Meta Data" };
	private BasicView[] tabs = { new MetaTileMapView(), new MetaDataView()};



	[MenuItem("Window/Matrix Check")]
	public static void ShowWindow()
	{
		GetWindow<MatrixCheckEditor>("Matrix Check");
	}

	public void OnEnable()
	{
		SceneView.onSceneGUIDelegate += OnSceneGUI;
	}

	public void OnDisable()
	{
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
	}

	private void OnSceneGUI(SceneView sceneView)
	{
		currentSceneView = sceneView;
	}

	private void OnGUI()
	{
		tab = GUILayout.Toolbar(tab, tabHeaders);

		tabs[tab].OnGUI();

		if (currentSceneView)
		{
			currentSceneView.Repaint();
		}
	}
}
