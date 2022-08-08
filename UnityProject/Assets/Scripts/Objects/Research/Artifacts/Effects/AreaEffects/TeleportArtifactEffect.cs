using HealthV2;
using Systems.Teleport;
using UnityEngine;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ArtifactTeleportEffect", menuName = "ScriptableObjects/Systems/Artifacts/ArtifactTeleportEffect")]
	public class TeleportArtifactEffect : AreaEffectBase
	{
		public int MinDistance;
		public int MaxDistance;

		public bool AvoidSpace = false;
		public bool AvoidImpassable = false;

		public override void OnEffect(PlayerScript player, BodyPart part = null)
		{
			if (part != null)
			{
				player.playerHealth.DismemberBodyPart(part);
				TeleportUtils.ServerTeleportRandom(part.gameObject, MinDistance, MaxDistance, AvoidSpace, AvoidImpassable);
				return;
			}
			else
			{
				TeleportUtils.ServerTeleportRandom(player.gameObject, MinDistance, MaxDistance, AvoidSpace, AvoidImpassable);
				return;
			}
		}
	}
}
