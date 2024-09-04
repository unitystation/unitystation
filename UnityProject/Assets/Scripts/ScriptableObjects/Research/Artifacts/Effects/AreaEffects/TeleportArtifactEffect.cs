using Core;
using HealthV2;
using Systems.Explosions;
using Systems.Scenes;
using Systems.Teleport;
using UnityEngine;

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
