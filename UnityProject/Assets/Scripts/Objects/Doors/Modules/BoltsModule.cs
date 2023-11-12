using AddressableReferences;
using System.Collections.Generic;
using Initialisation;
using Items;
using NaughtyAttributes;
using UnityEngine;

namespace Doors.Modules
{
	public class BoltsModule : DoorModuleBase, IServerSpawn
	{
		[SerializeField] private ItemTrait IDToggleCard;


		private bool boltsDown = false;
		public bool BoltsDown => boltsDown;

		private bool boltsLights = true;

		[SerializeField][Tooltip("If true, the door needs to be closed to see the bolts lights")]
		private bool needsClosedToLight = true;

		[SerializeField]
		private AddressableAudioSource boltsUpSound= null;

		[SerializeField]
		private AddressableAudioSource boltsDownSound= null;

		private bool CanShowLights
		{
			get
			{
				if (needsClosedToLight)
				{
					return boltsLights && master.HasPower && master.IsClosed;
				}

				return boltsLights && master.HasPower;
			}
		}


		public void OnSpawnServer(SpawnInfo info)
		{
			master.HackingProcessBase.RegisterPort(ToggleBolts, master.GetType());
			master.HackingProcessBase.RegisterPort(PreventBoltsFall, master.GetType());
		}
		/// <summary>
		/// Set the current state for this door's bolts.
		/// </summary>
		/// <param name="state">True means the bolts are down and the door can't be opened</param>
		[ContextMenu("Set bolt state")]
		private void SetBoltsState(bool state)
		{
			boltsDown = state;

			master.ToggleBlockAutoClose(state);

			SoundManager.PlayNetworkedAtPos(boltsDown ? boltsDownSound : boltsUpSound, master.RegisterTile.WorldPositionServer, sourceObj: master.gameObject);

			if (boltsDown && CanShowLights)
			{
				master.DoorAnimator.TurnOnBoltsLight();
			}
			else
			{
				master.DoorAnimator.TurnOffAllLights();
			}

			master.UpdateGui();
		}

		/// <summary>
		/// Set the current state for the bolts light.
		/// </summary>
		/// <param name="enable">True means this door will turn on its bolts lights when the bolts are down</param>
		public void SetBoltsLight(bool enable)
		{
			boltsLights = enable;
		}

		public void ToggleBolts()
		{
			SetBoltsState(!boltsDown);
		}

		public override void OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction != null && interaction.UsedObject != null)
			{
				if (interaction.UsedObject.GetComponent<ItemAttributesV2>().HasTrait(IDToggleCard))
				{
					PulseToggleBolts();
					States.Add(DoorProcessingStates.SoftwarePrevented);
				}

				if (PulsePreventBoltsFall())
				{
					SetBoltsState(true); //so Preveving all cables
				}
			}

			if (boltsDown)
			{
				States.Add(DoorProcessingStates.PhysicallyPrevented);
			}

			return;
		}

		public override void ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction != null && interaction.UsedObject != null)
			{
				if (interaction.UsedObject.GetComponent<ItemAttributesV2>().HasTrait(IDToggleCard))
				{
					PulseToggleBolts();
					States.Add(DoorProcessingStates.SoftwarePrevented);
				}
			}

			if (PulsePreventBoltsFall())
			{
				SetBoltsState(true); //so Preveving all cables
			}

			if (boltsDown)
			{
				States.Add(DoorProcessingStates.PhysicallyPrevented);
			}

			return;
		}

		public void PreventBoltsFall()
		{
			master.HackingProcessBase.ReceivedPulse(PreventBoltsFall);
		}

		public bool PulsePreventBoltsFall()
		{
			return master.HackingProcessBase.PulsePortConnectedNoLoop(PreventBoltsFall, true);
		}

		public void PulseToggleBolts(bool? State = null)
		{
			if (State != null)
			{
				if (State.Value == boltsDown) return;
				master.HackingProcessBase.ImpulsePort(ToggleBolts);
			}
			else
			{
				master.HackingProcessBase.ImpulsePort(ToggleBolts);
			}

		}

		public override void BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			if (PulsePreventBoltsFall())
			{
				SetBoltsState(true);
			}

			if (boltsDown)
			{
				States.Add(DoorProcessingStates.PhysicallyPrevented);
			}
		}

	}
}
