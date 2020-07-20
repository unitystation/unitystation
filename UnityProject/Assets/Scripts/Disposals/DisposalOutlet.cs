using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Disposals
{
	public class DisposalOutlet : DisposalMachine, IServerDespawn, IExaminable
	{
		const float ANIMATION_TIME = 4.2f; // As per sprite sheet JSON file.
		const float EJECTION_DELAY = 3;

		[SyncVar(hook = nameof(OnSyncOutletState))]
		bool outletOperating = false;
		[SyncVar(hook = nameof(OnSyncOrientation))]
		Orientation orientation;

		List<DisposalVirtualContainer> receivedContainers = new List<DisposalVirtualContainer>();

		public bool OutletOperating => outletOperating;
		public bool ServerHasContainers => receivedContainers.Count > 0;

		#region Initialisation

		protected override void Awake()
		{
			base.Awake();

			if (TryGetComponent(out Directional directional))
			{
				orientation = directional.InitialOrientation;
				directional.OnDirectionChange.AddListener(OnDirectionChanged);
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			UpdateSpriteOutletState();
			UpdateSpriteOrientation();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			foreach (DisposalVirtualContainer container in receivedContainers)
			{
				Despawn.ServerSingle(container.gameObject);
			}
		}

		#endregion Initialisation

		void OnDirectionChanged(Orientation newDir)
		{
			orientation = newDir;
		}

		#region Sync

		void OnSyncOutletState(bool oldState, bool newState)
		{
			outletOperating = newState;
			UpdateSpriteOutletState();
		}

		void OnSyncOrientation(Orientation oldState, Orientation newState)
		{
			orientation = newState;
			UpdateSpriteOrientation();
		}

		#endregion Sync

		#region Sprites

		void UpdateSpriteOutletState()
		{
			if (OutletOperating) baseSpriteHandler.ChangeSprite(1);
			else baseSpriteHandler.ChangeSprite(0);
		}

		void UpdateSpriteOrientation()
		{
			switch (orientation.AsEnum())
			{
				case OrientationEnum.Up:
					baseSpriteHandler.ChangeSpriteVariant(1);
					break;
				case OrientationEnum.Down:
					baseSpriteHandler.ChangeSpriteVariant(0);
					break;
				case OrientationEnum.Left:
					baseSpriteHandler.ChangeSpriteVariant(3);
					break;
				case OrientationEnum.Right:
					baseSpriteHandler.ChangeSpriteVariant(2);
					break;
			}
		}

		#endregion Sprites

		#region Interactions

		public override string Examine(Vector3 worldPos = default)
		{
			string baseString = "It";
			if (FloorPlatingExposed()) baseString = base.Examine().TrimEnd('.') + " and";

			if (OutletOperating) return $"{baseString} is currently ejecting its contents.";
			else return $"{baseString} is ready for use.";
		}

		#endregion Interactions

		/// <summary>
		/// Checks if disposal outlet can receive a container.
		/// </summary>
		/// <returns>False if outlet is not fully attached to a pipe terminal.</returns>
		public bool ServerCanReceiveContainer()
		{
			return MachineSecured;
		}

		/// <summary>
		/// Provide the disposal outlet a container, from the disposal pipe network,
		/// from which its contents will be ejected into the world. The container object will be despawned.
		/// </summary>
		/// <param name="virtualContainer">The virtual container whose contents will be ejected.</param>
		public void ServerReceiveAndEjectContainer(DisposalVirtualContainer virtualContainer)
		{
			receivedContainers.Add(virtualContainer);
			virtualContainer.GetComponent<ObjectBehaviour>().parentContainer = objectBehaviour;
			if (!OutletOperating) StartCoroutine(RunEjectionSequence());
		}

		IEnumerator RunEjectionSequence()
		{
			// If a container is received while in the closing orifice stage, (essentially) queue the container.
			while (ServerHasContainers)
			{
				// Outlet orifice opens...
				outletOperating = true;
				SoundManager.PlayNetworkedAtPos("DisposalMachineBuzzer", registerObject.WorldPositionServer, sourceObj: gameObject);
				yield return WaitFor.Seconds(EJECTION_DELAY);

				// Outlet orifice open. Release the charge.
				foreach (DisposalVirtualContainer container in receivedContainers)
				{
					container.EjectContentsAndThrow(orientation.Vector);
					Despawn.ServerSingle(container.gameObject);
				}
				receivedContainers.Clear();

				// Close orifice, restore charge (containers may be received during this period).
				yield return WaitFor.Seconds(ANIMATION_TIME - EJECTION_DELAY);
				outletOperating = false;
			}
		}
	}
}
