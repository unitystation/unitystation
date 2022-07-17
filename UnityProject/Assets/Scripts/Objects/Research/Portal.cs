using Gateway;
using Items;
using UnityEngine;

namespace Objects.Research
{
	public class Portal : EnterTileBase
	{
		private PortalTypes portalType;
		public PortalTypes PortalType => portalType;

		private Portal connectedPortal;
		public Portal ConnectedPortal => connectedPortal;

		private Vector3 worldCoord;

		public UniversalObjectPhysics ObjectPhysics => objectPhysics;

		public void SetNewPortal()
		{

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
			TransportUtility.TeleportToObject(eventData, connectedPortal.gameObject,
				ObjectPhysics.OfficialPosition);
		}

		public enum PortalTypes
		{
			Entrance,
			Exit,
			ToCoord
		}
	}
}