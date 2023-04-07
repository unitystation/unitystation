using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using AddressableReferences;
using Objects.Atmospherics;
using Systems.Disposals;
using Random = UnityEngine.Random;

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

		private ObjectContainer objectContainer;
		private GasContainer gasContainer;
		public DisposalTraversal traversal;

		// transform.position seems to be the only reliable method after OnDespawnServer() has been called.
		private Vector3 ContainerWorldPosition => transform.position;

		public bool SelfControlled { get; private set; } = false;

		private void Awake()
		{
			objectContainer = GetComponent<ObjectContainer>();
			gasContainer = GetComponent<GasContainer>();
		}

		#region EjectContents

		private void ThrowItem(UniversalObjectPhysics uop, Vector3 throwVector)
		{
			Vector3 vector = uop.transform.rotation * throwVector;
			uop.NewtonianPush(vector, Random.Range(1, 100)/10f , Random.Range(1, 85)/100f, Random.Range(1, 25)/100f, (BodyPartType) Random.Range(0, 13), gameObject, Random.Range(0, 13));
		}

		/// <summary>
		/// Ejects contents at the virtual container's position with no spin.
		/// </summary>
		public void EjectContents()
		{
			objectContainer.RetrieveObjects();
			gasContainer.ReleaseContentsInstantly();
			gasContainer.IsSealed = false;
		}

		/// <summary>
		/// Ejects contents at the virtual container's position, then throws or pushes each entity with the given exit vector.
		/// </summary>
		/// <param name="exitVector">The direction (and distance) to throw or push the contents with</param>
		public void EjectContentsWithVector(Vector3 exitVector)
		{
			var objects = objectContainer.GetStoredObjects().ToArray();
			objectContainer.RetrieveObjects();

			foreach (var obj in objects)
			{
				if (obj.TryGetComponent<UniversalObjectPhysics>(out var uop))
				{
					uop.AppearAtWorldPositionServer(this.gameObject.AssumedWorldPosServer() + exitVector);
					ThrowItem(uop, exitVector);
				}
				if (obj.TryGetComponent<PlayerScript>(out var script))
				{
					script.RegisterPlayer.ServerStun();
				}
			}

			gasContainer.ReleaseContentsInstantly();
			gasContainer.IsSealed = false;
		}

		#endregion EjectContents

		public void EntityTryEscape(GameObject entity, Action ifCompleted, MoveAction moveAction)
		{
			if (moveAction == MoveAction.NoMove) return;
			if (SelfControlled)
			{
				traversal.ChangeMovementTrajectory(moveAction);
				return;
			}
			SoundManager.PlayNetworkedAtPos(ClangSound, ContainerWorldPosition);
			var pb = StandardProgressAction.Create(new StandardProgressActionConfig(
				StandardProgressActionType.Escape, true, false, true, true),
				OnFinishStruggle);
			Chat.AddExamineMsg(entity, "You attempt to stop yourself from being sucked in by the oily air.");
			ProgressAction.ServerStartProgress(pb, gameObject.RegisterTile(), 3.25f, entity);
		}

		private void OnFinishStruggle()
		{
			SelfControlled = true;
		}

		public string Examine(Vector3 worldPos = default)
		{
			int contentsCount = objectContainer.GetStoredObjects().Count();
			return $"There {(contentsCount == 1 ? "is one entity" : $"are {contentsCount} entities")} inside.";
		}
	}
}
