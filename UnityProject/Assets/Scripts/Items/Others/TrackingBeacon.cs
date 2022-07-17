using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Items
{
	public class TrackingBeacon : MonoBehaviour, ICheckedInteractable<InventoryApply>
	{
		[SerializeField]
		private TrackingBeaconTypes trackingBeaconType = TrackingBeaconTypes.Station;
		public TrackingBeaconTypes TrackingBeaconType => trackingBeaconType;

		[SerializeField]
		private bool active = true;

		//First bool state (on/off), second bool is if it is being destroyed
		[NonSerialized]
		public UnityEvent<bool, bool> StateChangeEvent = new UnityEvent<bool, bool>();

		private SpriteHandler spriteHandler;
		private ItemAttributesV2 itemAttributesV2;
		public ItemAttributesV2 ItemAttributesV2 => itemAttributesV2;

		private UniversalObjectPhysics objectBehaviour;
		private Integrity integrity;

		//Static so we dont need to stick it on a manager, shouldn't have any issues as beacons are removed on disable
		private static IDictionary<TrackingBeaconTypes, HashSet<TrackingBeacon>>
			activeBeacons = new Dictionary<TrackingBeaconTypes, HashSet<TrackingBeacon>>();

		#region LifeCycle

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			itemAttributesV2 = GetComponent<ItemAttributesV2>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			integrity = GetComponent<Integrity>();
		}

		private void OnEnable()
		{
			integrity.OnWillDestroyServer.AddListener(OnBeaconDestruction);

			if(active == false) return;

			ActivateBeacon(true);
		}

		private void OnDisable()
		{
			integrity.OnWillDestroyServer.RemoveListener(OnBeaconDestruction);

			if(active) return;

			DeactivateBeacon(true);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void StaticClear()
		{
			activeBeacons.Clear();
		}

		#endregion

		[Server]
		public void ActivateBeacon(bool firstTime = false)
		{
			if(active && firstTime == false) return;

			active = true;
			spriteHandler.ChangeSprite(0);

			StateChangeEvent.Invoke(true, false);

			//Try to add if type already exists
			if (activeBeacons.TryGetValue(trackingBeaconType, out var beaconHash))
			{
				beaconHash.Add(this);
				return;
			}

			//Otherwise add type too
			activeBeacons.Add(trackingBeaconType, new HashSet<TrackingBeacon>(){this});
		}

		[Server]
		public void DeactivateBeacon(bool destroyed = false)
		{
			if(active == false) return;

			active = false;
			spriteHandler.ChangeSprite(1);

			StateChangeEvent.Invoke(false, destroyed);

			//Try to remove if already exists
			if (activeBeacons.TryGetValue(trackingBeaconType, out var beaconHash) == false) return;

			beaconHash.Remove(this);
		}

		[Server]
		public static List<TrackingBeacon> GetAllBeaconOfType(TrackingBeaconTypes type)
		{
			switch (type)
			{
				//Just station
				case TrackingBeaconTypes.Station:
					return GetType(type);

				//Centcom gets station and centcom
				case TrackingBeaconTypes.Centcom:
					var station = GetType(TrackingBeaconTypes.Station);
					station.AddRange(GetType(type));
					return station;

				//Syndicate gets station and syndicate
				case TrackingBeaconTypes.Syndicate:
					var station2 = GetType(TrackingBeaconTypes.Station);
					station2.AddRange(GetType(type));
					return station2;

				//Syndicate gets station and syndicate
				case TrackingBeaconTypes.All:
					var station3 = new List<TrackingBeacon>();

					// -1 so we dont get All enum
					for (int i = 0; i < Enum.GetNames(typeof(TrackingBeaconTypes)).Length - 1; i++)
					{
						station3.AddRange(GetType((TrackingBeaconTypes) i));
					}

					return station3;

				default:
					return new List<TrackingBeacon>();
			}
		}

		private static List<TrackingBeacon> GetType(TrackingBeaconTypes type)
		{
			if (activeBeacons.TryGetValue(type, out var beacons))
			{
				return beacons.ToList();
			}

			return new List<TrackingBeacon>();
		}

		[Server]
		public Vector3 CurrentBeaconPosition()
		{
			return objectBehaviour.OfficialPosition;
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (active)
			{
				DeactivateBeacon();
				Chat.AddActionMsgToChat(interaction.Performer, $"You turn off the tracking beacon",
					$"{interaction.Performer.ExpensiveName()} turns off the tracking beacon");
				return;
			}

			ActivateBeacon();
			Chat.AddActionMsgToChat(interaction.Performer, $"You turn on the tracking beacon",
				$"{interaction.Performer.ExpensiveName()} turns on the tracking beacon");
		}

		private void OnBeaconDestruction(DestructionInfo info)
		{
			if(active == false) return;

			StateChangeEvent.Invoke(false, true);
		}

		public enum TrackingBeaconTypes
		{
			Station,
			Centcom,
			Syndicate,

			//All must always be last
			All
		}
	}
}
