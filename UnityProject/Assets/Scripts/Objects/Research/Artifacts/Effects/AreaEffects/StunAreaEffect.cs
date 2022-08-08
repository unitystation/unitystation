using HealthV2;
using UnityEngine;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "StunAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/StunAreaEffect")]
	public class StunAreaEffect : AreaEffectBase
	{
		public int StunDuration = 3;
		public bool DropItems = false;
		public bool ArmourBlockable = false;
		public bool StopMovement = true;

		public override void OnEffect(PlayerScript player, BodyPart part = null)
		{
			player.GetComponent<RegisterPlayer>().ServerStun(StunDuration, DropItems, ArmourBlockable, StopMovement);
		}
	}
}
