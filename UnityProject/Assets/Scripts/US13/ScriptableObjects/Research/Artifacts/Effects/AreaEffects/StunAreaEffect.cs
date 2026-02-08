using UnityEngine;
using US13.HealthV2.Living.CirculatorySystem;
using US13.Player;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "StunAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/StunAreaEffect")]
	public class StunAreaEffect : AreaEffectBase
	{
		[SerializeField]
		private int StunDuration = 3;
		[SerializeField]
		private bool DropItems = false;
		[SerializeField]
		private bool ArmourBlockable = false;
		[SerializeField]
		private bool StopMovement = true;

		public override void OnEffect(PlayerScript player, BodyPart part = null)
		{
			player.RegisterPlayer.ServerStun(StunDuration, DropItems, ArmourBlockable, StopMovement);
		}
	}
}
