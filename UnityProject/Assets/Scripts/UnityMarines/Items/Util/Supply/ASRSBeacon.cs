using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Objects.TGMC
{
	public class ASRSBeacon : MonoBehaviour, ICheckedInteractable<HandActivate>
	{
		private bool active = false;

		private SpriteHandler spriteHandler;

		private RegisterTile registerTile;

		private static HashSet<ASRSBeacon> activeBeacons = new HashSet<ASRSBeacon>();

		[field: SerializeField]
		public string Name { get; private set; }

		#region LifeCycle

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			registerTile = GetComponent<RegisterTile>();
		}

		private void OnDisable()
		{
			DeactivateBeacon(true);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void StaticClear()
		{
			activeBeacons.Clear();
		}

		#endregion

		[Server]
		public void ActivateBeacon()
		{
			if(active) return;

			active = true;
			//spriteHandler.ChangeSprite(0); no alternate sprites rn

			activeBeacons.Add(this); //Shouldn't need to add test for this as should auto fail if already exists cause hashset
		}

		[Server]
		public void DeactivateBeacon(bool destroyed = false)
		{
			if(active == false) return;
			active = false;

			//spriteHandler.ChangeSprite(1); dont have multiple textures for this beacon rn

			if (activeBeacons.Contains(this)) activeBeacons.Remove(this);
		}

		[Server]
		public Vector3 CurrentBeaconPosition()
		{
			return registerTile.WorldPositionServer;
		}

		public static HashSet<ASRSBeacon> GetActiveBeacons()
		{
			return activeBeacons;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (active)
			{
				DeactivateBeacon();
				Chat.AddActionMsgToChat(interaction.Performer, $"You turn off the ASRS beacon",
					$"{interaction.Performer.ExpensiveName()} turns off the ASRS beacon");
				return;
			}

			ActivateBeacon();
			Chat.AddActionMsgToChat(interaction.Performer, $"You turn on the ASRS beacon",
				$"{interaction.Performer.ExpensiveName()} turns on the ASRS beacon");
		}

	}
}
