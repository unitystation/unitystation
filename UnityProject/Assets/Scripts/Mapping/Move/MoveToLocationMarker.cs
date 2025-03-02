using System.Linq;
using AddressableReferences;
using Core.Physics;
using Cysharp.Threading.Tasks;
using InGameGizmos;
using Logs;
using Managers;
using UnityEngine;

namespace Mapping.Move
{
	[RequireComponent(typeof(RegisterObject))]
	public class MoveToLocationMarker : MonoBehaviour
	{
		public int LocationIdToMoveTo = 0;
		public RegisterObject Register;

		[SerializeField] private AddressableAudioSource soundOnTeleport;

		public void MoveAllObjectsOnTileToId()
		{
			_ = MoveObjects();
		}

		private async UniTaskVoid MoveObjects()
		{
			await UniTask.DelayFrame(5);
			var toTeleport = MatrixManager.GetAt<UniversalObjectPhysics>(Register.WorldPosition, true, Register.Matrix.MatrixInfo);
			foreach (var obj in toTeleport)
			{
				if (obj.gameObject == gameObject) continue;
				GameManager.Instance.MoveToLocationMarker(LocationIdToMoveTo, obj, sound: soundOnTeleport);
			}
		}
	}
}