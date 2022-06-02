using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AddressableReferences;

namespace Objects.Disposals
{
	public class DisposalOutlet : DisposalMachine, IServerDespawn, IExaminable
	{
		private const float ANIMATION_TIME = 4.2f; // As per sprite sheet JSON file.
		private const float EJECTION_DELAY = 3;

		[SerializeField]
		private AddressableAudioSource ejectionAlarmSound;

		private Rotatable rotatable;
		private readonly List<DisposalVirtualContainer> receivedContainers = new List<DisposalVirtualContainer>();

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

			rotatable = GetComponent<Rotatable>();
		}

		private void OnEnable()
		{
			rotatable.OnRotationChange.AddListener(OnDirectionChanged);
		}

		private void OnDisable()
		{
			rotatable.OnRotationChange.RemoveListener(OnDirectionChanged);
		}

		private void Start()
		{
			UpdateSpriteState();
			UpdateSpriteOrientation();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			foreach (DisposalVirtualContainer container in receivedContainers)
			{
				if (container.gameObject == null) continue;

				_ = Despawn.ServerSingle(container.gameObject);
			}
		}

		#endregion Lifecycle

		private void OnDirectionChanged(OrientationEnum newDir)
		{
			UpdateSpriteOrientation();
		}

		private void SetOutletOperating(bool isOperating)
		{
			IsOperating = isOperating;
			UpdateSpriteState();
		}

		#region Sprites

		private void UpdateSpriteState()
		{
			baseSpriteHandler.ChangeSprite((int) (IsOperating ? SpriteState.Operating : SpriteState.Idle));
		}

		private void UpdateSpriteOrientation()
		{
			switch (rotatable.CurrentDirection)
			{
				case OrientationEnum.Up_By0:
					baseSpriteHandler.ChangeSpriteVariant(1);
					break;
				case OrientationEnum.Down_By180:
					baseSpriteHandler.ChangeSpriteVariant(0);
					break;
				case OrientationEnum.Left_By90:
					baseSpriteHandler.ChangeSpriteVariant(3);
					break;
				case OrientationEnum.Right_By270:
					baseSpriteHandler.ChangeSpriteVariant(2);
					break;
			}
		}

		#endregion Sprites

		#region Interactions

		public override string Examine(Vector3 worldPos = default)
		{
			string baseString = "It";
			if (FloorPlatingExposed())
			{
				baseString = base.Examine().TrimEnd('.') + " and";
			}

			if (IsOperating)
			{
				return $"{baseString} is currently ejecting its contents.";
			}
			else
			{
				return $"{baseString} is {(MachineSecured ? "ready" : "not ready")} for use.";
			}
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
			virtualContainer.GetComponent<UniversalObjectPhysics>().StoreTo(objectContainer);
			if (IsOperating == false)
			{
				StartCoroutine(RunEjectionSequence());
			}
		}

		private IEnumerator RunEjectionSequence()
		{
			// If a container is received while in the closing orifice stage, (essentially) queue the container.
			while (ServerHasContainers)
			{
				// Outlet orifice opens...
				SetOutletOperating(true);
				SoundManager.PlayNetworkedAtPos(ejectionAlarmSound, registerObject.WorldPositionServer, sourceObj: gameObject);
				yield return WaitFor.Seconds(EJECTION_DELAY);

				// Outlet orifice open. Release the charge.
				foreach (DisposalVirtualContainer container in receivedContainers)
				{

					container.EjectContentsWithVector(this.transform.TransformDirection(rotatable.CurrentDirection.ToLocalVector3()));
					_ = Despawn.ServerSingle(container.gameObject);
				}
				receivedContainers.Clear();

				// Close orifice, restore charge (containers may be received during this period).
				yield return WaitFor.Seconds(ANIMATION_TIME - EJECTION_DELAY);
				SetOutletOperating(false);
			}
		}
	}
}
