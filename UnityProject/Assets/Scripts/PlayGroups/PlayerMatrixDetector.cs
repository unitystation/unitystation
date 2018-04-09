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
	public class PlayerMatrixDetector : NetworkBehaviour
	{
		public LayerMask hitCheckLayers;
		[HideInInspector]public Collider2D curMatrixCol;
		private RaycastHit2D[] rayHit;

		public PlayerMove playerMove;
		public PlayerSync playerSync;

		private RegisterTile registerTile;
		private Matrix matrix => registerTile.Matrix;

		void Start()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public bool CanPass(Vector3 direction)
		{
			Vector3 newRayPos = transform.position + direction;
			rayHit = Physics2D.RaycastAll(newRayPos, direction, 0.2f, hitCheckLayers);
			Debug.DrawLine(newRayPos, newRayPos + (direction * 0.2f), Color.red, 1f);

			//Detect new matrices
			for (int i = 0; i < rayHit.Length; i++) {

				//Door closed layer (matrix independent)
				if (rayHit[i].collider.gameObject.layer == 17) {
					DoorController doorController = rayHit[i].collider.gameObject.GetComponent<DoorController>();

					// Attempt to open door that could be on another layer
					if (doorController != null && playerMove.allowInput) {
						playerMove.pna.CmdCheckDoorPermissions(doorController.gameObject, gameObject);
						playerMove.allowInput = false;
						playerMove.StartCoroutine(playerMove.DoorInputCoolDown());
					}
					return false;
				}

				//Detected windows or walls across matrices or from space:
				if (rayHit[i].collider.gameObject.layer == 9
				   || rayHit[i].collider.gameObject.layer == 18) {
					return false;
				}
			}
			return true;
		}

		public void ChangeMatrices(Transform newParent)
		{
//			if (isServer) {
//				NetworkIdentity netIdent = newParent.GetComponent<NetworkIdentity>();
//				if (registerTile.ParentNetId != netIdent.netId) {
//					registerTile.ParentNetId = netIdent.netId;
////						playerSync.SetPosition(transform.localPosition);
//				}
//			} else {
				registerTile.SetParentOnLocal(newParent);
				Camera.main.transform.parent = newParent;
//			}
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			//Debug.Log($"OnTriggerEntered {collision.name}");
			//if(collision.transform.parent.name == "OutpostStation"){
			//	Debug.Log($"OnTriggerEntered {collision.name}");
			//	return;
			//}
//			if (collision.gameObject.layer == 24 && collision != curMatrixCol) {
//				curMatrixCol = collision;
//				ChangeMatrices(collision.gameObject.transform.parent);
//				Debug.Log($"Change Matrices {collision.gameObject.transform.parent.name}");
//			}
		}
	}
}

