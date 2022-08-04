using UnityEngine;

namespace Systems.MobAIs
{
	/// <summary>
	/// Generic hostile AI that will only attack when attacked.
	/// </summary>
	[RequireComponent(typeof(MobMeleeAttack))]
	[RequireComponent(typeof(ConeOfSight))]
	public class GenericRetaliateAI: GenericHostileAI
	{
		protected override void Awake()
		{
			base.Awake();
			attackLastAttackerChance = 100;
		}

		protected override void HandleSearch()
		{
			moveWaitTime += MobController.UpdateTimeInterval;
			if (moveWaitTime <= movementTickRate)
			{
				return;
			}
			moveWaitTime = 0f;
			DoRandomMove();
			BeginSearch();
		}

		public override void LocalChatReceived(ChatEvent chatEvent)
		{
			//Do nothing, I don't care if you talk close to me
		}
	}
}