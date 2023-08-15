using System;
using System.Collections.Generic;
using Objects.Research;
using Systems.Explosions;
using Systems.Teleport;
using UnityEngine;
using Systems.Scenes;
using Random = UnityEngine.Random;

namespace Items.Others
{
	public class HandTeleporter : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<HandActivate>
	{
		[SerializeField]
		private TrackingBeacon.TrackingBeaconTypes trackingBeaconType = TrackingBeacon.TrackingBeaconTypes.Station;
		public TrackingBeacon.TrackingBeaconTypes TrackingBeaconType => trackingBeaconType;

		[SerializeField]
		private GameObject portalPrefab;

		private int maxPortalPairs = 3;

		[NonSerialized]
		public TrackingBeacon linkedBeacon;

		private UniversalObjectPhysics objectPhysics;

		private Portal entrancePortal;

		private Portal exitPortal;

		[field:SerializeField] private bool ignoreInhibitors = false;

		private void Awake()
		{
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//Don't allow alt click so that the nettab can be opened
			if(interaction.IsAltClick) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			var worldPosEntrance = objectPhysics.OfficialPosition.RoundToInt();

			if (ignoreInhibitors == false)
			{
				foreach(TeleportInhibitor inhib in TeleportInhibitor.Inhibitors)
				{
					var inhibPosition = inhib.GetComponent<UniversalObjectPhysics>().OfficialPosition.RoundToInt();
					if(Vector3.Distance(inhibPosition, worldPosEntrance) <= inhib.Range)
					{
						SparkUtil.TrySpark(worldPosEntrance, expose: false);
						Chat.AddExamineMsgFromServer(interaction.Performer, $"The teleporter sparks as it attempts to open a portal. An unknown force appears to be interfering with the device!");
						return;
					}
				}
			}

			//If theres more than 6 portals (3 pairs) don't allow more
			if (Portal.PortalPairs.Count >= maxPortalPairs * 2)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} is recharging!");
				return;
			}

			//Open the portals if destination tracking beacon selected
			var emergency = linkedBeacon == null;

			if (emergency)
			{
				//Close old portals!
				if (entrancePortal != null) entrancePortal.PortalDeath();
				if (exitPortal != null) exitPortal.PortalDeath();


				Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} flashes briefly. No target is locked in, opening unstable portal!");
			}
			else
			{
				if (entrancePortal != null || exitPortal != null)
				{
					Chat.AddExamineMsg(interaction.Performer, $"There is already a portal open, close it first!");
					return;
				}

				Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} flashes briefly. Opening portal to {linkedBeacon.gameObject.ExpensiveName()}!");
			}

			//Go to selected tracked beacon or open portal in random 10 x 10 coord
			Vector3 worldPosExit;

			if (emergency)
			{
				worldPosExit = worldPosEntrance + TeleportUtils.RandomTeleportLocation();
			}
			else
			{
				worldPosExit = linkedBeacon.ObjectBehaviour.OfficialPosition;
			}

			worldPosExit = worldPosExit.RoundToInt();

			//TODO maybe coroutine this for better effect??

			//Spark three times entrance
			for (int i = 0; i < 2; i++)
			{
				SparkUtil.TrySpark(worldPosEntrance, expose: false);
			}

			//Spark three times exit
			for (int i = 0; i < 2; i++)
			{
				SparkUtil.TrySpark(worldPosExit, expose: false);
			}

			//TODO play specific sound here

			var entrance = Spawn.ServerPrefab(portalPrefab, worldPosEntrance);
			if(entrance.Successful == false) return;

			entrancePortal = entrance.GameObject.GetComponent<Portal>();

			var exit = Spawn.ServerPrefab(portalPrefab, worldPosExit);
			if(exit.Successful == false) return;

			exitPortal = exit.GameObject.GetComponent<Portal>();

			entrancePortal.SetNewPortal(exitPortal, emergency ? 30 : 300);
			exitPortal.SetNewPortal(entrancePortal, emergency ? 30 : 300);

			exitPortal.SetOrange();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.TargetObject == null) return false;

			if (interaction.TargetObject.GetComponent<Portal>() == null) return false;

			return true;
		}

		/// <summary>
		/// Destroy portals when clicked on with the hand teleporter
		/// </summary>
		public void ServerPerformInteraction(HandApply interaction)
		{
			Chat.AddExamineMsg(interaction.Performer, $"The {gameObject.ExpensiveName()} flashes briefly.");

			var portalClicked = interaction.TargetObject.GetComponent<Portal>();
			portalClicked.ConnectedPortal.OrNull()?.PortalDeath();
			portalClicked.PortalDeath();
		}
	}
}