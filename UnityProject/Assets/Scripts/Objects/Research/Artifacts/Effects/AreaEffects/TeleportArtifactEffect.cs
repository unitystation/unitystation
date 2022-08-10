using HealthV2;
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
