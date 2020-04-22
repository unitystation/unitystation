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
#if UNITY_2019_3_OR_NEWER
	SceneView.duringSceneGui  += OnSceneGUI;
#else
	SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
	}

	public void OnDisable()
	{
#if UNITY_2019_3_OR_NEWER
		SceneView.duringSceneGui  += OnSceneGUI;
#else
	SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
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
