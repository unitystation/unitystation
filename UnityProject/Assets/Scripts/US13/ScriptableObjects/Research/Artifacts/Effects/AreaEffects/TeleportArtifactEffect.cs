using Core;
using UnityEngine;
using US13.HealthV2.Living.CirculatorySystem;
using US13.Player;
using US13.Systems.Explosions;
using US13.Systems.MaintRooms;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ArtifactTeleportEffect", menuName = "ScriptableObjects/Systems/Artifacts/ArtifactTeleportEffect")]
	public class TeleportArtifactEffect : AreaEffectBase
	{
		[SerializeField]
		private int MinDistance;
		[SerializeField]
		private int MaxDistance;

		[SerializeField]
		private bool AvoidSpace = false;
		[SerializeField]
		private bool AvoidImpassable = false;

		public override void OnEffect(PlayerScript player, BodyPart part = null)
		{
			bool CanTeleport = true;
			foreach(TeleportInhibitor inhib in TeleportInhibitor.Inhibitors)
			{
				var inhibPosition = inhib.GetComponent<UniversalObjectPhysics>().OfficialPosition.RoundToInt();
				if(Vector3.Distance(inhibPosition, player.gameObject.AssumedWorldPosServer()) <= inhib.Range)
				{
					SparkUtil.TrySpark(player.gameObject.AssumedWorldPosServer(), expose: false);
					CanTeleport = false;
				}
			}

			if (CanTeleport)
			{
				if (part != null)
				{
					player.playerHealth.DismemberBodyPart(part);
					TeleportUtils.ServerTeleportRandom(part.gameObject, MinDistance, MaxDistance, AvoidSpace, AvoidImpassable);
				}
				else
				{
					TeleportUtils.ServerTeleportRandom(player.gameObject, MinDistance, MaxDistance, AvoidSpace, AvoidImpassable);
				}
			}

		}
	}
}
