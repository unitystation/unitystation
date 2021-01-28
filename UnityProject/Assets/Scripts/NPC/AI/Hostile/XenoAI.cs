using UnityEngine;

namespace Systems.MobAIs
{
	public class XenoAI: GenericHostileAI
	{
		[SerializeField]
		[Tooltip("How many queens could there be at once in a round. " +
		         "If this cap is not reached, this Xeno could become a Queen to preserve the perfect life form!")]
		private int queenCap = 1;

		[SerializeField]
		[Tooltip("Reference to the Queen prefab to be spawned if this xeno becomes a Queen")]
		private GameObject queenPrefab = default;

		private void TryBecomingQueen()
		{
			if (QueenCapReached())
			{
				return;
			}

			Spawn.ServerPrefab(queenPrefab, gameObject.AssumedWorldPosServer());
			Despawn.ServerSingle(gameObject);
		}

		private bool QueenCapReached()
		{
			if (queenCap < 0)
			{
				return false;
			}

			return XenoQueenAI.CurrentQueensAmt >= queenCap;
		}

		protected override void OnSpawnMob()
		{
			base.OnSpawnMob();
			TryBecomingQueen();
		}
	}
}