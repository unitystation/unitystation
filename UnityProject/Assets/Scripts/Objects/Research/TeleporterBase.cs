using System;
using System.Text;
using Systems.Electricity;
using Systems.ObjectConnection;
using Items;
using UnityEngine;

namespace Objects.Research
{
	public class TeleporterBase : EnterTileBase, IAPCPowerable, IExaminable
	{
		protected bool active;
		protected bool powered;

		protected TrackingBeacon linkedBeacon;

		[SerializeField]
		private SpriteHandler spriteHandler;
		protected RegisterTile registerTile;

		[NonSerialized]
		public Integrity Integrity;

		protected TeleporterHub connectedHub;
		protected TeleporterStation connectedStation;
		protected TeleporterControl connectedControl;

		protected override void Awake()
		{
			base.Awake();

			if (spriteHandler == null)
			{
				spriteHandler = GetComponentInChildren<SpriteHandler>();
			}

			registerTile = GetComponent<RegisterTile>();
			Integrity = GetComponent<Integrity>();
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
			// 0 is unpowered, 1 is off, 2 is on
			spriteHandler.ChangeSprite(powered ?
					active ? 2 : 1
					: 0);
		}

		public void SetHub(TeleporterHub hub)
		{
			if (connectedHub != null)
			{
				connectedHub.Integrity.OnWillDestroyServer.RemoveListener(OnPartDestroy);
			}

			connectedHub = hub;

			connectedHub.Integrity.OnWillDestroyServer.AddListener(OnPartDestroy);
		}

		public void SetControl(TeleporterControl control)
		{
			if (connectedControl != null)
			{
				connectedControl.Integrity.OnWillDestroyServer.RemoveListener(OnPartDestroy);
			}

			connectedControl = control;

			connectedControl.Integrity.OnWillDestroyServer.AddListener(OnPartDestroy);
		}

		public void SetStation(TeleporterStation station)
		{
			if (connectedStation != null)
			{
				connectedStation.Integrity.OnWillDestroyServer.RemoveListener(OnPartDestroy);
			}

			connectedStation = station;

			connectedStation.Integrity.OnWillDestroyServer.AddListener(OnPartDestroy);
		}

		private void OnPartDestroy(DestructionInfo info)
		{
			//Any part destroyed set inactive
			SetActive(false);
		}

		public void PowerNetworkUpdate(float voltage) {}

		public void StateUpdate(PowerState state)
		{
			powered = state != PowerState.Off;

			UpdateSprite();
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var stringBuilder = new StringBuilder();

			stringBuilder.AppendLine($"Control console {(connectedControl != null ? "" : "not")} connected");
			stringBuilder.AppendLine($"Hub {(connectedHub != null ? "" : "not")} connected");
			stringBuilder.AppendLine($"Station {(connectedStation != null ? "" : "not")} connected");

			return stringBuilder.ToString();
		}

		public override bool WillAffectObject(GameObject eventData)
		{
			return false;
		}

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			return false;
		}
	}
}