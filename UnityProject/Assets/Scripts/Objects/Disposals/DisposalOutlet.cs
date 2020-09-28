using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Disposals
{
	public class DisposalOutlet : DisposalMachine, IServerDespawn, IExaminable
	{
		const float ANIMATION_TIME = 4.2f; // As per sprite sheet JSON file.
		const float EJECTION_DELAY = 3;

		Directional directional;
		List<DisposalVirtualContainer> receivedContainers = new List<DisposalVirtualContainer>();

		public bool IsOperating { get; private set; }
		public bool ServerHasContainers => receivedContainers.Count > 0;

		private enum SpriteState
		{
			Idle = 0,
			Operating = 1
		}

		#region Lifecycle

		protected override void Awake()
		{
			base.Awake();

			directional = GetComponent<Directional>();
		}

		void Start()
		{
			directional.OnDirectionChange.AddListener(OnDirectionChanged);
			UpdateSpriteState();
			UpdateSpriteOrientation();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			foreach (DisposalVirtualContainer container in receivedContainers)
			{
				Despawn.ServerSingle(container.gameObject);
			}
		}

		#endregion Lifecycle

		void OnDirectionChanged(Orientation newDir)
		{
			UpdateSpriteOrientation();
		}

		void SetOutletOperating(bool isOperating)
		{
			IsOperating = isOperating;
			UpdateSpriteState();
		}

		#region Sprites

		void UpdateSpriteState()
		{
			if (IsOperating)
			{
				baseSpriteHandler.ChangeSprite((int) SpriteState.Operating);
			}
			else
			{
				baseSpriteHandler.ChangeSprite((int) SpriteState.Idle);
			}
		}

		void UpdateSpriteOrientation()
		{
			switch (directional.CurrentDirection.AsEnum())
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

			if (IsOperating) return $"{baseString} is currently ejecting its contents.";
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
			if (!IsOperating) StartCoroutine(RunEjectionSequence());
		}

		IEnumerator RunEjectionSequence()
		{
			// If a container is received while in the closing orifice stage, (essentially) queue the container.
			while (ServerHasContainers)
			{
				// Outlet orifice opens...
				SetOutletOperating(true);
				SoundManager.PlayNetworkedAtPos("DisposalMachineBuzzer", registerObject.WorldPositionServer, sourceObj: gameObject);
				yield return WaitFor.Seconds(EJECTION_DELAY);

				// Outlet orifice open. Release the charge.
				foreach (DisposalVirtualContainer container in receivedContainers)
				{
					container.EjectContentsAndThrow(directional.CurrentDirection.Vector);
					Despawn.ServerSingle(container.gameObject);
				}
				receivedContainers.Clear();

				// Close orifice, restore charge (containers may be received during this period).
				yield return WaitFor.Seconds(ANIMATION_TIME - EJECTION_DELAY);
				SetOutletOperating(false);
			}
		}
	}
}
