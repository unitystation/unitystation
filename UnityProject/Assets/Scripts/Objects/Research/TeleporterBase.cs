using System;
using Systems.Electricity;
using Systems.ObjectConnection;
using Items;
using UnityEngine;

namespace Objects.Research
{
	public class TeleporterBase : MonoBehaviour, IAPCPowerable
	{
		protected bool active;
		protected bool powered;

		protected TrackingBeacon linkedBeacon;

		private SpriteHandler spriteHandler;
		protected RegisterTile registerTile;

		[NonSerialized]
		public TeleporterHub connectedHub;
		[NonSerialized]
		public TeleporterStation connectedStation;
		[NonSerialized]
		public TeleporterControl connectedControl;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			registerTile = GetComponent<RegisterTile>();
		}

		public virtual void SetBeacon(TrackingBeacon newBeacon)
		{
			SetActiveInternal(false);

			//Remove old listener
			if (linkedBeacon != null)
			{
				linkedBeacon.StateChangeEvent.RemoveListener(BeaconStateChangeEvent);
			}

			linkedBeacon = newBeacon;

			//Add new listener
			linkedBeacon.StateChangeEvent.AddListener(BeaconStateChangeEvent);
		}

		public void SetActive(bool newState)
		{
			//Dont allow to turn on if no linked beacon
			if(newState && linkedBeacon == null) return;

			SetActiveInternal(newState);
		}

		private void BeaconStateChangeEvent(bool newState, bool wasDestroyed)
		{
			if (newState == false)
			{
				SetActiveInternal(false);
			}
		}

		private void SetActiveInternal(bool newState)
		{
			active = newState;

			UpdateSprite();
		}

		private void UpdateSprite()
		{
			// 0 is off, 1 is on
			spriteHandler.ChangeSprite(
				powered ?
					active ? 1 : 0
					: 0);
		}

		public void PowerNetworkUpdate(float voltage) {}

		public void StateUpdate(PowerState state)
		{
			powered = state != PowerState.Off;

			UpdateSprite();
		}
	}
}