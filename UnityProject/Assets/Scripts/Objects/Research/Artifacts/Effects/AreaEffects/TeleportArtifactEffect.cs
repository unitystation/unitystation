using Systems.Teleport;
using UnityEngine;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ArtifactTeleportEffect", menuName = "ScriptableObjects/Systems/Artifacts/ArtifactTeleportEffect")]
	public class TeleportArtifactEffect : AreaArtifactEffect
	{
		public int MinDistance;
		public int MaxDistance;

		public bool AvoidSpace = false;
		public bool AvoidImpassable = false;

		public override void OnEffect(PlayerScript player)
		{
			TeleportUtils.ServerTeleportRandom(player.gameObject, MinDistance, MaxDistance, AvoidSpace, AvoidImpassable);
		}
	}
}
