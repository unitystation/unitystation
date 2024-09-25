using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "CommonManagerEditorOnly", menuName = "Singleton/CommonManagerEditorOnly")]
public class CommonManagerEditorOnly : SingletonScriptableObject<CommonManagerEditorOnly>
{
	public CustomNetworkManager CustomNetworkManagerPrefab;
	public VariableViewerManager VariableViewerManager;
	public TileManager TileManager;
	public GameObject Matrix;
	public GameObject MatrixSync;
}
