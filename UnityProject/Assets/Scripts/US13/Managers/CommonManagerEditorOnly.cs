using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.ScriptableObjects;
using US13.Tilemaps;
using US13.Variable_Viewer;

namespace US13.Managers
{
	[CreateAssetMenu(fileName = "CommonManagerEditorOnly", menuName = "Singleton/CommonManagerEditorOnly")]
	public class CommonManagerEditorOnly : SingletonScriptableObject<CommonManagerEditorOnly>
	{
		public CustomNetworkManager CustomNetworkManagerPrefab;
		public VariableViewerManager VariableViewerManager;
		public TileManager TileManager;
		public GameObject Matrix;
		public GameObject MatrixSync;
	}
}
