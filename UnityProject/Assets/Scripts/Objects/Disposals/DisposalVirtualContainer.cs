using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AddressableReferences;

namespace Objects.Disposals
{
	/// <summary>
	/// A virtual container for disposal instances. Contains the disposed contents,
	/// and allows the contents to be dealt with when the disposal instance ends.
	/// </summary>
	public class DisposalVirtualContainer : MonoBehaviour, IExaminable, IEscapable
	{
		[Tooltip("The sound made when someone is trying to move in pipes.")]
		[SerializeField]
		private AddressableAudioSource ClangSound = default;

		private ObjectContainer container;

		// transform.position seems to be the only reliable method after OnDespawnServer() has been called.
		private Vector3 ContainerWorldPosition => transform.position;

		private void Awake()
		{
			container = GetComponent<ObjectContainer>();
		}

		#region EjectContents

		private void ThrowItem(CustomNetTransform cnt, Vector3 throwVector)
		{
			Vector3 vector = cnt.transform.rotation * throwVector;
			ThrowInfo throwInfo = new ThrowInfo
			{
				ThrownBy = gameObject,
				Aim = (BodyPartType) Random.Range(0, 13),
				OriginWorldPos = ContainerWorldPosition,
				WorldTrajectory = vector,
				SpinMode = DMMath.Prob(50) ? SpinMode.Clockwise : SpinMode.CounterClockwise
			};

			cnt.Throw(throwInfo);
		}

		/// <summary>
		/// Ejects contents at the virtual container's position with no spin.
		/// </summary>
		public void EjectContents()
		{
			container.RetrieveObjects();
		}

		/// <summary>
		/// Ejects contents at the virtual container's position, then throws or pushes each entity with the given exit vector.
		/// </summary>
		/// <param name="exitVector">The direction (and distance) to throw or push the contents with</param>
		public void EjectContentsWithVector(Vector3 exitVector)
		{
			var objects = container.GetStoredObjects().ToArray();
			container.RetrieveObjects();

			foreach (var obj in objects)
			{
				if (obj.TryGetComponent<IPushable>(out var pushable) == false) continue;

				if (obj.TryGetComponent<RegisterObject>(out _) == false && obj.TryGetComponent<CustomNetTransform>(out var cnt))
				{
					ThrowItem(cnt, exitVector);
					return;
				}

				pushable.Push(exitVector.To2Int());

				if (obj.TryGetComponent<PlayerScript>(out var script))
				{
					script.registerTile.ServerStun();
				}
			}
		}

		#endregion EjectContents

		public void EntityTryEscape(GameObject entity)
		{
			SoundManager.PlayNetworkedAtPos(ClangSound, ContainerWorldPosition);
		}

		public string Examine(Vector3 worldPos = default)
		{
			int contentsCount = container.GetStoredObjects().Count();
			return $"There {(contentsCount == 1 ? "is one entity" : $"are {contentsCount} entities")} inside.";
		}
	}
}
