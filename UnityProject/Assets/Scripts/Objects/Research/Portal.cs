using System.Collections.Generic;
using Gateway;
using Items;
using Systems.Explosions;
using UnityEngine;

namespace Objects.Research
{
	public class Portal : EnterTileBase
	{
		private Portal connectedPortal;
		public Portal ConnectedPortal => connectedPortal;

		private static HashSet<Portal> portalPairs = new HashSet<Portal>();
		public static HashSet<Portal> PortalPairs => portalPairs;

		public UniversalObjectPhysics ObjectPhysics => objectPhysics;

		protected override void OnDisable()
		{
			base.OnDisable();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PortalDeath);
			portalPairs.Remove(this);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void StaticClear()
		{
			portalPairs.Clear();
		}

		public void PortalDeath()
		{
			if(CustomNetworkManager.IsServer == false) return;

			Chat.AddLocalMsgToChat("The portal fizzles out into nothing", gameObject);

			//Despawn after time is up
			portalPairs.Remove(this);
			_ = Despawn.ServerSingle(gameObject);
		}

		public void SetNewPortal(Portal connectedPortal, int time = 300)
		{
			this.connectedPortal = connectedPortal;

			UpdateManager.Add(PortalDeath, time, false);

			portalPairs.Add(this);
		}

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			//Allow players or ghosts to enter
			return playerScript.PlayerState == PlayerScript.PlayerStates.Normal ||
			       playerScript.PlayerState == PlayerScript.PlayerStates.Ghost;
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			Teleport(playerScript.gameObject);
		}

		public override bool WillAffectObject(GameObject eventData)
		{
			//Don't teleport tracking beacons as we open on them (might not be an issue but just incase)
			return eventData.GetComponent<TrackingBeacon>() == false;
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			Teleport(eventData);
		}

		private void Teleport(GameObject eventData)
		{
			if(connectedPortal == null) return;

			SparkUtil.TrySpark(gameObject, expose: false);

			TransportUtility.TeleportToObject(eventData, connectedPortal.gameObject,
				ObjectPhysics.OfficialPosition);
		}
	}
}