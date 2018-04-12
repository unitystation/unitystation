using Doors;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	/// <summary>
	/// Provides the higher level multi matrix detection system to the
	/// playermove component using Unitys physics2D matrix.
	/// </summary>
	public class PlayerMatrixDetector : NetworkBehaviour {
		public LayerMask hitCheckLayers;
		[HideInInspector] public Collider2D curMatrixCol;
		private RaycastHit2D[] rayHit;

		public PlayerMove playerMove;
		public PlayerSync playerSync;

		private RegisterTile registerTile;
		private Matrix matrix => registerTile.Matrix;

		void Start() {
			registerTile = GetComponent<RegisterTile>();
		}

		public bool CanPass( Vector3Int localPos, Vector3Int direction, Matrix currentMatrix ) {
			MatrixInfo matrixInfo = MatrixManager.Instance.Get( currentMatrix );
			if ( matrixInfo.MatrixMove ) {
				//Converting local direction to world direction
				direction = Vector3Int.RoundToInt(matrixInfo.MatrixMove.ClientState.Orientation.Euler * direction);
			}
			Vector3Int position = MatrixManager.LocalToWorld( localPos, MatrixManager.Instance.Get(currentMatrix) );

			if ( !MatrixManager.Instance.IsPassableAt( position, position + direction ) ) {
				return false;
			}
			
			return true;
		}
	}
}

