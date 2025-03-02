using Core.Physics;
using Managers;
using UnityEngine;

namespace Mapping.Move
{
	[RequireComponent(typeof(RegisterObject))]
	public class MoveToLocationMarker : MonoBehaviour
	{
		public int LocationIdToMoveTo = 0;
		public RegisterObject Register;

		public void MoveAllObjectsOnTileToId()
		{
			var toTeleport = MatrixManager.GetAt<UniversalObjectPhysics>(Register.LocalPosition, CustomNetworkManager.IsServer);
			foreach (var obj in toTeleport)
			{
				GameManager.Instance.MoveToLocationMarker(LocationIdToMoveTo, obj);
			}
		}
	}
}