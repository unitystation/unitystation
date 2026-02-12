using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Tilemaps.Tiles;

namespace US13.Tilemaps.Behaviours.Layers
{
	public class MatrixSpawner : MonoBehaviour
	{
		// Start is called before the first frame update

		public LayerTile Tile;

		void Start()
		{
			if (CustomNetworkManager.IsServer)
			{
				var Matrix =  MatrixManager.MakeNewMatrix();
				Matrix.MatrixMove.NetworkedMatrixMove.SetTransformPosition(transform.position);
				Matrix.MetaTileMap.SetTile(new Vector3Int(0,-1,0), Tile);
				_ = Despawn.ServerSingle(this.gameObject);
			}
		}

	}
}
